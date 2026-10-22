// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Audio.Sample;
using osu.Framework.Bindables;
using osu.Framework.Extensions.ObjectExtensions;
using osu.Framework.Graphics;
using osu.Framework.IO.Stores;
using osu.Framework.Utils;
using osu.Game.Configuration;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;

namespace osu.Game.Screens.Edit
{
    /// <summary>
    /// The sounds the editor's ASMR mode makes as objects are put down, taken away and moved about (2026-10-21).
    /// </summary>
    public enum AsmrSound
    {
        PlaceCircle,
        PlaceSlider,
        PlaceSpinner,
        SliderStart,
        SliderPoint,

        /// <summary>
        /// A slider's length crossing a snap, while it is placed or afterwards: one click per step, higher the longer it gets.
        /// </summary>
        SliderLength,

        /// <summary>
        /// A placed slider's shape being dragged.
        /// </summary>
        SliderShape,

        Delete,
        Select,
        Move,

        /// <summary>
        /// Rotating, scaling or flipping the selection.
        /// </summary>
        Transform,

        NewCombo,
        Undo,
        Redo,

        /// <summary>
        /// A slider's repeat added or taken away by dragging its end along the timeline.
        /// </summary>
        SliderRepeat,

        /// <summary>
        /// A button in the editor pressed, by the mouse or by its key.
        /// </summary>
        UiClick,

        /// <summary>
        /// The pointer coming onto a button in the editor.
        /// </summary>
        UiHover,

        /// <summary>
        /// A switch in the editor turned on or off: higher for on.
        /// </summary>
        UiToggle,

        /// <summary>
        /// A toolbox group pinned, moved or folded.
        /// </summary>
        UiPin,

        /// <summary>
        /// The playhead stepped along by the wheel or the arrow keys: quiet, a roll of small clicks.
        /// </summary>
        Seek,

        /// <summary>
        /// A slider bar in the editor dragged, standing in for its notch: higher towards its top.
        /// </summary>
        UiSlider,
    }

    /// <summary>
    /// Plays a soft sound for each thing done to the objects in the editor, from the side of the playfield it was done
    /// on, while the track is stopped (2026-10-21, asked for by the user).
    /// </summary>
    /// <remarks>
    /// The sounds are made by <c>tools/asmr-sounds.py</c>, each rendered from <see cref="POSITIONS"/> places between
    /// the left ear and the right: panning alone can only turn one side down, and the rendered ones also arrive later
    /// and duller at the far ear, which is what puts a sound beside the head.
    ///
    /// What is asked for during a frame is played at its end, one of each, so that one action heard through several
    /// paths sounds once: undoing a deletion adds the objects back, and taking objects away also deselects them.
    /// </remarks>
    public partial class EditorAsmr : Component
    {
        /// <summary>
        /// How many places between the ears each sound is rendered from.
        /// </summary>
        public const int POSITIONS = 7;

        /// <summary>
        /// The width of the playfield the objects' positions are measured across.
        /// </summary>
        private const float playfield_width = 512;

        /// <summary>
        /// The least time between two of a sound that comes with every step of a drag, so a fast drag does not buzz.
        /// </summary>
        private const double drag_interval = 35;

        /// <summary>
        /// Fires as each sound is played, with the place it was played from (0 at the left ear).
        /// </summary>
        public event Action<AsmrSound, int>? Played;

        [Resolved]
        private EditorClock clock { get; set; } = null!;

        [Resolved]
        private EditorBeatmap editorBeatmap { get; set; } = null!;

        private readonly Bindable<bool> enabled = new Bindable<bool>();
        private readonly Bindable<float> volume = new Bindable<float>();

        private ISampleStore samples = null!;
        private readonly Dictionary<string, Sample?> loaded = new Dictionary<string, Sample?>();

        private readonly List<(AsmrSound sound, int position, double pitch)> pending = new List<(AsmrSound, int, double)>();
        private readonly Dictionary<AsmrSound, double> lastPlayed = new Dictionary<AsmrSound, double>();

        public EditorAsmr()
        {
            // the editor's whole screen, so a button's place across it says which ear it is heard from.
            RelativeSizeAxes = Axes.Both;
        }

        [BackgroundDependencyLoader]
        private void load(AudioManager audio, OsuConfigManager config)
        {
            config.BindWith(OsuSetting.EditorAsmrSounds, enabled);
            config.BindWith(OsuSetting.EditorAsmrVolume, volume);

            // under `Resources/` in the project, which stays in the embedded name: see LazerKaiArtwork.
            samples = audio.GetSampleStore(new NamespacedResourceStore<byte[]>(new DllResourceStore(typeof(EditorAsmr).Assembly),
                @"Resources/Samples/LazerKai/Asmr"));
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            editorBeatmap.HitObjectAdded += onAdded;
            editorBeatmap.HitObjectRemoved += onRemoved;
            editorBeatmap.SelectedHitObjects.CollectionChanged += onSelectionChanged;
        }

