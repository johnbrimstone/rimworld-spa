using RimWorld;
using Verse;
using Verse.AI;

namespace SpaMod
{
    // Hard-gated (Resolved Decision #20): never offered as a joy option to a pawn
    // without a currently-active sauna buff Hediff — not a weighted nudge, a real
    // eligibility gate. JobDriver_UseColdPlungeTub has a matching FailOn as a safety
    // net for manually-forced jobs that bypass this.
    public class JoyGiver_ColdPlungeTub : JoyGiver
    {
        public override bool CanBeGivenTo(Pawn pawn)
        {
            if (!SaunaUtility.HasActiveSaunaBuff(pawn))
            {
                return false;
            }

            return base.CanBeGivenTo(pawn);
        }

        public override Job TryGiveJob(Pawn pawn)
        {
            if (!SaunaUtility.HasActiveSaunaBuff(pawn))
            {
                return null;
            }

            Map map = pawn.Map;
            if (map == null || def.thingDefs == null)
            {
                return null;
            }

            foreach (ThingDef tubDef in def.thingDefs)
            {
                foreach (Thing tub in map.listerThings.ThingsOfDef(tubDef))
                {
                    if (!CanUseTub(pawn, tub))
                    {
                        continue;
                    }

                    return JobMaker.MakeJob(def.jobDef, tub);
                }
            }

            return null;
        }

        private static bool CanUseTub(Pawn pawn, Thing tub)
        {
            if (tub.IsForbidden(pawn) || tub.Fogged())
            {
                return false;
            }
            if (!tub.IsSociallyProper(pawn) || !tub.IsPoliticallyProper(pawn))
            {
                return false;
            }

            return pawn.CanReserveAndReach(tub, PathEndMode.OnCell, Danger.Some);
        }
    }
}
