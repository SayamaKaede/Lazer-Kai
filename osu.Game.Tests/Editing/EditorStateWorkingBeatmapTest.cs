// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using osu.Framework.Audio.Track;
using osu.Framework.Extensions;
using osu.Framework.Graphics.Textures;
using osu.Game.Beatmaps;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Catch;
using osu.Game.Rulesets.Mania;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Osu.Beatmaps;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Taiko;
using osu.Game.Screens.Edit.DifficultyGraph;
using osu.Game.Skinning;
using osu.Game.Tests.Beatmaps;
using osuTK;

namespace osu.Game.Tests.Editing
{
    /// <summary>
    /// The difficulty graph works out the beatmap as it stands in the editor, in whichever ruleset it is.
    /// </summary>
    [TestFixture]
    public class EditorStateWorkingBeatmapTest
    {
        private static readonly Ruleset[] rulesets = { new OsuRuleset(), new TaikoRuleset(), new CatchRuleset(), new ManiaRuleset() };

        /// <summary>
        /// Reported through the error reports on 2026-09-25: opening a mania beatmap, the graph could not be worked out,
        /// its calculator wanting a mania beatmap (for its columns) and being handed a plain one.
        /// </summary>
        [TestCaseSource(nameof(rulesets))]
        public void TestTheGraphCanBeWorkedOut(Ruleset ruleset)
        {
            // what the editor holds: the beatmap made playable in the ruleset it is edited in.
            var editorState = new TestWorkingBeatmap(new TestBeatmap(ruleset.RulesetInfo)).GetPlayableBeatmap(ruleset.RulesetInfo);

            var working = new EditorStateWorkingBeatmap(editorState);

            Assert.DoesNotThrow(() => ruleset.CreateDifficultyCalculator(working).CalculateBreakdown(Array.Empty<Mod>()));
        }

        /// <summary>
        /// Reported through the error reports on 2026-09-29 (DOTNET-9), opening a beatmap with a hidden test play mod on:
        /// the composer puts the mods through the editor's objects as it loads, taking each one's defaults apart and
        /// building them again, while the graph reads those same objects. A slider's head is there for a moment without
        /// its hit windows, and the calculator finds it so.
        /// </summary>
        [Test]
        public void TestTheGraphIsNotReadingWhatTheEditorIsRebuilding()
        {
            var ruleset = new OsuRuleset();
            var editorState = new OsuBeatmap { BeatmapInfo = { Ruleset = ruleset.RulesetInfo } };

            for (int i = 0; i < 300; i++)
            {
                editorState.HitObjects.Add(new Slider
                {
                    StartTime = 1000 + i * 400,
                    Position = new Vector2(100 + i % 5 * 40, 100),
                    Path = new SliderPath(new[] { new PathControlPoint(Vector2.Zero, PathType.LINEAR), new PathControlPoint(new Vector2(100, 0)) }),
                    RepeatCount = 1,
                });
            }

            foreach (var hitObject in editorState.HitObjects)
                hitObject.ApplyDefaults(editorState.ControlPointInfo, editorState.Difficulty);

            var working = new EditorStateWorkingBeatmap(editorState);
            var rebuilding = true;

            var rebuilder = Task.Run(() =>
            {
                while (rebuilding)
                {
                    foreach (var hitObject in editorState.HitObjects)
                        hitObject.ApplyDefaults(editorState.ControlPointInfo, editorState.Difficulty);
                }
            });

            try
            {
                var deadline = DateTime.UtcNow.AddSeconds(5);

                Assert.DoesNotThrow(() =>
                {
                    while (DateTime.UtcNow < deadline)
                        ruleset.CreateDifficultyCalculator(working).CalculateBreakdown(Array.Empty<Mod>());
                });
            }
            finally
            {
                rebuilding = false;
                rebuilder.WaitSafely();
            }
        }

