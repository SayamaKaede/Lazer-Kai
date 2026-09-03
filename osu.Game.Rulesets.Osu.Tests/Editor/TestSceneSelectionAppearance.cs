// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Testing;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Rulesets.Osu.Edit.Blueprints.HitCircles.Components;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Screens.Edit;
using osu.Game.Screens.Edit.Compose.Components;
using osu.Game.Screens.Edit.Compose.Components.Timeline;
using osu.Game.Tests.Beatmaps;
using osuTK;

namespace osu.Game.Rulesets.Osu.Tests.Editor
{
    /// <summary>
    /// The outline a selected object is drawn with, and whether changing how it looks reaches it.
    /// </summary>
    /// <remarks>
    /// Everything drawing one reads the settings through an <see cref="EditorSelectionAppearance"/> and is told
    /// when they change. A drawable that only keeps that in a local is holding nothing: the object and the handler
    /// reference each other and nothing else references either, so the pair is collected, and osu!framework binds
    /// weakly -- the setting goes on changing and nobody is listening. It came back on the next visit to the
    /// editor, which is what made it look like the setting needed one.
    /// </remarks>
    public partial class TestSceneSelectionAppearance : TestSceneOsuEditor
    {
        [Resolved]
        private OsuConfigManager config { get; set; } = null!;

        protected override IBeatmap CreateBeatmap(RulesetInfo ruleset) => new TestBeatmap(ruleset);

        [SetUpSteps]
        public void SelectSomething()
        {
            AddStep("start from two circles", () =>
            {
                EditorBeatmap.Clear();
                EditorBeatmap.Add(new HitCircle { StartTime = 1000, Position = new Vector2(200, 192) });
                EditorBeatmap.Add(new HitCircle { StartTime = 1200, Position = new Vector2(312, 192) });
            });

            AddStep("go to them", () => EditorClock.Seek(1000));

            // two of them, so the selection can be resized and the eight points are drawn.
            AddStep("select them", () => EditorBeatmap.SelectedHitObjects.AddRange(EditorBeatmap.HitObjects));

            AddUntilStep("its outline is drawn", () => Editor.ChildrenOfType<HitCirclePiece>().Any(), () => Is.True);
        }

        [TearDownSteps]
        public void PutTheColourBack()
        {
            AddStep("back to the stock colour", () => config.GetBindable<EditorSelectionColour>(OsuSetting.EditorSelectionColour).SetDefault());
            AddStep("back to the stock outline colour", () => config.GetBindable<EditorSelectionColour>(OsuSetting.EditorObjectOutlineColour).SetDefault());
            AddStep("back to the stock outline opacity", () => config.GetBindable<float>(OsuSetting.EditorObjectOutlineOpacity).SetDefault());
            AddStep("back to the stock handle size", () => config.GetBindable<float>(OsuSetting.EditorSelectionHandleScale).SetDefault());
        }

        /// <summary>
        /// The colour has to reach the outline while the editor is open, including once nothing but the object
        /// itself is holding the pieces that read it.
        /// </summary>
        [Test]
        public void TestTheOutlineFollowsItsColourAfterACollection()
        {
            float red = 0;

            AddStep("remember what it is drawn in", () => red = outlineRed());
            AddStep("collect what nothing is holding", collect);

            AddStep("choose blue", () => config.SetValue(OsuSetting.EditorObjectOutlineColour, EditorSelectionColour.Blue));
            AddUntilStep("the outline turned", () => outlineRed(), () => Is.LessThan(red));
        }

        /// <summary>
        /// The outline drawn on the object itself is set apart from the box dragged out around a selection, so it
        /// follows its own colour.
        /// </summary>
        [Test]
        public void TestTheObjectOutlineFollowsItsOwnColour()
        {
            float red = 0;

            AddStep("remember what it is drawn in", () => red = outlineRed());

            AddStep("choose blue for the outline", () => config.SetValue(OsuSetting.EditorObjectOutlineColour, EditorSelectionColour.Blue));
            AddUntilStep("the outline turned", () => outlineRed(), () => Is.LessThan(red));
        }

