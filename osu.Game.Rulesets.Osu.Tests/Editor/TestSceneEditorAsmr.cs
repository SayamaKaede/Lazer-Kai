// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Testing;
using osu.Framework.Graphics.Sprites;
using osu.Game.Configuration;
using osu.Game.Graphics.UserInterface;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Edit.Tools;
using osu.Game.Rulesets.Osu.Edit;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.UI;
using osu.Game.Screens.Edit;
using osu.Game.Screens.Edit.Compose.Components.Timeline;
using osu.Game.Tests.Visual;
using osuTK;
using osuTK.Input;

namespace osu.Game.Rulesets.Osu.Tests.Editor
{
    /// <summary>
    /// The editor's ASMR mode (2026-10-21): a sound for each thing done to the objects, from their side of the playfield,
    /// only while the track is stopped, and once for one action.
    /// </summary>
    public partial class TestSceneEditorAsmr : EditorTestScene
    {
        protected override Ruleset CreateEditorRuleset() => new OsuRuleset();

        [Resolved]
        private OsuConfigManager config { get; set; } = null!;

        private readonly List<(AsmrSound sound, int position)> played = new List<(AsmrSound, int)>();

        private EditorAsmr asmr => Editor.ChildrenOfType<EditorAsmr>().Single();

        [SetUpSteps]
        public void ListenAndTurnOn()
        {
            AddStep("turn the sounds on", () => config.SetValue(OsuSetting.EditorAsmrSounds, true));
            AddStep("listen", () =>
            {
                played.Clear();
                asmr.Played += (sound, position) => played.Add((sound, position));
            });
        }

        [TearDownSteps]
        public void TurnOff()
        {
            AddStep("turn the sounds off", () => config.SetValue(OsuSetting.EditorAsmrSounds, false));
        }

        [Test]
        public void TestEverySoundIsBuiltIn()
        {
            AddAssert("each sound from each place is there", () =>
                Enum.GetValues<AsmrSound>().SelectMany(s => Enumerable.Range(0, EditorAsmr.POSITIONS).Where(p => asmr.GetSample(s, p) == null).Select(p => $@"{s}-{p}")),
                () => Is.Empty);
        }

        [Test]
        public void TestACirclePlacedSoundsFromItsSide()
        {
            placeCircleAt(40, 3800);
            AddUntilStep("a circle from the left ear", () => played, () => Does.Contain((AsmrSound.PlaceCircle, 0)));

            placeCircleAt(480, 5600);
            AddUntilStep("then one from the right ear", () => played, () => Does.Contain((AsmrSound.PlaceCircle, EditorAsmr.POSITIONS - 1)));
        }

        [Test]
        public void TestNothingWhileOff()
        {
            AddStep("turn the sounds off", () => config.SetValue(OsuSetting.EditorAsmrSounds, false));
            placeCircleAt(256, 3800);
            AddWaitStep("a few frames", 3);
            AddAssert("nothing was heard", () => played, () => Is.Empty);
        }

        [Test]
        public void TestNothingWhileTheTrackPlays()
        {
            AddStep("choose the circle tool", () => InputManager.Key(Key.Number2));
            // choosing the tool by its key is heard, with the track stopped (2026-10-21); what follows is the point.
            AddWaitStep("let it through", 2);
            AddStep("forget it", () => played.Clear());
            AddStep("play the track", () => EditorClock.Start());
            AddStep("place a circle", () =>
            {
                InputManager.MoveMouseTo(playfield.ToScreenSpace(new Vector2(256, 192)));
                InputManager.Click(MouseButton.Left);
            });
            AddUntilStep("the circle is placed", () => EditorBeatmap.HitObjects.OfType<HitCircle>().Any(h => h.Position == new Vector2(256, 192)));
            AddWaitStep("a few frames", 3);
            AddStep("stop the track", () => EditorClock.Stop());
            AddAssert("nothing was heard", () => played, () => Is.Empty);
        }

        [Test]
        public void TestDeletingIsOneSound()
        {
            AddStep("select the first object", () => EditorBeatmap.SelectedHitObjects.Add(EditorBeatmap.HitObjects.First()));
            AddUntilStep("its selection was heard", () => played.Select(p => p.sound), () => Does.Contain(AsmrSound.Select));
            AddStep("forget it", () => played.Clear());

            AddStep("delete it", () => InputManager.Key(Key.Delete));
            AddUntilStep("a deletion was heard", () => played.Select(p => p.sound), () => Does.Contain(AsmrSound.Delete));
            AddWaitStep("a few frames", 3);
            AddAssert("and not its deselection", () => played.Select(p => p.sound), () => Is.EqualTo(new[] { AsmrSound.Delete }));
        }

