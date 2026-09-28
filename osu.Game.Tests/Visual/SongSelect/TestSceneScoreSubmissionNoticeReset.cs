// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Screens;
using osu.Game.Configuration;

namespace osu.Game.Tests.Visual.SongSelect
{
    /// <summary>
    /// The notice that a score will not be submitted is said once until song select is come back to (2026-09-29
    /// request), so song select is what says it may be said again.
    /// </summary>
    public partial class TestSceneScoreSubmissionNoticeReset : SongSelectTestScene
    {
        [Resolved]
        private SessionStatics statics { get; set; } = null!;

        [Test]
        public void TestComingToSongSelectClearsIt()
        {
            AddStep("it was said", () => statics.SetValue(Static.ScoreSubmissionFailureShown, true));

            LoadSongSelect();

            AddAssert("it may be said again", () => statics.Get<bool>(Static.ScoreSubmissionFailureShown), () => Is.False);
        }

        [Test]
        public void TestComingBackToSongSelectClearsIt()
        {
            LoadSongSelect();

            AddStep("push a screen on top", () => Stack.Push(new TestSceneOsuScreenStack.TestScreen("gameplay, say")));
            AddUntilStep("it is on top", () => Stack.CurrentScreen != SongSelect);

            AddStep("it was said", () => statics.SetValue(Static.ScoreSubmissionFailureShown, true));

            AddStep("go back", () => Stack.CurrentScreen.Exit());
            AddUntilStep("song select is current", () => Stack.CurrentScreen == SongSelect);

            AddAssert("it may be said again", () => statics.Get<bool>(Static.ScoreSubmissionFailureShown), () => Is.False);
        }
    }
}
