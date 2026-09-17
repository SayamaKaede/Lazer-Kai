// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Testing;
using osu.Framework.Utils;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Graphics;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Screens.Edit.Compose.Components.Timeline;
using osu.Game.Skinning;
using osu.Game.Tests.Beatmaps;
using osuTK;
using osuTK.Input;

namespace osu.Game.Rulesets.Osu.Tests.Editor
{
    /// <summary>
    /// The timeline drawing its objects with the skin's own pieces.
    /// </summary>
    public partial class TestSceneTimelineSkin : TestSceneOsuEditor
    {
        [Resolved]
        private OsuConfigManager config { get; set; } = null!;

        [Resolved]
        private SkinManager skins { get; set; } = null!;

        protected override IBeatmap CreateBeatmap(RulesetInfo ruleset) => new TestBeatmap(ruleset);

        [SetUpSteps]
        public override void SetUpSteps()
        {
            base.SetUpSteps();

            // the game's own skins draw their circles rather than shipping them, so there is nothing to borrow from
            // one. The classic skin is what a mapper turning this on is asking to see.
            AddStep("wear the classic skin", () => skins.CurrentSkinInfo.Value = skins.DefaultClassicSkin.SkinInfo);
            AddStep("start from nothing", () =>
            {
                EditorBeatmap.Clear();

                // set here as well as put back afterwards: a test that fails part way through never reaches its own
                // clean-up, and the next one would then be reading a setting it did not ask for.
                config.SetValue(OsuSetting.EditorTimelineUseSkin, false);
                config.SetValue(OsuSetting.EditorTimelineStyle, EditorTimelineStyle.Lazer);
            });
        }

        [TearDownSteps]
        public void PutEverythingBack()
        {
            // a test that fails while the button is down leaves it down for whatever runs next.
            AddStep("let go of the mouse", () => InputManager.ReleaseButton(MouseButton.Left));
            AddStep("turn the setting off", () => config.SetValue(OsuSetting.EditorTimelineUseSkin, false));
            AddStep("put the layout back", () => config.SetValue(OsuSetting.EditorTimelineStyle, EditorTimelineStyle.Lazer));
        }

        /// <summary>
        /// Off by default, and off is the timeline exactly as it was.
        /// </summary>
        [Test]
        public void TestNothingIsBorrowedUntilTheSettingIsOn()
        {
            AddStep("add a circle", () => EditorBeatmap.Add(new HitCircle { StartTime = 1000 }));
            AddUntilStep("it is on the timeline", blueprints, () => Is.EqualTo(1));

            AddAssert("drawn the way it always was", skinCircles, () => Is.Zero);

            AddStep("turn the setting on", () => config.SetValue(OsuSetting.EditorTimelineUseSkin, true));
            AddUntilStep("the skin's circle is drawn", skinCircles, () => Is.EqualTo(1));
        }

        /// <summary>
        /// A slider is where lazer's timeline says least: the ends are not drawn at all, and a reverse is a dot.
        /// </summary>
        [Test]
        public void TestASliderGetsBothEndsAndAnArrowForEachReverse()
        {
            AddStep("turn the setting on", () => config.SetValue(OsuSetting.EditorTimelineUseSkin, true));

            AddStep("add a slider that reverses twice", () => EditorBeatmap.Add(new Slider
            {
                StartTime = 1000,
                RepeatCount = 2,
                Path = new SliderPath(new[] { new PathControlPoint(Vector2.Zero), new PathControlPoint(new Vector2(100, 0)) }),
            }));

            // both ends plus a circle for each reverse: a reverse is a moment the player hits, and stable draws it
            // as a circle with the arrow where the number would be.
            AddUntilStep("a circle for each end and each reverse", skinCircles, () => Is.EqualTo(4));
            AddUntilStep("one arrow per reverse", skinArrows, () => Is.EqualTo(2));
        }

