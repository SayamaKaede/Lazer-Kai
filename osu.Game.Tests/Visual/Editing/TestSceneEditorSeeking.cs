// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Input.Events;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Input.Bindings;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Screens.Edit;
using osuTK.Input;

namespace osu.Game.Tests.Visual.Editing
{
    public partial class TestSceneEditorSeeking : EditorTestScene
    {
        protected override Ruleset CreateEditorRuleset() => new OsuRuleset();

        protected override IBeatmap CreateBeatmap(RulesetInfo ruleset)
        {
            var beatmap = base.CreateBeatmap(ruleset);

            beatmap.BeatmapInfo.BeatDivisor = 1;

            beatmap.ControlPointInfo.Clear();
            beatmap.ControlPointInfo.Add(0, new TimingControlPoint { BeatLength = 1000 });
            beatmap.ControlPointInfo.Add(2000, new TimingControlPoint { BeatLength = 500 });
            beatmap.ControlPointInfo.Add(20000, new TimingControlPoint { BeatLength = 500 });

            return beatmap;
        }

        [Test]
        public void TestSeekToFirst()
        {
            pressAndCheckTime(Key.Z, 2170);
            pressAndCheckTime(Key.Z, 0);
            pressAndCheckTime(Key.Z, 2170);

            AddAssert("track not running", () => !EditorClock.IsRunning);
        }

        [Test]
        public void TestRestart()
        {
            pressAndCheckTime(Key.V, 227170);

            AddAssert("track not running", () => !EditorClock.IsRunning);

            AddStep("press X", () => InputManager.Key(Key.X));

            AddAssert("track running", () => EditorClock.IsRunning);
            AddAssert("time restarted", () => EditorClock.CurrentTime < 100000);
        }

        [Test]
        public void TestPauseResume()
        {
            AddAssert("track not running", () => !EditorClock.IsRunning);

            AddStep("press C", () => InputManager.Key(Key.C));
            AddAssert("track running", () => EditorClock.IsRunning);

            AddStep("press C", () => InputManager.Key(Key.C));
            AddAssert("track not running", () => !EditorClock.IsRunning);
        }

        [Test]
        public void TestSeekToLast()
        {
            pressAndCheckTime(Key.V, 227170);
            pressAndCheckTime(Key.V, 229170);
            pressAndCheckTime(Key.V, 227170);

            AddAssert("track not running", () => !EditorClock.IsRunning);
        }

        [Test]
        public void TestSnappedSeeking()
        {
            AddStep("seek to 0", () => EditorClock.Seek(0));
            AddAssert("time is 0", () => EditorClock.CurrentTime == 0);

            pressAndCheckTime(Key.Right, 1000);
            pressAndCheckTime(Key.Right, 2000);
            pressAndCheckTime(Key.Right, 2500);
            pressAndCheckTime(Key.Right, 3000);

            pressAndCheckTime(Key.Left, 2500);
            pressAndCheckTime(Key.Left, 2000);
            pressAndCheckTime(Key.Left, 1000);
        }

        [Test]
        public void TestSnappedSeekingAfterControlPointChange()
        {
            AddStep("seek to 0", () => EditorClock.Seek(0));
            AddAssert("time is 0", () => EditorClock.CurrentTime == 0);

            pressAndCheckTime(Key.Right, 1000);
            pressAndCheckTime(Key.Right, 2000);
            pressAndCheckTime(Key.Right, 2500);
            pressAndCheckTime(Key.Right, 3000);

            AddStep("remove 2nd timing point", () =>
            {
                EditorBeatmap.BeginChange();
                var group = EditorBeatmap.ControlPointInfo.GroupAt(2000);
                EditorBeatmap.ControlPointInfo.RemoveGroup(group);
                EditorBeatmap.EndChange();
            });

            pressAndCheckTime(Key.Left, 2000);
            pressAndCheckTime(Key.Left, 1000);

            pressAndCheckTime(Key.Right, 2000);
            pressAndCheckTime(Key.Right, 3000);
        }

        [Test]
        public void TestSeekBetweenControlPoints()
        {
            AddStep("seek to 0", () => EditorClock.Seek(0));
            AddAssert("time is 0", () => EditorClock.CurrentTime == 0);

            // already at first control point, noop
            pressAndCheckTime(Key.Up, 0);

            pressAndCheckTime(Key.Down, 2000);

            pressAndCheckTime(Key.Down, 20000);
            // at last control point, noop
            pressAndCheckTime(Key.Down, 20000);

            pressAndCheckTime(Key.Up, 2000);
            pressAndCheckTime(Key.Up, 0);
            pressAndCheckTime(Key.Up, 0);
        }

