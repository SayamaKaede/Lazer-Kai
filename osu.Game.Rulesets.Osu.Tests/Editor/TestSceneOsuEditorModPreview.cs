// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Extensions.ObjectExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Testing;
using osu.Game.Configuration;
using osu.Game.Graphics.Sprites;
using osu.Game.Localisation;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects.Legacy;
using osu.Game.Rulesets.Osu.Edit;
using osu.Game.Rulesets.Osu.Mods;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.UI;
using osu.Game.Screens.Edit.Components.Menus;
using osu.Game.Screens.Edit.Compose.Components.Timeline;
using osuTK;
using osuTK.Input;
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

        public override void SetUpSteps()
        {
            // both are kept in the config, which every test here shares: the mods a test picked would otherwise be
            // previewed from the start of the next one.
            AddStep("no test play mods", () => config.GetBindable<string>(OsuSetting.EditorTestPlayMods).SetDefault());
            AddStep("mods not previewed", () => config.GetBindable<bool>(OsuSetting.EditorApplyTestPlayModsToComposer).SetDefault());

            base.SetUpSteps();
        }

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
        /// Hard rock's circle size and flip reach the composer, as they do test play (2026-09-29 request), while the
        /// beatmap's own object is left as it is.
        /// </summary>
        [Test]
        public void TestHardRockIsShownInTheComposer()
        {
            AddStep("add a circle", () => EditorBeatmap.Add(new HitCircle { StartTime = 1000, Position = new Vector2(100, 100) }));
            AddStep("go to it", () => EditorClock.Seek(1000));

            previewMods(new OsuModHardRock());

            AddUntilStep("the circle is shown flipped", () => shownCircle()?.Position, () => Is.EqualTo(new Vector2(100, OsuPlayfield.BASE_SIZE.Y - 100)));
            AddAssert("and smaller", () => shownCircle()!.Scale, () => Is.LessThan(ownCircle().Scale));
            AddAssert("the beatmap's own circle is where it was", () => ownCircle().Position, () => Is.EqualTo(new Vector2(100, 100)));
            AddAssert("and as large", () => ownCircle().Scale, () => Is.EqualTo(unmoddedScale()));
        }

        /// <summary>
        /// The notice saying why nothing can be edited goes after about five seconds (2026-09-29 request).
        /// </summary>
        [Test]
        public void TestTheLockNoticeGoesAfterAFewSeconds()
        {
            AddStep("add a circle", () => EditorBeatmap.Add(new HitCircle { StartTime = 1000, Position = new Vector2(100, 100) }));

            previewMods(new OsuModHardRock());

            AddUntilStep("the notice is shown", () => lockNotice(), () => Is.Not.Empty);
            AddUntilStep("it goes", () => lockNotice(), () => Is.Empty);
        }

        private IEnumerable<OsuSpriteText> lockNotice() =>
            Editor.ChildrenOfType<OsuSpriteText>().Where(t => t.Text.Equals(EditorStrings.EditingLockedByPreviewMods));

        /// <summary>
        /// What the composer shows under hard rock is not the beatmap being edited, so nothing on it can be picked up.
        /// </summary>
        [Test]
        public void TestObjectsCannotBeSelectedWhileTheirSizeOrPlaceIsChanged()
        {
            AddStep("add a circle", () => EditorBeatmap.Add(new HitCircle { StartTime = 1000, Position = new Vector2(256, 192) }));
            AddStep("go to it", () => EditorClock.Seek(1000));

            previewMods(new OsuModHardRock());

            AddAssert("editing is locked", () => composer().EditingLocked, () => Is.True);
            AddUntilStep("the circle is shown", () => shownCircle() != null);

            AddStep("click it", () =>
            {
                InputManager.MoveMouseTo(composer().Playfield.ToScreenSpace(shownCircle()!.Position));
                InputManager.Click(MouseButton.Left);
            });

            AddAssert("nothing is selected", () => EditorBeatmap.SelectedHitObjects, () => Is.Empty);
        }

        /// <summary>
        /// Freeze frame only changes when objects come in, and the composer goes on editing the beatmap under it.
        /// </summary>
        [Test]
        public void TestObjectsCanBeSelectedUnderFreezeFrame()
        {
            AddStep("add a circle", () => EditorBeatmap.Add(new HitCircle { StartTime = 1000, Position = new Vector2(256, 192) }));
            AddStep("go to it", () => EditorClock.Seek(1000));

            previewMods(new OsuModFreezeFrame());

            AddAssert("editing is not locked", () => composer().EditingLocked, () => Is.False);
            AddUntilStep("the circle is shown", () => shownCircle() != null);

            AddStep("click it", () =>
            {
                InputManager.MoveMouseTo(composer().Playfield.ToScreenSpace(shownCircle()!.Position));
                InputManager.Click(MouseButton.Left);
            });

            AddAssert("it is selected", () => EditorBeatmap.SelectedHitObjects, () => Has.Count.EqualTo(1));
        }

        /// <summary>
        /// The copy the composer shows is made again when the beatmap changes some other way, such as an undo.
        /// </summary>
        [Test]
        public void TestAnEditMadeElsewhereIsShown()
        {
            AddStep("add a circle", () => EditorBeatmap.Add(new HitCircle { StartTime = 1000, Position = new Vector2(100, 100) }));
            AddStep("go to it", () => EditorClock.Seek(1000));

            previewMods(new OsuModHardRock());

            AddUntilStep("the circle is shown flipped", () => shownCircle()?.Position, () => Is.EqualTo(new Vector2(100, OsuPlayfield.BASE_SIZE.Y - 100)));

            AddStep("move it", () =>
            {
                EditorBeatmap.BeginChange();
                ownCircle().Position = new Vector2(200, 100);
                EditorBeatmap.Update(ownCircle());
                EditorBeatmap.EndChange();
            });

            AddUntilStep("the move is shown, flipped", () => shownCircle()?.Position, () => Is.EqualTo(new Vector2(200, OsuPlayfield.BASE_SIZE.Y - 100)));
        }

        /// <summary>
        /// Random used to be applied to the beatmap's own objects, so what was saved after previewing it was the shuffle.
        /// </summary>
        [Test]
        public void TestRandomLeavesTheBeatmapWhereItWas()
        {
            Vector2[] positions = [];

            AddStep("add circles", () =>
            {
                for (int i = 0; i < 10; i++)
                    EditorBeatmap.Add(new HitCircle { StartTime = 1000 + i * 500, Position = new Vector2(100 + i * 30, 100) });
            });

            AddStep("remember where everything is", () => positions = ownPositions());

            previewMods(new OsuModRandom { Seed = { Value = 1 } });

            AddAssert("everything is where it was", ownPositions, () => Is.EqualTo(positions));
        }

        private Vector2[] ownPositions() => EditorBeatmap.HitObjects.OfType<OsuHitObject>().Select(h => h.Position).ToArray();

        private void previewMods(params Mod[] mods)
        {
            AddStep("preview test play mods", () => config.SetValue(OsuSetting.EditorApplyTestPlayModsToComposer, true));
            AddStep($"pick {string.Join(", ", mods.Select(m => m.Acronym))}", () => Editor.TestPlayMods.Mods.Value = mods);
            waitForComposerReload();
        }

        /// <summary>The circle at 1000 as the composer draws it, or null while there is no composer.</summary>
        private HitCircle? shownCircle() =>
            Editor.ChildrenOfType<HitObjectComposer>().SingleOrDefault()?.Playfield.AllHitObjects
                  .Select(d => d.HitObject).OfType<HitCircle>().SingleOrDefault(h => h.StartTime == 1000);

        /// <summary>The circle at 1000 in the beatmap being edited.</summary>
        private HitCircle ownCircle() => EditorBeatmap.HitObjects.OfType<HitCircle>().Single(h => h.StartTime == 1000);

        private float unmoddedScale() => LegacyRulesetExtensions.CalculateScaleFromCircleSize(EditorBeatmap.Difficulty.CircleSize, true);

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

        /// <summary>
        /// Without autoplay's cursor on screen there is nothing else to look at, so the lit area follows the mapper's own
        /// pointer, as in play (2026-09-26: it followed autoplay's path whether or not the cursor was shown).
        /// </summary>
        [Test]
        public void TestTheMouseLeadsTheFlashlightWithoutTheAutoCursor()
        {
            Vector2 mouse = default;

            AddStep("auto cursor hidden", () => config.SetValue(OsuSetting.EditorShowAutoCursor, false));
            previewFlashlightOverACircle();

            AddStep("move the mouse away from the circle", () =>
            {
                var area = composer().ScreenSpaceDrawQuad;
                mouse = area.TopLeft + new Vector2(area.Width * 0.8f, area.Height * 0.8f);
                InputManager.MoveMouseTo(mouse);
            });

            AddUntilStep("the lit area is at the mouse", () => Vector2.Distance(litPosition(), mouse), () => Is.LessThan(5));

            AddStep("setting back", () => config.GetBindable<bool>(OsuSetting.EditorShowAutoCursor).SetDefault());
        }

        /// <summary>
        /// With autoplay's cursor shown, the lit area goes where it goes, whatever the mouse is doing.
        /// </summary>
        [Test]
        public void TestTheAutoCursorLeadsTheFlashlightWhileShown()
        {
            Vector2 mouse = default;

            AddStep("auto cursor shown", () => config.SetValue(OsuSetting.EditorShowAutoCursor, true));
            previewFlashlightOverACircle();

            AddStep("move the mouse away from the circle", () =>
            {
                var area = composer().ScreenSpaceDrawQuad;
                mouse = area.TopLeft + new Vector2(area.Width * 0.8f, area.Height * 0.8f);
                InputManager.MoveMouseTo(mouse);
            });

            AddUntilStep("the lit area is at the auto cursor", () => autoCursor().ScreenSpacePosition is Vector2 cursor && Vector2.Distance(litPosition(), cursor) < 5);
            AddAssert("not at the mouse", () => Vector2.Distance(litPosition(), mouse), () => Is.GreaterThan(50));

            AddStep("setting back", () => config.GetBindable<bool>(OsuSetting.EditorShowAutoCursor).SetDefault());
        }

        /// <summary>
        /// The menus open down over the rest of the editor, and flashlight darkens everything below the menu bar: they
        /// have to be drawn over it, or an open menu is hidden in the dark.
        /// </summary>
        [Test]
        public void TestTheMenusAreDrawnOverTheFlashlight()
        {
            previewFlashlightOverACircle();

            AddAssert("the menu bar is drawn after the flashlight", () =>
            {
                var layers = ((Container<Drawable>)Editor.ModOverlays.Parent!).Children.ToList();
                Drawable menuBar = Editor.ChildrenOfType<EditorMenuBar>().Single();

                while (menuBar.Parent != Editor.ModOverlays.Parent)
                    menuBar = menuBar.Parent!;

                return layers.IndexOf(menuBar) > layers.IndexOf(Editor.ModOverlays);
            });
        }

        private void previewFlashlightOverACircle()
        {
            AddStep("add a circle near the corner", () => EditorBeatmap.Add(new HitCircle { StartTime = 1000, Position = new Vector2(50, 50) }));
            AddStep("preview test play mods", () => config.SetValue(OsuSetting.EditorApplyTestPlayModsToComposer, true));
            AddStep("pick flashlight", () => Editor.TestPlayMods.Mods.Value = new Mod[] { new OsuModFlashlight() });
            waitForComposerReload();

            AddUntilStep("the flashlight is up", () => flashlight() != null);
            AddStep("go to the circle", () => EditorClock.Seek(1000));
        }

        /// <summary>
        /// Where the lit area is, on screen.
        /// </summary>
        private Vector2 litPosition()
        {
            var lit = flashlight().AsNonNull();
            var position = (Vector2)typeof(ModFlashlight<OsuHitObject>.Flashlight)
                                    .GetProperty("FlashlightPosition", BindingFlags.NonPublic | BindingFlags.Instance)!
                                    .GetValue(lit)!;

            return lit.ToScreenSpace(position);
        }

        private AutoCursorDisplay autoCursor() => Editor.ChildrenOfType<AutoCursorDisplay>().Single();

        private HitObjectComposer composer() => Editor.ChildrenOfType<HitObjectComposer>().Single();

        private ModFlashlight<OsuHitObject>.Flashlight? flashlight() =>
            Editor.ChildrenOfType<ModFlashlight<OsuHitObject>.Flashlight>().SingleOrDefault();

        private HitCircle circle() => (HitCircle)EditorBeatmap.PlayableBeatmap.HitObjects.Single(h => h.StartTime == 10500);

        private void waitForComposerReload() =>
            AddUntilStep("composer rebuilt", () => Editor.ChildrenOfType<HitObjectComposer>().SingleOrDefault()?.IsLoaded == true);
    }
}
