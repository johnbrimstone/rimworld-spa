using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace SpaMod
{
    // Targets: A = the tub itself. Unlike the sauna bench mechanic, the pawn paths
    // directly onto the tub's own footprint (PathEndMode.OnCell resolves to the 2x2
    // building's Position cell, which is part of its own occupied rect) rather than an
    // adjacent seat — "goes in" the tub rather than sitting next to it.
    public class JobDriver_UseColdPlungeTub : JobDriver
    {
        // ~10 minutes (2500 ticks/hour ÷ 6) — independent of the sauna's 1-hour rule.
        private const int SessionDurationTicks = 417;

        private Thing Tub => TargetThingA;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(Tub, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);
            // Safety net for manually-forced jobs bypassing JoyGiver_ColdPlungeTub's
            // eligibility check (Resolved Decision #20).
            AddFailCondition(() => !SaunaUtility.HasActiveSaunaBuff(pawn));

            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.OnCell);

            Toil soak = ToilMaker.MakeToil("UseColdPlungeTub");
            soak.tickIntervalAction = delta =>
            {
                JoyUtility.JoyTickCheckEnd(pawn, delta, JoyTickFullJoyAction.None, 1f, Tub as Building);
            };
            soak.defaultCompleteMode = ToilCompleteMode.Delay;
            soak.defaultDuration = SessionDurationTicks;
            soak.handlingFacing = true;
            // All-or-nothing, same as the sauna's own 1-hour rule (Resolved Decision
            // #22), just on Cold Plunge Tub's own much shorter threshold.
            soak.AddFinishAction(delegate
            {
                if (ticksLeftThisToil <= 0)
                {
                    ApplyColdPlungeBuff(soak.actor);
                }
            });
            yield return soak;
        }

        // Fixed strength — not linked to any heater, so no intensity to scale by
        // (Resolved Decisions #19/#25/#28).
        private static void ApplyColdPlungeBuff(Pawn pawn)
        {
            Hediff hediff = pawn.health.GetOrAddHediff(SaunaDefOf.Sauna_ColdPlunge);
            hediff.Severity = 1f;

            // Contrast therapy: a completed cold plunge fully cures any heatstroke,
            // regardless of source or severity — not a partial reduction. This also
            // doubles as the intended way around JoyGiver_Sauna's heatstroke cooldown
            // gate (SaunaUtility.HasMinorOrWorseHeatstroke): a pawn who actively cools
            // down can go straight back in, one who doesn't has to wait it out.
            Hediff heatstroke = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Heatstroke);
            if (heatstroke != null)
            {
                pawn.health.RemoveHediff(heatstroke);
            }
        }
    }
}
