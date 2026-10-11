// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Audio.Sample;
using osu.Framework.Bindables;
using osu.Framework.Extensions;
using osu.Framework.Extensions.ObjectExtensions;
using osu.Framework.Graphics;
using osu.Framework.IO.Stores;
using osu.Framework.Platform;
using osu.Game.Audio;
using osu.Game.Beatmaps;
using osu.Game.Models;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Legacy;
using osu.Game.Rulesets.Objects.Types;

namespace osu.Game.Screens.Edit.Hitsounds
{
    /// <summary>
    /// The sounds of the lanes of a difficulty made to hitsound in, and what keeps every note sounding as its lane does.
    /// </summary>
    /// <remarks>
    /// A note has no sound of its own here: it is its lane's, from the moment it is put down, wherever it is moved to,
    /// and whenever its lane is given another. That is the whole of it for the mapper, who puts notes down and hears
    /// them, rather than copying notes that already sound right as stable has it done (2026-10-11).
    ///
    /// The lanes are not a part of what is undone. A note put back by an undo as it sounded before its lane was given
    /// another sound is made to sound as its lane does now, without that being an edit of its own.
    /// </remarks>
    public partial class LaneSounds : Component
    {
        /// <summary>
        /// The folder of the beatmap set the sounds given to lanes are copied into.
        /// </summary>
        public const string SET_DIRECTORY = @"HS";

        private readonly LaneSoundProject project;

        [Resolved]
        private EditorBeatmap editorBeatmap { get; set; } = null!;

        [Resolved]
        private EditorClock clock { get; set; } = null!;

        [Resolved]
        private BeatmapManager beatmaps { get; set; } = null!;

        [Resolved]
        private IBindable<WorkingBeatmap> working { get; set; } = null!;

        [Resolved(canBeNull: true)]
        private Editor? editor { get; set; }

        /// <summary>
        /// The sounds kept beside the game, which the lanes are given theirs from.
        /// </summary>
        public HitsoundLibrary Library { get; private set; } = null!;

        private LaneSoundProjectStore store = null!;

        private ISampleStore librarySamples = null!;

        /// <summary>
        /// A lane was given another sound, or told what it is to be in an osu! difficulty.
        /// </summary>
        public event Action? Changed;

        public LaneSounds(LaneSoundProject project)
        {
            this.project = project;
        }

        [BackgroundDependencyLoader]
        private void load(Storage storage, AudioManager audio)
        {
            Library = new HitsoundLibrary(storage);
            store = new LaneSoundProjectStore(storage);
            librarySamples = audio.GetSampleStore(new StorageBackedResourceStore(storage.GetStorageForDirectory(HitsoundLibrary.DIRECTORY)));

            project.EnsureLanes(LaneCount);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            foreach (var hitObject in editorBeatmap.HitObjects)
                apply(hitObject);

            editorBeatmap.HitObjectAdded += added;
            editorBeatmap.HitObjectUpdated += updated;

            if (editor != null)
                editor.Saved += keep;
        }

        /// <summary>
        /// How many lanes the difficulty has.
        /// </summary>
        public int LaneCount => Math.Max(1, (int)Math.Round(editorBeatmap.Difficulty.CircleSize));

        /// <summary>
        /// The sound of <paramref name="lane"/>.
        /// </summary>
        public LaneSound this[int lane] => project[lane];

        /// <summary>
        /// Gives <paramref name="lane"/> a sound from the library, copying it into the beatmap set; every note in the
        /// lane sounds as it from then on.
        /// </summary>
        /// <param name="lane">The lane.</param>
        /// <param name="libraryFile">The sound, as <see cref="HitsoundLibrary.Files"/> names it; null to take the lane's sound away.</param>
        /// <returns>Whether it was given: a sound no longer in the library is not.</returns>
        public bool Give(int lane, string? libraryFile)
        {
            string file = string.Empty;

            if (libraryFile != null)
            {
                file = copyIntoSet(libraryFile);

                if (file.Length == 0)
                    return false;
            }

            project.EnsureLanes(lane + 1);
            project.Lanes[lane].File = file;
            project.Lanes[lane].Source = libraryFile ?? string.Empty;

            keep();

            // one edit, so that one undo puts the notes back as they sounded: which they are then made to sound as their
            // lane does again, so it undoes nothing that is heard.
            editorBeatmap.BeginChange();

            foreach (var hitObject in editorBeatmap.HitObjects.Where(h => h is IHasColumn column && column.Column == lane).ToArray())
            {
                if (apply(hitObject))
                    editorBeatmap.Update(hitObject);
            }

            editorBeatmap.EndChange();

            Changed?.Invoke();
            return true;
        }