        /// <summary>
        /// Working it out on objects of its own must not change what comes out: sliders of different velocities, the
        /// difficulty they are worked out with, and the time between them all have to survive being written out.
        /// </summary>
        [TestCase(true, true)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(false, false)]
        public void TestTheGraphIsWhatTheObjectsThemselvesGive(bool varyVelocity, bool repeat)
        {
            var ruleset = new OsuRuleset();
            var editorState = new OsuBeatmap { BeatmapInfo = { Ruleset = ruleset.RulesetInfo } };

            editorState.Difficulty.OverallDifficulty = 8;
            editorState.Difficulty.ApproachRate = 9.3f;

            for (int i = 0; i < 60; i++)
            {
                editorState.HitObjects.Add(new Slider
                {
                    StartTime = 1000 + i * 300,
                    Position = new Vector2(60 + i % 7 * 50, 80 + i % 3 * 90),
                    Path = new SliderPath(new[] { new PathControlPoint(Vector2.Zero, PathType.LINEAR), new PathControlPoint(new Vector2(40 + i % 4 * 30, 20)) }),
                    RepeatCount = repeat ? i % 3 : 0,
                    SliderVelocityMultiplier = varyVelocity ? 0.6 + i % 4 * 0.5 : 1,
                });

                editorState.HitObjects.Add(new HitCircle { StartTime = 1000 + i * 300 + 150, Position = new Vector2(300 - i % 5 * 40, 200) });
            }

            foreach (var hitObject in editorState.HitObjects)
                hitObject.ApplyDefaults(editorState.ControlPointInfo, editorState.Difficulty);

            double[] onItsOwn = ruleset.CreateDifficultyCalculator(new EditorStateWorkingBeatmap(editorState)).CalculateBreakdown(Array.Empty<Mod>()).GetCombinedDifficulties();
            double[] directly = ruleset.CreateDifficultyCalculator(new SameObjectsWorkingBeatmap(editorState)).CalculateBreakdown(Array.Empty<Mod>()).GetCombinedDifficulties();

            Assert.That(onItsOwn, Is.Not.Empty);
            Assert.That(onItsOwn, Is.EqualTo(directly).Within(0.0001));
        }

        [Test]
        public void TestTheGraphHasObjectsOfItsOwn()
        {
            var ruleset = new OsuRuleset();
            var editorState = new OsuBeatmap { BeatmapInfo = { Ruleset = ruleset.RulesetInfo } };

            editorState.HitObjects.Add(new HitCircle { StartTime = 1000 });
            editorState.HitObjects.Add(new HitCircle { StartTime = 1500, Position = new Vector2(200, 100) });

            foreach (var hitObject in editorState.HitObjects)
                hitObject.ApplyDefaults(editorState.ControlPointInfo, editorState.Difficulty);

            var working = new EditorStateWorkingBeatmap(editorState);
            var snapshot = working.GetPlayableBeatmap(ruleset.RulesetInfo, Array.Empty<Mod>(), default);

            Assert.That(snapshot.HitObjects, Has.Count.EqualTo(2));
            Assert.That(snapshot.HitObjects.Any(h => editorState.HitObjects.Contains(h)), Is.False);
        }

        /// <summary>
        /// What the graph used to be handed: the editor's own objects.
        /// </summary>
        private class SameObjectsWorkingBeatmap : WorkingBeatmap
        {
            private readonly IBeatmap beatmap;

            public SameObjectsWorkingBeatmap(IBeatmap beatmap)
                : base(beatmap.BeatmapInfo, null)
            {
                this.beatmap = beatmap;
            }

            public override IBeatmap GetPlayableBeatmap(IRulesetInfo ruleset, IReadOnlyList<Mod> mods, CancellationToken cancellationToken) => beatmap;

            protected override IBeatmap GetBeatmap() => beatmap;

            public override Texture GetBackground() => throw new NotImplementedException();

            protected override Track GetBeatmapTrack() => throw new NotImplementedException();

            protected internal override ISkin GetSkin() => throw new NotImplementedException();

            public override Stream GetStream(string storagePath) => throw new NotImplementedException();
        }
    }
}
