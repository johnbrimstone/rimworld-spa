using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace SpaMod
{
    public static class SaunaUtility
    {
        // Spec: "Multiple linked facilities stack, capped at 3 counted facilities —
        // additional links beyond that have no further effect." Vanilla's facility
        // system has no built-in cap on the receiving side, so this is enforced here
        // rather than via CompProperties_Facility/_AffectedByFacilities fields.
        public const int MaxCountedFacilities = 3;

        // 5 intensity percentage points per facilityBonus point (see CompSaunaFacility),
        // summed across the top-3 highest-value linked facilities.
        private const float IntensityPerBonusPoint = 0.05f;

        // Number of currently-linked, active decorative facilities on a heater,
        // capped at MaxCountedFacilities. Slot-count only (unweighted) — used for
        // Psyfocus's "fully decorated" unlock, not the intensity formula.
        public static int CountLinkedFacilities(Thing heater)
        {
            CompAffectedByFacilities comp = heater?.TryGetComp<CompAffectedByFacilities>();
            if (comp == null)
            {
                return 0;
            }

            int count = 0;
            foreach (Thing facility in comp.LinkedFacilitiesListForReading)
            {
                if (comp.IsFacilityActive(facility))
                {
                    count++;
                }
            }

            return count < MaxCountedFacilities ? count : MaxCountedFacilities;
        }

        // Sum of facilityBonus points from the top 3 highest-value active linked
        // facilities (not just the first 3 linked) — "if more than 3 are linked, the
        // 3 highest facilityBonus values automatically count."
        public static int GetTopFacilityBonusPoints(Thing heater)
        {
            CompAffectedByFacilities comp = heater?.TryGetComp<CompAffectedByFacilities>();
            if (comp == null)
            {
                return 0;
            }

            List<int> values = new List<int>();
            foreach (Thing facility in comp.LinkedFacilitiesListForReading)
            {
                if (!comp.IsFacilityActive(facility))
                {
                    continue;
                }

                CompSaunaFacility facilityComp = facility.TryGetComp<CompSaunaFacility>();
                if (facilityComp != null)
                {
                    values.Add(facilityComp.Props.facilityBonus);
                }
            }

            values.Sort();
            values.Reverse();

            int total = 0;
            for (int i = 0; i < values.Count && i < MaxCountedFacilities; i++)
            {
                total += values[i];
            }

            return total;
        }

        // Matches every intensity-scaled HediffDef's maxSeverity (SaunaBuffBase) and the
        // top Mood stage in ThoughtDefs_Sauna.xml.
        public const float MaxIntensityMultiplier = 1.5f;

        // Room intensity as a multiplier on an archetype's 100%-baseline Mood/stat
        // values: 1.0 (undecorated) up to 1.5. Clamped explicitly: duplicate high-value
        // facilities (e.g. 3 herb baskets = 12 points) would otherwise reach 1.6, which
        // only the Hediff's maxSeverity was silently absorbing.
        public static float GetIntensityMultiplier(Thing heater)
        {
            float intensity = 1f + GetTopFacilityBonusPoints(heater) * IntensityPerBonusPoint;
            return intensity < MaxIntensityMultiplier ? intensity : MaxIntensityMultiplier;
        }

        // Archetype of a sauna heater ThingDef, or null if it isn't one.
        public static SaunaArchetype? HeaterArchetypeOf(ThingDef def)
        {
            return def?.GetCompProperties<CompProperties_SaunaHeater>()?.archetype;
        }

        // Archetype of a built heater — or, with includePlanned, of a heater's blueprint or
        // frame (Blueprint/Frame defs point at the heater via entityDefToBuild, install
        // blueprints included), so placement can't sneak a second type in while the first
        // is still under construction.
        public static SaunaArchetype? HeaterArchetypeOf(Thing thing, bool includePlanned)
        {
            CompSaunaHeater comp = thing.TryGetComp<CompSaunaHeater>();
            if (comp != null)
            {
                return comp.Props.archetype;
            }
            if (includePlanned && (thing is Blueprint || thing is Frame))
            {
                return HeaterArchetypeOf(thing.def.entityDefToBuild as ThingDef);
            }

            return null;
        }

        // Same enclosure rule as RoomRoleWorker_Sauna. The one-heater-type rule is only
        // enforced inside enclosed rooms: before walls are finished, a heater's "room" is
        // the whole outdoors, and blocking on that would wrongly link unrelated saunas
        // under construction elsewhere on the map.
        public static bool IsEnclosedRoom(Room room)
        {
            return room != null && !room.TouchesMapEdge && !room.IsDoorway;
        }

        // One sauna heater type per room. Fills conflicts with every heater in the room
        // whose archetype differs from the given one (1x1 and 2x2 of the same archetype
        // are fine together). ignoreA/ignoreB are excluded — the thing being placed or
        // reinstalled, for PlaceWorker_SaunaNoMixedHeaters.
        public static void FindConflictingHeaters(Room room, SaunaArchetype archetype, bool includePlanned,
            List<Thing> conflicts, Thing ignoreA = null, Thing ignoreB = null)
        {
            conflicts.Clear();
            if (!IsEnclosedRoom(room))
            {
                return;
            }

            foreach (Thing thing in room.ContainedAndAdjacentThings)
            {
                if (thing == ignoreA || thing == ignoreB)
                {
                    continue;
                }

                SaunaArchetype? other = HeaterArchetypeOf(thing, includePlanned);
                if (other != null && other.Value != archetype)
                {
                    conflicts.Add(thing);
                }
            }
        }

        // True if an enclosed room contains built sauna heaters of more than one archetype.
        // Such a room is never scored as a Sauna (RoomRoleWorker_Sauna) — mixing is blocked
        // at placement (PlaceWorker_SaunaNoMixedHeaters), so this only happens when walls
        // change and two rooms merge.
        public static bool HasMixedHeaters(Room room)
        {
            if (!IsEnclosedRoom(room))
            {
                return false;
            }

            SaunaArchetype? first = null;
            foreach (Thing thing in room.ContainedAndAdjacentThings)
            {
                SaunaArchetype? archetype = HeaterArchetypeOf(thing, includePlanned: false);
                if (archetype == null)
                {
                    continue;
                }
                if (first == null)
                {
                    first = archetype;
                }
                else if (first.Value != archetype.Value)
                {
                    return true;
                }
            }

            return false;
        }

        // The temperature every sauna heater in this room stops actively heating at: the
        // lowest maxGateTemperature among the room's powered sauna heaters. Mixed heater
        // types are blocked (see HasMixedHeaters), but a merged room can still briefly
        // contain them until the player fixes it — without a shared ceiling the hotter
        // heater would keep pushing toward its own maximum (e.g. a Detoxified heater
        // taking a Serenity room to 90C) in the meantime. Same-archetype heaters share
        // the same maximum, so this is a no-op for a valid sauna. Returns null if no
        // powered heater is found.
        public static float? GetRoomHeatingCeiling(Room room)
        {
            if (room == null)
            {
                return null;
            }

            float? ceiling = null;
            foreach (Thing thing in room.ContainedAndAdjacentThings)
            {
                CompSaunaHeater heaterComp = thing.TryGetComp<CompSaunaHeater>();
                if (heaterComp == null)
                {
                    continue;
                }

                CompPowerTrader power = thing.TryGetComp<CompPowerTrader>();
                if (power != null && !power.PowerOn)
                {
                    continue;
                }

                float max = heaterComp.Props.maxGateTemperature;
                if (ceiling == null || max < ceiling.Value)
                {
                    ceiling = max;
                }
            }

            return ceiling;
        }

        // Closest sittable thing in the room that the pawn can reserve and reach.
        // Shared by the joy-seeking AI (JoyGiver_Sauna) and the player-forced
        // "Use sauna" float menu option (Building_SaunaHeater).
        public static Thing FindReservableSeat(Pawn pawn, Room room)
        {
            Thing best = null;
            float bestDistSq = float.MaxValue;

            foreach (Thing thing in room.ContainedAndAdjacentThings)
            {
                if (!(thing.def.building?.isSittable ?? false))
                {
                    continue;
                }
                if (!pawn.CanReserveAndReach(thing, PathEndMode.OnCell, Danger.Some))
                {
                    continue;
                }

                float distSq = (thing.Position - pawn.Position).LengthHorizontalSquared;
                if (distSq < bestDistSq)
                {
                    bestDistSq = distSq;
                    best = thing;
                }
            }

            return best;
        }

        // Used by the apparel-hiding Harmony patches so pawns render naked (appearance
        // only — WornApparel is never touched) while the sauna job is active. Checks
        // all 3 JoyKindDef-bucket JobDefs (Phase 4) — a pawn in any of them is still
        // "using the sauna".
        public static bool IsUsingSauna(Pawn pawn)
        {
            JobDef def = pawn?.CurJob?.def;
            return def == SaunaDefOf.Sauna_UseHeater_Hydrotherapy
                || def == SaunaDefOf.Sauna_UseHeater_Meditative
                || def == SaunaDefOf.Sauna_UseHeater_BathRelaxing;
        }

        // Which JoyKindDef bucket (and therefore which JobDef) an archetype's heater
        // uses — see the design spec's "Recreation Type (JoyKindDef) Mapping" section.
        public static JobDef JobDefForArchetype(SaunaArchetype archetype)
        {
            switch (archetype)
            {
                case SaunaArchetype.Detoxified:
                case SaunaArchetype.Purified:
                case SaunaArchetype.Invigorated:
                case SaunaArchetype.Cleansed:
                    return SaunaDefOf.Sauna_UseHeater_Hydrotherapy;
                case SaunaArchetype.Serenity:
                    return SaunaDefOf.Sauna_UseHeater_Meditative;
                case SaunaArchetype.Radiant:
                case SaunaArchetype.Rejuvenated:
                    return SaunaDefOf.Sauna_UseHeater_BathRelaxing;
                default:
                    return null;
            }
        }

        // Cold Plunge Tub's hard gate (Resolved Decision #20): only true if the pawn
        // currently has one of the 7 archetype buffs active. The cross-cutting buffs
        // (Heat-Numbed, Fresh Air, etc.) are always granted alongside an archetype buff
        // on the same session, never alone, so checking the 7 archetype Hediffs alone is
        // a complete "did this pawn just finish a sauna session" signal.
        public static bool HasActiveSaunaBuff(Pawn pawn)
        {
            HediffSet hediffSet = pawn.health.hediffSet;
            return hediffSet.HasHediff(SaunaDefOf.Sauna_Serenity)
                || hediffSet.HasHediff(SaunaDefOf.Sauna_Detoxified)
                || hediffSet.HasHediff(SaunaDefOf.Sauna_Purified)
                || hediffSet.HasHediff(SaunaDefOf.Sauna_Radiant)
                || hediffSet.HasHediff(SaunaDefOf.Sauna_Rejuvenated)
                || hediffSet.HasHediff(SaunaDefOf.Sauna_Invigorated)
                || hediffSet.HasHediff(SaunaDefOf.Sauna_Cleansed);
        }

        // Heatstroke safety gate: refuses a new/continuing sauna session once the pawn
        // is already at "minor" heatstroke (vanilla Heatstroke HediffDef's minSeverity
        // for that stage is 0.2) or worse. Sauna sessions are safe on their own — a full
        // 1-hour session at the hottest archetype's ceiling only reaches ~0.11 severity
        // for an unprotected pawn (checked against the real HediffGiver_Heat formula,
        // not guessed) — this exists to stop severity compounding across back-to-back
        // sessions before a previous session's mild heatstroke has decayed.
        public static bool HasMinorOrWorseHeatstroke(Pawn pawn)
        {
            Hediff heatstroke = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Heatstroke);
            return heatstroke != null && heatstroke.Severity >= 0.2f;
        }
    }
}
