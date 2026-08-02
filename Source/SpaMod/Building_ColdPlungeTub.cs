using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace SpaMod
{
    // Right-click "Use" order, matching Building_SaunaHeater's pattern — but still
    // respects the hard gate (Resolved Decision #20 applies to forced jobs too, not
    // just AI-driven ones), so a pawn without an active sauna buff sees a disabled
    // explanation instead of a usable option.
    public class Building_ColdPlungeTub : Building
    {
        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn selPawn)
        {
            foreach (FloatMenuOption option in base.GetFloatMenuOptions(selPawn))
            {
                yield return option;
            }

            if (!SaunaUtility.HasActiveSaunaBuff(selPawn))
            {
                yield return new FloatMenuOption("Use cold plunge tub (needs an active sauna buff)", null);
                yield break;
            }

            if (!selPawn.CanReserveAndReach(this, PathEndMode.OnCell, Danger.Some))
            {
                yield break;
            }

            Thing tub = this;
            yield return new FloatMenuOption("Use cold plunge tub", delegate
            {
                Job job = JobMaker.MakeJob(SaunaDefOf.Sauna_UseColdPlungeTub, tub);
                selPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
            });
        }
    }
}
