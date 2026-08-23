// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Testing;
using osu.Framework.Utils;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Configuration;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Osu.Beatmaps;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.UI;
using osu.Game.Screens.Edit;
using osu.Game.Screens.Edit.Compose.Components.Timeline;
using osuTK.Graphics;

namespace osu.Game.Tests.Visual.Editing
{
    /// <summary>
    /// Drawing another difficulty from the same set faintly underneath the one being edited. Its own fixture, because
    /// the opacity lives in the config and would otherwise carry into the next test.
    /// </summary>
    [TestFixture]
    public partial class TestSceneEditorReferenceDifficulty : EditorTestScene
    {
        protected override Ruleset CreateEditorRuleset() => new OsuRuleset();

        [Resolved]
        private OsuConfigManager config { get; set; } = null!;

        private EditorReferenceDifficulty reference => Editor.ChildrenOfType<EditorReferenceDifficulty>().Single();

        /// <summary>
        /// Every drawable ruleset in the editor: the difficulty being edited, and the one drawn underneath it.
        /// </summary>
        private DrawableRuleset[] rulesets => Editor.ChildrenOfType<DrawableRuleset>().ToArray();

        private BeatmapInfo otherDifficulty => EditorBeatmap.BeatmapInfo.BeatmapSet!.Beatmaps.First(b => !b.Equals(EditorBeatmap.BeatmapInfo));

        [SetUpSteps]
        public void SetUpReferenceSteps()
        {
            AddStep("go to compose", () => Editor.Mode.Value = EditorScreenMode.Compose);
            AddUntilStep("composer is up", () => rulesets.Length > 0);
        }

        [TearDownSteps]
        public void TearDownReferenceSteps()
        {
            AddStep("restore the opacity", () => config.GetBindable<float>(OsuSetting.EditorReferenceDifficultyOpacity).SetDefault());
            AddStep("restore the colour", () => config.GetBindable<EditorSelectionColour>(OsuSetting.EditorReferenceDifficultyColour).SetDefault());
        }

        [Test]
        public void TestNothingIsDrawnUntilOneIsChosen()
        {
            AddAssert("only the difficulty being edited", () => rulesets.Length, () => Is.EqualTo(1));
        }

        /// <remarks>
        /// The beatmap is handed over rather than loaded here: the test harness has no files behind its beatmap set,
        /// and what is worth testing is that a difficulty handed to the composer is drawn and taken away again.
        /// </remarks>
        [Test]
        public void TestChosenDifficultyIsDrawnAndCanBeCleared()
        {
            AddAssert("the set has another difficulty", () => EditorBeatmap.BeatmapInfo.BeatmapSet!.Beatmaps.Count, () => Is.GreaterThan(1));

            AddStep("hand over a difficulty", () => reference.Beatmap.Value = referenceBeatmap());
            AddUntilStep("a second ruleset is drawn", () => rulesets.Length, () => Is.EqualTo(2));

            AddStep("take it away", () => reference.Beatmap.Value = null);
            AddUntilStep("it went away", () => rulesets.Length, () => Is.EqualTo(1));
        }

        /// <remarks>
        /// The whole point is that it stays out of the way, so it has to be faint and it has to be untouchable.
        /// </remarks>
        [Test]
        public void TestItIsFaintAndTakesNoInput()
        {
            AddStep("hand over a difficulty", () => reference.Beatmap.Value = referenceBeatmap());
            AddUntilStep("a second ruleset is drawn", () => rulesets.Length, () => Is.EqualTo(2));

            AddStep("make it half solid", () => config.SetValue(OsuSetting.EditorReferenceDifficultyOpacity, 0.5f));
            AddUntilStep("it followed the setting", () => referenceLayer().Alpha, () => Is.EqualTo(0.5f).Within(0.01f));

            AddAssert("nothing in it takes input", () => !referenceLayer().PropagatePositionalInputSubTree);
        }

        /// <remarks>
        /// The timeline is where two difficulties are actually read against each other, so the objects being edited
        /// give up half of it rather than the referenced ones being drawn on top of them.
        /// </remarks>
        [Test]
        public void TestTheTimelineSplitsIntoTwoRows()
        {
            AddStep("keep the velocity and volume rows on", () => config.SetValue(OsuSetting.EditorHideTimelinePoints, false));
            AddUntilStep("the timeline is up", () => Editor.ChildrenOfType<TimelineBlueprintContainer>().Any());

            AddAssert("the rows are showing", () => Editor.ChildrenOfType<SamplePointPiece>().Any(p => p.IsPresent));

            AddStep("hand over a difficulty", () => reference.Beatmap.Value = referenceBeatmap());

            AddUntilStep("the other difficulty is drawn", () => Editor.ChildrenOfType<TimelineReferenceObjectDisplay>().Single().Children.Count, () => Is.GreaterThan(0));

            // two rows of objects and two rows of points do not fit, so the points give way whether or not the user
            // asked them to.
            AddUntilStep("the rows gave way", () => Editor.ChildrenOfType<SamplePointPiece>().All(p => !p.IsPresent));
            AddUntilStep("the gaps came out even", () => gapsAreEven(), () => Is.True);

            AddStep("take it away", () => reference.Beatmap.Value = null);
            AddUntilStep("nothing is drawn for it", () => Editor.ChildrenOfType<TimelineReferenceObjectDisplay>().Single().Children.Count, () => Is.Zero);
            AddUntilStep("the rows came back", () => Editor.ChildrenOfType<SamplePointPiece>().Any(p => p.IsPresent));
        }

