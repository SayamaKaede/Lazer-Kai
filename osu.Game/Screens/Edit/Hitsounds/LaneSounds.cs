// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
using osu.Game.Beatmaps.Formats;
using osu.Game.Extensions;
using osu.Game.Localisation;
using osu.Game.Models;
using osu.Game.Overlays;
using osu.Game.Overlays.Dialog;
using osu.Game.Overlays.Notifications;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Legacy;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Utils;

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

        [Resolved]
        private RulesetStore rulesets { get; set; } = null!;

        [Resolved(canBeNull: true)]
        private INotificationOverlay? notifications { get; set; }

        [Resolved(canBeNull: true)]
        private IDialogOverlay? dialogOverlay { get; set; }

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
            editorBeatmap.HitObjectRemoved += removed;

            Changed += mixesMayHaveChanged;
            mixesMayHaveChanged();

            if (editor != null)
                editor.Saved += keep;
        }

        #region Lanes that will be mixed

        /// <summary>
        /// The lanes that will be heard together in one file once this is turned into a hitsound difficulty, for each
        /// such file: lanes that are the same sound of a circle and have notes at the same time. Shown on the lanes, as
        /// it is a file more for every way they come together, and not what was put in either lane (2026-10-14).
        /// </summary>
        public IReadOnlyList<IReadOnlyList<int>> Mixes { get; private set; } = Array.Empty<IReadOnlyList<int>>();

        /// <summary>
        /// <see cref="Mixes"/> are others than they were.
        /// </summary>
        public event Action? MixesChanged;

        private static readonly Colour4[] mix_colours =
        {
            Colour4.FromHex(@"ff6b6b"),
            Colour4.FromHex(@"4dd0e1"),
            Colour4.FromHex(@"ffd54f"),
            Colour4.FromHex(@"81c784"),
            Colour4.FromHex(@"ba68c8"),
            Colour4.FromHex(@"ff8a65"),
            Colour4.FromHex(@"64b5f6"),
            Colour4.FromHex(@"f06292"),
        };

        /// <summary>
        /// The colour the lanes of one of <see cref="Mixes"/> are marked in, by its place among them.
        /// </summary>
        public static Colour4 ColourOfMix(int number) => mix_colours[number % mix_colours.Length];

        /// <summary>
        /// How long after the notes or the lanes last changed the lanes mixed are worked out again, in milliseconds:
        /// notes dragged change every frame, and every note is gone over.
        /// </summary>
        private const double mixes_settle_time = 100;

        private double? mixesStaleSince;

        private void mixesMayHaveChanged() => mixesStaleSince ??= Time.Current;

        protected override void Update()
        {
            base.Update();

            if (mixesStaleSince is not double since || Time.Current - since < mixes_settle_time)
                return;

            mixesStaleSince = null;

            var set = working.Value.BeatmapSetInfo;
            var mixes = LaneSoundConversion.Mixes(notes(), lanes(), project.Fallback, f => set.GetFile(f) != null);

            if (mixes.Count == Mixes.Count && mixes.Zip(Mixes).All(m => m.First.SequenceEqual(m.Second)))
                return;

            Mixes = mixes;
            MixesChanged?.Invoke();
        }

        #endregion

        private LaneSound[] lanes() => Enumerable.Range(0, LaneCount).Select(l => project[l]).ToArray();

        private IEnumerable<LaneSoundConversion.Note> notes() =>
            editorBeatmap.HitObjects.Where(h => h is IHasColumn).Select(h => new LaneSoundConversion.Note(h.StartTime, ((IHasColumn)h).Column));

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
                editorBeatmap.BeatmapSkin?.GetSample(sampleOf(sound, project.VolumeAt(clock.CurrentTime)))?.Play();
        }

        #region Volume

        /// <summary>
        /// How loud a note at <paramref name="time"/> is, in percent.
        /// </summary>
        public int VolumeAt(double time) => project.VolumeAt(time);

        /// <summary>
        /// Where the volume changes, in the order of their times.
        /// </summary>
        public IReadOnlyList<LaneVolume> Volumes => project.Volumes;

        /// <summary>
        /// Where a change of volume made for the playhead goes: on the notes that are at it, which it is for, and
        /// otherwise on the playhead's millisecond.
        /// </summary>
        public double VolumePlace()
        {
            double now = clock.CurrentTime;
            double? note = null;

            foreach (var hitObject in editorBeatmap.HitObjects)
            {
                if (Math.Abs(hitObject.StartTime - now) <= HitPoints.LENIENCY && (note == null || hitObject.StartTime < note))
                    note = hitObject.StartTime;
            }

            return note ?? Math.Round(now);
        }

        /// <summary>
        /// Makes notes <paramref name="volume"/> loud from the playhead on, until the volume next changes: as the volume
        /// of the green line at the playhead is set on the hitsound screen of an osu! difficulty, there being no green
        /// lines here to set it of.
        /// </summary>
        public void SetVolume(int volume)
        {
            double time = VolumePlace();

            if (project.VolumeAt(time) == volume)
                return;

            project.SetVolume(time, volume);
            keep();

            applyToEveryNote();
            Changed?.Invoke();
        }

        #endregion

        #region Turning into a hitsound difficulty

        /// <summary>
        /// The hit sound of a circle none of whose lanes is one.
        /// </summary>
        public LaneSound Fallback => project.Fallback;

        /// <summary>
        /// Gives the circles none of whose lanes is a hit sound one from the library, or with null, the skin's.
        /// </summary>
        /// <returns>Whether it was given: a sound no longer in the library is not.</returns>
        public bool GiveFallback(string? libraryFile)
        {
            string file = string.Empty;

            if (libraryFile != null)
            {
                file = copyIntoSet(libraryFile);

                if (file.Length == 0)
                    return false;
            }

            project.Fallback.File = file;
            project.Fallback.Source = libraryFile ?? string.Empty;

            keep();
            Changed?.Invoke();
            return true;
        }

        /// <summary>
        /// Says which sample set the hit sound of such circles is of.
        /// </summary>
        public void SetFallbackBank(string bank)
        {
            project.Fallback.Bank = bank;

            keep();
            Changed?.Invoke();
        }

        /// <summary>
        /// Whether the volumes set here go into the hitsound difficulty. Left out, the volumes of the one this was last
        /// turned into are kept, for the volume to be set there instead.
        /// </summary>
        public bool ConvertVolumes
        {
            get => project.ConvertVolumes;
            set
            {
                if (project.ConvertVolumes == value)
                    return;

                project.ConvertVolumes = value;
                keep();
            }
        }

        /// <summary>
        /// Whether sliders are made silent as they are followed, where the hitsounds of the hitsound difficulty are
        /// copied to: see <see cref="LaneSoundProject.MuteSliderBodies"/>.
        /// </summary>
        public bool MuteSliderBodies
        {
            get => project.MuteSliderBodies;
            set
            {
                if (project.MuteSliderBodies == value)
                    return;

                project.MuteSliderBodies = value;
                keep();
            }
        }

        /// <summary>
        /// The hitsound difficulty this was last turned into, if it is still there.
        /// </summary>
        public BeatmapInfo? Converted => converter.Converted;

        private HitsoundDifficultyConverter converter => new HitsoundDifficultyConverter(beatmaps, working.Value, project, keep, notifications, dialogOverlay);

        /// <summary>
        /// Turns the difficulty into an osu! hitsound difficulty of the same beatmap set: a circle wherever lanes sound,
        /// and the sound files those circles are to find. Done again, it is put in place of the one made before.
        /// </summary>
        /// <remarks>
        /// As Mapping Tools' Hitsound Studio does it (see <see cref="LaneSoundConversion"/>), but for where it is
        /// written: straight into the beatmap set, where Mapping Tools writes to a folder of its own for the mapper to
        /// move from. So nothing of the mapper's own is written over without asking: only the difficulty and the files
        /// this made the last time are.
        /// </remarks>
        public void ConvertToHitsoundDifficulty()
        {
            var osu = rulesets.GetRuleset(0);

            if (osu == null)
                return;

            // written from a difficulty with nothing in it: this one is not kept as stable has it, and has no file of
            // its own to write the other from.
            converter.Convert(notes(), lanes(), project.Fallback, () => HitsoundDifficultyWriter.EmptyLike(editorBeatmap, osu),
                project.Volumes.Select(v => (v.Time, v.Volume)).ToArray());
        }

        #endregion

        /// <summary>
        /// Makes every note sound as its lane does, and as loud as is said for where it is, as one edit.
        /// </summary>
        private void applyToEveryNote()
        {
            editorBeatmap.BeginChange();

            foreach (var hitObject in editorBeatmap.HitObjects.ToArray())
            {
                if (apply(hitObject))
                    editorBeatmap.Update(hitObject);
            }

            editorBeatmap.EndChange();
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

        private void removed(HitObject hitObject) => mixesMayHaveChanged();

        private void added(HitObject hitObject)
        {
            mixesMayHaveChanged();

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
            mixesMayHaveChanged();

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

            // as loud as is said for where it is, which a note moved in time may not be yet.
            int volume = project.VolumeAt(hitObject.StartTime);

            if (sound.HasFile)
            {
                if (hitObject.Samples.Count == 1 && hitObject.Samples[0] is ConvertHitObjectParser.FileHitSampleInfo file && file.Filename == sound.File && file.Volume == volume)
                    return false;

                hitObject.Samples = new List<HitSampleInfo> { sampleOf(sound, volume) };
                return true;
            }

            // a lane with no sound of its own: a note brought from one that has, or left from when this one had, goes
            // back to the plain hit.
            if (hitObject.Samples.Any(s => s is ConvertHitObjectParser.FileHitSampleInfo))
            {
                hitObject.Samples = new List<HitSampleInfo> { new HitSampleInfo(HitSampleInfo.HIT_NORMAL, volume: volume) };
                return true;
            }

            if (hitObject.Samples.All(s => s.Volume == volume))
                return false;

            hitObject.Samples = hitObject.Samples.Select(s => s.With(newVolume: volume)).ToList();
            return true;
        }

        private static HitSampleInfo sampleOf(LaneSound sound, int volume) => new ConvertHitObjectParser.FileHitSampleInfo(sound.File, volume);

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            if (editorBeatmap.IsNotNull())
            {
                editorBeatmap.HitObjectAdded -= added;
                editorBeatmap.HitObjectUpdated -= updated;
                editorBeatmap.HitObjectRemoved -= removed;
            }

            if (editor != null)
                editor.Saved -= keep;

            librarySamples?.Dispose();
        }
    }
}
