// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.ObjectExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Logging;
using osu.Framework.Threading;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Configuration;
using osu.Game.Rulesets.Osu.Beatmaps;
using osu.Game.Rulesets.Osu.Configuration;
using osu.Game.Rulesets.Osu.Replays;
using osu.Game.Rulesets.Osu.Skinning;
using osu.Game.Rulesets.Osu.UI.Cursor;
using osu.Game.Screens.Edit;
using osu.Game.Skinning;
using osuTK;

namespace osu.Game.Rulesets.Osu.Edit
{
    /// <summary>
    /// Where the cursor would be, drawn on the playfield as the editor is scrubbed or played.
    /// </summary>
    /// <remarks>
    /// The path comes out of <see cref="OsuAutoGenerator"/> rather than being worked out here, so it is the movement
    /// autoplay actually makes and not an approximation of it -- which is the only version worth showing, since the
    /// question being asked of it is whether a pattern moves the way it looks like it should.
    /// </remarks>
    public partial class AutoCursorDisplay : CompositeDrawable
    {
        /// <summary>
        /// How long the beatmap has to sit still before the path is worked out again.
        /// </summary>
        /// <remarks>
        /// Dragging a slider's anchor reprocesses the beatmap on every frame of the drag, and the path is built for
        /// the whole beatmap at once. Waiting for the dragging to stop is what keeps that off the update thread.
        /// </remarks>
        private const double settle_delay = 400;

        private readonly Bindable<bool> enabled = new Bindable<bool>();

        /// <summary>
        /// Whether something other than this display needs the path worked out.
        /// </summary>
        /// <remarks>
        /// Flashlight follows autoplay rather than the mouse while it is being previewed, and it needs the path for
        /// that whether or not the cursor itself is being drawn. Generating it is a whole-beatmap job, so it is only
        /// done when somebody is actually reading it.
        /// </remarks>
        public Bindable<bool> PathWanted { get; } = new BindableBool();

        private EditorAutoCursor cursor = null!;
        private SkinnableDrawable trail = null!;
        private CursorRippleVisualiser ripples = null!;
        private bool held;

        private readonly Bindable<bool> showTrail = new Bindable<bool>();
        private readonly Bindable<bool> showRipples = new Bindable<bool>();

        [Resolved]
        private EditorBeatmap editorBeatmap { get; set; } = null!;

        [Resolved]
        private EditorClock editorClock { get; set; } = null!;

