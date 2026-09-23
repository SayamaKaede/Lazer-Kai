// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Game.Audio;
using osu.Game.Rulesets.Objects.Drawables;
using osuTK;
using osuTK.Graphics;
using osu.Framework.Graphics.Shapes;
using osu.Game.Skinning;
using osu.Framework.Graphics.Containers;

namespace osu.Game.Rulesets.Osu.Objects.Drawables
{
    public partial class DrawableSliderTick : DrawableOsuHitObject
    {
        public const double ANIM_DURATION = 150;

        public const float DEFAULT_TICK_SIZE = 16;

        protected DrawableSlider DrawableSlider => (DrawableSlider)ParentHitObject;

        private SkinnableDrawable scaleContainer;

        public DrawableSliderTick()
            : base(null)
        {
        }

        public DrawableSliderTick(SliderTick sliderTick)
            : base(sliderTick)
        {
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Size = OsuHitObject.OBJECT_DIMENSIONS;
            Origin = Anchor.Centre;

            AddInternal(scaleContainer = new SkinnableDrawable(new OsuSkinComponentLookup(OsuSkinComponents.SliderScorePoint), _ => new CircularContainer
            {
                Masking = true,
                Origin = Anchor.Centre,
                Size = new Vector2(DEFAULT_TICK_SIZE),
                BorderThickness = DEFAULT_TICK_SIZE / 4,
                BorderColour = Color4.White,
                Child = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = AccentColour.Value,
                    Alpha = 0.3f,
                }
            })
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
            });

            ScaleBindable.BindValueChanged(scale => scaleContainer.Scale = new Vector2(scale.NewValue));

            Samples?.AddAdjustment(AdjustableProperty.Volume, lineVolume);
        }

        [Resolved(CanBeNull = true)]
        private ILineVolumes lineVolumes { get; set; }

        private readonly BindableDouble lineVolume = new BindableDouble(1);

        /// <summary>
        /// Whether the tick's sounds were loaded to follow the lines: at full volume, turned down by the line in effect.
        /// </summary>
        private bool followingLines;

        // stable plays a tick at whatever the green line in effect where it is says, not at the slider's own volume.
        public override IEnumerable<HitSampleInfo> GetSamples()
        {
            followingLines = lineVolumes?.Enabled == true;
            return followingLines ? base.GetSamples().Select(s => s.With(newVolume: 100)) : base.GetSamples();
        }

        protected override void Update()
        {
            base.Update();

            if ((lineVolumes?.Enabled == true) != followingLines)
                LoadSamples();

            lineVolume.Value = followingLines ? lineVolumes!.VolumeAt(HitObject.StartTime) / 100.0 : 1;
        }

        protected override void OnApply()
        {
            base.OnApply();

            Position = HitObject.Position - DrawableSlider.HitObject.Position;
        }

        protected override void CheckForResult(bool userTriggered, double timeOffset) => DrawableSlider.SliderInputManager.TryJudgeNestedObject(this, timeOffset);

        protected override void UpdateInitialTransforms()
        {
            this.FadeOut().FadeIn(ANIM_DURATION);
            this.ScaleTo(0.5f).ScaleTo(1f, ANIM_DURATION * 4, Easing.OutElasticHalf);
        }

        protected override void UpdateHitStateTransforms(ArmedState state)
        {
            base.UpdateHitStateTransforms(state);

            switch (state)
            {
                case ArmedState.Idle:
                    this.Delay(HitObject.TimePreempt).FadeOut();
                    break;

                case ArmedState.Miss:
                    this.FadeOut(ANIM_DURATION, Easing.OutQuint);
                    break;

                case ArmedState.Hit:
                    this.FadeOut(ANIM_DURATION, Easing.OutQuint);
                    this.ScaleTo(Scale * 1.5f, ANIM_DURATION, Easing.Out);
                    break;
            }
        }
    }
}