        /// <summary>
        /// Asks for a sound at the end of this frame.
        /// </summary>
        /// <param name="sound">Which.</param>
        /// <param name="x">Where across the playfield it happened, in osu! pixels, or null for straight ahead.</param>
        /// <param name="pitch">How much higher than its own pitch, for the steps of a slider's length.</param>
        public void Play(AsmrSound sound, float? x = null, double pitch = 1)
        {
            if (!PlaysInterface)
                return;

            enqueue(sound, x == null ? null : x.Value / playfield_width, pitch);
        }

        /// <summary>
        /// Whether the editor's own sounds are being played now, and the interface's sounds give way to them:
        /// while the track plays the interface sounds as it always has (2026-10-21, the user's choice).
        /// </summary>
        public bool PlaysInterface => enabled.Value && !clock.IsRunning;

        /// <summary>
        /// Asks for a sound from where a control is on the screen.
        /// </summary>
        public void PlayInterface(AsmrSound sound, Drawable from, double pitch = 1)
        {
            if (!PlaysInterface)
                return;

            var screen = ScreenSpaceDrawQuad;
            float? across = screen.Width > 0 ? (from.ScreenSpaceDrawQuad.Centre.X - screen.TopLeft.X) / screen.Width : null;

            enqueue(sound, across, pitch);
        }

        private int quiet;

        /// <summary>
        /// Hears nothing until disposed: for a tool that rebuilds the objects itself as it is adjusted, where the
        /// control being moved is the sound and the objects coming and going under it are not (2026-10-21).
        /// </summary>
        public IDisposable Quiet()
        {
            quiet++;
            return new InvokeOnDisposal(() => quiet--);
        }

        private bool otherSoundThisFrame;

        /// <summary>
        /// Hears nothing asked for in this frame, before or after: for a change that is heard as something else, such as
        /// a hitsound changed and played on the hitsound screen, where the key or button behind it would be heard over
        /// it (2026-10-21).
        /// </summary>
        public void LeaveThisFrameToAnotherSound() => otherSoundThisFrame = true;

        // across: from 0 at the left to 1 at the right, or null for straight ahead.
        private void enqueue(AsmrSound sound, float? across, double pitch)
        {
            if (quiet > 0)
                return;

            int position = across == null
                ? POSITIONS / 2
                : (int)Math.Round(Math.Clamp(across.Value, 0, 1) * (POSITIONS - 1));

            pending.Add((sound, position, pitch));
        }

        /// <summary>
        /// Asks for a sound from where an object is.
        /// </summary>
        public void Play(AsmrSound sound, HitObject hitObject) => Play(sound, (hitObject as IHasXPosition)?.X);

        /// <summary>
        /// The pitch of a slider's length step: rising as it gets longer, so stretching it reads like winding a ratchet.
        /// </summary>
        public static double PitchForLength(double length) => 0.8 + 0.6 * Math.Clamp(length / 600, 0, 1);

        /// <summary>
        /// The same for an object's length in time, as a spinner's is stretched along the timeline.
        /// </summary>
        public static double PitchForDuration(double duration) => 0.8 + 0.6 * Math.Clamp(duration / 2400, 0, 1);

        /// <summary>
        /// The pitch of a slider's repeat being added or taken away: a step up for each.
        /// </summary>
        public static double PitchForRepeats(int repeats) => 0.85 + 0.08 * Math.Clamp(repeats, 0, 8);

        private void onAdded(HitObject hitObject) => Play(soundFor(hitObject), hitObject);

        private void onRemoved(HitObject hitObject) => Play(AsmrSound.Delete, hitObject);

        private void onSelectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            var hitObject = e.NewItems?.OfType<HitObject>().FirstOrDefault();

            if (hitObject != null)
                Play(AsmrSound.Select, hitObject);
            else
                Play(AsmrSound.Select);
        }

        private static AsmrSound soundFor(HitObject hitObject)
        {
            switch (hitObject)
            {
                case IHasPath:
                    return AsmrSound.PlaceSlider;

                case IHasDuration:
                    return AsmrSound.PlaceSpinner;

                default:
                    return AsmrSound.PlaceCircle;
            }
        }

