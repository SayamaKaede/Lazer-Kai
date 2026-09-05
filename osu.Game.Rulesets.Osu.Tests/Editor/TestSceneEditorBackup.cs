// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Testing;
using osu.Game.Localisation;
using osu.Game.Overlays;
using osu.Game.Overlays.Notifications;
using osu.Game.Screens.Edit.Components.Menus;
using osu.Game.Tests.Visual;

namespace osu.Game.Rulesets.Osu.Tests.Editor
{
    /// <summary>
    /// Asking for a backup says what happened, including when there was nothing to write.
    /// </summary>
    public partial class TestSceneEditorBackup : TestSceneOsuEditor
    {
        [Cached(typeof(INotificationOverlay))]
        private readonly RecordingNotificationOverlay notifications = new RecordingNotificationOverlay();

        [SetUpSteps]
        public void ClearWhatWasSaid()
        {
            AddStep("forget what was said", () => notifications.Said.Clear());
        }

        /// <summary>
        /// A keypress that writes nothing and says nothing cannot be told apart from one that never arrived.
        /// </summary>
        [Test]
        public void TestAskingAgainWithNothingChangedSaysSo()
        {
            AddStep("change something", () => EditorBeatmap.Add(new Objects.HitCircle { StartTime = 4000 }));

            AddStep("ask for a backup", askForABackup);
            AddUntilStep("it was written", () => notifications.Said, () => Does.Contain(EditorStrings.BackupWritten.ToString()));

            AddStep("forget what was said", () => notifications.Said.Clear());

            AddStep("ask again without changing anything", askForABackup);
            AddUntilStep("it says the backup is already up to date",
                () => notifications.Said, () => Does.Contain(EditorStrings.BackupUpToDate.ToString()));
        }

        private void askForABackup()
            => flatten(Editor.ChildrenOfType<EditorMenuBar>().Single().Items)
                .Single(i => i.Text.Value.Equals(EditorStrings.BackupNow))
                .Action.Value?.Invoke();

        /// <summary>
        /// The entry sits inside a submenu, so the bar's own items are only the first level of the way there.
        /// </summary>
        private static IEnumerable<MenuItem> flatten(IEnumerable<MenuItem> items)
            => items.SelectMany(i => new[] { i }.Concat(flatten(i.Items)));

        /// <summary>
        /// Keeps what was posted. What is being checked is that something was said and what it was, not how it looks
        /// on screen, so this stands in for the real overlay rather than being one.
        /// </summary>
        private class RecordingNotificationOverlay : INotificationOverlay
        {
            public readonly List<string> Said = new List<string>();

            private readonly List<Notification> posted = new List<Notification>();

            public void Post(Notification notification)
            {
                Said.Add(notification.Text.ToString());
                posted.Add(notification);
            }

            public void Hide()
            {
            }

            public IBindable<int> UnreadCount { get; } = new BindableInt();

            public IEnumerable<Notification> AllNotifications => posted;
        }
    }
}
