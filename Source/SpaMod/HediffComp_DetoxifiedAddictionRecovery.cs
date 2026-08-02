using System.Collections.Generic;
using RimWorld;
using Verse;

namespace SpaMod
{
    public class HediffCompProperties_DetoxifiedAddictionRecovery : HediffCompProperties
    {
        // Fraction faster the pawn's existing addictions recover, at 100% room
        // intensity (severity 1.0) — scales with the parent buff's severity, same as
        // its stat effects (e.g. 0.75 at the max 150% intensity).
        public float baseRecoveryBoostFactor = 0.5f;

        public HediffCompProperties_DetoxifiedAddictionRecovery()
        {
            compClass = typeof(HediffComp_DetoxifiedAddictionRecovery);
        }
    }

    // Addictions are hediffs (vanilla Hediff_Addiction) that recover toward cured via a
    // HediffComp_SeverityPerDay on the addiction itself. Vanilla already has a public
    // precedent for reaching into another hediff's addiction state this way
    // (CompUseEffect_FixWorstHealthCondition cures Hediff_Addiction directly), so this
    // isn't a private-method Harmony patch.
    public class HediffComp_DetoxifiedAddictionRecovery : HediffComp
    {
        public HediffCompProperties_DetoxifiedAddictionRecovery Props =>
            (HediffCompProperties_DetoxifiedAddictionRecovery)props;

        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);

            float boostFactor = Props.baseRecoveryBoostFactor * parent.Severity;
            List<Hediff> hediffs = Pawn.health.hediffSet.hediffs;

            for (int i = 0; i < hediffs.Count; i++)
            {
                if (!(hediffs[i] is Hediff_Addiction addiction))
                {
                    continue;
                }

                HediffComp_SeverityPerDay recovery = addiction.TryGetComp<HediffComp_SeverityPerDay>();
                float dailyRate = recovery?.SeverityChangePerDay() ?? 0f;
                if (dailyRate >= 0f)
                {
                    continue;
                }

                // dailyRate is negative (recovering); adding a boosted fraction of it
                // reduces severity further, on top of the addiction's own normal
                // per-interval recovery — net effect: (1 + boostFactor)x the normal rate.
                addiction.Severity += dailyRate * boostFactor / 60000f * delta;
            }
        }
    }
}
