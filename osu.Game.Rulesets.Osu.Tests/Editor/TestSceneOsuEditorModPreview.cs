// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Extensions.ObjectExtensions;
using osu.Framework.Testing;
using osu.Game.Configuration;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Mods;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Screens.Edit.Compose.Components.Timeline;
using osu.Game.Tests.Visual;
using osu.Framework.Graphics.Primitives;

namespace osu.Game.Rulesets.Osu.Tests.Editor
{
    /// <summary>
    /// Covers showing the test play mods in the composer, which rebuilds the composer around a playfield that has no
    /// gameplay screen behind it. Deliberately kept out of <see cref="TestSceneOsuEditor"/>, which a dozen fixtures
    /// inherit from.
    /// </summary>
    [TestFixture]
    public partial class TestSceneOsuEditorModPreview : EditorTestScene
    {
        protected override Ruleset CreateEditorRuleset() => new OsuRuleset();

        [Resolved]
        private OsuConfigManager config { get; set; } = null!;

        /// <summary>
        /// Turning the preview on or off rebuilds the composer, which disposes the playfield the beatmap's objects were
        /// handed to. Anything the old playfield left subscribed to those objects keeps firing after it is gone, so moving
        /// an object would throw from a dead playfield — on every later move, whichever way the setting was left, until
        /// the game gave up and crashed.
        /// </summary>
        [Test]
        public void TestObjectMovedAfterComposerReload()
        {
            AddStep("add an object", () => EditorBeatmap.Add(new HitCircle { StartTime = 1000 }));

            AddStep("preview test play mods", () => config.SetValue(OsuSetting.EditorApplyTestPlayModsToComposer, true));
            waitForComposerReload();

            AddStep("move the object", () => EditorBeatmap.HitObjects.First().StartTime += 100);

            AddStep("stop previewing test play mods", () => config.SetValue(OsuSetting.EditorApplyTestPlayModsToComposer, false));
            waitForComposerReload();

            AddStep("move the object again", () => EditorBeatmap.HitObjects.First().StartTime += 100);
        }

        /// <summary>
        /// Selecting and editing objects after the composer has been rebuilt. The parts of the old composer which listen
        /// for selection changes and edits must have stopped listening by the time it is gone.
        /// </summary>
        [Test]
        public void TestObjectSelectedAndEditedAfterComposerReload()
        {
            AddStep("add an object", () => EditorBeatmap.Add(new HitCircle { StartTime = 1000 }));

            AddStep("preview test play mods", () => config.SetValue(OsuSetting.EditorApplyTestPlayModsToComposer, true));
            waitForComposerReload();

            AddStep("select the object", () => EditorBeatmap.SelectedHitObjects.Add(EditorBeatmap.HitObjects.First()));

            AddStep("edit the object", () =>
            {
                EditorBeatmap.BeginChange();
                EditorBeatmap.HitObjects.First().StartTime += 100;
                EditorBeatmap.Update(EditorBeatmap.HitObjects.First());
                EditorBeatmap.EndChange();
            });

            AddStep("deselect", () => EditorBeatmap.SelectedHitObjects.Clear());
        }

        /// <summary>
        /// The preview hands the chosen mods to a playfield with no gameplay around it. Every mod has to survive that, so
        /// each one is selected in turn and left running for a few frames.
        /// </summary>
        [Test]
        public void TestEveryModPreviewedInComposer()
        {
            AddStep("add an object", () => EditorBeatmap.Add(new HitCircle { StartTime = 1000 }));
            AddStep("preview test play mods", () => config.SetValue(OsuSetting.EditorApplyTestPlayModsToComposer, true));

            // the composer runs an autoplay of its own, so a second one has nothing to add here.
            foreach (var mod in new OsuRuleset().CreateAllMods().Where(m => m is not ModAutoplay))
            {
                AddStep($"select {mod.Acronym}", () => Editor.TestPlayMods.Mods.Value = new[] { mod });
                waitForComposerReload();
                AddWaitStep($"let {mod.Acronym} run", 5);
            }

            AddStep("clear mods", () => Editor.TestPlayMods.Mods.Value = []);
            AddStep("stop previewing test play mods", () => config.SetValue(OsuSetting.EditorApplyTestPlayModsToComposer, false));
        }

