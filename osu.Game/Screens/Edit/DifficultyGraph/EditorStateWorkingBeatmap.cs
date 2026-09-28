// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using osu.Framework.Audio.Track;
using osu.Framework.Graphics.Textures;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.Formats;
using osu.Game.IO;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;
using osu.Game.Skinning;

namespace osu.Game.Screens.Edit.DifficultyGraph
{
    /// <summary>
    /// Hands a difficulty calculator the beatmap as it currently stands in the editor.
    /// </summary>
    /// <remarks>
    /// A difficulty calculator can only be handed an <see cref="IWorkingBeatmap"/>, never an <see cref="IBeatmap"/>,
    /// and the editor's own working beatmap re-reads what is on disk — so calculating through it would describe the
    /// beatmap as it was last saved and miss everything placed since.
    ///
    /// The beatmap is written out and read back when this is made, on the thread the editor is edited on, so that the
    /// calculation runs on objects of its own. It runs off that thread, and the editor keeps taking its objects apart
    /// and building them again — for an edit, and for the preview mods as the composer loads. A calculator reading
    /// those same objects finds a slider's head there without its hit windows (2026-09-29, DOTNET-9).
    /// </remarks>
    public class EditorStateWorkingBeatmap : WorkingBeatmap
    {
        private readonly Lazy<IBeatmap> snapshot;

        public EditorStateWorkingBeatmap(IBeatmap editorState)
            : base(editorState.BeatmapInfo, null)
        {
            using var stream = new MemoryStream();

            using (var writer = new StreamWriter(stream, Encoding.UTF8, 1024, true))
                new LegacyBeatmapEncoder(editorState, null, null).Encode(writer);

            byte[] written = stream.ToArray();
            var ruleset = editorState.BeatmapInfo.Ruleset;

            // made in the ruleset's own kind of beatmap, which a calculator can want (mania's counts the columns of a
            // ManiaBeatmap, and could not be handed anything else — 2026-09-25). The reading back is left to whoever asks.
            snapshot = new Lazy<IBeatmap>(() =>
            {
                using var readStream = new MemoryStream(written);
                using var reader = new LineBufferedReader(readStream);

                var decoded = Beatmaps.Formats.Decoder.GetDecoder<Beatmap>(reader).Decode(reader);
                decoded.BeatmapInfo.Ruleset = ruleset;

                return new FlatWorkingBeatmap(decoded).GetPlayableBeatmap(ruleset);
            }, LazyThreadSafetyMode.ExecutionAndPublication);
        }

        public override IBeatmap GetPlayableBeatmap(IRulesetInfo ruleset, IReadOnlyList<Mod> mods, CancellationToken cancellationToken) => snapshot.Value;

        protected override IBeatmap GetBeatmap() => snapshot.Value;

        public override Texture GetBackground() => throw new NotImplementedException();

        protected override Track GetBeatmapTrack() => throw new NotImplementedException();

        protected internal override ISkin GetSkin() => throw new NotImplementedException();

        public override Stream GetStream(string storagePath) => throw new NotImplementedException();
    }
}
