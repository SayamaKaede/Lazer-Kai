// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Testing;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Configuration;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.Skinning.Default;

using osu.Game.Tests.Beatmaps;
using osuTK;

namespace osu.Game.Rulesets.Osu.Tests.Editor
{
    /// <summary>
    /// Leaving kiai's flashing out of the editor, where it is a distraction rather than a flourish.
    /// </summary>
    public partial class TestSceneHideKiaiFlash : TestSceneOsuEditor
    {
        protected override IBeatmap CreateBeatmap(RulesetInfo ruleset) => new TestBeatmap(ruleset);

        [Resolved]
        private OsuConfigManager config { get; set; } = null!;

        [Test]
        public void TestTheFlashStopsFiring()
        {
            AddStep("put the whole beatmap in kiai", () =>
            {
                EditorBeatmap.ControlPointInfo.Add(0, new EffectControlPoint { KiaiMode = true });

                EditorBeatmap.RemoveRange(EditorBeatmap.HitObjects.ToArray());

                for (double time = 2000; time < 12000; time += 500)
                    EditorBeatmap.Add(new HitCircle { StartTime = time, Position = new Vector2(100, 100) });
            });

            setHidden(false);

            AddStep("play from the start of the kiai", () =>
            {
                EditorClock.Seek(1800);
                EditorClock.Start();
            });

            AddUntilStep("something flashed", () => brightestFlash(), () => Is.GreaterThan(0.01f));

            setHidden(true);
            AddStep("let the flash fade out", () => { });
            AddUntilStep("nothing is lit any more", () => brightestFlash(), () => Is.LessThan(0.01f));

            // held for a while: a beat passing without lighting anything is the whole point.
            AddWaitStep("keep playing", 20);
            AddAssert("still nothing", () => brightestFlash(), () => Is.LessThan(0.01f));

            setHidden(false);
            AddUntilStep("flashing again", () => brightestFlash(), () => Is.GreaterThan(0.01f));

            AddStep("stop", () => EditorClock.Stop());
        }

        private void setHidden(bool hidden)
            => AddStep(hidden ? "hide the kiai flash" : "show the kiai flash", () => config.SetValue(OsuSetting.EditorHideKiaiFlash, hidden));

        /// <summary>
        /// The brightest any kiai flash in the composer is lit right now.
        /// </summary>
        private float brightestFlash() => Editor.ChildrenOfType<KiaiFlash>()
                                                .SelectMany(f => f.ChildrenOfType<Box>())
                                                .Select(b => b.Alpha)
                                                .DefaultIfEmpty(0)
                                                .Max();


    }
}