        [Test]
        public void TestUndoIsItsOwnSound()
        {
            placeCircleAt(256, 3800);
            AddUntilStep("the circle was heard", () => played.Select(p => p.sound), () => Does.Contain(AsmrSound.PlaceCircle));
            AddStep("forget it", () => played.Clear());

            AddStep("undo", () =>
            {
                InputManager.PressKey(Key.ControlLeft);
                InputManager.Key(Key.Z);
                InputManager.ReleaseKey(Key.ControlLeft);
            });
            AddUntilStep("the undo was heard", () => played.Select(p => p.sound), () => Does.Contain(AsmrSound.Undo));
            AddWaitStep("a few frames", 3);
            AddAssert("and not the circle going", () => played.Select(p => p.sound), () => Is.EqualTo(new[] { AsmrSound.Undo }));
        }

        [Test]
        public void TestStretchingASliderClicksOnEachStep()
        {
            AddStep("choose the slider tool", () => InputManager.Key(Key.Number3));
            // between the beatmap's first two objects, after its first timing point.
            AddStep("seek clear of the objects", () => EditorClock.Seek(3800));
            AddStep("start a slider", () =>
            {
                InputManager.MoveMouseTo(playfield.ToScreenSpace(new Vector2(100, 192)));
                InputManager.Click(MouseButton.Left);
            });
            AddUntilStep("its start was heard", () => played.Select(p => p.sound), () => Does.Contain(AsmrSound.SliderStart));

            for (int i = 1; i <= 4; i++)
            {
                int step = i;
                AddStep($"stretch it to {100 + step * 60}", () => InputManager.MoveMouseTo(playfield.ToScreenSpace(new Vector2(100 + step * 60, 192))));
                AddWaitStep("let the click through", 3);
            }

            AddAssert("a click for each step", () => played.Count(p => p.sound == AsmrSound.SliderLength), () => Is.GreaterThanOrEqualTo(4));
        }

        /// <summary>
        /// Reported 2026-10-21: adding a repeat by dragging a slider's end along the timeline made no sound.
        /// </summary>
        [Test]
        public void TestARepeatAddedOnTheTimelineIsHeard()
        {
            Slider slider = null!;

            AddStep("seek clear of the objects", () => EditorClock.Seek(3800));
            AddStep("add a slider", () => EditorBeatmap.Add(slider = new Slider
            {
                Position = new Vector2(100, 300),
                StartTime = 3800,
                Path = new SliderPath(new[] { new PathControlPoint(Vector2.Zero), new PathControlPoint(new Vector2(100, 0)) }),
            }));
            AddStep("forget it", () => played.Clear());

            AddStep("hold its end on the timeline", () =>
            {
                var blueprint = Editor.ChildrenOfType<TimelineHitObjectBlueprint>().Single(b => b.Item == slider);
                InputManager.MoveMouseTo(blueprint.ChildrenOfType<TimelineHitObjectBlueprint.DragArea>().Single(d => d.HandlePositionalInput));
                InputManager.PressButton(MouseButton.Left);
            });
            AddStep("drag it on past twice its length", () =>
            {
                var blueprint = Editor.ChildrenOfType<TimelineHitObjectBlueprint>().Single(b => b.Item == slider);
                InputManager.MoveMouseTo(blueprint.ScreenSpaceDrawQuad.TopRight + new Vector2(blueprint.ScreenSpaceDrawQuad.Width * 1.2f, 0));
            });
            AddStep("let go", () => InputManager.ReleaseButton(MouseButton.Left));

            AddAssert("it has a repeat", () => slider.RepeatCount, () => Is.GreaterThan(0));
            AddUntilStep("the repeat was heard", () => played.Select(p => p.sound), () => Does.Contain(AsmrSound.SliderRepeat));
        }

        /// <summary>
        /// Asked for 2026-10-21: the editor's buttons in the same voice, standing in for the game's own interface
        /// sounds, by the mouse or by their keys.
        /// </summary>
        [Test]
        public void TestAToolButtonClickedIsHeardFromTheLeft()
        {
            AddStep("click the slider tool", () =>
            {
                InputManager.MoveMouseTo(toolButton<SliderCompositionTool>());
                InputManager.Click(MouseButton.Left);
            });
            AddUntilStep("a click from the left", () => played.Where(p => p.sound == AsmrSound.UiClick).Select(p => p.position),
                () => Has.Some.LessThan(EditorAsmr.POSITIONS / 2));
        }

        [Test]
        public void TestPointingAtAButtonIsHeard()
        {
            AddStep("point at the slider tool", () => InputManager.MoveMouseTo(toolButton<SliderCompositionTool>()));
            AddUntilStep("a hover was heard", () => played.Select(p => p.sound), () => Does.Contain(AsmrSound.UiHover));
        }

        [Test]
        public void TestAToolChosenByItsKeyIsHeard()
        {
            AddStep("press the slider tool's key", () => InputManager.Key(Key.Number3));
            AddUntilStep("a click was heard", () => played.Select(p => p.sound), () => Does.Contain(AsmrSound.UiClick));
        }

