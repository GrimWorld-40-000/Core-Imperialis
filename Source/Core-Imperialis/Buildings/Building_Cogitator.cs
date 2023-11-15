using Core_Imp.Dialogs;
using GW_Frame;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace Core_Imp
{
    [StaticConstructorOnStartup]
    public class Building_Cogitator : Building, IThingHolder
    {
        public ThingOwner innerContainer;
        public List<StudyRequirement> studyRequirements;

        private static readonly Texture2D CancelIcon = ContentFinder<Texture2D>.Get("UI/Designators/Cancel");
        private static readonly Texture2D StudySTCFragmentsIcon = ContentFinder<Texture2D>.Get("UI/GW_STCFragment_study");

        [Unsaved(false)]
        private CompPowerTrader cachedPowerComp;

        [Unsaved(false)]
        private float lastWorkAmount = -1f;

        private int lastWorkedTick = -999;
        private ResearchProjectDef project;
        private float totalWorkRequired;
        private float workDone;
        private bool workingInt;

        public AcceptanceReport CanBeWorkedOnNow
        {
            get
            {
                if (!Working)
                {
                    return false;
                }
                if (NeedsItems)
                {
                    return false;
                }
                if (!PowerOn)
                {
                    return "NoPower".Translate().CapitalizeFirst();
                }
                return true;
            }
        }

        public bool NeedsItems => StudyRequirementsNotMet().Any();

        public bool PowerOn => PowerTraderComp.PowerOn;

        public float ProgressPercent => workDone / totalWorkRequired;

        public string ResearchName => project?.LabelCap;

        public bool Working => workingInt;

        private CompPowerTrader PowerTraderComp
        {
            get
            {
                if (cachedPowerComp == null)
                {
                    cachedPowerComp = this.TryGetComp<CompPowerTrader>();
                }
                return cachedPowerComp;
            }
        }

        public void DoWork(float workAmount)
        {
            workDone += workAmount;
            lastWorkAmount = workAmount;
            lastWorkedTick = Find.TickManager.TicksGame;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref innerContainer, "innerContainer", this);
            Scribe_Values.Look(ref workingInt, "workingInt", false);
            Scribe_Values.Look(ref workDone, "workDone", 0f);
            Scribe_Values.Look(ref totalWorkRequired, "totalWorkRequired", 0f);
            Scribe_Values.Look(ref lastWorkedTick, "lastWorkedTick", -999);
            Scribe_Values.Look(ref lastWorkedTick, "lastWorkedTick", -999);
            Scribe_Collections.Look(ref studyRequirements, "studyRequirements", LookMode.Deep);

            Scribe_Defs.Look(ref project, "project");
        }

        public void Finish(Pawn pawn = null, bool debugFinish = false)
        {
            if (studyRequirements.Any())
            {
                if (debugFinish)
                    Find.World.GetComponent<WorldComponent_StudyManager>().CompleteAllRequirements(project);

                for (int num = innerContainer.Count - 1; num >= 0; num--)
                {
                    Thing thing = innerContainer[num];
                    Find.World.GetComponent<WorldComponent_StudyManager>().AddCompletedRequirement(project, thing.def);
                    thing.Destroy();
                }
            }
            Find.LetterStack.ReceiveLetter("GW_NewResearchDiscovered".Translate(), "GW_StudyCompleteMessage".Translate(pawn?.Name, project.LabelCap), LetterDefOf.PositiveEvent);
            Reset();
        }

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
        }

        public ThingOwner GetDirectlyHeldThings()
        {
            return innerContainer;
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }

            Command_Action command_Action = new Command_Action();
            command_Action.defaultLabel = "GW_StudySTCFragments".Translate() + "...";
            command_Action.defaultDesc = "GW_StudySTCFragmentsDescription".Translate();
            command_Action.icon = StudySTCFragmentsIcon;
            command_Action.action = delegate
            {
                Find.WindowStack.Add(new Dialog_StudySTCFragments(this));
            };
            if (!PowerOn)
            {
                command_Action.Disable("CannotUseNoPower".Translate());
            }
            if (!Working)
            {
                yield return command_Action;
                yield break;
            }
            Command_Action command_Action2 = new Command_Action();
            command_Action2.defaultLabel = "Cancel".Translate();
            command_Action2.action = delegate
            {
                Reset();
            };
            command_Action2.icon = CancelIcon;
            yield return command_Action2;
            if (DebugSettings.ShowDevGizmos)
            {
                Command_Action command_Action3 = new Command_Action();
                command_Action3.defaultLabel = "DEV: Complete study STC";
                command_Action3.action = delegate
                {
                    Finish(debugFinish: true);
                };
                yield return command_Action3;
            }
        }

        public override string GetInspectString()
        {
            string text = base.GetInspectString();
            if (Working)
            {
                if (!text.NullOrEmpty())
                {
                    text += "\n";
                }
                text = text + "GW_DiscoveringProject".Translate() + project.LabelCap;
                text += "\n" + "GW_Progress".Translate() + ": " + ProgressPercent.ToStringPercent();
                int numTicks = Mathf.RoundToInt((totalWorkRequired - workDone) / ((lastWorkAmount > 0f) ? lastWorkAmount : this.GetStatValue(StatDefOf.ResearchSpeedFactor)));
                text = text + " (" + "GW_DurationLeft".Translate(numTicks.ToStringTicksToPeriod()).Resolve() + ")";
                AcceptanceReport canBeWorkedOnNow = CanBeWorkedOnNow;
                if (!canBeWorkedOnNow.Accepted && !canBeWorkedOnNow.Reason.NullOrEmpty())
                {
                    text = text + "\n" + ("GW_StudyingPaused".Translate() + ": " + canBeWorkedOnNow.Reason).Colorize(ColorLibrary.RedReadable);
                }
                if (studyRequirements.Any())
                {
                    foreach (var item in studyRequirements.Join(StudyRequirementsNotMet(),
                        x => x.StudyObject,
                        y => y.StudyObject,
                        (spec, missing) => new { name = spec.StudyObject, need = spec.NumberRequired, have = missing.NumberRequired }))
                    {
                        text = text + $"\n{item.name.LabelCap}: {item.need - item.have}/{item.have}";
                    }
                }
            }
            return text;
        }

        public override void PostPostMake()
        {
            base.PostPostMake();
            studyRequirements = new List<StudyRequirement>();
            innerContainer = new ThingOwner<Thing>(this);
        }

        public void Start(ResearchProjectDef research)
        {
            const float TicksPerHour = 2500f;
            Reset();
            project = research;
            var extraReqExtension = project.GetModExtension<DefModExtension_ExtraPrerequisiteActions>();
            studyRequirements = extraReqExtension.ItemStudyRequirements.ListFullCopy();
            workingInt = true;
            totalWorkRequired = studyRequirements.Sum(x => x.NumberRequired) * TicksPerHour;
        }

        public IEnumerable<StudyRequirement> StudyRequirementsNotMet()
        {
            foreach (var req in studyRequirements)
            {
                if (innerContainer.Count == 0)
                {
                    yield return new StudyRequirement() { StudyObject = req.StudyObject, NumberRequired = req.NumberRequired };
                }
                else
                {
                    bool foundItem = false;
                    for (int i = 0; i < innerContainer.Count; i++)
                    {
                        if (innerContainer[i].def == req.StudyObject)
                        {
                            foundItem = true;
                            if (innerContainer[i].stackCount >= req.NumberRequired)
                                break;
                            yield return new StudyRequirement() { StudyObject = req.StudyObject, NumberRequired = req.NumberRequired - innerContainer[i].stackCount };
                        }
                    }

                    if (!foundItem)
                        yield return new StudyRequirement() { StudyObject = req.StudyObject, NumberRequired = req.NumberRequired };
                }
            }
        }

        public override void Tick()
        {
            base.Tick();
            innerContainer.ThingOwnerTick();
            if (this.IsHashIntervalTick(250))
            {
                bool flag = lastWorkedTick + 250 + 2 >= Find.TickManager.TicksGame;
                PowerTraderComp.PowerOutput = (flag ? (0f - PowerComp.Props.PowerConsumption) : (0f - PowerComp.Props.idlePowerDraw));
            }
        }

        private void Reset()
        {
            workingInt = false;
            project = null;
            workDone = 0f;
            lastWorkedTick = -999;
            studyRequirements = new List<StudyRequirement>();
            innerContainer.TryDropAll(def.hasInteractionCell ? InteractionCell : Position, Map, ThingPlaceMode.Near);
        }
    }
}