        /// <summary>
        /// The handle that drags a slider's end about is drawn over the skin's own end, hiding the thing the setting
        /// was turned on to see. Stable draws no handle and takes the whole end instead.
        /// </summary>
        [Test]
        public void TestTheDragHandleGetsOutOfTheWayAndLeavesItsWholeEndTakingTheDrag()
        {
            AddStep("add a spinner", () => EditorBeatmap.Add(new Spinner { StartTime = 1000, Duration = 2000 }));
            AddUntilStep("it is on the timeline", blueprints, () => Is.EqualTo(1));

            AddAssert("the handle is drawn", handleAlpha, () => Is.EqualTo(1));
            float shrunken = 0;
            AddStep("remember how much of the end it takes", () => shrunken = handle().Scale.X);

            AddStep("turn the setting on", () => config.SetValue(OsuSetting.EditorTimelineUseSkin, true));

            AddUntilStep("nothing of the handle is drawn", handleAlpha, () => Is.Zero);
            AddUntilStep("the whole end takes the drag", () => handle().Scale.X, () => Is.GreaterThan(shrunken));
        }

        /// <summary>
        /// Stable's objects let the waveform through, which is how a moment is read against the sound it belongs to.
        /// </summary>
        [Test]
        public void TestTheComboColourThinsOutOnlyWhileTheSkinIsUsed()
        {
            AddStep("add a circle", () => EditorBeatmap.Add(new HitCircle { StartTime = 1000 }));
            AddUntilStep("it is on the timeline", blueprints, () => Is.EqualTo(1));

            AddAssert("drawn solid", barAlpha, () => Is.EqualTo(1));

            AddStep("turn the setting on", () => config.SetValue(OsuSetting.EditorTimelineUseSkin, true));
            AddUntilStep("the waveform comes through it", barAlpha, () => Is.LessThan(1));

            AddStep("turn it off again", () => config.SetValue(OsuSetting.EditorTimelineUseSkin, false));
            AddUntilStep("solid again", barAlpha, () => Is.EqualTo(1));
        }

        /// <summary>
        /// With no handle drawn on the end, a drag has to say some other way that it has hold of it rather than of
        /// the timeline behind. Taking hold is also working on that object, so it becomes the selected one.
        /// </summary>
        [Test]
        public void TestTakingHoldOfAnEndSelectsItAndSaysSo()
        {
            AddStep("turn the setting on", () => config.SetValue(OsuSetting.EditorTimelineUseSkin, true));
            AddStep("add a spinner", () => EditorBeatmap.Add(new Spinner { StartTime = 1000, Duration = 2000 }));

            AddUntilStep("it is on the timeline", blueprints, () => Is.EqualTo(1));
            AddAssert("nothing is selected", () => EditorBeatmap.SelectedHitObjects.Count, () => Is.Zero);

            AddStep("take hold of its end", () =>
            {
                InputManager.MoveMouseTo(handle());
                InputManager.PressButton(MouseButton.Left);
            });

            AddAssert("it is selected", () => EditorBeatmap.SelectedHitObjects.Count, () => Is.EqualTo(1));
            AddUntilStep("its end says it is held", gripAlpha, () => Is.EqualTo(1));

            AddStep("let go", () => InputManager.ReleaseButton(MouseButton.Left));
            AddUntilStep("it stops saying so", gripAlpha, () => Is.Zero);
        }

        /// <summary>
        /// How much comes through follows the layout: the Lazer-Kai one reaches its ticks in from both edges, so
        /// there is more behind the objects worth seeing.
        /// </summary>
        [Test]
        public void TestHowMuchComesThroughFollowsTheLayout()
        {
            AddStep("turn the setting on", () => config.SetValue(OsuSetting.EditorTimelineUseSkin, true));
            AddStep("add a circle", () => EditorBeatmap.Add(new HitCircle { StartTime = 1000 }));
            AddUntilStep("it is on the timeline", blueprints, () => Is.EqualTo(1));

            float stable = 0;

            AddStep("lay it out stable's way", () => config.SetValue(OsuSetting.EditorTimelineStyle, EditorTimelineStyle.Stable));
            AddUntilStep("the waveform comes through it", barAlpha, () => Is.LessThan(1));
            AddStep("remember how much", () => stable = barAlpha());

            AddStep("lay it out Lazer-Kai's way", () => config.SetValue(OsuSetting.EditorTimelineStyle, EditorTimelineStyle.LazerKai));
            AddUntilStep("more comes through", barAlpha, () => Is.LessThan(stable));
        }

