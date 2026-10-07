using System.Collections.Generic;
using Verse;

namespace SpaMod
{
    public enum SaunaArchetype
    {
        Detoxified,
        Purified,
        Serenity,
        Radiant,
        Rejuvenated,
        Invigorated,
        Cleansed,
    }

    // 1x1 vs 2x2 heater. Per Resolved Decision #27/#30/#31, size no longer scales buff
    // intensity or a direct "capacity" stat — it only drives mote throw frequency
    // (CompSaunaHeaterMotes), room-size power rating (CompSaunaHeaterPowerScaling), and
    // active heating rate (CompProperties_HeatPusher.heatPerSecond).
    public enum SaunaHeatTier
    {
        Small,
        Large,
    }

    public class CompProperties_SaunaHeater : CompProperties
    {
        public SaunaArchetype archetype = SaunaArchetype.Serenity;
        public SaunaHeatTier heatTier = SaunaHeatTier.Small;

        // Resolved Decision #31: room size (in tiles) this heater's base power draw is
        // rated for — 12 for 1x1, 25 for 2x2. Read by CompSaunaHeaterPowerScaling.
        public int ratedRoomSize = 12;

        // "Room temperature acts as a simple gate... the room must be within the heater
        // variant's expected temperature range for the buff to apply at all" (Room &
        // Sauna Definition). No numeric range is given in the spec for this — it's a
        // Claude Code implementation detail, chosen per archetype to match its real-world
        // theme (e.g. a dry Finnish-style heater runs hotter than a meditation brazier).
        // Read by JobDriver_UseSauna's temperature gate; maxGateTemperature also feeds
        // the heater's CompProperties_HeatPusher.heatPushMaxTemperature so it naturally
        // stops actively heating once the room is within range.
        public float minGateTemperature = 30f;
        public float maxGateTemperature = 45f;

        public CompProperties_SaunaHeater()
        {
            compClass = typeof(CompSaunaHeater);
        }
    }

    // Marker comp: identifies a building as a sauna heater and carries the archetype/heatTier
    // that the room role scoring and JobDriver read. New heater variants are pure XML —
    // no new C# comp needed per variant.
    public class CompSaunaHeater : ThingComp
    {
        // How often the one-heater-type-per-room check re-runs (~2 s at 1x speed). Rooms
        // only become mixed through wall changes, so this needn't be instant; it also
        // covers save load, where rooms aren't built yet when comps spawn.
        private const int MixedRoomCheckIntervalTicks = 120;

        // Archetypes of the other heater types sharing this heater's room, or null if
        // the room isn't mixed. Drives the warning overlay, the alert, the inspect
        // string, and the disabled "Use sauna" option — see SaunaMixedHeaters.cs.
        private List<SaunaArchetype> mixedWith;

        private static readonly List<Thing> conflictsBuffer = new List<Thing>();

        public CompProperties_SaunaHeater Props => (CompProperties_SaunaHeater)props;

        public bool InMixedRoom => mixedWith != null;

        // Resolved Decision #31 / "Room & Sauna Definition" temperature gate: is this
        // room currently within the heater variant's expected range? Room may be null
        // (not spawned / not enclosed yet), which reads as "not in range".
        public bool IsRoomTemperatureInRange(Room room)
        {
            if (room == null)
            {
                return false;
            }

            float temperature = room.Temperature;
            return temperature >= Props.minGateTemperature && temperature <= Props.maxGateTemperature;
        }

        public override void CompTick()
        {
            base.CompTick();

            if (parent.Spawned && parent.IsHashIntervalTick(MixedRoomCheckIntervalTicks))
            {
                CheckMixedRoom();
            }
        }

        public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
        {
            base.PostDeSpawn(map, mode);
            mixedWith = null;
            map.GetComponent<MapComponent_SaunaMixedHeaters>()?.SetFlagged(parent, false);
        }

        // Built heaters only (includePlanned: false): a blueprint can't actually mix the
        // room until it's built, and placement already refuses conflicting blueprints.
        private void CheckMixedRoom()
        {
            SaunaUtility.FindConflictingHeaters(parent.GetRoom(), Props.archetype, includePlanned: false,
                conflictsBuffer, parent);

            if (conflictsBuffer.Count == 0)
            {
                mixedWith = null;
            }
            else
            {
                mixedWith = new List<SaunaArchetype>();
                foreach (Thing conflict in conflictsBuffer)
                {
                    SaunaArchetype other = conflict.TryGetComp<CompSaunaHeater>().Props.archetype;
                    if (!mixedWith.Contains(other))
                    {
                        mixedWith.Add(other);
                    }
                }
            }

            parent.Map.GetComponent<MapComponent_SaunaMixedHeaters>()?.SetFlagged(parent, InMixedRoom);
        }

        public override string CompInspectStringExtra()
        {
            int max = SaunaUtility.MaxCountedFacilities;
            int count = SaunaUtility.CountLinkedFacilities(parent);
            string suffix = count >= max ? " (fully decorated)" : "";
            string facilityLine = "Linked sauna facilities: " + count + " / " + max + suffix;

            Room room = parent.GetRoom();
            string tempLine = "Sauna temperature range: " + Props.minGateTemperature.ToStringTemperature("F0")
                + " ~ " + Props.maxGateTemperature.ToStringTemperature("F0")
                + (IsRoomTemperatureInRange(room) ? " (in range)" : " (out of range)");

            if (InMixedRoom)
            {
                return "Mixed heater types in this room (" + Props.archetype + " + "
                    + string.Join(", ", mixedWith) + ") — not a sauna until only one type remains"
                    + "\n" + facilityLine + "\n" + tempLine;
            }

            return facilityLine + "\n" + tempLine;
        }
    }
}
