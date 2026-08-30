// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using osu.Framework.Audio.Sample;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Localisation;
using osu.Framework.Logging;
using osu.Game.Audio;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.Formats;
using osu.Game.IO;
using osu.Game.Localisation;
using osu.Game.Overlays.Notifications;
using osu.Game.Skinning;
using osu.Game.Storyboards;
using osu.Game.Utils;
using osuTK.Graphics;

namespace osu.Game.Screens.Edit
{
    /// <summary>
    /// Dropping a `.osu` file onto the open editor puts it into the set being edited.
    /// </summary>
    /// <remarks>
    /// Dropping one anywhere else imports it as its own thing, which is rarely what is wanted while a set is
    /// already open — the file almost always belongs to the set on screen. Guest difficulties arrive as loose
    /// `.osu` files, and putting one in by hand means finding the set's folder, copying the file in under the
    /// right name and reloading.
    /// </remarks>
    public partial class Editor
    {
        /// <summary>
        /// Whether a dropped file is one the open editor takes rather than the game importing it.
        /// </summary>
        public static bool IsDroppedDifficulty(string path) =>
            string.Equals(Path.GetExtension(path), @".osu", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Puts the given `.osu` files into the set being edited, replacing difficulties of the same name.
        /// </summary>
        public void ImportDroppedDifficulties(IEnumerable<string> paths)
        {
            string[] dropped = paths.Where(IsDroppedDifficulty).ToArray();

            if (dropped.Length > 0)
                Schedule(() => importDropped(dropped));
        }

        private void importDropped(string[] paths)
        {
            var set = editorBeatmap.BeatmapInfo.BeatmapSet;

            DroppedFile replacingCurrent = null;
            BeatmapInfo currentTarget = null;

            foreach (string path in paths)
            {
                DroppedFile dropped;

                try
                {
                    dropped = read(path);
                }
                catch (Exception e)
                {
                    // the exception carries the whole path it failed on, and a dropped file usually comes from
                    // somewhere under the account folder of whoever dropped it.
                    Logger.Error(e, PathDisplay.MaskIn($@"Could not read ""{Path.GetFileName(path)}"" as a difficulty."));
                    post(EditorStrings.DroppedDifficultyUnreadable(Path.GetFileName(path)), failed: true);
                    continue;
                }

                // a file with no `Version` line has no name to go by, and the name is what decides whether
                // anything is replaced. The filename is what the person dragging it sees, so use that.
                if (string.IsNullOrEmpty(dropped.Beatmap.BeatmapInfo.DifficultyName))
                    dropped.Beatmap.BeatmapInfo.DifficultyName = Path.GetFileNameWithoutExtension(path);

                string name = dropped.Beatmap.BeatmapInfo.DifficultyName;

                switch (DroppedDifficulty.DecideFor(set, editorBeatmap.BeatmapInfo, name, dropped.Beatmap.BeatmapInfo.Ruleset.OnlineID, out var target))
                {
                    case DroppedDifficulty.Outcome.Added:
                        add(set, dropped);
                        post(EditorStrings.DroppedDifficultyAdded(name));
                        break;

                    case DroppedDifficulty.Outcome.Replaced:
                        replace(target, dropped);
                        post(EditorStrings.DroppedDifficultyReplaced(name));
                        break;

                    case DroppedDifficulty.Outcome.ReplacedCurrent:
                        // held back rather than done here: it is the one case that asks first, and the editor
                        // can only reopen on one difficulty, so a drop naming this one twice takes the last.
                        replacingCurrent = dropped;
                        currentTarget = target;
                        break;

                    case DroppedDifficulty.Outcome.NameTaken:
                        post(EditorStrings.DroppedDifficultyNameTaken(name), failed: true);
                        break;
                }
            }

            if (replacingCurrent != null)
                confirmReplacingCurrent(currentTarget, replacingCurrent);
        }

        /// <summary>
        /// Everything needed to write a difficulty back out, read out of the one file.
        /// </summary>
        /// <remarks>
        /// A `.osu` is written from three things rather than one: the objects and timing come from the beatmap,
        /// the combo colours from the skin, and the events from the storyboard. Reading only the first is what
        /// reset the combo colours of the first difficulty this was tried on — nothing had gone wrong at the
        /// point of writing, there was simply nothing to write them from.
        /// </remarks>
        private static DroppedFile read(string path)
        {
            IBeatmap beatmap;
            SkinConfiguration configuration;
            Storyboard storyboard;

            using (var stream = File.OpenRead(path))
            using (var reader = new LineBufferedReader(stream))
                beatmap = Decoder.GetDecoder<Beatmap>(reader).Decode(reader);

            using (var stream = File.OpenRead(path))
            using (var reader = new LineBufferedReader(stream))
                configuration = new LegacySkinDecoder().Decode(reader);

            using (var stream = File.OpenRead(path))
            using (var reader = new LineBufferedReader(stream))
                storyboard = Decoder.GetDecoder<Storyboard>(reader).Decode(reader);

            storyboard.Beatmap = beatmap;

            return new DroppedFile
            {
                Beatmap = beatmap,
                Skin = new DroppedFileSkin(configuration),
                Storyboard = storyboard,
            };
        }

        private class DroppedFile
        {
            public IBeatmap Beatmap;
            public ISkin Skin;
            public Storyboard Storyboard;
        }

        /// <summary>
        /// Answers with the combo colours the dropped file carried, and nothing else.
        /// </summary>
        /// <remarks>
        /// The colours are asked for through the skin, but a dropped file has no skin to speak of — its
        /// `[Colours]` section is the whole of it. <see cref="SkinConfiguration.ComboColours"/> is deliberately
        /// not used: it stands in the default set when a file has none, which would write colours into a
        /// difficulty that never had any.
        /// </remarks>
        private class DroppedFileSkin : ISkin
        {
            private readonly SkinConfiguration configuration;

            public DroppedFileSkin(SkinConfiguration configuration)
            {
                this.configuration = configuration;
            }

            public IBindable<TValue> GetConfig<TLookup, TValue>(TLookup lookup)
            {
                if (lookup is GlobalSkinColours.ComboColours && configuration.CustomComboColours.Count > 0)
                    return SkinUtils.As<TValue>(new Bindable<IReadOnlyList<Color4>>(configuration.CustomComboColours));

                return null;
            }

            public Drawable GetDrawableComponent(ISkinComponentLookup lookup) => null;

            public Texture GetTexture(string componentName, WrapMode wrapModeS, WrapMode wrapModeT) => null;

            public ISample GetSample(ISampleInfo sampleInfo) => null;
        }

        private void add(BeatmapSetInfo set, DroppedFile dropped)
        {
            var info = dropped.Beatmap.BeatmapInfo;

            // the file carries whatever it was in the set it came from. Inside this one it is new, and local.
            info.ID = Guid.NewGuid();
            info.Hash = string.Empty;
            info.ResetOnlineInfo();

            // artist, title and creator belong to the set rather than to one difficulty, and the file's own
            // would otherwise show up as a second entry in the song select list.
            info.Metadata = editorBeatmap.BeatmapInfo.Metadata.DeepClone();

            // both directions, the same as `BeatmapManager.CreateNewDifficulty` does: saving walks from the
            // difficulty back to its set.
            set.Beatmaps.Add(info);
            info.BeatmapSet = set;

            // the background is written out of whichever difficulty the storyboard is pointed at, so it has to
            // be the one being written rather than the one the file arrived describing.
            dropped.Storyboard.BeatmapInfo = info;

            beatmapManager.Save(info, dropped.Beatmap, dropped.Skin, dropped.Storyboard);
            ((IWorkingBeatmapCache)beatmapManager).Invalidate(set);
        }

        private void replace(BeatmapInfo target, DroppedFile dropped)
        {
            // the contents, the difficulty settings and the combo colours come from the file; the name, the set
            // it belongs to and the identity everything else in the game refers to it by stay as they were.
            // `Save` does exactly this given the difficulty already there and the contents to put in it.
            dropped.Storyboard.BeatmapInfo = target;

            beatmapManager.Save(target, dropped.Beatmap, dropped.Skin, dropped.Storyboard);
            ((IWorkingBeatmapCache)beatmapManager).Invalidate(target);
        }

        private void confirmReplacingCurrent(BeatmapInfo target, DroppedFile dropped)
        {
            if (dialogOverlay == null)
            {
                replaceCurrent(target, dropped);
                return;
            }

            dialogOverlay.Push(new OverwriteCurrentDifficultyDialog(target.DifficultyName, () => replaceCurrent(target, dropped), () => { }));
        }

        private void replaceCurrent(BeatmapInfo target, DroppedFile dropped)
        {
            attemptMutationOperation(() =>
            {
                replace(target, dropped);

                // the same reasoning as deleting the difficulty being edited: the editor is about to be torn
                // down still holding the old contents, and the exit flow would otherwise offer to save them
                // back over the file that was just written.
                updateLastSavedHash();

                Beatmap.Value = beatmapManager.GetWorkingBeatmap(target, true);
                SwitchToDifficulty(target);
                return true;
            });
        }

        private void post(LocalisableString text, bool failed = false)
        {
            if (failed)
            {
                notifications?.Post(new SimpleErrorNotification
                {
                    Icon = FontAwesome.Solid.ExclamationTriangle,
                    Text = text,
                });
            }
            else
            {
                notifications?.Post(new SimpleNotification
                {
                    Icon = FontAwesome.Solid.FileImport,
                    Text = text,
                });
            }
        }
    }
}
