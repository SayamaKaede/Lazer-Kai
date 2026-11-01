// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using osu.Framework.Bindables;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input.Events;
using osu.Framework.Input.States;
using osu.Framework.Testing;
using osu.Game.Beatmaps;
using osu.Game.Graphics.UserInterface;
using osu.Game.Input.Bindings;
using osu.Game.Localisation;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Screens.Edit;
using osu.Game.Screens.Edit.Components.Menus;

namespace osu.Game.Tests.Visual.Editing
{
    /// <summary>
    /// Reaching the ghost difficulty from the keyboard: showing it, putting it away, and stepping up and down the
    /// spread without opening the menu.
    /// </summary>
    /// <remarks>
    /// Only which difficulty is selected is asserted. Loading one needs files behind the beatmap set, which the test
    /// harness has none of — what that costs is covered in <see cref="TestSceneEditorReferenceDifficulty"/>, which
    /// hands the composer a beatmap directly.
    /// </remarks>
    [TestFixture]
    public partial class TestSceneEditorReferenceDifficultyBinding : EditorTestScene
    {
        protected override Ruleset CreateEditorRuleset() => new OsuRuleset();

        private BeatmapInfo easiest = null!;
        private BeatmapInfo middle = null!;
        private BeatmapInfo hardest = null!;

        private EditorReferenceDifficulty reference => Editor.ChildrenOfType<EditorReferenceDifficulty>().Single();

        private Guid shown => reference.Selected.Value;

        [SetUpSteps]
        public void SetUpBindingSteps()
        {
            AddStep("go to compose", () => Editor.Mode.Value = EditorScreenMode.Compose);
            AddStep("show nothing", () => reference.Selected.Value = Guid.Empty);
        }

        [Test]
        public void TestTogglingShowsTheEasiestThenPutsItAway()
        {
            AddAssert("nothing is ghosted", () => shown, () => Is.EqualTo(Guid.Empty));

            press(GlobalAction.EditorToggleReferenceDifficulty);
            AddAssert("the easiest is ghosted", () => shown, () => Is.EqualTo(easiest.ID));

            press(GlobalAction.EditorToggleReferenceDifficulty);
            AddAssert("nothing is ghosted", () => shown, () => Is.EqualTo(Guid.Empty));
        }

        /// <remarks>
        /// Putting the ghost away to look at something and bringing it back is the common move, and coming back to a
        /// different difficulty than the one that was there is worse than useless.
        /// </remarks>
        [Test]
        public void TestTogglingBringsBackTheSameDifficulty()
        {
            AddStep("ghost the hardest", () => reference.Selected.Value = hardest.ID);

            press(GlobalAction.EditorToggleReferenceDifficulty);
            AddAssert("nothing is ghosted", () => shown, () => Is.EqualTo(Guid.Empty));

            press(GlobalAction.EditorToggleReferenceDifficulty);
            AddAssert("the hardest is back", () => shown, () => Is.EqualTo(hardest.ID));
        }

        [Test]
        public void TestSteppingUpMovesThroughTheSpreadAndWraps()
        {
            press(GlobalAction.EditorNextReferenceDifficulty);
            AddAssert("the easiest", () => shown, () => Is.EqualTo(easiest.ID));

            press(GlobalAction.EditorNextReferenceDifficulty);
            AddAssert("the middle one", () => shown, () => Is.EqualTo(middle.ID));

            press(GlobalAction.EditorNextReferenceDifficulty);
            AddAssert("the hardest", () => shown, () => Is.EqualTo(hardest.ID));

            press(GlobalAction.EditorNextReferenceDifficulty);
            AddAssert("back to the easiest", () => shown, () => Is.EqualTo(easiest.ID));
        }

        [Test]
        public void TestSteppingDownFromNothingStartsAtTheHardest()
        {
            press(GlobalAction.EditorPreviousReferenceDifficulty);
            AddAssert("the hardest", () => shown, () => Is.EqualTo(hardest.ID));

            press(GlobalAction.EditorPreviousReferenceDifficulty);
            AddAssert("the middle one", () => shown, () => Is.EqualTo(middle.ID));
        }

