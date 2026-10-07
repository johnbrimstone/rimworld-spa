using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace SpaMod
{
    // Adds a right-click "Use sauna" order so the feature can be tested (and played)
    // without waiting on the AI's joy-seeking behavior or enabling Dev Mode.
    public class Building_SaunaHeater : Building
    {
        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn selPawn)
        {
            foreach (FloatMenuOption option in base.GetFloatMenuOptions(selPawn))
            {
                yield return option;
            }

            if (!selPawn.CanReach(this, PathEndMode.Touch, Danger.Some))
            {
                yield break;
            }

            // Checked before the room role: a mixed room isn't a Sauna, and silently
            // offering nothing would leave the player guessing why.
            CompSaunaHeater comp = this.TryGetComp<CompSaunaHeater>();
            if (comp != null && comp.InMixedRoom)
            {
                yield return new FloatMenuOption("Use sauna (mixed heater types in this room)", null);
                yield break;
            }

            Room room = this.GetRoom();
            if (room == null || room.Role != SaunaDefOf.Sauna)
            {
                yield break;
            }

            if (SaunaUtility.HasMinorOrWorseHeatstroke(selPawn))
            {
                yield return new FloatMenuOption("Use sauna (still recovering from heat)", null);
                yield break;
            }

            Thing seat = SaunaUtility.FindReservableSeat(selPawn, room);
            if (seat == null)
            {
                yield return new FloatMenuOption("Use sauna (no available seat)", null);
                yield break;
            }

            JobDef jobDef = comp != null ? SaunaUtility.JobDefForArchetype(comp.Props.archetype) : null;
            if (jobDef == null)
            {
                yield break;
            }

            Thing heater = this;
            yield return new FloatMenuOption("Use sauna", delegate
            {
                Job job = JobMaker.MakeJob(jobDef, seat, heater);
                selPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
            });
        }
    }
}
