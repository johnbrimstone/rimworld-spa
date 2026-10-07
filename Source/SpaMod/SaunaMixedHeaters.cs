using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace SpaMod
{
    // "One sauna heater type per room", enforced in three layers so the player is told
    // rather than left guessing why a room stopped working:
    //   1. PlaceWorker_SaunaNoMixedHeaters — refuses the placement up front (red ghost,
    //      reason at the cursor, red outline + line to each conflicting heater).
    //   2. MapComponent_SaunaMixedHeaters — if a room becomes mixed anyway (walls removed
    //      so two saunas merge), a pulsing red X is drawn over every heater involved.
    //   3. Alert_SaunaMixedHeaters — the same heaters listed in the alerts readout, with
    //      click-to-jump culprits.
    // The gameplay consequence itself is in RoomRoleWorker_Sauna: a mixed room isn't a
    // Sauna. CompSaunaHeater.CheckMixedRoom keeps the flags in sync.

    public class PlaceWorker_SaunaNoMixedHeaters : PlaceWorker
    {
        // Reused every frame while the ghost is shown — avoids per-frame allocations.
        private static readonly List<Thing> conflictsBuffer = new List<Thing>();
        private static readonly List<IntVec3> cellsBuffer = new List<IntVec3>();

        public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map,
            Thing thingToIgnore = null, Thing thing = null)
        {
            SaunaArchetype? archetype = SaunaUtility.HeaterArchetypeOf(checkingDef as ThingDef);
            if (archetype == null)
            {
                return AcceptanceReport.WasAccepted;
            }

            SaunaUtility.FindConflictingHeaters(loc.GetRoom(map), archetype.Value, includePlanned: true,
                conflictsBuffer, thingToIgnore, thing);
            if (conflictsBuffer.Count == 0)
            {
                return AcceptanceReport.WasAccepted;
            }

            SaunaArchetype existing = SaunaUtility.HeaterArchetypeOf(conflictsBuffer[0], includePlanned: true).Value;
            return new AcceptanceReport("Only one sauna heater type per room: this room already has a "
                + existing + " heater");
        }

        public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing thing = null)
        {
            SaunaArchetype? archetype = SaunaUtility.HeaterArchetypeOf(def);
            Map map = Find.CurrentMap;
            if (archetype == null || map == null)
            {
                return;
            }

            SaunaUtility.FindConflictingHeaters(center.GetRoom(map), archetype.Value, includePlanned: true,
                conflictsBuffer, thing);
            if (conflictsBuffer.Count == 0)
            {
                return;
            }

            Vector3 ghostCenter = GenThing.TrueCenter(center, rot, def.size, AltitudeLayer.MetaOverlays.AltitudeFor());
            foreach (Thing conflict in conflictsBuffer)
            {
                GenDraw.DrawLineBetween(ghostCenter, conflict.TrueCenter(), SimpleColor.Red);

                cellsBuffer.Clear();
                foreach (IntVec3 cell in conflict.OccupiedRect())
                {
                    cellsBuffer.Add(cell);
                }
                GenDraw.DrawFieldEdges(cellsBuffer, Color.red);
            }
        }
    }

    // Owns the list of heaters currently in a mixed room on this map (populated by
    // CompSaunaHeater.CheckMixedRoom) and draws the warning overlay for them. Drawn here
    // rather than in a comp's PostDraw because PostDraw doesn't run for MapMeshOnly
    // things (the 2x2 heaters), and vanilla's OverlayDrawer only supports its own fixed
    // OverlayTypes. Not saved: comps re-flag themselves within one check interval after
    // load. MapComponents are instantiated automatically by Map.FillComponents.
    [StaticConstructorOnStartup]
    public class MapComponent_SaunaMixedHeaters : MapComponent
    {
        // Vanilla's red "cancel" X — reads as "don't do this" without new art.
        private static readonly Material WarningMat =
            MaterialPool.MatFrom("UI/Designators/Cancel", ShaderDatabase.MetaOverlay);

        // Same pulse curve and frequency as vanilla's OverlayDrawer pulsing overlays.
        private const float PulseFrequency = 4f;
        private const float MinAlpha = 0.3f;

        private readonly List<Thing> flaggedHeaters = new List<Thing>();

        public MapComponent_SaunaMixedHeaters(Map map) : base(map)
        {
        }

        public List<Thing> FlaggedHeaters => flaggedHeaters;

        public void SetFlagged(Thing heater, bool flagged)
        {
            if (flagged)
            {
                if (!flaggedHeaters.Contains(heater))
                {
                    flaggedHeaters.Add(heater);
                }
            }
            else
            {
                flaggedHeaters.Remove(heater);
            }
        }

        public override void MapComponentUpdate()
        {
            base.MapComponentUpdate();

            if (flaggedHeaters.Count == 0 || !WorldRendererUtility.DrawingMap || Find.CurrentMap != map)
            {
                return;
            }

            float altitude = AltitudeLayer.MetaOverlays.AltitudeFor();
            for (int i = 0; i < flaggedHeaters.Count; i++)
            {
                Thing heater = flaggedHeaters[i];
                if (!heater.Spawned)
                {
                    continue;
                }

                float pulse = (Mathf.Sin((Time.realtimeSinceStartup + heater.thingIDNumber % 571) * PulseFrequency) + 1f) * 0.5f;
                Material material = FadedMaterialPool.FadedVersionOf(WarningMat, MinAlpha + pulse * (1f - MinAlpha));

                // Icon size scales with the footprint so it reads clearly on a 2x2 too.
                Vector3 drawPos = heater.TrueCenter();
                drawPos.y = altitude;
                float size = heater.def.size.x >= 2 ? 1.4f : 0.9f;
                Matrix4x4 matrix = Matrix4x4.TRS(drawPos, Quaternion.identity, new Vector3(size, 1f, size));
                Graphics.DrawMesh(MeshPool.plane10, matrix, material, 0);
            }
        }
    }

    public class Alert_SaunaMixedHeaters : Alert
    {
        private readonly List<Thing> culprits = new List<Thing>();

        public Alert_SaunaMixedHeaters()
        {
            defaultLabel = "Mixed sauna heaters";
            defaultExplanation = "A room contains sauna heaters of more than one type. A sauna can only use one "
                + "heater type, so this room no longer counts as a sauna and grants no buffs.\n\n"
                + "Deconstruct the extra heaters, or wall the room off so each heater type has its own room.";
            defaultPriority = AlertPriority.High;
        }

        public override AlertReport GetReport()
        {
            culprits.Clear();
            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                MapComponent_SaunaMixedHeaters comp = maps[i].GetComponent<MapComponent_SaunaMixedHeaters>();
                if (comp != null)
                {
                    culprits.AddRange(comp.FlaggedHeaters);
                }
            }

            return AlertReport.CulpritsAre(culprits);
        }
    }
}