        /// <summary>
        /// Says what <paramref name="lane"/> is to be once turned into an osu! hitsound difficulty.
        /// </summary>
        /// <param name="lane">The lane.</param>
        /// <param name="bank">The sample set, one of <see cref="HitSampleInfo.ALL_BANKS"/>.</param>
        /// <param name="sound">The sound of that sample set: <see cref="HitSampleInfo.HIT_NORMAL"/> or one of <see cref="HitSampleInfo.ALL_ADDITIONS"/>.</param>
        public void SetSlot(int lane, string bank, string sound)
        {
            project.EnsureLanes(lane + 1);
            project.Lanes[lane].Bank = bank;
            project.Lanes[lane].Sound = sound;

            keep();
            Changed?.Invoke();
        }

        /// <summary>
        /// Plays the sound of <paramref name="lane"/>, as a note put in it would sound.
        /// </summary>
        public void Play(int lane)
        {
            var sound = project[lane];

            if (sound.HasFile)
                editorBeatmap.BeatmapSkin?.GetSample(sampleOf(sound))?.Play();
        }

        /// <summary>
        /// Plays a sound of the library, to hear it before a lane is given it.
        /// </summary>
        public void PlayFromLibrary(string libraryFile) => librarySamples.Get(libraryFile)?.Play();

        private void keep() => store.Save(editorBeatmap.BeatmapInfo, project);

        /// <summary>
        /// Copies a sound of the library into the beatmap set.
        /// </summary>
        /// <returns>Its name in the set, or nothing if it is not in the library.</returns>
        private string copyIntoSet(string libraryFile)
        {
            using var stream = Library.Open(libraryFile);

            if (stream == null)
                return string.Empty;

            var set = working.Value.BeatmapSetInfo;

            string hash = stream.ComputeSHA2Hash();
            stream.Seek(0, SeekOrigin.Begin);

            string extension = Path.GetExtension(libraryFile);
            string baseName = $@"{SET_DIRECTORY}/{Path.GetFileNameWithoutExtension(libraryFile)}";
            string name = baseName + extension;

            // the very sound, already in the set, is used as it is. Another sound of the same name, from another folder
            // of the library, is given a name of its own.
            for (int number = 2; set.GetFile(name) is RealmNamedFileUsage existing; number++)
            {
                if (existing.File.Hash == hash)
                    return name;

                name = $@"{baseName}-{number}{extension}";
            }

            beatmaps.AddFile(set, stream, name);
            return name;
        }

        /// <summary>
        /// Notes added one at a time this frame, to be heard: one put down is, a paste of many is not.
        /// </summary>
        private readonly List<HitObject> addedThisFrame = new List<HitObject>();

        private void added(HitObject hitObject)
        {
            // a hold note's head was made from it before this, as it sounded then.
            if (apply(hitObject))
                editorBeatmap.Update(hitObject);

            if (hitObject is not IHasColumn)
                return;

            addedThisFrame.Add(hitObject);

            if (addedThisFrame.Count > 1)
                return;

            Schedule(() =>
            {
                // playing, a note is heard as the playhead reaches it.
                if (addedThisFrame.Count == 1 && !clock.IsRunning && addedThisFrame[0] is IHasColumn column)
                    Play(column.Column);

                addedThisFrame.Clear();
            });
        }

        private void updated(HitObject hitObject)
        {
            // a note's parts are made from it as it is updated, so one made to sound as its lane is updated once more.
            if (apply(hitObject))
                editorBeatmap.Update(hitObject);
        }

        /// <summary>
        /// Makes <paramref name="hitObject"/> sound as its lane does.
        /// </summary>
        /// <returns>Whether it sounded as anything else.</returns>
        private bool apply(HitObject hitObject)
        {
            if (hitObject is not IHasColumn column)
                return false;

            var sound = project[column.Column];

            if (sound.HasFile)
            {
                if (hitObject.Samples.Count == 1 && hitObject.Samples[0] is ConvertHitObjectParser.FileHitSampleInfo file && file.Filename == sound.File)
                    return false;

                hitObject.Samples = new List<HitSampleInfo> { sampleOf(sound) };
                return true;
            }

            // a lane with no sound of its own: a note brought from one that has, or left from when this one had, goes
            // back to the plain hit.
            if (!hitObject.Samples.Any(s => s is ConvertHitObjectParser.FileHitSampleInfo))
                return false;

            hitObject.Samples = new List<HitSampleInfo> { new HitSampleInfo(HitSampleInfo.HIT_NORMAL) };
            return true;
        }

        private static HitSampleInfo sampleOf(LaneSound sound) => new ConvertHitObjectParser.FileHitSampleInfo(sound.File, 100);

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            if (editorBeatmap.IsNotNull())
            {
                editorBeatmap.HitObjectAdded -= added;
                editorBeatmap.HitObjectUpdated -= updated;
            }

            if (editor != null)
                editor.Saved -= keep;

            librarySamples?.Dispose();
        }
    }
}
