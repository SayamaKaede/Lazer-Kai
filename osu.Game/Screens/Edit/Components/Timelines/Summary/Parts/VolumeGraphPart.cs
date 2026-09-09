// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Extensions.ObjectExtensions;
using osu.Framework.Graphics.Lines;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Graphics;
using osu.Game.Rulesets.Objects;
using osuTK;

namespace osu.Game.Screens.Edit.Components.Timelines.Summary.Parts
{
    /// <summary>
    /// Draws how loud the beatmap is over its length, across the summary timeline.
    /// </summary>
    /// <remarks>
    /// Drawn as steps rather than as a line running from one point to the next: a volume holds until something
    /// changes it, so a slope between two moments would draw a fade that the beatmap does not have.
    ///
    /// Takes no input. It is something to read at a glance while the markers over it are what gets clicked.
    /// </remarks>
    public partial class VolumeGraphPart : CompositeDrawable
    {
        private readonly Bindable<bool> shown = new Bindable<bool>();
        private readonly Bindable<EditorSelectionColour> colour = new Bindable<EditorSelectionColour>();
        private readonly Bindable<float> thickness = new Bindable<float>(2);
        private readonly Bindable<float> opacity = new Bindable<float>(1);

        private SmoothPath path = null!;

        private IReadOnlyList<VolumeCurve.Point> points = Array.Empty<VolumeCurve.Point>();
        private double lastObjectEnds;

        private readonly IBindable<WorkingBeatmap> beatmap = new Bindable<WorkingBeatmap>();

        [Resolved]
        private EditorBeatmap editorBeatmap { get; set; } = null!;

        [Resolved]
        private EditorClock editorClock { get; set; } = null!;

        [Resolved]
        private OsuColour colours { get; set; } = null!;

        public override bool ReceivePositionalInputAt(Vector2 screenSpacePos) => false;

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config, IBindable<WorkingBeatmap> beatmap)
        {
            this.beatmap.BindTo(beatmap);

            RelativeSizeAxes = Axes.Both;

            InternalChild = path = new SmoothPath { PathRadius = 1 };

            config.BindWith(OsuSetting.EditorShowVolumeGraph, shown);
            config.BindWith(OsuSetting.EditorVolumeGraphColour, colour);
            config.BindWith(OsuSetting.EditorVolumeGraphThickness, thickness);
            config.BindWith(OsuSetting.EditorVolumeGraphOpacity, opacity);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            shown.BindValueChanged(_ => updateAppearance());
            colour.BindValueChanged(_ => updateAppearance());
            opacity.BindValueChanged(_ => updateAppearance(), true);

            // the radius is half the thickness, and the shape has to be laid out again around the new one.
            thickness.BindValueChanged(_ => reread());

            editorBeatmap.BeatmapReprocessed += reread;

            // the curve is laid out against the length of the song, so it has to be laid out again once that is
            // known -- a track reports no length until it has loaded -- and again if the track itself changes.
            beatmap.ValueChanged += _ => redraw();
            editorClock.TrackChanged += redraw;

            reread();
        }

        private void reread()
        {
            points = VolumeCurve.Read(editorBeatmap);
            lastObjectEnds = editorBeatmap.HitObjects.Count > 0 ? editorBeatmap.HitObjects[^1].GetEndTime() : 0;

            redraw();
        }

        private void updateAppearance()
        {
            Alpha = shown.Value ? opacity.Value : 0;
            path.Colour = colour.Value.ToColour() ?? colours.Blue1;
        }

        protected override void Update()
        {
            base.Update();

            if (DrawSize != lastDrawSize || trackLength != lastTrackLength)
                redraw();
        }

        private Vector2 lastDrawSize;
        private double lastTrackLength;

        /// <remarks>
        /// A track has no length until it has loaded, and every other part of this timeline stands in a minute for
        /// it in the meantime rather than dividing by nothing.
        /// </remarks>
        private double trackLength => beatmap.Value.Track.IsLoaded ? Math.Max(1, beatmap.Value.Track.Length) : 60000;

        /// <summary>
        /// Lays the curve out across the current size.
        /// </summary>
        /// <remarks>
        /// In pixels rather than relative units: a path's vertices are its own, and it has no relative sizing to
        /// stretch them with. Redone when the timeline is resized for that reason.
        /// </remarks>
        private void redraw()
        {
            lastDrawSize = DrawSize;
            lastTrackLength = trackLength;

            path.PathRadius = Math.Max(thickness.Value, 0.1f) / 2;
            path.Vertices = VolumeCurve.Trace(points, lastObjectEnds, lastTrackLength, DrawSize);
            path.Position = Vector2.Zero;
        }

        protected override void Dispose(bool isDisposing)
        {
            if (editorBeatmap.IsNotNull())
                editorBeatmap.BeatmapReprocessed -= reread;

            if (editorClock.IsNotNull())
                editorClock.TrackChanged -= redraw;

            base.Dispose(isDisposing);
        }
    }
}
