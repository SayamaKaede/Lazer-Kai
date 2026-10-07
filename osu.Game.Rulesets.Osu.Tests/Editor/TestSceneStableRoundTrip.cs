// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Testing;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Beatmaps.Formats;
using osu.Game.Configuration;
using osu.Game.Database;
using osu.Game.Graphics.UserInterface;
using osu.Game.IO;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Osu.Edit;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.UI;
using osu.Game.Tests.Beatmaps;
using osuTK;
using osuTK.Input;

namespace osu.Game.Rulesets.Osu.Tests.Editor
{
    /// <summary>
    /// What is made in this editor, written out for stable and read back the way stable reads it, is what it was.
    /// </summary>
    /// <remarks>
    /// Stable cannot be run here, so this is the half of going back and forth between the two that can be held to: every
    /// way a beatmap leaves for stable -- a save kept in stable's format, an export, a conversion -- goes through
    /// <see cref="LegacyBeatmapExporter.ConvertForStable"/> and stable's format, and the file is read back with the
    /// coordinates cut to whole numbers, as stable does. Positions, stacks and the shape of every slider are compared.
    /// Times are only held to within a millisecond: the export cuts them to whole ones while the editor keeps the
    /// fraction, which was left for another time (2026-10-05).
    /// </remarks>
    [TestFixture]
    public partial class TestSceneStableRoundTrip : TestSceneOsuEditor
    {
        private const double first_beat = 978;

        // 178 BPM: a beat that is never a whole number of milliseconds, as most are.
        private const double beat_length = 337.07865168539325;

        [Resolved]
        private OsuConfigManager config { get; set; } = null!;

        private OsuPlayfield playfield = null!;

        protected override IBeatmap CreateBeatmap(RulesetInfo ruleset)
        {
            var beatmap = new TestBeatmap(Ruleset.Value, false);

            beatmap.ControlPointInfo = new ControlPointInfo();
            beatmap.ControlPointInfo.Add(first_beat, new TimingControlPoint { BeatLength = beat_length });
            beatmap.Difficulty.CircleSize = 4;

            return beatmap;
        }

        public override void SetUpSteps()
        {
            base.SetUpSteps();

            AddStep("whole coordinates, as they are out of the box", () => config.SetValue(OsuSetting.EditorIntegerPositions, true));
            AddStep("get playfield", () => playfield = Editor.ChildrenOfType<OsuPlayfield>().First());
            AddStep("no distance snap", () => Editor.ChildrenOfType<OsuHitObjectComposer>().Single().DistanceSnapProvider.DistanceSnapToggle.Value = TernaryState.False);
        }

        [Test]
        public void TestCirclesPlacedAndStacked()
        {
            AddStep("pick the circle tool", () => InputManager.Key(Key.Number2));

            placeCircleAt(1, new Vector2(128.6f, 192.7f));
            placeCircleAt(2, new Vector2(129.2f, 193.1f));
            placeCircleAt(3, new Vector2(300.3f, 100.8f));

            AddAssert("three circles", () => EditorBeatmap.HitObjects.Count, () => Is.EqualTo(3));
            AddAssert("two of them stacked", () => EditorBeatmap.HitObjects.OfType<OsuHitObject>().Count(h => h.StackHeight != 0), () => Is.EqualTo(1));

            assertTheRoundTripChangesNothing();
        }

        [TestCase("linear")]
        [TestCase("linear with a reverse")]
        [TestCase("perfect curve")]
        [TestCase("bezier")]
        [TestCase("bezier with a red anchor")]
        [TestCase("freehand")]
        public void TestCirclesStackedOnASliderEnd(string shape)
        {
            Slider slider = null!;

            AddStep($"add a {shape} slider", () => EditorBeatmap.Add(slider = new Slider
            {
                StartTime = first_beat + beat_length,
                Position = new Vector2(156, 192),
                RepeatCount = shape == "linear with a reverse" ? 1 : 0,
                Path = new SliderPath(anchorsOf(shape)),
            }));

            AddStep("pick the circle tool", () => InputManager.Key(Key.Number2));

            for (int i = 0; i < 2; i++)
            {
                int after = i;

                AddStep($"seek to beat {after + 1} after its end", () => EditorClock.Seek(beatAfter(slider.GetEndTime()) + after * beat_length));
                AddStep($"aim at its end ({after + 1})", () => InputManager.MoveMouseTo(playfield.GamefieldToScreenSpace(slider.EndPosition + new Vector2(1, 0))));
                AddStep($"place ({after + 1})", () => InputManager.Click(MouseButton.Left));
            }

            AddAssert("both circles are stacked on the end", () => EditorBeatmap.HitObjects.OfType<HitCircle>().Select(c => c.StackHeight), () => Has.All.LessThan(0));

            assertTheRoundTripChangesNothing();
        }

        [Test]
        public void TestARotatedSelection()
        {
            AddStep("add a slider and a circle after it", () =>
            {
                EditorBeatmap.Add(new Slider
                {
                    StartTime = first_beat + beat_length,
                    Position = new Vector2(156, 192),
                    Path = new SliderPath(anchorsOf("bezier")),
                });

                EditorBeatmap.Add(new HitCircle { StartTime = first_beat + beat_length * 4, Position = new Vector2(320, 240) });

                EditorBeatmap.SelectedHitObjects.Clear();
                EditorBeatmap.SelectedHitObjects.AddRange(EditorBeatmap.HitObjects);
            });

            AddStep("turn it", () =>
            {
                var rotation = Editor.ChildrenOfType<OsuSelectionRotationHandler>().Single();

                rotation.Begin();
                rotation.Update(23);
                rotation.Commit();
            });

            assertTheRoundTripChangesNothing();
        }

