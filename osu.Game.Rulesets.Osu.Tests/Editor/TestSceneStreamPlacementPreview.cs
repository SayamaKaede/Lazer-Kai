// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics.Containers;
using osu.Framework.Testing;
using osu.Game.Rulesets.Osu.Edit;
using osu.Game.Rulesets.Osu.Objects.Drawables;
using osu.Game.Screens.Edit;
using osu.Game.Screens.Edit.Compose.Components;
using osuTK;

namespace osu.Game.Rulesets.Osu.Tests.Editor
{
    /// <summary>
    /// The circles shown while a stream's shape is being drawn.
    /// </summary>
    /// <remarks>
    /// Driven directly rather than by drawing a shape, which has not been managed from a test. What is checked here
    /// is that it puts real drawn circles up, follows a plan as it changes, and leaves nothing in the beatmap --
    /// adding to the beatmap is what took the editor down when this was first written.
    /// </remarks>
    public partial class TestSceneStreamPlacementPreview : TestSceneOsuEditor
    {
        private StreamPlacementPreview preview;

        [SetUpSteps]
        public void AddThePreview()
        {
            AddStep("clear the beatmap", () => EditorBeatmap.Clear());

            // put where the composer's own things live, so it is given the same dependencies a placement would get.
            AddStep("add a preview to the composer", () =>
            {
                preview = new StreamPlacementPreview();
                ((Container)Editor.ChildrenOfType<ComposeBlueprintContainer>().Single().Parent!).Add(preview);
            });
        }

        [Test]
        public void TestItDrawsRealCircles()
        {
            AddStep("show five", () => preview.Set(plan(5)));

            AddUntilStep("five circles are drawn", () => preview.ChildrenOfType<DrawableHitCircle>().Count(), () => Is.EqualTo(5));
            AddAssert("nothing went into the beatmap", () => EditorBeatmap.HitObjects, () => Is.Empty);
        }

        [Test]
        public void TestItFollowsThePlan()
        {
            AddStep("show five", () => preview.Set(plan(5)));
            AddUntilStep("five of them", () => preview.Count, () => Is.EqualTo(5));

            AddStep("ask for nine", () => preview.Set(plan(9)));
            AddUntilStep("nine of them", () => preview.ChildrenOfType<DrawableHitCircle>().Count(), () => Is.EqualTo(9));

            AddStep("back to two", () => preview.Set(plan(2)));
            AddUntilStep("two of them", () => preview.ChildrenOfType<DrawableHitCircle>().Count(), () => Is.EqualTo(2));

            AddStep("take it away", () => preview.Clear());
            AddUntilStep("none of them", () => preview.ChildrenOfType<DrawableHitCircle>(), () => Is.Empty);
        }

        /// <summary>
        /// The whole run is drawn at once, however far past the current moment it reaches.
        /// </summary>
        [Test]
        public void TestTheFarEndIsDrawnToo()
        {
            AddStep("show a long one", () => preview.Set(plan(24)));

            AddUntilStep("all of them are drawn", () => preview.ChildrenOfType<DrawableHitCircle>().Count(d => d.Alpha > 0), () => Is.EqualTo(24));
        }

        /// <summary>A run of notes spread along a line, the way a drawn shape gives one.</summary>
        private IReadOnlyList<StreamConversion.Note> plan(int count)
        {
            var notes = new List<StreamConversion.Note>();

            for (int i = 0; i < count; i++)
                notes.Add(new StreamConversion.Note(EditorClock.CurrentTime + i * 100, new Vector2(100 + i * 10, 200)));

            return notes;
        }
    }
}
