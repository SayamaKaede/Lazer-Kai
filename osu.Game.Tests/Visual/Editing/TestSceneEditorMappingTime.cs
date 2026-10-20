// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Extensions;
using osu.Game.Beatmaps;
using osu.Game.Database;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Screens.Edit;
using osu.Game.Screens.Menu;
using osu.Game.Tests.Resources;
using osuTK.Input;

namespace osu.Game.Tests.Visual.Editing
{
    /// <summary>
    /// Time in the editor counts as mapping only around the changes made in it.
    /// </summary>
    public partial class TestSceneEditorMappingTime : EditorSavingTestScene
    {
        protected override Ruleset CreateRuleset() => new OsuRuleset();

        private BeatmapInfo imported = null!;

        public override void SetUpSteps()
        {
            CreateInitialBeatmap = () =>
            {
                if (Game.Storage.Exists(MappingTimeStore.FILENAME))
                    Game.Storage.Delete(MappingTimeStore.FILENAME);

                var importedSet = Game.BeatmapManager.Import(new ImportTask(TestResources.GetTestBeatmapForImport())).GetResultSafely();
                imported = importedSet!.Value.Beatmaps.Single(b => b.Path == @"Soleily - Renatus (Gamu) [Hard].osu");

                return Game.BeatmapManager.GetWorkingBeatmap(imported);
            };

            base.SetUpSteps();
        }

        [Test]
        public void TestLookingWithoutChangingCountsNothing()
        {
            AddStep("seek", () => EditorClock.Seek(10000));
            AddStep("select everything", () => EditorBeatmap.SelectedHitObjects.AddRange(EditorBeatmap.HitObjects));
            AddStep("look at the timing", () => Editor.Mode.Value = EditorScreenMode.Timing);
            AddStep("and back", () => Editor.Mode.Value = EditorScreenMode.Compose);

            // escape would only clear the selection otherwise.
            AddStep("select nothing", () => EditorBeatmap.SelectedHitObjects.Clear());

            exit();

            AddAssert("no time changing it", () => spans(MappingSpanKind.Edit), () => Is.Empty);
            AddAssert("but the time it was open", () => spans(MappingSpanKind.Open).Single().BeatmapID, () => Is.EqualTo(imported.ID));
            AddAssert("and the day lists nothing", () => new MappingTimeSummary(new MappingTimeStore(Game.Storage).Load(), TimeZoneInfo.Local).SetsOn(DateTime.Today),
                () => Is.Empty);
        }

        [Test]
        public void TestAChangeCountsUntilTheEditorIsLeft()
        {
            DateTimeOffset before = default;

            AddStep("place a circle", () =>
            {
                before = DateTimeOffset.Now;
                EditorBeatmap.Add(new HitCircle { StartTime = 1234 });
            });
            SaveEditor();
            AddWaitStep("let the save settle", 5);

            exit();

            AddAssert("one stretch, of this difficulty", () => spans(MappingSpanKind.Edit).Single().BeatmapID, () => Is.EqualTo(imported.ID));
            AddAssert("ending where the editor was left", () =>
            {
                var span = spans(MappingSpanKind.Edit).Single();
                return span.Start >= before && span.End < span.Start + MappingTimeRecorder.IDLE_LIMIT;
            });
            AddAssert("inside the time it was open", () =>
            {
                var edit = spans(MappingSpanKind.Edit).Single();
                var open = spans(MappingSpanKind.Open).Single();
                return open.Start <= edit.Start && edit.End <= open.End;
            });
        }

        private MappingSpan[] spans(MappingSpanKind kind) => new MappingTimeStore(Game.Storage).Load().Where(s => s.Kind == kind).ToArray();

        private EditorClock EditorClock => (EditorClock)Editor.Dependencies.Get(typeof(EditorClock));

        private void exit()
        {
            AddStep("exit", () => InputManager.Key(Key.Escape));
            AddUntilStep("wait for main menu", () => Game.ScreenStack.CurrentScreen is MainMenu);
        }
    }
}
