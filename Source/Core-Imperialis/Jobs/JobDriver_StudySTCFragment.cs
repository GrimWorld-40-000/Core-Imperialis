using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace Core_Imp
{
    internal class JobDriver_StudySTCFragment : JobDriver
    {
        private const int JobEndInterval = 4000;

        private Building_Cogitator Cogitator => (Building_Cogitator)base.TargetThingA;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (pawn.Reserve(Cogitator, job, 1, -1, null, errorOnFailed))
            {
                return pawn.ReserveSittableOrSpot(Cogitator.InteractionCell, job, errorOnFailed);
            }
            return false;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            this.FailOn(() => !Cogitator.CanBeWorkedOnNow.Accepted);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);
            Toil toil = ToilMaker.MakeToil("MakeNewToils");
            toil.tickAction = delegate
            {
                float workAmount = pawn.GetStatValue(StatDefOf.ResearchSpeed) * Cogitator.GetStatValue(StatDefOf.ResearchSpeedFactor);
                Cogitator.DoWork(workAmount);
                pawn.skills.Learn(SkillDefOf.Intellectual, 0.1f);
                pawn.GainComfortFromCellIfPossible(chairsOnly: true);
                if (Cogitator.ProgressPercent >= 1f)
                {
                    Cogitator.Finish(pawn);
                    pawn.jobs.EndCurrentJob(JobCondition.Succeeded);
                }
            };
            toil.FailOnCannotTouch(TargetIndex.A, PathEndMode.InteractionCell);
            toil.WithProgressBar(TargetIndex.A, () => Cogitator.ProgressPercent, interpolateBetweenActorAndTarget: false, 0f);
            toil.defaultCompleteMode = ToilCompleteMode.Never;
            toil.defaultDuration = 4000;
            toil.activeSkill = () => SkillDefOf.Intellectual;
            yield return toil;
        }
    }
}