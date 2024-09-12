using GW_Frame;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace Core_Imp.Dialogs
{
    [StaticConstructorOnStartup]
    public class Dialog_StudySTCFragments : Window
    {
        protected const float HeaderHeight = 35f;
        protected static readonly Vector2 ButSize = new Vector2(150f, 38f);
        protected string Header = "GW_StudyFragmentsDialogTitle".Translate();
        protected QuickSearchWidget quickSearchWidget = new QuickSearchWidget();
        protected float searchWidgetOffsetX;
        private static readonly Color MenuSectionBGBorderColor = new ColorInt(135, 135, 135).ToColor;
        private static readonly Color MissingPrerequisiteColor = ColorLibrary.RedReadable;
        private static readonly Color NoMatchTintColor = Widgets.MenuSectionBGFillColor;
        private Dictionary<ResearchProjectDef, List<Pair<ResearchPrerequisitesUtility.UnlockedHeader, List<Def>>>> cachedUnlockedDefsGroupedByPrerequisites;
        private Building_Cogitator cogitator;
        private float rightScrollViewHeight = 0;
        private Vector2 scrollPosition = Vector2.zero;
        private Vector2 scrollPositionRight = Vector2.zero;
        private ResearchProjectDef selectedProject = null;
        private List<string> tmpSuffixesForUnlocked = new List<string>();

        public Dialog_StudySTCFragments(Building_Cogitator cogitator)
        {
            this.cogitator = cogitator;
            forcePause = true;
            absorbInputAroundWindow = true;
        }

        public override Vector2 InitialSize => new Vector2(1000, 500);

        public override void DoWindowContents(Rect rect)
        {
            var insideRect = new Rect(rect.x, rect.y + 35, rect.width, rect.height - 35);
            var leftHalf = insideRect.LeftHalf();
            var righthalf = insideRect.RightHalf();
            leftHalf.width -= 5;
            righthalf.x += 5;
            righthalf.width -= 5;
            var rightOutRect = new Rect(righthalf.x + 5, righthalf.y + 35, righthalf.width - 10, righthalf.height - 40);

            var outrect = new Rect(leftHalf.x + 5, leftHalf.y + 35, leftHalf.width - 10, leftHalf.height - 35 - 50);
            Rect rect3 = new Rect(rect.x, rect.y, rect.width, 30f);
            Text.Font = GameFont.Medium;
            Widgets.Label(rect3, Header);
            Widgets.DrawMenuSection(leftHalf);
            Widgets.DrawMenuSection(righthalf);
            GUI.color = MenuSectionBGBorderColor;
            Widgets.DrawBox(new Rect(leftHalf.x, leftHalf.y + 30, 200, 1));
            Widgets.DrawBox(new Rect(righthalf.x, righthalf.y + 30, 200, 1));
            GUI.color = Color.white;
            leftHalf.x += 5;
            righthalf.x += 5;
            Widgets.Label(leftHalf, "GW_Projects".Translate());
            Widgets.Label(righthalf, "GW_DiscoveredResearchDetails".Translate());
            leftHalf.x -= 5;
            righthalf.x -= 5;
            var stcManager = Find.World.GetComponent<WorldComponent_StudyManager>();
            Text.Font = GameFont.Small;

            
            var visibleProjects = stcManager.ResearchProjects.Where(x => (!x.prerequisites?.Where(p => !p.IsFinished).Any() ?? false) && !stcManager.CompletedAllRequirements(x));
            const int YPerRequirement = 33;
            const int YMarginPerRequirement = 5;
            const int ItemSpacing = 10;
            var requirementCount = visibleProjects.Select(x => x.GetModExtension<DefModExtension_ExtraPrerequisiteActions>()).SelectMany(y => y.ItemStudyRequirements).Count();
            int cumulativeHeight = (requirementCount * YPerRequirement) + (visibleProjects.Count() * (YMarginPerRequirement + ItemSpacing));
            Widgets.BeginScrollView(outrect, ref scrollPosition, new Rect(outrect.x, outrect.y, outrect.width - 16f, cumulativeHeight));
            outrect.y += 3;
            foreach (var project in visibleProjects)
            {
                var stcProj = project.GetModExtension<DefModExtension_ExtraPrerequisiteActions>();
                int itemHeight = (stcProj.ItemStudyRequirements.Count * YPerRequirement) + YMarginPerRequirement;
                var backgroundrect = new Rect(outrect.x + 3, outrect.y, outrect.width - 25, itemHeight);
                Widgets.DrawOptionBackground(backgroundrect, project == selectedProject);
                Widgets.Label(new Rect(backgroundrect.x + 10, backgroundrect.y + 10, backgroundrect.width, backgroundrect.height), project.LabelCap);

                var yOffset = 0;
                foreach (var item in stcProj.ItemStudyRequirements)
                {
                    string label = $"{item.NumberRequired} x ";
                    var labelSize = Text.CalcSize(label);
                    Widgets.Label(new Rect(backgroundrect.xMax - 40 - labelSize.x, backgroundrect.y + 10 + yOffset, labelSize.x, backgroundrect.height), label);
                    Widgets.DrawTextureFitted(new Rect(backgroundrect.xMax - 40, backgroundrect.y + yOffset, 40, 40), item.StudyObject.uiIcon, 1);
                    yOffset += YPerRequirement;
                }
                if (Widgets.ButtonInvisible(backgroundrect, false))
                {
                    selectedProject = project;
                }

                outrect.y += itemHeight + ItemSpacing;
            }
            Widgets.EndScrollView();

            DoBottomButtons(new Rect(rect.x + 5, rect.yMax - 38 - 5, rect.width, 38));

            var viewRect = new Rect(rightOutRect.x, rightOutRect.y, rightOutRect.width - 16f, rightScrollViewHeight);
            Widgets.BeginScrollView(rightOutRect, ref scrollPositionRight, viewRect);
            if (selectedProject != null)
                rightScrollViewHeight = DrawUnlockableHyperlinks(rightOutRect, selectedProject);
            Widgets.EndScrollView();
        }

        protected virtual bool CanAccept()
        {
            return selectedProject != null;
        }

        protected virtual void DoBottomButtons(Rect rect)
        {
            if (Widgets.ButtonText(new Rect(rect.x + ButSize.x + 10, rect.y, ButSize.x, ButSize.y), "GW_StartStudy".Translate(), active: CanAccept()))
            {
                Accept();
            }
            if (Widgets.ButtonText(new Rect(rect.x, rect.y, ButSize.x, ButSize.y), "Close".Translate()))
            {
                Close(true);
            }
        }

        private void Accept()
        {
            StartAssembly();
        }

        private float DrawUnlockableHyperlinks(Rect rect, ResearchProjectDef project)
        {
            List<Pair<ResearchPrerequisitesUtility.UnlockedHeader, List<Def>>> list = UnlockedDefsGroupedByPrerequisites(project);
            if (list.NullOrEmpty())
            {
                return 0f;
            }
            float yMin = rect.yMin;
            float x = rect.x;
            foreach (Pair<ResearchPrerequisitesUtility.UnlockedHeader, List<Def>> item in list)
            {
                ResearchPrerequisitesUtility.UnlockedHeader first = item.First;
                rect.x = x;
                if (!first.unlockedBy.Any())
                {
                    Widgets.LabelCacheHeight(ref rect, "Unlocks".Translate() + ":");
                }
                else
                {
                    Widgets.LabelCacheHeight(ref rect, string.Concat("UnlockedWith".Translate(), " ", HeaderLabel(first), ":"));
                }
                rect.x += 6f;
                rect.yMin += rect.height;
                foreach (Def item2 in item.Second)
                {
                    Rect rect2 = new Rect(rect.x, rect.yMin, rect.width, 24f);
                    Color? color = null;
                    if (quickSearchWidget.filter.Active)
                    {
                        if (MatchesUnlockedDef(item2))
                        {
                            Widgets.DrawTextHighlight(rect2);
                        }
                        else
                        {
                            color = NoMatchTint(Widgets.NormalOptionColor);
                        }
                    }
                    Dialog_InfoCard.Hyperlink hyperlink = new Dialog_InfoCard.Hyperlink(item2);
                    Widgets.HyperlinkWithIcon(rect2, hyperlink, null, 2f, 6f, color, truncateLabel: false, LabelSuffixForUnlocked(item2));
                    rect.yMin += 24f;
                }
            }
            return rect.yMin - yMin;
        }

        private string HeaderLabel(ResearchPrerequisitesUtility.UnlockedHeader headerProject)
        {
            StringBuilder stringBuilder = new StringBuilder();
            string value = "";
            for (int i = 0; i < headerProject.unlockedBy.Count; i++)
            {
                ResearchProjectDef researchProjectDef = headerProject.unlockedBy[i];
                string text = researchProjectDef.LabelCap;
                if (!researchProjectDef.IsFinished)
                {
                    text = text.Colorize(MissingPrerequisiteColor);
                }
                stringBuilder.Append(text).Append(value);
                value = ", ";
            }
            return stringBuilder.ToString();
        }

        private string LabelSuffixForUnlocked(Def unlocked)
        {
            if (!ModLister.IdeologyInstalled)
            {
                return null;
            }
            tmpSuffixesForUnlocked.Clear();
            foreach (MemeDef allDef in DefDatabase<MemeDef>.AllDefs)
            {
                if (allDef.AllDesignatorBuildables.Contains(unlocked))
                {
                    tmpSuffixesForUnlocked.AddDistinct(allDef.LabelCap);
                }
                if (allDef.thingStyleCategories.NullOrEmpty())
                {
                    continue;
                }
                foreach (ThingStyleCategoryWithPriority thingStyleCategory in allDef.thingStyleCategories)
                {
                    if (thingStyleCategory.category.AllDesignatorBuildables.Contains(unlocked))
                    {
                        tmpSuffixesForUnlocked.AddDistinct(allDef.LabelCap);
                    }
                }
            }
            foreach (CultureDef allDef2 in DefDatabase<CultureDef>.AllDefs)
            {
                if (allDef2.thingStyleCategories.NullOrEmpty())
                {
                    continue;
                }
                foreach (ThingStyleCategoryWithPriority thingStyleCategory2 in allDef2.thingStyleCategories)
                {
                    if (thingStyleCategory2.category.AllDesignatorBuildables.Contains(unlocked))
                    {
                        tmpSuffixesForUnlocked.AddDistinct(allDef2.LabelCap);
                    }
                }
            }
            if (!tmpSuffixesForUnlocked.Any())
            {
                return null;
            }
            return " (" + tmpSuffixesForUnlocked.ToCommaList() + ")";
        }

        private bool MatchesUnlockedDef(Def unlocked)
        {
            return quickSearchWidget.filter.Matches(unlocked.label);
        }

        private Color NoMatchTint(Color color)
        {
            return Color.Lerp(color, NoMatchTintColor, 0.4f);
        }

        private void StartAssembly()
        {
            cogitator.Start(selectedProject);
            Close(true);
        }

        private List<Pair<ResearchPrerequisitesUtility.UnlockedHeader, List<Def>>> UnlockedDefsGroupedByPrerequisites(ResearchProjectDef project)
        {
            if (cachedUnlockedDefsGroupedByPrerequisites == null)
            {
                cachedUnlockedDefsGroupedByPrerequisites = new Dictionary<ResearchProjectDef, List<Pair<ResearchPrerequisitesUtility.UnlockedHeader, List<Def>>>>();
            }
            if (!cachedUnlockedDefsGroupedByPrerequisites.TryGetValue(project, out var value))
            {
                value = ResearchPrerequisitesUtility.UnlockedDefsGroupedByPrerequisites(project);
                cachedUnlockedDefsGroupedByPrerequisites.Add(project, value);
            }
            return value;
        }
    }
}