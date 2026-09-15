// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Testing;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Osu;
using osu.Game.Screens.Edit.Components;

namespace osu.Game.Tests.Visual.Editing
{
    /// <summary>
    /// The readouts under the timestamp at the bottom left of the editor.
    /// </summary>
    public partial class TestSceneEditorTimeInfoReadouts : EditorTestScene
    {
        protected override Ruleset CreateEditorRuleset() => new OsuRuleset();

        private TimeInfoContainer timeInfo => Editor.ChildrenOfType<TimeInfoContainer>().Single();

        private bool showing(string text) => timeInfo.ChildrenOfType<SpriteText>().Any(t => t.IsPresent && t.Text.ToString() == text);

        [SetUpSteps]
        public void SetUpClock()
        {
            AddStep("stop the clock", () => EditorClock.Stop());
        }

        [Test]
        public void TestProgressShowsATenthOfAPercent()
        {
            AddStep("seek to 12.34%", () => EditorClock.Seek(EditorClock.TrackLength * 0.1234));
            AddUntilStep("progress reads 12.3%", () => showing("12.3%"));
        }

        [Test]
        public void TestKiaiIsShownOnlyDuringKiai()
        {
            AddStep("kiai from 10s to 20s only", () =>
            {
                var controlPoints = EditorBeatmap.ControlPointInfo;

                foreach (var group in controlPoints.Groups.ToArray())
                {
                    foreach (var point in group.ControlPoints.OfType<EffectControlPoint>().ToArray())
                        group.Remove(point);
                }

                controlPoints.Add(10000, new EffectControlPoint { KiaiMode = true });
                controlPoints.Add(20000, new EffectControlPoint());
            });

            AddStep("seek into kiai", () => EditorClock.Seek(15000));
            AddUntilStep("kiai is shown", () => showing("Kiai"));

            AddStep("seek past kiai", () => EditorClock.Seek(25000));
            AddUntilStep("kiai is not shown", () => !showing("Kiai"));

            AddStep("seek before kiai", () => EditorClock.Seek(5000));
            AddUntilStep("kiai is still not shown", () => !showing("Kiai"));
        }
    }
}