        protected override void Update()
        {
            base.Update();

            if (otherSoundThisFrame)
            {
                otherSoundThisFrame = false;
                pending.Clear();
            }

            if (pending.Count == 0)
                return;

            var asked = pending.ToList();
            pending.Clear();

            // what an undo puts back or takes away is the undo, not a placement or a deletion of its own.
            if (asked.Any(a => a.sound == AsmrSound.Undo || a.sound == AsmrSound.Redo))
                asked.RemoveAll(a => a.sound != AsmrSound.Undo && a.sound != AsmrSound.Redo);

            // putting an object down where one was replaces it, and taking objects away also deselects them.
            if (asked.Any(a => isPlacement(a.sound)))
                asked.RemoveAll(a => a.sound == AsmrSound.Delete);
            if (asked.Any(a => a.sound != AsmrSound.Select && !isInterface(a.sound)))
                asked.RemoveAll(a => a.sound == AsmrSound.Select);

            // a button that does something to the objects is heard as what it did; a switch or a pin is heard as
            // itself rather than as the click that flipped it; and a press is not also a hover.
            if (asked.Any(a => !isInterface(a.sound)))
                asked.RemoveAll(a => isInterface(a.sound));
            if (asked.Any(a => a.sound == AsmrSound.UiToggle || a.sound == AsmrSound.UiPin))
                asked.RemoveAll(a => a.sound == AsmrSound.UiClick);
            if (asked.Any(a => a.sound != AsmrSound.UiHover))
                asked.RemoveAll(a => a.sound == AsmrSound.UiHover);

            foreach (var a in asked.GroupBy(a => a.sound).Select(g => g.First()))
                play(a.sound, a.position, a.pitch);
        }

        private static bool isPlacement(AsmrSound sound) => sound == AsmrSound.PlaceCircle || sound == AsmrSound.PlaceSlider || sound == AsmrSound.PlaceSpinner;

        private static bool isInterface(AsmrSound sound) =>
            sound == AsmrSound.UiClick || sound == AsmrSound.UiHover || sound == AsmrSound.UiToggle || sound == AsmrSound.UiPin || sound == AsmrSound.UiSlider;

        private static bool comesWithEveryStep(AsmrSound sound) =>
            sound == AsmrSound.Move || sound == AsmrSound.Transform || sound == AsmrSound.SliderShape || sound == AsmrSound.SliderLength || sound == AsmrSound.Select
            || sound == AsmrSound.UiHover || sound == AsmrSound.Seek;

        /// <summary>
        /// The sounds whose pitch says something, which are not varied at random.
        /// </summary>
        private static bool pitchIsMeant(AsmrSound sound) =>
            sound == AsmrSound.SliderLength || sound == AsmrSound.SliderRepeat || sound == AsmrSound.UiToggle || sound == AsmrSound.UiSlider;

        private void play(AsmrSound sound, int position, double pitch)
        {
            if (comesWithEveryStep(sound) && lastPlayed.TryGetValue(sound, out double last) && Time.Current - last < drag_interval)
                return;

            lastPlayed[sound] = Time.Current;

            var sample = GetSample(sound, position);

            if (sample != null)
            {
                var channel = sample.GetChannel();
                channel.Volume.Value = volume.Value;
                // a little different each time, so a run of the same sound does not tire the ear.
                channel.Frequency.Value = pitch * (pitchIsMeant(sound) ? 1 : 0.95 + RNG.NextDouble(0.1));
                channel.Play();
            }

            Played?.Invoke(sound, position);
        }

        /// <summary>
        /// The sound as rendered from one place, or null where there is no such file: a name that does not line up
        /// with what <c>tools/asmr-sounds.py</c> writes plays nothing and says nothing, so a test asks for every one.
        /// </summary>
        public Sample? GetSample(AsmrSound sound, int position)
        {
            string name = $@"{kebab(sound)}-{position}";

            if (!loaded.TryGetValue(name, out var sample))
                loaded[name] = sample = samples.Get(name);

            return sample;
        }

        private static string kebab(AsmrSound sound) =>
            string.Concat(sound.ToString().Select((c, i) => char.IsUpper(c) && i > 0 ? $@"-{char.ToLowerInvariant(c)}" : char.ToLowerInvariant(c).ToString()));

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            if (editorBeatmap.IsNotNull())
            {
                editorBeatmap.HitObjectAdded -= onAdded;
                editorBeatmap.HitObjectRemoved -= onRemoved;
                editorBeatmap.SelectedHitObjects.CollectionChanged -= onSelectionChanged;
            }

            samples?.Dispose();
        }
    }
}
