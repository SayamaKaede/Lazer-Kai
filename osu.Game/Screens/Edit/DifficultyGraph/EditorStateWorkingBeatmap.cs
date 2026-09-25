// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using osu.Framework.Audio.Track;
using osu.Framework.Graphics.Textures;
using osu.Game.Beatmaps;
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
    /// The objects are copied into a list of their own first, because the calculation runs off the update thread and
    /// the editor is free to keep changing its own list while it does.
    /// </remarks>
    public class EditorStateWorkingBeatmap : WorkingBeatmap
    {
        private readonly IBeatmap snapshot;

        public EditorStateWorkingBeatmap(IBeatmap editorState)
            : base(editorState.BeatmapInfo, null)
        {
            // a copy of the editor's own beatmap rather than a plain one: a ruleset's calculator can want its own kind
            // (mania's counts the columns of a ManiaBeatmap, and could not be handed anything else — 2026-09-25).
            var copy = editorState.Clone();
            copy.ControlPointInfo = editorState.ControlPointInfo.DeepClone();

            // a copy made member by member still shares the editor's list, so the objects go into one of its own. Its
            // type is the ruleset's, which only the beatmap itself knows.
            var hitObjects = copy.GetType().GetProperty(nameof(Beatmap.HitObjects))!;
            var list = (IList)Activator.CreateInstance(hitObjects.PropertyType)!;

            foreach (var hitObject in editorState.HitObjects)
                list.Add(hitObject);

            hitObjects.SetValue(copy, list);
            snapshot = copy;
        }

        public override IBeatmap GetPlayableBeatmap(IRulesetInfo ruleset, IReadOnlyList<Mod> mods, CancellationToken cancellationToken) => snapshot;

        protected override IBeatmap GetBeatmap() => snapshot;

        public override Texture GetBackground() => throw new NotImplementedException();

        protected override Track GetBeatmapTrack() => throw new NotImplementedException();

        protected internal override ISkin GetSkin() => throw new NotImplementedException();

        public override Stream GetStream(string storagePath) => throw new NotImplementedException();
    }
}
