using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Core_Imp
{
    public class WorkGiver_StudySTCFragment : WorkGiver_Scanner
    {
        public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForDef(CoreImperialisDefOf.GW_Cogitator);

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (!(t is Building_Cogitator building_Cogitator))
            {
                return false;
            }
            if (building_Cogitator.NeedsItems)
            {
                foreach (var item in building_Cogitator.StudyRequirementsNotMet())
                {
                    if (FindFragment(pawn, item.StudyObject) == null)
                    {
                        JobFailReason.Is("NoIngredient".Translate(item.StudyObject));
                        return false;
                    }
                }

                return true;
            }
            if (!building_Cogitator.CanBeWorkedOnNow.Accepted)
            {
                return false;
            }
            if (!pawn.CanReserve(t, 1, -1, null, forced) || !pawn.CanReserveSittableOrSpot(t.InteractionCell, forced))
            {
                return false;
            }
            return true;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (!(t is Building_Cogitator cogitator))
            {
                return null;
            }
            if (cogitator.NeedsItems)
            {
                foreach (var item in cogitator.StudyRequirementsNotMet())
                {
                    Thing thing = FindFragment(pawn, item.StudyObject);
                    if (thing != null)
                    {
                        Job job = JobMaker.MakeJob(JobDefOf.HaulToContainer, thing, t);
                        job.count = Mathf.Min(item.NumberRequired, thing.stackCount);
                        return job;
                    }
                }
            }
            return JobMaker.MakeJob(CoreImperialisDefOf.GW_StudySTCFragment, t, 1200, checkOverrideOnExpiry: true);
        }

        private Thing FindFragment(Pawn pawn, ThingDef defToFind)
        {
            return GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForDef(defToFind), PathEndMode.ClosestTouch, TraverseParms.For(pawn), 9999f, (Thing x) => !x.IsForbidden(pawn) && pawn.CanReserve(x));
        }
    }
}