        /// <summary>
        /// The other half of the split: the box's colour reaches the box and stops there.
        /// </summary>
        [Test]
        public void TestTheBoxColourLeavesTheObjectOutlineAlone()
        {
            float red = 0;

            AddStep("remember what it is drawn in", () => red = outlineRed());

            AddStep("choose blue for the box", () => config.SetValue(OsuSetting.EditorSelectionColour, EditorSelectionColour.Blue));
            AddUntilStep("the box turned", () => boxRed(), () => Is.LessThan(1f));
            AddAssert("the outline stayed", () => outlineRed(), () => Is.EqualTo(red));
        }

        /// <summary>
        /// The timeline keeps an outline on every object and hides the ones that are not selected, so writing the
        /// opacity setting straight to it drew an outline on all of them the moment the setting was touched.
        /// </summary>
        [Test]
        public void TestTheTimelineOutlineStaysOffWhatIsNotSelected()
        {
            AddStep("select nothing", () => EditorBeatmap.SelectedHitObjects.Clear());

            AddUntilStep("the timeline drew them", () => timelineOutlines().Any(), () => Is.True);
            AddAssert("none of them is outlined", () => brightestTimelineOutline(), () => Is.Zero);

            AddStep("choose blue for the outline", () => config.SetValue(OsuSetting.EditorObjectOutlineColour, EditorSelectionColour.Blue));
            AddAssert("still none of them", () => brightestTimelineOutline(), () => Is.Zero);
        }

        /// <summary>
        /// The timeline showed its outline at full opacity whatever the setting said, since showing it wrote the
        /// alpha itself.
        /// </summary>
        [Test]
        public void TestTheTimelineOutlineFollowsItsOpacity()
        {
            AddUntilStep("a selected one is outlined", () => brightestTimelineOutline(), () => Is.EqualTo(1f));

            AddStep("make it faint", () => config.SetValue(OsuSetting.EditorObjectOutlineOpacity, 0.2f));

            // select again: showing the outline is what used to write full opacity over the setting, so a run that
            // only changes the setting says nothing about whether the setting is read at all.
            AddStep("select nothing", () => EditorBeatmap.SelectedHitObjects.Clear());
            AddStep("select them again", () => EditorBeatmap.SelectedHitObjects.AddRange(EditorBeatmap.HitObjects));

            AddUntilStep("the outline came back faint", () => brightestTimelineOutline(), () => Is.LessThan(1f).And.GreaterThan(0f));
        }

        /// <summary>
        /// The eight points a selection is resized by take their size once and never look again, so the setting
        /// only ever showed on the next visit to the editor.
        /// </summary>
        [Test]
        public void TestTheHandlesFollowTheirSize()
        {
            float was = 0;

            AddUntilStep("the handles are drawn", () => Editor.ChildrenOfType<SelectionBoxScaleHandle>().Any(), () => Is.True);
            AddStep("remember how big they are", () => was = handleSize());

            AddStep("make them smaller", () => config.SetValue(OsuSetting.EditorSelectionHandleScale, 0.5f));
            AddUntilStep("they shrank", () => handleSize(), () => Is.LessThan(was));
        }

        private static void collect()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        private float outlineRed() => Editor.ChildrenOfType<HitCirclePiece>().First().Colour.TopLeft.SRGB.R;

        /// <summary>
        /// The box dragged out to make a selection, which is drawn in white until the user picks a colour.
        /// </summary>
        private float boxRed() => Editor.ChildrenOfType<DragBox>().First().Colour.TopLeft.SRGB.R;

        private IEnumerable<TimelineHitObjectBlueprint.Border> timelineOutlines() => Editor.ChildrenOfType<TimelineHitObjectBlueprint.Border>();

        private float brightestTimelineOutline() => timelineOutlines().Max(outline => outline.Alpha);

        private float handleSize() => Editor.ChildrenOfType<SelectionBoxScaleHandle>().First().Size.X;
    }
}
