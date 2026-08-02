using RimWorld;
using Verse;
using Verse.AI;

namespace SpaMod
{
    // Unlike vanilla's JoyGiver_WatchBuilding (which requires standing next to a specific
    // building, facing a specific direction), the sauna's joy source is the room itself:
    // any room scored as Sauna qualifies, and the pawn can relax at any sittable spot in
    // it, not just next to the heater.
    public class JoyGiver_Sauna : JoyGiver
    {
        public override Job TryGiveJob(Pawn pawn)
        {
            // Heatstroke safety gate (see SaunaUtility.HasMinorOrWorseHeatstroke) — never
            // offered to a pawn still recovering from a previous session's mild heatstroke.
            if (SaunaUtility.HasMinorOrWorseHeatstroke(pawn))
            {
                return null;
            }

            Map map = pawn.Map;
            if (map == null || def.thingDefs == null)
            {
                return null;
            }

            foreach (ThingDef heaterDef in def.thingDefs)
            {
                foreach (Thing heater in map.listerThings.ThingsOfDef(heaterDef))
                {
                    if (!CanUseHeater(pawn, heater))
                    {
                        continue;
                    }

                    Room room = heater.GetRoom();
                    if (room == null || room.Role != SaunaDefOf.Sauna)
                    {
                        continue;
                    }

                    Thing seat = SaunaUtility.FindReservableSeat(pawn, room);
                    if (seat == null)
                    {
                        continue;
                    }

                    return JobMaker.MakeJob(def.jobDef, seat, heater);
                }
            }

            return null;
        }

        private bool CanUseHeater(Pawn pawn, Thing heater)
        {
            if (heater.IsForbidden(pawn) || heater.Fogged())
            {
                return false;
            }
            if (!heater.IsSociallyProper(pawn) || !heater.IsPoliticallyProper(pawn))
            {
                return false;
            }
            CompPowerTrader power = heater.TryGetComp<CompPowerTrader>();
            return power == null || power.PowerOn;
        }
    }
}
