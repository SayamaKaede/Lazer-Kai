// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics.Containers;
using osu.Framework.Testing;
using osu.Game.Audio;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Graphics.Sprites;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Screens.Edit;
using osu.Game.Screens.Edit.Compose.Components;
using osuTK;

namespace osu.Game.Tests.Visual.Editing
{
    /// <summary>
    /// What the composer's status panel says the hitsounds are at the playhead.
    /// </summary>
    /// <remarks>
    /// A slider's head, repeats and tail each carry their own bank and volume. The panel read the object's own samples
    /// and so showed the head's everywhere along it, which is exactly where a mapper looks to check what they just set
    /// on the tail.
    /// </remarks>
    [TestFixture]
    public partial class TestSceneComposeStatusSamples : EditorTestScene
    {
        protected override Ruleset CreateEditorRuleset() => new OsuRuleset();

        [SetUpSteps]
        public void SetUpSampleSteps()
        {
            AddStep("go to compose", () => Editor.Mode.Value = EditorScreenMode.Compose);
            AddUntilStep("the panel is up", () => Editor.ChildrenOfType<ComposeStatusDisplay>().Any());
        }

        private ComposeStatusDisplay status => Editor.ChildrenOfType<ComposeStatusDisplay>().Single();

        /// <summary>
        /// The value beside a row's label. Each row is a label and a value, so the row is found by its label.
        /// </summary>
        private string valueOf(string label)
        {
            foreach (var row in status.ChildrenOfType<Container>())
            {
                var texts = row.ChildrenOfType<OsuSpriteText>().ToArray();

                if (texts.Length == 2 && texts[0].Text.ToString() == label)
                    return texts[1].Text.ToString();
            }

            return string.Empty;
        }

        private Slider slider => EditorBeatmap.HitObjects.OfType<Slider>().Single();

        private void seekTo(double time) => AddStep($"seek to {time}", () => EditorClock.Seek(time));

        /// <summary>
        /// Seeks to a node of the slider. Its length comes from its path and velocity, so the times are read off the
        /// object rather than assumed.
        /// </summary>
        private void seekToNode(double node) => AddStep($"seek to node {node}", () =>
            EditorClock.Seek(slider.StartTime + slider.Duration / slider.SpanCount() * node));

        private void assertSamples(string normal, string addition, string volume)
        {
            AddUntilStep($"normal is {normal}", () => valueOf("Normal"), () => Is.EqualTo(normal));
            AddUntilStep($"addition is {addition}", () => valueOf("Addition"), () => Is.EqualTo(addition));
            AddUntilStep($"volume is {volume}", () => valueOf("Volume"), () => Is.EqualTo(volume));
        }

        [Test]
        public void TestEachNodeOfASliderIsReported()
        {
            // the head, the repeat and the tail are each set differently so none can be mistaken for another.
            seekToNode(0);
            assertSamples("normal", "-", "100%");

            seekToNode(1);
            assertSamples("soft", "drum", "50%");

            seekToNode(2);
            assertSamples("drum", "soft", "5%");
        }

        /// <remarks>
        /// Between two nodes the one already passed is still the last thing that played.
        /// </remarks>
        [Test]
        public void TestPartWayAlongASpanTheNodeBeforeItHolds()
        {
            seekToNode(0.5);
            assertSamples("normal", "-", "100%");

            seekToNode(1.5);
            assertSamples("soft", "drum", "50%");
        }

        /// <remarks>
        /// The panel keeps showing the last thing in effect rather than blanking between objects, so past the end of a
        /// slider that is the tail and not the head.
        /// </remarks>
        [Test]
        public void TestPastTheEndTheTailHolds()
        {
            seekToNode(2.5);
            assertSamples("drum", "soft", "5%");
        }

        [Test]
        public void TestACircleStillReportsItsOwnSamples()
        {
            seekTo(500);
            assertSamples("soft", "-", "70%");
        }

        [Test]
        public void TestNothingIsReportedBeforeTheFirstObject()
        {
            seekTo(0);
            assertSamples("-", "-", "-");
        }

        protected override IBeatmap CreateBeatmap(RulesetInfo ruleset)
        {
            var beatmap = new Beatmap
            {
                BeatmapInfo = { Ruleset = ruleset },
                HitObjects =
                {
                    new HitCircle
                    {
                        StartTime = 500,
                        Samples = samples("soft", null, 70),
                    },
                    new Slider
                    {
                        StartTime = 1000,
                        Path = new SliderPath(PathType.LINEAR, [Vector2.Zero, new Vector2(100, 0)]),
                        RepeatCount = 1,
                        Samples = samples("normal", null, 100),
                        NodeSamples = new List<IList<HitSampleInfo>>
                        {
                            samples("normal", null, 100),
                            samples("soft", "drum", 50),
                            samples("drum", "soft", 5),
                        },
                    },
                },
            };

            beatmap.ControlPointInfo.Add(0, new TimingControlPoint { BeatLength = 400 });

            // a slider's length comes from its velocity, which is nothing until the objects have been told about the
            // beatmap — and a slider of infinite length gives the test harness a track of infinite length.
            foreach (var hitObject in beatmap.HitObjects)
                hitObject.ApplyDefaults(beatmap.ControlPointInfo, beatmap.Difficulty);

            return beatmap;
        }

        private static List<HitSampleInfo> samples(string bank, string? additionBank, int volume)
        {
            var list = new List<HitSampleInfo> { new HitSampleInfo(HitSampleInfo.HIT_NORMAL, bank, volume: volume) };

            if (additionBank != null)
                // without this the addition reports "auto" rather than the bank it was given.
                list.Add(new HitSampleInfo(HitSampleInfo.HIT_CLAP, additionBank, volume: volume, editorAutoBank: false));

            return list;
        }
    }
}