        /// <summary>
        /// The preview writes into the beatmap's own objects, so it has to write them back before the composer goes.
        /// </summary>
        /// <remarks>
        /// Left to disposal this ran on the disposal thread, where touching a hit object throws: the drawables still
        /// bound to it react to being given fresh defaults, and they may only be touched from the update thread.
        /// </remarks>
        [Test]
        public void TestBeatmapLeftUnmoddedAfterThePreviewIsTurnedOff()
        {
            double preemptWhenUnmodded = 0;

            // freeze frame stretches an object's preempt by how far it sits into its combo, so a combo of one would
            // come out unchanged and the test would agree with a broken build.
            AddStep("start a combo", () => EditorBeatmap.Add(new HitCircle { StartTime = 10000, NewCombo = true }));
            AddStep("add a second object to it", () => EditorBeatmap.Add(new HitCircle { StartTime = 10500 }));
            AddStep("remember its preempt", () => preemptWhenUnmodded = circle().TimePreempt);

            AddStep("preview test play mods", () => config.SetValue(OsuSetting.EditorApplyTestPlayModsToComposer, true));
            AddStep("pick freeze frame", () => Editor.TestPlayMods.Mods.Value = new Mod[] { new OsuModFreezeFrame() });
            waitForComposerReload();

            AddUntilStep("freeze frame stretched the preempt", () => circle().TimePreempt, () => Is.Not.EqualTo(preemptWhenUnmodded));

            AddStep("stop previewing test play mods", () => config.SetValue(OsuSetting.EditorApplyTestPlayModsToComposer, false));
            waitForComposerReload();

            AddUntilStep("the preempt is back", () => circle().TimePreempt, () => Is.EqualTo(preemptWhenUnmodded));
        }

        /// <summary>
        /// Flashlight is drawn by the ruleset, which only ever fills the space the playfield was given. Left there, the
        /// beatmap goes on showing everywhere else the editor puts it — the edges of the composer, the timeline, and the
        /// bottom bar — which is the rest of what a mapper practising their reading would be looking at.
        /// </summary>
        [Test]
        public void TestFlashlightReachesTheTimelineAndTheBottomBar()
        {
            AddStep("add an object", () => EditorBeatmap.Add(new HitCircle { StartTime = 1000 }));
            AddStep("preview test play mods", () => config.SetValue(OsuSetting.EditorApplyTestPlayModsToComposer, true));
            AddStep("pick flashlight", () => Editor.TestPlayMods.Mods.Value = new Mod[] { new OsuModFlashlight() });
            waitForComposerReload();

            AddUntilStep("the flashlight is up", () => flashlight() != null);

            AddAssert("it covers the composer", () => covers(composer().ScreenSpaceDrawQuad), () => Is.True);
            AddAssert("it covers the timeline", () => covers(timeline().ScreenSpaceDrawQuad), () => Is.True);

            // the bottom bar is internal, so the bottom edge of the editor stands in for it: the bar is the last thing
            // down there, and nothing reaching that far can be stopping short of it.
            AddAssert("it reaches the bottom of the editor", () => covers(bottomOf(Editor.ScreenSpaceDrawQuad)), () => Is.True);
        }

        private bool covers(Quad area)
        {
            var darkened = flashlight().AsNonNull().ScreenSpaceDrawQuad;

            return darkened.TopLeft.X <= area.TopLeft.X
                   && darkened.TopLeft.Y <= area.TopLeft.Y
                   && darkened.BottomRight.X >= area.BottomRight.X
                   && darkened.BottomRight.Y >= area.BottomRight.Y;
        }

        /// <summary>A strip along the bottom edge of the given area, one pixel tall.</summary>
        private static Quad bottomOf(Quad area) =>
            new Quad(area.BottomLeft.X, area.BottomLeft.Y - 1, area.Width, 1);

        private TimelineArea timeline() => Editor.ChildrenOfType<TimelineArea>().Single();

        /// <summary>
        /// The lit area narrows at a combo of 100 and again at 200. Nothing in the editor keeps score, so without being
        /// told what the combo is the flashlight would sit at the widest one for the whole beatmap.
        /// </summary>
        [Test]
        public void TestTheLitAreaNarrowsAsTheComboClimbs()
        {
            float sizeAtTheStart = 0;

            AddStep("lay out a combo of over a hundred", () =>
            {
                for (int i = 0; i < 120; i++)
                    EditorBeatmap.Add(new HitCircle { StartTime = 1000 + i * 500 });
            });

            AddStep("preview test play mods", () => config.SetValue(OsuSetting.EditorApplyTestPlayModsToComposer, true));
            AddStep("pick flashlight", () => Editor.TestPlayMods.Mods.Value = new Mod[] { new OsuModFlashlight() });
            waitForComposerReload();

            AddUntilStep("the flashlight is up", () => flashlight() != null);

            AddStep("go to the start", () => EditorClock.Seek(0));
            AddStep("read how wide it is there", () => sizeAtTheStart = flashlight().AsNonNull().GetSize());

            AddStep("go past the hundredth object", () => EditorClock.Seek(1000 + 110 * 500));
            AddUntilStep("it has narrowed", () => flashlight().AsNonNull().GetSize(), () => Is.LessThan(sizeAtTheStart));
        }

        private HitObjectComposer composer() => Editor.ChildrenOfType<HitObjectComposer>().Single();

        private ModFlashlight<OsuHitObject>.Flashlight? flashlight() =>
            Editor.ChildrenOfType<ModFlashlight<OsuHitObject>.Flashlight>().SingleOrDefault();

        private HitCircle circle() => (HitCircle)EditorBeatmap.PlayableBeatmap.HitObjects.Single(h => h.StartTime == 10500);

        private void waitForComposerReload() =>
            AddUntilStep("composer rebuilt", () => Editor.ChildrenOfType<HitObjectComposer>().SingleOrDefault()?.IsLoaded == true);
    }
}