        /// <remarks>
        /// Stepping never lands on nothing: putting the ghost away belongs to the toggle, so a step past the end of
        /// the spread comes round to the other end rather than through an empty position.
        /// </remarks>
        [Test]
        public void TestSteppingNeverLandsOnNothing()
        {
            for (int i = 0; i < 8; i++)
            {
                press(GlobalAction.EditorNextReferenceDifficulty);
                AddAssert($"still ghosting something ({i})", () => shown, () => Is.Not.EqualTo(Guid.Empty));
            }
        }

        /// <remarks>
        /// A binding with nothing on screen to show for it leaves the user unsure whether the key registered, which is
        /// what the menu's own reminder of the key is for.
        /// </remarks>
        [Test]
        public void TestMenuItemNamesTheBinding()
        {
            AddAssert("menu item carries the hotkey",
                () => findMenuItem()?.Hotkey.GlobalAction,
                () => Is.EqualTo(GlobalAction.EditorToggleReferenceDifficulty));
        }

        /// <remarks>
        /// The timeline makes and throws away an object's piece as it scrolls in and out of view. One that went on
        /// listening to which difficulty is ghosted was kept alive by it, and laid out again on every switch.
        /// </remarks>
        [Test]
        public void TestTimelinePiecesStopListeningWhenThrownAway()
        {
            HitObject[] objects = null!;
            int listening = 0;

            AddStep("remove every object", () =>
            {
                objects = EditorBeatmap.HitObjects.ToArray();
                EditorBeatmap.Clear();
            });
            AddStep("count what listens with none", () => listening = listenersOf(reference.Beatmap));

            AddStep("put them back", () => EditorBeatmap.AddRange(objects));
            AddUntilStep("the timeline has pieces for them", () => Editor.ChildrenOfType<Screens.Edit.Compose.Components.Timeline.TimelineHitObjectBlueprint>().Any());

            AddStep("remove every object again", () => EditorBeatmap.Clear());
            AddAssert("no more listen than before", () => listenersOf(reference.Beatmap), () => Is.EqualTo(listening));
        }

        private static int listenersOf(Bindable<IBeatmap?> bindable)
        {
            var handlers = (Delegate?)typeof(Bindable<IBeatmap?>).GetField(nameof(bindable.ValueChanged), BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(bindable);
            return handlers?.GetInvocationList().Length ?? 0;
        }

        private void press(GlobalAction action) => AddStep($"press {action}",
            () => Editor.OnPressed(new KeyBindingPressEvent<GlobalAction>(new InputState(), action)));

        private OsuMenuItem? findMenuItem()
        {
            var bar = Editor.ChildrenOfType<EditorMenuBar>().Single();
            return search(bar.Items);

            static OsuMenuItem? search(IEnumerable<MenuItem> items)
            {
                foreach (var item in items)
                {
                    if (item is OsuMenuItem osuItem && item.Text.Value.Equals(EditorStrings.ReferenceDifficulty))
                        return osuItem;

                    var found = search(item.Items);

                    if (found != null)
                        return found;
                }

                return null;
            }
        }

        /// <remarks>
        /// Three other difficulties, so that stepping has somewhere to go and the order it goes in can be told apart
        /// from the order they were added in.
        /// </remarks>
        protected override IBeatmap CreateBeatmap(RulesetInfo ruleset)
        {
            var beatmap = new Beatmap
            {
                BeatmapInfo = { Ruleset = ruleset, DifficultyName = "being edited", StarRating = 3 },
                HitObjects = Enumerable.Range(0, 10).Select(i => (HitObject)new HitCircle { StartTime = 1000 + i * 500 }).ToList(),
            };

            // deliberately added hardest first: the menu and the stepping both go by star rating, not by this order.
            hardest = new BeatmapInfo { Ruleset = ruleset, DifficultyName = "hardest", StarRating = 6 };
            easiest = new BeatmapInfo { Ruleset = ruleset, DifficultyName = "easiest", StarRating = 1 };
            middle = new BeatmapInfo { Ruleset = ruleset, DifficultyName = "middle", StarRating = 4 };

            var set = new BeatmapSetInfo();

            foreach (var info in new[] { beatmap.BeatmapInfo, hardest, easiest, middle })
            {
                set.Beatmaps.Add(info);
                info.BeatmapSet = set;
            }

            return beatmap;
        }
    }
}