        private float gripAlpha() => Editor.ChildrenOfType<TimelineSkinCircle>()
                                           .SelectMany(c => c.ChildrenOfType<CircularContainer>())
                                           .Max(g => g.Alpha);

        private TimelineHitObjectBlueprint.DragArea handle()
            => Editor.ChildrenOfType<TimelineHitObjectBlueprint.DragArea>().Single();

        private float handleAlpha() => handle().ChildrenOfType<Box>().Single().Alpha;

        private float barAlpha() => bar().Colour.TopLeft.Linear.A;

        /// <summary>
        /// Stable leaves a spinner out of the combo colours and lazer does not, so which of the two happens follows
        /// the style the timeline is laid out in. This fork's layout leaves it out as well, which
        /// <see cref="TestSceneTimelineStyle"/> covers; what this adds is the colour coming back.
        /// </summary>
        [Test]
        public void TestASpinnerKeepsItsComboColourOnlyInLazersStyle()
        {
            AddStep("lay the timeline out lazer's way", () => config.SetValue(OsuSetting.EditorTimelineStyle, EditorTimelineStyle.Lazer));
            AddStep("add a spinner", () => EditorBeatmap.Add(new Spinner { StartTime = 1000, Duration = 2000 }));

            AddUntilStep("it is on the timeline", blueprints, () => Is.EqualTo(1));
            AddAssert("drawn in its combo colour", spinnerIsGrey, () => Is.False);

            AddStep("lay it out stable's way", () => config.SetValue(OsuSetting.EditorTimelineStyle, EditorTimelineStyle.Stable));
            AddUntilStep("drawn in grey", spinnerIsGrey, () => Is.True);

            AddStep("back to lazer's", () => config.SetValue(OsuSetting.EditorTimelineStyle, EditorTimelineStyle.Lazer));
            AddUntilStep("its colour came back", spinnerIsGrey, () => Is.False);
        }

        /// <summary>
        /// Read off the bar rather than off the setting: what is being checked is that the object is drawn grey, not
        /// that a branch was taken.
        /// </summary>
        /// <remarks>
        /// Grey the way the game's own greys are, rather than equal channels: the fork's theme puts a little colour
        /// in every one of them, so under it the three channels of a grey are never equal.
        /// </remarks>
        private bool spinnerIsGrey()
        {
            var colour = bar().Colour.TopLeft.SRGB;
            var grey = OsuColour.Gray(0.6f);

            return Precision.AlmostEquals(colour.R, grey.R, 0.01f)
                   && Precision.AlmostEquals(colour.G, grey.G, 0.01f)
                   && Precision.AlmostEquals(colour.B, grey.B, 0.01f);
        }

        /// <summary>
        /// The rounded bar the object is drawn as, which is not the outline drawn over it when it is selected.
        /// </summary>
        private TimelineHitObjectBlueprint.ExtendableCircle bar()
            => Editor.ChildrenOfType<TimelineHitObjectBlueprint>().Single()
                     .ChildrenOfType<TimelineHitObjectBlueprint.ExtendableCircle>()
                     .First(c => c is not TimelineHitObjectBlueprint.Border);

        private int blueprints() => Editor.ChildrenOfType<TimelineHitObjectBlueprint>().Count();

        private int skinCircles() => Editor.ChildrenOfType<TimelineSkinCircle>().Count();

        private int skinArrows() => Editor.ChildrenOfType<TimelineSkinReverseArrow>().Count();
    }
}
