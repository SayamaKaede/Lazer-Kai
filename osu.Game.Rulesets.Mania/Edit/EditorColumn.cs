// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.ObjectExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Game.Configuration;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Rulesets.Mania.Objects.Drawables;
using osu.Game.Rulesets.Mania.UI;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Drawables;
using osu.Game.Screens.Edit.Hitsounds;
using osuTK.Graphics;

namespace osu.Game.Rulesets.Mania.Edit
{
    public partial class EditorColumn : Column
    {
        public EditorColumn(int index, bool isSpecial)
            : base(index, isSpecial)
        {
        }

        /// <summary>
        /// The sounds of the lanes, on a difficulty made to hitsound in.
        /// </summary>
        [Resolved(canBeNull: true)]
        private LaneSounds? laneSounds { get; set; }

        /// <summary>
        /// What this lane sounds as, at its top: the lanes are told apart by nothing else.
        /// </summary>
        public TruncatingSpriteText? SoundLabel { get; private set; }

        /// <summary>
        /// Where <see cref="SoundLabel"/> is, across the lane: clicked, the notes of the lane are selected. The lanes
        /// have no mouse of their own in the editor, so that is done by what is over them and has it (see
        /// <see cref="ManiaBlueprintContainer"/>), which also says when the mouse is on this for it to be lit.
        /// </summary>
        public Drawable? SoundLabelArea { get; private set; }

        private Box? soundLabelLit;

        /// <summary>
        /// Whether <see cref="SoundLabelArea"/> is lit, as something the mouse is on that can be clicked.
        /// </summary>
        public bool SoundLabelLit
        {
            set
            {
                // at once: the lanes go by the editor's clock, and nothing fades while that is stopped.
                if (soundLabelLit != null)
                    soundLabelLit.Alpha = value ? 0.3f : 0;
            }
        }

        /// <summary>
        /// Whether the keys under the judgement line are left out, and the lane goes on there with the notes that have
        /// gone by, as in stable's editor (2026-10-12).
        /// </summary>
        private readonly Bindable<bool> hideKeys = new Bindable<bool>();

        private IBindable<double> timeRange = null!;

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config)
        {
            config.BindWith(OsuSetting.EditorManiaHideKeys, hideKeys);
            timeRange = ScrollingInfo.TimeRange.GetBoundCopy();

            if (laneSounds == null)
                return;

            AddInternal(SoundLabelArea = new Container
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Depth = float.MinValue,
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Color4.Black,
                        Alpha = 0.6f,
                    },
                    soundLabelLit = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Color4.White,
                        Alpha = 0,
                    },
                    SoundLabel = new TruncatingSpriteText
                    {
                        RelativeSizeAxes = Axes.X,
                        Font = OsuFont.GetFont(size: 12, weight: FontWeight.SemiBold),
                        Padding = new MarginPadding { Horizontal = 3, Vertical = 2 },
                    },
                    // right under what the lane sounds as, across the lane: lanes that will be mixed into one file
                    // are told from the rest by it, and from the lanes of another such file by its colour.
                    new LaneMixMarks(Index, osu.Framework.Graphics.Direction.Horizontal)
                    {
                        RelativeSizeAxes = Axes.X,
                        Height = 4,
                        Anchor = Anchor.BottomLeft,
                        Origin = Anchor.TopLeft,
                        BypassAutoSizeAxes = Axes.Y,
                    },
                }
            });

            laneSounds.Changed += updateSoundLabel;
            updateSoundLabel();
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            hideKeys.BindValueChanged(hide =>
            {
                KeyArea.Alpha = hide.NewValue ? 0 : 1;
                refreshNotesGoneBy();
            }, true);

            // how long a note that has gone by is kept for is worked out from it.
            timeRange.BindValueChanged(_ => refreshNotesGoneBy());
        }

        /// <summary>
        /// Has the notes on screen do again what they do once they have gone by, which depends on <see cref="hideKeys"/>.
        /// </summary>
        private void refreshNotesGoneBy()
        {
            foreach (var drawable in HitObjectContainer.AliveObjects)
                drawable.RefreshStateTransforms();
        }

        /// <summary>
        /// Keeps a note that has been hit or missed as it was, to go on down the lane under the judgement line until
        /// it is off the bottom.
        /// </summary>
        private void keepShown(DrawableHitObject note)
        {
            // hit or missed, what a note does is fade out: at once, or in a moment.
            foreach (var fade in note.Transforms.Where(t => t.TargetMember == nameof(Alpha) && t.StartTime >= note.HitStateUpdateTime).ToArray())
                note.RemoveTransform(fade);

            using (note.BeginAbsoluteSequence(note.HitStateUpdateTime))
                note.FadeTo(1);

            // what is under the line is less than the lane above it, so the time the lane shows is always enough.
            note.LifetimeEnd = note.HitObject.GetEndTime() + timeRange.Value;
        }

        private void updateSoundLabel()
        {
            if (laneSounds != null && SoundLabel != null)
                SoundLabel.Text = laneSounds[Index].DisplayName;
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            if (laneSounds.IsNotNull())
                laneSounds.Changed -= updateSoundLabel;
        }

        protected override void OnNewDrawableHitObject(DrawableHitObject drawableHitObject)
        {
            base.OnNewDrawableHitObject(drawableHitObject);
            drawableHitObject.ApplyCustomUpdateState += (dho, state) =>
            {
                // hold notes are left as they were: what has been held of one is cut off at the judgement line by the
                // hold note itself.
                if (hideKeys.Value && dho is DrawableNote and not (DrawableHoldNoteHead or DrawableHoldNoteTail))
                {
                    if (state != ArmedState.Idle)
                        keepShown(dho);

                    return;
                }

                switch (dho)
                {
                    // hold note heads are exempt from what follows due to the "freezing" mechanic
                    // which already ensures they'll never fade away on their own.
                    case DrawableHoldNoteHead:
                        break;

                    // mania features instantaneous hitobject fade-outs.
                    // this means that without manual intervention stopping the clock at the precise time of hitting the object
                    // means the object will fade out.
                    // this is anti-user in editor contexts, as the user is expecting to continue the see the note on the receptor line.
                    // therefore, apply a crude workaround to prevent it from going away.
                    default:
                    {
                        if (state == ArmedState.Hit)
                            dho.FadeTo(1).Delay(1).FadeOut().Expire();
                        break;
                    }
                }
            };
        }
    }
}