        private void placeCircleAt(int beat, Vector2 position)
        {
            AddStep($"seek to beat {beat}", () => EditorClock.Seek(first_beat + beat * beat_length));
            AddStep($"aim at {position}", () => InputManager.MoveMouseTo(playfield.GamefieldToScreenSpace(position)));
            AddStep($"place on beat {beat}", () => InputManager.Click(MouseButton.Left));
        }

        private void assertTheRoundTripChangesNothing()
        {
            IBeatmap readBack = null!;

            AddStep("write it for stable and read it back", () => readBack = writtenForStableAndReadBack());

            AddAssert("as many objects", () => readBack.HitObjects.Count, () => Is.EqualTo(EditorBeatmap.HitObjects.Count));
            AddAssert("each where it was", () => positionsOf(readBack), () => Is.EqualTo(positionsOf(EditorBeatmap)));
            AddAssert("each stacked as it was", () => stackHeightsOf(readBack), () => Is.EqualTo(stackHeightsOf(EditorBeatmap)));
            AddAssert("each slider the shape it was", () => largestShapeDifference(EditorBeatmap, readBack), () => Is.LessThan(0.5f));
            AddAssert("each within a millisecond of when it was", () => largestTimeDifference(EditorBeatmap, readBack), () => Is.LessThan(1));
        }

        /// <summary>
        /// What the editor saves, through what every way to stable does to it, read back as stable reads it.
        /// </summary>
        private IBeatmap writtenForStableAndReadBack()
        {
            byte[] saved;

            using (var stream = new MemoryStream())
            {
                using (var writer = new StreamWriter(stream, Encoding.UTF8, 1024, true))
                    new LegacyBeatmapEncoder(EditorBeatmap.PlayableBeatmap, null, null).Encode(writer);

                saved = stream.ToArray();
            }

            using var forStable = LegacyBeatmapExporter.EncodeForStable(() => new MemoryStream(saved), EditorBeatmap.BeatmapInfo, LegacyBeatmapExporter.ConvertForStable)!;
            using var reader = new LineBufferedReader(forStable);

            var decoded = osu.Game.Beatmaps.Formats.Decoder.GetDecoder<Beatmap>(reader).Decode(reader);

            return new FlatWorkingBeatmap(decoded).GetPlayableBeatmap(new OsuRuleset().RulesetInfo);
        }

        private static PathControlPoint[] anchorsOf(string shape)
        {
            switch (shape)
            {
                case "linear":
                case "linear with a reverse":
                    return new[] { new PathControlPoint(Vector2.Zero, PathType.LINEAR), new PathControlPoint(new Vector2(100, 7)) };

                case "perfect curve":
                    return new[] { new PathControlPoint(Vector2.Zero, PathType.PERFECT_CURVE), new PathControlPoint(new Vector2(50, -40)), new PathControlPoint(new Vector2(100, 3)) };

                case "bezier":
                    return new[] { new PathControlPoint(Vector2.Zero, PathType.BEZIER), new PathControlPoint(new Vector2(40, -60)), new PathControlPoint(new Vector2(103, 5)) };

                case "bezier with a red anchor":
                    return new[]
                    {
                        new PathControlPoint(Vector2.Zero, PathType.BEZIER), new PathControlPoint(new Vector2(40, -40)),
                        new PathControlPoint(new Vector2(60, 0), PathType.BEZIER), new PathControlPoint(new Vector2(101, 33)),
                    };

                // what drawing a slider by hand makes.
                case "freehand":
                    return new[]
                    {
                        new PathControlPoint(Vector2.Zero, PathType.BSpline(4)), new PathControlPoint(new Vector2(30, -40)), new PathControlPoint(new Vector2(60, -10)),
                        new PathControlPoint(new Vector2(90, -50)), new PathControlPoint(new Vector2(121, 3)),
                    };

                default:
                    throw new ArgumentOutOfRangeException(nameof(shape), shape, null);
            }
        }

        private static double beatAfter(double time) => first_beat + (Math.Floor((time - first_beat) / beat_length) + 1) * beat_length;

        private static Vector2[] positionsOf(IBeatmap beatmap) => beatmap.HitObjects.OfType<OsuHitObject>().Select(h => h.Position).ToArray();

        private static int[] stackHeightsOf(IBeatmap beatmap) => beatmap.HitObjects.OfType<OsuHitObject>().Select(h => h.StackHeight).ToArray();

        private static double largestTimeDifference(IBeatmap made, IBeatmap readBack) =>
            made.HitObjects.Zip(readBack.HitObjects, (a, b) => Math.Max(Math.Abs(a.StartTime - b.StartTime), Math.Abs(a.GetEndTime() - b.GetEndTime()))).DefaultIfEmpty().Max();

        /// <summary>
        /// The furthest any slider's path, from its head to its end, is from where it was.
        /// </summary>
        private static float largestShapeDifference(IBeatmap made, IBeatmap readBack)
        {
            var differences = new List<float> { 0 };

            foreach (var (a, b) in made.HitObjects.OfType<Slider>().Zip(readBack.HitObjects.OfType<Slider>()))
            {
                for (int i = 0; i <= 20; i++)
                    differences.Add(Vector2.Distance(a.Position + a.Path.PositionAt(i / 20.0), b.Position + b.Path.PositionAt(i / 20.0)));

                differences.Add(Vector2.Distance(a.EndPosition, b.EndPosition));
            }

            return differences.Max();
        }
    }
}
