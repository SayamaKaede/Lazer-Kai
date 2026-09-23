// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using System.Linq;
using System.Text;
using osu.Game.Beatmaps.Formats;
using osu.Game.Rulesets.Objects;

namespace osu.Game.Screens.Edit
{
    public partial class BeatmapEditorChangeHandler : EditorChangeHandler
    {
        /// <summary>
        /// Where the timing lines start in a state, after everything the encoder wrote.
        /// </summary>
        private static readonly byte[] timing_lines_heading = Encoding.UTF8.GetBytes(@"[LazerKaiTimingLines]");

        private readonly LegacyEditorBeatmapPatcher patcher;
        private readonly EditorBeatmap editorBeatmap;
        private readonly SavedTimingLines? timingLines;

        /// <summary>
        /// Creates a new <see cref="EditorChangeHandler"/>.
        /// </summary>
        /// <param name="editorBeatmap">The <see cref="EditorBeatmap"/> to track the <see cref="HitObject"/>s of.</param>
        /// <param name="timingLines">The red and green lines as a stable file has them, which the encoder does not write.</param>
        public BeatmapEditorChangeHandler(EditorBeatmap editorBeatmap, SavedTimingLines? timingLines = null)
        {
            this.editorBeatmap = editorBeatmap;
            this.timingLines = timingLines;

            editorBeatmap.TransactionBegan += BeginChange;
            editorBeatmap.TransactionEnded += EndChange;
            editorBeatmap.SaveStateTriggered += SaveState;

            patcher = new LegacyEditorBeatmapPatcher(editorBeatmap);
        }

        protected override void WriteCurrentStateToStream(MemoryStream stream)
        {
            using (var sw = new StreamWriter(stream, Encoding.UTF8, 1024, true))
                new LegacyBeatmapEncoder(editorBeatmap, editorBeatmap.BeatmapSkin, editorBeatmap.Storyboard).Encode(sw);

            if (timingLines == null)
                return;

            // after the encoder's own, so the patcher never sees them: the encoder writes the lines it makes up from
            // the objects, and these are the ones a stable file keeps.
            using (var sw = new StreamWriter(stream, new UTF8Encoding(false), 1024, true))
            {
                sw.WriteLine();
                sw.WriteLine(Encoding.UTF8.GetString(timing_lines_heading));

                foreach (var line in timingLines.Lines)
                    sw.WriteLine(line.Raw);
            }
        }

        protected override void ApplyStateChange(byte[] previousState, byte[] newState)
        {
            var (previousBeatmap, _) = split(previousState);
            var (newBeatmap, newLines) = split(newState);

            patcher.Patch(previousBeatmap, newBeatmap);

            if (timingLines == null || newLines == null)
                return;

            var lines = newLines.Select(SavedTimingLines.Parse).OfType<SavedTimingLine>().ToList();

            if (!lines.SequenceEqual(timingLines.Lines))
            {
                timingLines.Lines.Clear();
                timingLines.Lines.AddRange(lines);
            }
        }

        private static (byte[] beatmap, string[]? lines) split(byte[] state)
        {
            int heading = state.AsSpan().IndexOf(timing_lines_heading);

            if (heading < 0)
                return (state, null);

            string lines = Encoding.UTF8.GetString(state, heading + timing_lines_heading.Length, state.Length - heading - timing_lines_heading.Length);
            return (state[..heading], lines.Split('\n').Select(l => l.TrimEnd('\r')).Where(l => l.Length > 0).ToArray());
        }
    }
}