        [Test]
        public void TestSeekBetweenObjects()
        {
            AddStep("add objects", () =>
            {
                EditorBeatmap.Clear();
                EditorBeatmap.AddRange(new[]
                {
                    new HitCircle { StartTime = 1000, },
                    new HitCircle { StartTime = 2250, },
                    new HitCircle { StartTime = 3600, },
                });
            });
            AddStep("seek to 0", () => EditorClock.Seek(0));

            pressAndCheckTime(Key.Right, 1000, Key.ControlLeft);
            pressAndCheckTime(Key.Right, 2250, Key.ControlLeft);
            pressAndCheckTime(Key.Right, 3600, Key.ControlLeft);
            pressAndCheckTime(Key.Right, 3600, Key.ControlLeft);
            pressAndCheckTime(Key.Left, 2250, Key.ControlLeft);
            pressAndCheckTime(Key.Left, 1000, Key.ControlLeft);
            pressAndCheckTime(Key.Left, 1000, Key.ControlLeft);
        }

        /// <summary>
        /// While playing, a notch of the wheel goes from white tick to white tick, as stable's editor does (2026-10-11).
        /// </summary>
        /// <remarks>
        /// It went back half a second whatever the tempo, to wherever that came to: a beat and a half at 180 BPM, and
        /// nowhere a tick is.
        ///
        /// It plays on from where the wheel took it until the next step stops it, about half a second here. So the white
        /// ticks are put ten seconds apart from 100000, with the ticks at quarters of that: the first tick after the white
        /// tick at 110000 is at 112500, and the half second is nothing to any of it.
        /// </remarks>
        [TestCase(111000, 1, 100000, Description = "back, before the first tick after a white tick has gone by: the white tick before it")]
        [TestCase(114000, 1, 110000, Description = "back, after it: that white tick")]
        [TestCase(111000, -1, 120000, Description = "forward: the next white tick")]
        public void TestTheWheelGoesByWhiteTicksWhilePlaying(double from, int notches, double expected)
        {
            AddStep("a beat ten seconds long from 100000", () => EditorBeatmap.ControlPointInfo.Add(100000, new TimingControlPoint { BeatLength = 10000 }));
            AddStep("ticks at quarters", () => ((BindableBeatDivisor)Editor.Dependencies.Get(typeof(BindableBeatDivisor))).Value = 4);
            AddStep("move over the editor", () => InputManager.MoveMouseTo(Editor));
            AddStep($"seek to {from}", () => EditorClock.Seek(from));

            AddStep("play", () => EditorClock.Start());
            AddUntilStep("playing", () => EditorClock.IsRunning);
            AddStep("turn the wheel", () => InputManager.ScrollVerticalBy(notches));
            AddStep("stop", () => EditorClock.Stop());

            AddAssert($"just past {expected}", () => EditorClock.CurrentTime, () => Is.InRange(expected, expected + 2000));
        }

        /// <summary>
        /// The arrow keys go the same way while playing (2026-10-11).
        /// </summary>
        [TestCase(111000, Key.Left, 100000)]
        [TestCase(114000, Key.Left, 110000)]
        [TestCase(111000, Key.Right, 120000)]
        public void TestTheArrowKeysGoByWhiteTicksWhilePlaying(double from, Key key, double expected)
        {
            AddStep("a beat ten seconds long from 100000", () => EditorBeatmap.ControlPointInfo.Add(100000, new TimingControlPoint { BeatLength = 10000 }));
            AddStep("ticks at quarters", () => ((BindableBeatDivisor)Editor.Dependencies.Get(typeof(BindableBeatDivisor))).Value = 4);
            AddStep($"seek to {from}", () => EditorClock.Seek(from));

            AddStep("play", () => EditorClock.Start());
            AddUntilStep("playing", () => EditorClock.IsRunning);
            AddStep($"press {key}", () => InputManager.Key(key));
            AddStep("stop", () => EditorClock.Stop());

            AddAssert($"just past {expected}", () => EditorClock.CurrentTime, () => Is.InRange(expected, expected + 2000));
        }

