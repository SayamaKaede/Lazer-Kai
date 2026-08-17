// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using System.Text;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Logging;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Drawables;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.UI;
using osu.Game.Screens.Edit;
using osu.Game.Screens.Edit.Components;

namespace osu.Game.Rulesets.Osu.Edit
{
    /// <summary>
    /// Temporary: writes what the editor believes is on screen to the log, so the numbers behind selection and click
    /// behaviour can be read rather than guessed at. Draws nothing. Not meant to ship.
    /// </summary>
    public partial class OsuEditorDiagnostics : Component
    {
        private const double log_interval = 500;

        private readonly Playfield playfield;

        [Resolved(CanBeNull = true)]
        private EditorClock? editorClock { get; set; }

        private double lastLogTime = double.NegativeInfinity;
        private string lastDragReport = string.Empty;

        public OsuEditorDiagnostics(Playfield playfield)
        {
            this.playfield = playfield;
        }

        protected override void Update()
        {
            base.Update();

            try
            {
                if (EditorButtonDragProbe.LastHandler != lastDragReport)
                {
                    lastDragReport = EditorButtonDragProbe.LastHandler;
                    Logger.Log($"[diag] drag start taken by: {lastDragReport}", LoggingTarget.Runtime, LogLevel.Important);
                }

                if (Time.Current - lastLogTime < log_interval)
                    return;

                lastLogTime = Time.Current;

                logObjects();
            }
            catch (Exception e)
            {
                Logger.Log($"[diag] failed: {e.Message}", LoggingTarget.Runtime, LogLevel.Important);
            }
        }

        private void logObjects()
        {
            double clockTime = editorClock?.CurrentTime ?? 0;

            var builder = new StringBuilder();

            builder.Append($"[diag] clock={clockTime:0}");

            foreach (var drawable in playfield.HitObjectContainer.AliveObjects.OfType<DrawableHitObject>())
            {
                if (drawable.HitObject is not OsuHitObject hitObject)
                    continue;

                double since = clockTime - hitObject.GetEndTime();

                // only objects already past are interesting here: the question is how long they stay grabbable.
                if (since < 0)
                    continue;

                builder.Append($" | t={hitObject.StartTime:0} since={since:0} alpha={drawable.Alpha:0.000} lifeEnd={drawable.LifetimeEnd:0} alive={drawable.IsAlive}");
            }

            Logger.Log(builder.ToString(), LoggingTarget.Runtime, LogLevel.Important);
        }
    }
}
