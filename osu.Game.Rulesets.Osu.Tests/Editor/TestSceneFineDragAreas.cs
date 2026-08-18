// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Testing;
using osu.Game.Beatmaps;
using osu.Game.Input;
using osu.Game.Screens.Edit.Components.Timelines.Summary;
using osu.Game.Screens.Edit.Compose.Components;
using osu.Game.Screens.Edit.Compose.Components.Timeline;
using osu.Game.Tests.Beatmaps;

namespace osu.Game.Rulesets.Osu.Tests.Editor
{
    /// <summary>
    /// Which parts of the editor take the reduced drag threshold.
    /// </summary>
    /// <remarks>
    /// The threshold is applied by the game's input manager, which a test scene replaces, so what is checked here is
    /// that the three areas ask for it and that nothing else in the editor does.
    /// </remarks>
    public partial class TestSceneFineDragAreas : TestSceneOsuEditor
    {
        protected override IBeatmap CreateBeatmap(RulesetInfo ruleset) => new TestBeatmap(ruleset);

        /// <summary>
        /// Asking for it is not enough: a drawable which handles no positional input of its own never appears in the
        /// queue the threshold is decided from, so the request would never be seen.
        /// </summary>
        [Test]
        public void TestEachAreaIsReachedByThePointer()
        {
            assertReachable<ComposeBlueprintContainer>();
            assertReachable<Timeline>();
            assertReachable<SummaryTimeline>();
        }

        private void assertReachable<T>() where T : Drawable, IFineDragArea
        {
            AddStep($"hover the {typeof(T).Name}", () => InputManager.MoveMouseTo(Editor.ChildrenOfType<T>().First()));
            AddAssert($"{typeof(T).Name} is under the pointer", () => InputManager.PositionalInputQueue.OfType<IFineDragArea>().OfType<T>().Any(), () => Is.True);
        }

        [Test]
        public void TestOnlyTheComposerAndTimelinesAskForIt()
        {
            AddUntilStep("composer asks for it", () => Editor.ChildrenOfType<ComposeBlueprintContainer>().Any(c => c is IFineDragArea), () => Is.True);
            AddAssert("top timeline asks for it", () => Editor.ChildrenOfType<Timeline>().Any(t => t is IFineDragArea), () => Is.True);
            AddAssert("bottom timeline asks for it", () => Editor.ChildrenOfType<SummaryTimeline>().Any(t => t is IFineDragArea), () => Is.True);

            AddAssert("nothing else does", () => Editor.ChildrenOfType<IFineDragArea>()
                                                       .Select(a => a.GetType())
                                                       .All(t => t.IsAssignableTo(typeof(ComposeBlueprintContainer))
                                                                 || t.IsAssignableTo(typeof(Timeline))
                                                                 || t.IsAssignableTo(typeof(SummaryTimeline))), () => Is.True);
        }
    }
}