        /// <summary>
        /// Held, the left key goes back a white tick each time it repeats (2026-10-11).
        /// </summary>
        /// <remarks>
        /// With beats ten seconds long, three repeats are back past where they start at 100000. As it was, a repeat went
        /// back half a second whatever the tempo, and a second and a half of them got nowhere near.
        /// </remarks>
        [Test]
        public void TestHoldingLeftGoesBackAWhiteTickARepeat()
        {
            AddStep("a beat ten seconds long from 100000", () => EditorBeatmap.ControlPointInfo.Add(100000, new TimingControlPoint { BeatLength = 10000 }));
            AddStep("ticks at quarters", () => ((BindableBeatDivisor)Editor.Dependencies.Get(typeof(BindableBeatDivisor))).Value = 4);
            AddStep("seek to 131000", () => EditorClock.Seek(131000));

            holdLeftWhilePlaying();

            AddAssert("back past where the long beats start", () => EditorClock.CurrentTime, () => Is.LessThan(100000));
        }

        /// <summary>
        /// A repeat of the held key goes on from the white tick the one before it went to, not from the playhead.
        /// </summary>
        /// <remarks>
        /// Going by the playhead, a repeat that comes after the first tick past that white tick has gone by goes back to
        /// the same white tick, over and over: wherever ticks are shorter than the time between two repeats, which at
        /// sixteenths is any tempo above 54 BPM.
        ///
        /// The press and its repeat are handed over here rather than typed, with the time between them put in by a seek:
        /// how far a test plays on between two repeats is not something it can count on.
        /// </remarks>
        [Test]
        public void TestARepeatGoesOnFromTheWhiteTickTheLastWentTo()
        {
            AddStep("a beat ten seconds long from 100000", () => EditorBeatmap.ControlPointInfo.Add(100000, new TimingControlPoint { BeatLength = 10000 }));
            AddStep("ticks at quarters", () => ((BindableBeatDivisor)Editor.Dependencies.Get(typeof(BindableBeatDivisor))).Value = 4);
            AddStep("seek to 134000", () => EditorClock.Seek(134000));

            AddStep("play", () => EditorClock.Start());
            AddUntilStep("playing", () => EditorClock.IsRunning);

            AddStep("press left, play on past the first tick, and repeat", () =>
            {
                // past the first tick after 130000, which is at 132500: back to 130000.
                Editor.OnPressed(new KeyBindingPressEvent<GlobalAction>(InputManager.CurrentState, GlobalAction.EditorSeekBackwards));

                // four seconds on, the first tick after it has gone by again.
                EditorClock.Seek(134000);
                Editor.OnPressed(new KeyBindingPressEvent<GlobalAction>(InputManager.CurrentState, GlobalAction.EditorSeekBackwards, true));

                EditorClock.Stop();
            });

            AddAssert("at the white tick before the one the press went to", () => EditorClock.CurrentTime, () => Is.InRange(120000, 122000));
        }

        private void holdLeftWhilePlaying()
        {
            AddStep("play", () => EditorClock.Start());
            AddUntilStep("playing", () => EditorClock.IsRunning);
            AddStep("hold left", () => InputManager.PressKey(Key.Left));
            AddWaitStep("keep it held", 15);
            AddStep("release, and stop", () =>
            {
                InputManager.ReleaseKey(Key.Left);
                EditorClock.Stop();
            });
        }

        /// <summary>
        /// Stopped, it goes tick by tick as it always has.
        /// </summary>
        [Test]
        public void TestTheWheelStillGoesTickByTickWhileStopped()
        {
            AddStep("ticks at quarters", () => ((BindableBeatDivisor)Editor.Dependencies.Get(typeof(BindableBeatDivisor))).Value = 4);
            AddStep("move over the editor", () => InputManager.MoveMouseTo(Editor));
            AddStep("seek to 2500", () => EditorClock.Seek(2500));

            AddStep("turn the wheel back", () => InputManager.ScrollVerticalBy(1));
            AddUntilStep("a tick back", () => EditorClock.CurrentTime, () => Is.EqualTo(2375).Within(1));
        }

        private void pressAndCheckTime(Key key, double expectedTime, params Key[] modifiers)
        {
            AddStep($"press {key} with {(modifiers.Any() ? string.Join(',', modifiers) : "no modifiers")}", () =>
            {
                foreach (var modifier in modifiers)
                    InputManager.PressKey(modifier);

                InputManager.Key(key);

                foreach (var modifier in modifiers)
                    InputManager.ReleaseKey(modifier);
            });
            AddUntilStep($"time is {expectedTime}", () => EditorClock.CurrentTime, () => Is.EqualTo(expectedTime).Within(1));
        }
    }
}