        private IReadOnlyList<OsuReplayFrame> frames = Array.Empty<OsuReplayFrame>();
        private CancellationTokenSource? generation;
        private ScheduledDelegate? settling;

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config, IRulesetConfigCache rulesetConfigCache)
        {
            config.BindWith(OsuSetting.EditorShowAutoCursor, enabled);

            // the cursor's own settings live with the ruleset rather than with the game, and the editor is not
            // inside a ruleset's configuration the way a gameplay screen is -- so they are fetched rather than
            // resolved. Without this the trail and the ripples would run on their own defaults and ignore what the
            // user set for the game they actually play.
            if (rulesetConfigCache.GetConfigFor(new OsuRuleset()) is OsuRulesetConfigManager rulesetConfig)
            {
                rulesetConfig.BindWith(OsuRulesetSetting.ShowCursorTrail, showTrail);
                rulesetConfig.BindWith(OsuRulesetSetting.ShowCursorRipples, showRipples);
            }

            RelativeSizeAxes = Axes.Both;

            // the skin's own cursor rather than one drawn here: what is being asked of this is what the play would
            // look like, and the cursor a mapper plays with -- its skin, its size, its trail -- is part of that.
            // Laid out the way a gameplay screen lays them out: the trail behind, then the ripples, then the cursor.
            InternalChildren = new Drawable[]
            {
                trail = new SkinnableDrawable(new OsuSkinComponentLookup(OsuSkinComponents.CursorTrail),
                    _ => new OsuCursorContainer.DefaultCursorTrail(), confineMode: ConfineMode.NoScaling),
                ripples = new CursorRippleVisualiser(),
                cursor = new EditorAutoCursor { Alpha = 0 },
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            enabled.BindValueChanged(_ => regenerate(), true);
            PathWanted.BindValueChanged(_ => regenerate());
            editorBeatmap.BeatmapReprocessed += onReprocessed;

            // the setting alone does not decide it: the trail also goes when the cursor does, below.
            showTrail.BindValueChanged(_ =>
            {
                trailShowing = null;
                showTrailAndRipples(enabled.Value && frames.Count > 0);
            }, true);

            cursor.CursorScale.BindValueChanged(scale =>
            {
                ripples.CursorScale = new Vector2(scale.NewValue);
                updateTrailScale();
            }, true);

            trail.OnSkinChanged += leadTheTrail;
            leadTheTrail();
        }

        /// <summary>
        /// Hands the trail the cursor to follow, in place of the mouse.
        /// </summary>
        /// <remarks>
        /// Done again whenever the skin changes, because the skin decides which trail is there to be led.
        /// </remarks>
        private void leadTheTrail()
        {
            updateTrailScale();

            if (trail.Drawable is CursorTrail drawn)
                drawn.ScreenSpacePositionSource = () => cursor.Alpha > 0 ? cursor.ScreenSpaceDrawQuad.Centre : null;
        }

        private void updateTrailScale()
        {
            if (trail.Drawable is CursorTrail drawn)
                drawn.CursorScale = new Vector2(cursor.CursorScale.Value);
        }

        private bool? trailShowing;

        /// <summary>
        /// Takes the trail and the ripples away with the cursor.
        /// </summary>
        /// <remarks>
        /// A trail is a line of parts that each fade on their own, so turning the cursor off only stops new ones
        /// being laid down -- whatever was already drawn stays where the cursor last was. Fading the whole thing is
        /// what actually clears it.
        /// </remarks>
        private void showTrailAndRipples(bool show)
        {
            if (trailShowing == show)
                return;

            trailShowing = show;

            trail.FadeTo(show && showTrail.Value ? 1 : 0, 200);
            ripples.FadeTo(show ? 1 : 0, 200);
        }

        /// <summary>
        /// Where the cursor is on screen, or null while there is no path to read.
        /// </summary>
        public Vector2? ScreenSpacePosition => frames.Count == 0 ? null : cursor.ScreenSpaceDrawQuad.Centre;

        private void onReprocessed() => regenerate();

        private void regenerate()
        {
            settling?.Cancel();
            generation?.Cancel();

            if (!enabled.Value && !PathWanted.Value)
            {
                frames = Array.Empty<OsuReplayFrame>();
                return;
            }

            settling = Scheduler.AddDelayed(() =>
            {
                generation = new CancellationTokenSource();

                var token = generation.Token;
                var beatmap = editorBeatmap.PlayableBeatmap;

                Task.Run(() =>
                {
                    var generated = GenerateFor(beatmap);

                    Schedule(() =>
                    {
                        if (token.IsCancellationRequested)
                            return;

                        frames = generated;
                    });
                }, token);
            }, settle_delay);
        }

        /// <summary>
        /// The frames autoplay would make for this beatmap, or nothing when it cannot make any.
        /// </summary>
        /// <remarks>
        /// A beatmap being edited passes through states no finished one does -- an empty one to begin with, an
        /// object placed before the first timing point -- and the generator is written for finished beatmaps. What
        /// it does with the rest is not worth finding out on the update thread, so anything it throws is taken as
        /// "no path to show" rather than being allowed to take the editor down.
        /// </remarks>
        public static IReadOnlyList<OsuReplayFrame> GenerateFor(IBeatmap beatmap)
        {
            // the generator reads its beatmap as an OsuBeatmap and will not be talked out of it.
            if (beatmap is not OsuBeatmap osuBeatmap || osuBeatmap.HitObjects.Count == 0)
                return Array.Empty<OsuReplayFrame>();

            try
            {
                // no mods: what is being checked is how the pattern reads as it was written, and a mod that moves
                // the objects would be answering a different question.
                return new OsuAutoGenerator(osuBeatmap, Array.Empty<Mod>()).Generate()
                                                                           .Frames
                                                                           .OfType<OsuReplayFrame>()
                                                                           .OrderBy(frame => frame.Time)
                                                                           .ToArray();
            }
            catch (Exception e)
            {
                // logged rather than swallowed: a cursor that quietly never appears is the hardest kind of thing
                // to work out afterwards, and this catch exists for states no finished beatmap reaches, not to
                // hide a mistake in here.
                Logger.Log($"Autoplay's path could not be worked out: {e.Message}", level: LogLevel.Important);
                return Array.Empty<OsuReplayFrame>();
            }
        }

        protected override void Update()
        {
            base.Update();

            if (frames.Count == 0)
            {
                cursor.Alpha = 0;
                showTrailAndRipples(false);
                return;
            }

            // kept moving even while hidden: flashlight reads the position off it, and the setting only governs
            // whether the cursor itself is drawn.
            cursor.Alpha = enabled.Value ? 1 : 0;
            cursor.Position = PositionAt(frames, editorClock.CurrentTime);

            showTrailAndRipples(enabled.Value);

            bool holding = HeldAt(frames, editorClock.CurrentTime);

            if (holding != held)
            {
                held = holding;

                // the same swell the cursor makes in play, rather than a marker of this display's own.
                if (holding)
                {
                    cursor.Expand();

                    // gated here rather than left to the ripples themselves: without a ruleset's configuration
                    // around them they fall back to showing, and this display is outside one.
                    if (enabled.Value && showRipples.Value)
                        ripples.Ripple(ripples.ToLocalSpace(cursor.ScreenSpaceDrawQuad.Centre));
                }
                else
                    cursor.Contract();
            }
        }

        /// <summary>
        /// Where the cursor is at <paramref name="time"/>, between the two frames that surround it.
        /// </summary>
        /// <remarks>
        /// Frames are only laid down where the movement changes, so the gap between two of them can be most of a
        /// second. Reading the nearest one instead of interpolating would make the cursor jump between objects
        /// rather than travel, which is the part being looked at.
        /// </remarks>
        public static Vector2 PositionAt(IReadOnlyList<OsuReplayFrame> frames, double time)
        {
            int next = nextFrameIndex(frames, time);

            if (next <= 0)
                return frames[0].Position;

            if (next >= frames.Count)
                return frames[^1].Position;

            var before = frames[next - 1];
            var after = frames[next];

            double span = after.Time - before.Time;

            if (span <= 0)
                return after.Position;

            return Vector2.Lerp(before.Position, after.Position, (float)((time - before.Time) / span));
        }

        /// <summary>Whether autoplay is holding a button at <paramref name="time"/>.</summary>
        public static bool HeldAt(IReadOnlyList<OsuReplayFrame> frames, double time)
        {
            int next = nextFrameIndex(frames, time);

            return next > 0 && frames[next - 1].Actions.Count > 0;
        }

        /// <summary>The index of the first frame at or after <paramref name="time"/>.</summary>
        private static int nextFrameIndex(IReadOnlyList<OsuReplayFrame> frames, double time)
        {
            int low = 0;
            int high = frames.Count;

            while (low < high)
            {
                int middle = (low + high) / 2;

                if (frames[middle].Time < time)
                    low = middle + 1;
                else
                    high = middle;
            }

            return low;
        }

        protected override void Dispose(bool isDisposing)
        {
            settling?.Cancel();
            generation?.Cancel();

            if (editorBeatmap.IsNotNull())
                editorBeatmap.BeatmapReprocessed -= onReprocessed;

            base.Dispose(isDisposing);
        }
    }
}
