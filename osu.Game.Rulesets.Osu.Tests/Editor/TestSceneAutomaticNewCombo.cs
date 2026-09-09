// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using NUnit.Framework;
using osu.Game.Rulesets.Objects;
using osu.Framework.Allocation;
using osu.Framework.Testing;
using osu.Game.Configuration;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Osu.UI;
using osu.Game.Tests.Visual;
using osuTK;
using osuTK.Input;

namespace osu.Game.Rulesets.Osu.Tests.Editor
{
    /// <summary>
    /// Starting combos while placing, through the editor rather than through the rule on its own.
    /// </summary>
    public partial class TestSceneAutomaticNewCombo : TestSceneOsuEditor
    {
        [Resolved]
        private OsuConfigManager config { get; set; } = null!;

        [SetUpSteps]
        public void StartFromNothing()
        {
            // the processor gives the first object after a break a combo of its own, whatever anything else says,
            // and an almost empty beatmap is nothing but breaks. Turned off so that what is being read here is this
            // feature rather than that one.
            AddStep("stop generating breaks", () => config.SetValue(OsuSetting.EditorAutoGenerateBreaks, false));

            AddStep("clear the beatmap", () => EditorBeatmap.Clear());

            // something at the start so that what gets placed later is never the beatmap's own first object.
            AddStep("open the beatmap with a bar of objects", () =>
            {
                var timing = EditorBeatmap.ControlPointInfo.TimingPoints.First();

                for (int i = 0; i < timing.TimeSignature.Numerator; i++)
                    EditorBeatmap.Add(new Objects.HitCircle { StartTime = timing.Time + timing.BeatLength * i, NewCombo = false });
            });

            AddStep("take the circle tool", () => InputManager.Key(Key.Number2));
            AddStep("move over the playfield", () => InputManager.MoveMouseTo(playfield().ScreenSpaceDrawQuad.Centre));
        }

        [TearDownSteps]
        public void TurnItOffAgain()
        {
            AddStep("stop starting combos", () => config.GetBindable<bool>(OsuSetting.EditorAutoNewCombo).SetDefault());
            AddStep("back to the stock interval", () => config.GetBindable<AutoNewComboInterval>(OsuSetting.EditorAutoNewComboInterval).SetDefault());
            AddStep("generate breaks again", () => config.GetBindable<bool>(OsuSetting.EditorAutoGenerateBreaks).SetDefault());
        }

        /// <summary>
        /// Off to begin with, so a beatmap being worked on reads the same as it did before this existed.
        /// </summary>
        [Test]
        public void TestNothingHappensUntilItIsTurnedOn()
        {
            AddStep("go to a bar line", seekToABarLine);
            AddStep("place one", place);

            AddAssert("no combo was started", () => firstPlaced().NewCombo, () => Is.False);
        }

        /// <summary>
        /// The point of it: the first object of a bar opens the bar, without pressing anything.
        /// </summary>
        [Test]
        public void TestTheFirstObjectOfABarGetsOne()
        {
            AddStep("start combos while placing", () => config.SetValue(OsuSetting.EditorAutoNewCombo, true));

            AddStep("go to a bar line", seekToABarLine);
            AddStep("place one", place);

            AddAssert("a combo was started", () => firstPlaced().NewCombo, () => Is.True);
        }

        /// <summary>
        /// And only the first: the rest of the bar carries on, which is what stops a combo starting under every
        /// object placed.
        /// </summary>
        [Test]
        public void TestTheNextObjectInTheSameBarDoesNot()
        {
            AddStep("start combos while placing", () => config.SetValue(OsuSetting.EditorAutoNewCombo, true));

            AddStep("go to a bar line", seekToABarLine);
            AddStep("place one", place);
            AddStep("move on a beat", () => EditorClock.Seek(EditorClock.CurrentTime + beatLength()));
            AddStep("place another", place);

            AddAssert("the second did not open one", () => firstPlaced().NewCombo, () => Is.False);
        }

        /// <summary>
        /// Nothing already in the beatmap is touched, whatever the rule would have said about it.
        /// </summary>
        [Test]
        public void TestWhatIsAlreadyThereIsLeftAlone()
        {
            double onTheLine = 0;

            AddStep("put one on a bar line by hand, without a combo", () =>
            {
                seekToABarLine();
                onTheLine = EditorClock.CurrentTime;
                EditorBeatmap.Add(new Objects.HitCircle { StartTime = onTheLine, NewCombo = false });
            });

            AddStep("start combos while placing", () => config.SetValue(OsuSetting.EditorAutoNewCombo, true));
            AddStep("move on a beat", () => EditorClock.Seek(EditorClock.CurrentTime + beatLength()));
            AddStep("place one", place);

            AddAssert("the one already there was not given a combo", () => at(onTheLine).NewCombo, () => Is.False);
        }

        private void seekToABarLine()
        {
            var timing = EditorBeatmap.ControlPointInfo.TimingPoints.First();

            // four bars in: clear of the bar the beatmap opens with, and empty.
            EditorClock.Seek(timing.Time + timing.BeatLength * timing.TimeSignature.Numerator * 4);
        }

        private double beatLength() => EditorBeatmap.ControlPointInfo.TimingPointAt(EditorClock.CurrentTime).BeatLength;

        private void place() => InputManager.Click(MouseButton.Left);

        /// <remarks>
        /// The last one by time, which is what was just put down: the beatmap opens with a bar of its own so that
        /// nothing being placed here is ever the first object in it.
        /// </remarks>
        private IHasComboInformation at(double time)
            => EditorBeatmap.HitObjects.OfType<IHasComboInformation>().Single(h => Math.Abs(((HitObject)h).StartTime - time) < 1);

        private IHasComboInformation firstPlaced()
            => EditorBeatmap.HitObjects.OrderBy(h => h.StartTime).OfType<IHasComboInformation>().Last();

        private OsuPlayfield playfield() => Editor.ChildrenOfType<OsuPlayfield>().First();
    }
}
