using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace SpaMod
{
    // Targets: A = seat to relax at (anywhere in the sauna room), B = a heater in that
    // room (kept only for the JoyGainFactor stat and joyKind consistency check, not for
    // positioning). The buff is determined by re-checking the room at completion, per the
    // design spec, so it stays current if the player changes the room's heater/decor later.
    public class JobDriver_UseSauna : JobDriver
    {
        private Thing Seat => TargetThingA;
        private Thing Heater => TargetThingB;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(Seat, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);
            // Safety net for manually-forced jobs bypassing JoyGiver_Sauna's heatstroke
            // eligibility check — also catches a pawn crossing into "minor" heatstroke
            // mid-session (e.g. residual severity from a prior session that hadn't fully
            // decayed yet), pulling them out rather than letting it compound further.
            AddFailCondition(() => SaunaUtility.HasMinorOrWorseHeatstroke(pawn));

            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.OnCell);

            Toil relax = ToilMaker.MakeToil("UseSauna");
            relax.tickIntervalAction = delta =>
            {
                pawn.GainComfortFromCellIfPossible(delta);
                // JoyTickFullJoyAction.None (rather than EndJob): a maxed-out Joy need
                // should stop granting more joy, but the pawn still needs to stay the
                // full defaultDuration to earn the full buff — see ApplyRoomBuff.
                JoyUtility.JoyTickCheckEnd(pawn, delta, JoyTickFullJoyAction.None, 1f, Heater as Building);
                GainPsyfocusIfFullyDecorated(pawn, Heater, delta);
            };
            relax.defaultCompleteMode = ToilCompleteMode.Delay;
            relax.defaultDuration = job.def.joyDuration;
            relax.handlingFacing = true;
            // The buff has to be granted via AddFinishAction (fires on this toil's
            // Cleanup regardless of how it ends: natural completion or interruption),
            // the same pattern vanilla uses for the "watched TV" thought. ticksLeftThisToil
            // reaching 0 is how ToilCompleteMode.Delay itself detects natural completion
            // (see JobDriver.DriverTick), so checking it here distinguishes "sat the full
            // hour" from "got interrupted partway through". All-or-nothing: an interrupted
            // session grants no buff at all — a scaled partial reward was judged
            // exploitable via short repeated sessions.
            relax.AddFinishAction(delegate
            {
                if (ticksLeftThisToil <= 0)
                {
                    ApplyRoomBuff(relax.actor);
                }
            });
            yield return relax;
        }

        private const int ElderlyAgeThreshold = 50;
        private const int DeeplyRelaxedGainBase = 1;
        private const int DeeplyRelaxedGainFullyDecorated = 2;
        private const float DeeplyRelaxedMaxStacks = 25f;

        // Room intensity (1.0 baseline, up to 1.5 with 3 max-value facilities linked —
        // see SaunaUtility.GetIntensityMultiplier) is applied as each intensity-scaled
        // Hediff's severity; every one of those HediffDefs uses multiplyStatChangesBySeverity
        // (or, for Vitality's painFactor, discrete stages — see HediffDefs_Sauna.xml) so
        // their 100%-baseline values scale continuously with this number. Everything here
        // only runs on a full, uninterrupted session (see the AddFinishAction above).
        private static void ApplyRoomBuff(Pawn pawn)
        {
            Room room = pawn.GetRoom();
            if (room == null || room.Role != SaunaDefOf.Sauna)
            {
                return;
            }

            Thing heater = FindHeaterThing(room);
            CompSaunaHeater comp = heater?.TryGetComp<CompSaunaHeater>();
            if (comp == null)
            {
                return;
            }

            // Temperature gate ("Room & Sauna Definition" / Resolved Decision #30-31):
            // a simple pass/fail — outside the heater variant's expected range, no buff
            // is granted at all, same all-or-nothing philosophy as an interrupted session.
            if (!comp.IsRoomTemperatureInRange(room))
            {
                return;
            }

            float intensity = SaunaUtility.GetIntensityMultiplier(heater);

            HediffDef archetypeBuffDef = HediffDefForArchetype(comp.Props.archetype);
            if (archetypeBuffDef != null)
            {
                SetSeverity(pawn, archetypeBuffDef, intensity);
            }

            // Cross-cutting: every archetype, every full session.
            SetSeverity(pawn, SaunaDefOf.Sauna_HeatNumbed, intensity);

            // Serenity-exclusive: layered on top of the Serenity buff itself, not a
            // replacement for it.
            if (comp.Props.archetype == SaunaArchetype.Serenity)
            {
                SetSeverity(pawn, SaunaDefOf.Sauna_Vitality, intensity);
                SetSeverity(pawn, SaunaDefOf.Sauna_Clarity, intensity);
            }

            // Flat, not intensity-scaled, from here down.
            if (HasBondingPartner(pawn, room))
            {
                pawn.health.GetOrAddHediff(SaunaDefOf.Sauna_SaunaBonding);
            }

            pawn.health.GetOrAddHediff(SaunaDefOf.Sauna_FreshAir);

            if (IsEasedJointsEligible(pawn))
            {
                pawn.health.GetOrAddHediff(SaunaDefOf.Sauna_EasedJoints);
            }

            GainDeeplyRelaxedStack(pawn, heater);
        }

        private static void SetSeverity(Pawn pawn, HediffDef def, float severity)
        {
            Hediff hediff = pawn.health.GetOrAddHediff(def);
            hediff.Severity = severity;
        }

        // Simple room-occupancy check at the moment this pawn's session completes,
        // rather than tracking overlap across the whole session — a partner who
        // finishes and leaves earlier won't retroactively grant this to either pawn.
        // Accepted as a minor edge case for what's a small flat mood bonus.
        private static bool HasBondingPartner(Pawn pawn, Room room)
        {
            foreach (Thing thing in room.ContainedAndAdjacentThings)
            {
                if (thing is Pawn otherPawn
                    && otherPawn != pawn
                    && otherPawn.IsColonist
                    && SaunaUtility.IsUsingSauna(otherPawn))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsEasedJointsEligible(Pawn pawn)
        {
            if (pawn.ageTracker != null && pawn.ageTracker.AgeBiologicalYears >= ElderlyAgeThreshold)
            {
                return true;
            }

            return pawn.health.hediffSet.HasHediff(SaunaDefOf.BadBack)
                || pawn.health.hediffSet.HasHediff(SaunaDefOf.Frail);
        }

        // Persistent across sessions (gained, not overwritten) — separate from every
        // other buff here, which is reset to the current session's value each time.
        private static void GainDeeplyRelaxedStack(Pawn pawn, Thing heater)
        {
            int gain = SaunaUtility.CountLinkedFacilities(heater) >= SaunaUtility.MaxCountedFacilities
                ? DeeplyRelaxedGainFullyDecorated
                : DeeplyRelaxedGainBase;

            Hediff hediff = pawn.health.GetOrAddHediff(SaunaDefOf.Sauna_DeeplyRelaxed);
            float newSeverity = hediff.Severity + gain;
            hediff.Severity = newSeverity < DeeplyRelaxedMaxStacks ? newSeverity : DeeplyRelaxedMaxStacks;
        }

        // Second, independent path to Psyfocus (alongside the separate vanilla Meditate
        // job every fully-decorated heater already supports via CompMeditationFocus):
        // a psycaster relaxing in the sauna itself also gains Psyfocus directly, but only
        // once the room is fully decorated, matching the same "Unlock condition" the spec
        // gives for Psyfocus generally. Reuses vanilla's own gain formula/method
        // (GainPsyfocus_NewTemp), just called from our job instead of JobDriver_Meditate.
        private static void GainPsyfocusIfFullyDecorated(Pawn pawn, Thing heater, int delta)
        {
            if (heater == null || !pawn.HasPsylink)
            {
                return;
            }
            if (SaunaUtility.CountLinkedFacilities(heater) < SaunaUtility.MaxCountedFacilities)
            {
                return;
            }
            pawn.psychicEntropy?.GainPsyfocus_NewTemp(delta, heater);
        }

        private static Thing FindHeaterThing(Room room)
        {
            foreach (Thing thing in room.ContainedAndAdjacentThings)
            {
                if (thing.TryGetComp<CompSaunaHeater>() != null)
                {
                    return thing;
                }
            }

            return null;
        }

        private static HediffDef HediffDefForArchetype(SaunaArchetype archetype)
        {
            switch (archetype)
            {
                case SaunaArchetype.Serenity:
                    return SaunaDefOf.Sauna_Serenity;
                case SaunaArchetype.Detoxified:
                    return SaunaDefOf.Sauna_Detoxified;
                case SaunaArchetype.Purified:
                    return SaunaDefOf.Sauna_Purified;
                case SaunaArchetype.Radiant:
                    return SaunaDefOf.Sauna_Radiant;
                case SaunaArchetype.Rejuvenated:
                    return SaunaDefOf.Sauna_Rejuvenated;
                case SaunaArchetype.Invigorated:
                    return SaunaDefOf.Sauna_Invigorated;
                case SaunaArchetype.Cleansed:
                    return SaunaDefOf.Sauna_Cleansed;
                default:
                    return null;
            }
        }
    }
}