        /// <summary>
        /// Whether the space above the upper row, between the two, and below the lower one are the same.
        /// </summary>
        private bool gapsAreEven()
        {
            var timeline = Editor.ChildrenOfType<Timeline>().Single();
            var edited = Editor.ChildrenOfType<TimelineHitObjectBlueprint>().FirstOrDefault();
            var referenced = Editor.ChildrenOfType<TimelineReferenceObjectDisplay>().Single().Children.FirstOrDefault();

            if (edited == null || referenced == null)
                return false;

            float top = timeline.ScreenSpaceDrawQuad.TopLeft.Y;
            float bottom = timeline.ScreenSpaceDrawQuad.BottomLeft.Y;

            float above = edited.ScreenSpaceDrawQuad.TopLeft.Y - top;
            float between = referenced.ScreenSpaceDrawQuad.TopLeft.Y - edited.ScreenSpaceDrawQuad.BottomLeft.Y;
            float below = bottom - referenced.ScreenSpaceDrawQuad.BottomLeft.Y;

            return Precision.AlmostEquals(above, between, 1.5f) && Precision.AlmostEquals(between, below, 1.5f);
        }

        /// <remarks>
        /// Faint alone is not enough — a faint copy of the same combo colours reads as part of the difficulty being
        /// edited. One flat colour is what tells them apart.
        /// </remarks>
        [Test]
        public void TestItIsTintedSoItCannotBeMistakenForTheRealThing()
        {
            AddStep("hand over a difficulty", () => reference.Beatmap.Value = referenceBeatmap());
            AddUntilStep("a second ruleset is drawn", () => rulesets.Length, () => Is.EqualTo(2));

            AddStep("tint it red", () => config.SetValue(OsuSetting.EditorReferenceDifficultyColour, EditorSelectionColour.Red));
            AddUntilStep("it took the colour", () => referenceLayer().Colour.TopLeft.SRGB, () => Is.EqualTo(EditorSelectionColour.Red.ToColour()!.Value));

            AddStep("leave it alone", () => config.SetValue(OsuSetting.EditorReferenceDifficultyColour, EditorSelectionColour.Default));
            AddUntilStep("the combo colours come back", () => referenceLayer().Colour.TopLeft.SRGB, () => Is.EqualTo(Color4.White));
        }

        private osu.Framework.Graphics.Drawable referenceLayer() =>
            Editor.ChildrenOfType<osu.Framework.Graphics.Containers.Container>()
                  .First(c => c.GetType().Name == "ReferenceRulesetContainer");

        [Test]
        public void TestTheDifficultyBeingEditedIsUntouched()
        {
            int before = 0;

            AddStep("count the objects", () => before = EditorBeatmap.HitObjects.Count);

            AddStep("hand over a difficulty", () => reference.Beatmap.Value = referenceBeatmap());
            AddUntilStep("a second ruleset is drawn", () => rulesets.Length, () => Is.EqualTo(2));

            AddAssert("nothing was added to this one", () => EditorBeatmap.HitObjects.Count, () => Is.EqualTo(before));
        }

        /// <summary>
        /// A difficulty with objects nowhere near the one being edited, so the two cannot be confused.
        /// </summary>
        private IBeatmap referenceBeatmap()
        {
            var beatmap = new OsuBeatmap
            {
                BeatmapInfo = { Ruleset = Ruleset.Value },
                HitObjects = Enumerable.Range(0, 4).Select(i => (OsuHitObject)new HitCircle { StartTime = 1250 + i * 500 }).ToList(),
            };

            beatmap.ControlPointInfo.Add(0, new TimingControlPoint { BeatLength = 500 });

            // the real thing arrives already converted; here the objects have to be told about the beatmap by hand
            // before anything can draw them.
            foreach (var hitObject in beatmap.HitObjects)
                hitObject.ApplyDefaults(beatmap.ControlPointInfo, beatmap.Difficulty);

            return beatmap;
        }

        /// <remarks>
        /// The set needs a second difficulty for there to be anything to reference.
        /// </remarks>
        protected override IBeatmap CreateBeatmap(RulesetInfo ruleset)
        {
            var beatmap = new Beatmap
            {
                BeatmapInfo = { Ruleset = ruleset, DifficultyName = "being edited" },
                HitObjects = Enumerable.Range(0, 10).Select(i => (HitObject)new HitCircle { StartTime = 1000 + i * 500 }).ToList(),
            };

            var other = new BeatmapInfo { Ruleset = ruleset, DifficultyName = "the other one" };
            var set = new BeatmapSetInfo();

            set.Beatmaps.Add(beatmap.BeatmapInfo);
            set.Beatmaps.Add(other);

            beatmap.BeatmapInfo.BeatmapSet = set;
            other.BeatmapSet = set;

            return beatmap;
        }
    }
}