        [Test]
        public void TestASwitchFlippedByItsKeyIsHeard()
        {
            AddStep("choose the circle tool", () => InputManager.Key(Key.Number2));
            AddStep("forget it", () => played.Clear());
            AddStep("toggle new combo by its key", () => InputManager.Key(Key.Q));
            AddUntilStep("a switch was heard", () => played.Select(p => p.sound), () => Does.Contain(AsmrSound.UiToggle));
        }

        [Test]
        public void TestPinningAGroupIsHeard()
        {
            AddStep("pin the first toolbox group", () =>
            {
                var pin = Editor.ChildrenOfType<EditorToolboxGroup>().First().ChildrenOfType<IconButton>().First(b => b.Icon.Equals(FontAwesome.Solid.Thumbtack));
                InputManager.MoveMouseTo(pin);
                InputManager.Click(MouseButton.Left);
            });
            AddUntilStep("a pin was heard", () => played.Select(p => p.sound), () => Does.Contain(AsmrSound.UiPin));
        }

        [Test]
        public void TestButtonsSoundAsUsualWhileTheTrackPlays()
        {
            AddStep("play the track", () => EditorClock.Start());
            AddStep("click the slider tool", () =>
            {
                InputManager.MoveMouseTo(toolButton<SliderCompositionTool>());
                InputManager.Click(MouseButton.Left);
            });
            AddWaitStep("a few frames", 3);
            AddStep("stop the track", () => EditorClock.Stop());
            AddAssert("nothing of the mode's was heard", () => played, () => Is.Empty);
        }

        /// <summary>
        /// Reported 2026-10-21: Ctrl+G made no sound.
        /// </summary>
        [Test]
        public void TestReversingIsHeard()
        {
            selectFirstTwo();
            AddStep("reverse them", () =>
            {
                InputManager.PressKey(Key.ControlLeft);
                InputManager.Key(Key.G);
                InputManager.ReleaseKey(Key.ControlLeft);
            });
            AddUntilStep("it was heard", () => played.Select(p => p.sound), () => Does.Contain(AsmrSound.Transform));
        }

        /// <summary>
        /// Reported 2026-10-21: scaling from a handle made no sound when the first object stayed where it was, which it
        /// does when it is what the scale is held at.
        /// </summary>
        [Test]
        public void TestScalingFromTheFirstObjectIsHeard()
        {
            selectFirstTwo();
            AddStep("scale them from the first", () =>
            {
                var handler = Editor.ChildrenOfType<OsuSelectionScaleHandler>().Single();
                var first = (OsuHitObject)EditorBeatmap.SelectedHitObjects.OrderBy(h => h.StartTime).First();

                handler.Begin();
                handler.Update(new Vector2(1.2f), first.Position);
                handler.Commit();
            });
            AddUntilStep("it was heard", () => played.Select(p => p.sound), () => Does.Contain(AsmrSound.Transform));
        }

        /// <summary>
        /// The same for turning them about the first.
        /// </summary>
        [Test]
        public void TestRotatingAboutTheFirstObjectIsHeard()
        {
            selectFirstTwo();
            AddStep("turn them about the first", () =>
            {
                var handler = Editor.ChildrenOfType<OsuSelectionRotationHandler>().Single();
                var first = (OsuHitObject)EditorBeatmap.SelectedHitObjects.OrderBy(h => h.StartTime).First();

                handler.Begin();
                handler.Update(30, first.Position);
                handler.Commit();
            });
            AddUntilStep("it was heard", () => played.Select(p => p.sound), () => Does.Contain(AsmrSound.Transform));
        }

        /// <summary>
        /// Asked for 2026-10-21: stepping along the timeline by the arrows or the wheel rolls quietly.
        /// </summary>
        [Test]
        public void TestSteppingAlongIsHeard()
        {
            AddStep("step forwards", () => InputManager.Key(Key.Right));
            AddUntilStep("a step was heard", () => played.Select(p => p.sound), () => Does.Contain(AsmrSound.Seek));
        }

        private void selectFirstTwo()
        {
            AddStep("select the first two objects", () => EditorBeatmap.SelectedHitObjects.AddRange(EditorBeatmap.HitObjects.Take(2)));
            AddWaitStep("let the selection through", 2);
            AddStep("forget it", () => played.Clear());
        }

        private HitObjectCompositionToolButton toolButton<T>() where T : CompositionTool
            => Editor.ChildrenOfType<HitObjectCompositionToolButton>().First(b => b.Tool is T);

        private OsuPlayfield playfield => Editor.ChildrenOfType<OsuPlayfield>().First();

        private void placeCircleAt(float x, double time)
        {
            AddStep("choose the circle tool", () => InputManager.Key(Key.Number2));
            AddStep($"seek to {time}", () => EditorClock.Seek(time));
            AddStep($"place a circle at x {x}", () =>
            {
                InputManager.MoveMouseTo(playfield.ToScreenSpace(new Vector2(x, 300)));
                InputManager.Click(MouseButton.Left);
            });
            AddUntilStep("the circle is placed", () => EditorBeatmap.HitObjects.OfType<HitCircle>().Any(h => h.Position == new Vector2(x, 300)));
        }
    }
}
