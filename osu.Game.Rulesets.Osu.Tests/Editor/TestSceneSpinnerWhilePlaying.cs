// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Testing;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.Objects.Drawables;
using osu.Game.Rulesets.Osu.UI;
using osu.Game.Tests.Beatmaps;

namespace osu.Game.Rulesets.Osu.Tests.Editor
{
    /// <summary>
    /// Playing the editor's clock over a spinner fills it a little at a time, the way autoplay does in gameplay -- every
    /// time, not only the first.
    /// </summary>
    /// <remarks>
    /// Measured on 2026-10-01, and the same on ppy/osu's own master: after playing over a spinner and going back to
    /// before it, the spin history kept what had been spun the first time. The spinner began full, its bonus at the
    /// maximum, and went on spinning from there.
    /// </remarks>
    [TestFixture]
    public partial class TestSceneSpinnerWhilePlaying : TestSceneOsuEditor
    {
        private const double spinner_start = 1000;
        private const double spinner_end = 4000;

        private List<Sample> samples = new List<Sample>();

        private Watcher? watcher;

        protected override IBeatmap CreateBeatmap(RulesetInfo ruleset)
        {
            var beatmap = new TestBeatmap(ruleset, false);

            // in the middle of the playfield, where a spinner is placed and where autoplay spins.
            beatmap.HitObjects.Add(new Spinner { StartTime = spinner_start, EndTime = spinner_end, Position = OsuPlayfield.BASE_SIZE / 2 });

            beatmap.BeatmapInfo.EditorTimestamp = 0;

            return beatmap;
        }

        public override void SetUpSteps()
        {
            base.SetUpSteps();

            // every frame: a step is polled far less often than the playfield is drawn.
            AddStep("watch the spinner", () =>
            {
                // a list of its own, so that a watcher left over from an earlier test could never write into this one's.
                var seen = samples = new List<Sample>();

                Add(watcher = new Watcher(() =>
                {
                    try
                    {
                        var spinner = spinnerOnScreen();

                        if (spinner != null)
                            seen.Add(new Sample(EditorClock.CurrentTime, spinner.Progress, spinner.CompletedFullSpins.Value, spinner.CurrentBonusScore, spinner.Judged));
                    }
                    catch (InvalidOperationException)
                    {
                        // the editor's drawables were being added or removed while they were counted: one sample missed.
                    }
                }));
            });
        }

        [TearDownSteps]
        public void StopWatching()
        {
            AddStep("stop watching the spinner", () => watcher?.Expire());
        }

        [Test]
        public void TestTheSpinnerFillsAsTheClockPlaysOverIt()
        {
            AddAssert("the editor opened before the spinner", () => EditorClock.CurrentTime, () => Is.LessThan(spinner_start));

            playOverTheSpinner();

            assertFilledAtItsOwnPace();
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TestTheSpinnerFillsAgainWhenPlayedOverASecondTime(bool smoothly)
        {
            playOverTheSpinner();

            AddStep("go back to before it", () =>
            {
                if (smoothly)
                    EditorClock.SeekSmoothlyTo(500);
                else
                    EditorClock.Seek(500);
            });
            AddUntilStep("arrived", () => EditorClock.CurrentTime < 560);

            // what was seen on the way back is the spinner as the first time left it, which going back past its start
            // is what puts right: only what is seen from here counts.
            AddStep("forget what was seen", () => samples.Clear());

            playOverTheSpinner();

            AddAssert("it was seen before it began", () => samples.Count(s => s.Time < spinner_start), () => Is.GreaterThan(5));
            AddAssert("it began empty", () => samples.Where(s => s.Time < spinner_start).Max(s => s.Progress), () => Is.Zero);
            AddAssert("with no bonus", () => samples.Where(s => s.Time < spinner_start).Max(s => s.BonusScore), () => Is.Zero);
            AddAssert("and no spins", () => samples.Where(s => s.Time < spinner_start).Max(s => s.Spins), () => Is.Zero);

            assertFilledAtItsOwnPace();
        }

        [Test]
        public void TestTheSpinnerIsEmptyOnceBackBeforeIt()
        {
            playOverTheSpinner();

            AddStep("go back to just before it", () => EditorClock.Seek(spinner_start - 100));
            AddUntilStep("it is on screen", () => spinnerOnScreen() != null);
            AddWaitStep("let it settle", 5);

            AddAssert("it is empty", () => spinnerOnScreen()!.Progress, () => Is.Zero);
            AddAssert("with no bonus", () => spinnerOnScreen()!.CurrentBonusScore, () => Is.Zero);
            AddAssert("and no spins", () => spinnerOnScreen()!.CompletedFullSpins.Value, () => Is.Zero);
        }

        private void playOverTheSpinner()
        {
            AddStep("play", () => EditorClock.Start());
            AddUntilStep("the clock is past the spinner", () => EditorClock.CurrentTime > spinner_end + 200);
            AddStep("stop", () => EditorClock.Stop());
        }

        /// <summary>
        /// What was seen while the clock went over the spinner: empty as it began, full some time before it ended, and
        /// in more than a few steps in between.
        /// </summary>
        private void assertFilledAtItsOwnPace()
        {
            // the first moments of it: nothing has been spun yet, whatever the pace
            AddAssert("it was seen as it began", () => samples.Count(s => s.Time > spinner_start && s.Time < spinner_start + 250), () => Is.GreaterThan(0));
            AddAssert("it was not full as soon as it began", () => samples.Where(s => s.Time > spinner_start && s.Time < spinner_start + 250).Max(s => s.Progress), () => Is.LessThan(0.5f));
            AddAssert("it was not judged as soon as it began", () => samples.Where(s => s.Time > spinner_start && s.Time < spinner_start + 250).Any(s => s.Judged), () => Is.False);

            AddAssert("it filled in more than a few steps", () => samples.Where(s => s.Time > spinner_start && s.Time < spinner_end).Select(s => s.Progress).Where(p => p > 0.02f && p < 0.98f).Distinct().Count(), () => Is.GreaterThan(5));
            AddAssert("its bonus went up in more than a few steps", () => samples.Where(s => s.Time > spinner_start && s.Time < spinner_end).Select(s => s.BonusScore).Distinct().Count(), () => Is.GreaterThan(3));
        }

        private DrawableSpinner? spinnerOnScreen() => Editor.ChildrenOfType<DrawableSpinner>().FirstOrDefault(s => s.IsLoaded);

        private readonly record struct Sample(double Time, float Progress, int Spins, double BonusScore, bool Judged);

        private partial class Watcher : Drawable
        {
            private readonly Action onUpdate;

            public Watcher(Action onUpdate)
            {
                this.onUpdate = onUpdate;
            }

            protected override void Update()
            {
                base.Update();
                onUpdate();
            }
        }
    }
}
