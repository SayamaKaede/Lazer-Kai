// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Extensions.ObjectExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Localisation;
using osu.Game.Localisation;

namespace osu.Game.Screens.Edit.Hitsounds
{
    /// <summary>
    /// Marks a lane that will be heard together with others in one file once turned into a hitsound difficulty: a mark
    /// in the colour of each such file the lane is in, which the other lanes of that file are marked in as well.
    /// </summary>
    public partial class LaneMixMarks : CompositeDrawable
    {
        private readonly int lane;
        private readonly Direction along;

        [Resolved]
        private LaneSounds laneSounds { get; set; } = null!;

        /// <summary>
        /// The colours the lane is marked in, in the order of the marks.
        /// </summary>
        public IEnumerable<Colour4> Colours => InternalChildren.OfType<Mark>().Select(mark => mark.Of);

        /// <param name="lane">The lane.</param>
        /// <param name="along">Which way the marks are side by side where there are more than one, sharing the space.</param>
        public LaneMixMarks(int lane, Direction along)
        {
            this.lane = lane;
            this.along = along;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            laneSounds.MixesChanged += refresh;
            refresh();
        }

        private void refresh()
        {
            ClearInternal();

            var mine = laneSounds.Mixes.Select((lanes, number) => (lanes, number)).Where(mix => mix.lanes.Contains(lane)).ToArray();

            for (int i = 0; i < mine.Length; i++)
            {
                AddInternal(new Mark(mine[i].lanes, LaneSounds.ColourOfMix(mine[i].number))
                {
                    RelativeSizeAxes = Axes.Both,
                    RelativePositionAxes = Axes.Both,
                    Width = along == Direction.Horizontal ? 1f / mine.Length : 1,
                    Height = along == Direction.Vertical ? 1f / mine.Length : 1,
                    X = along == Direction.Horizontal ? (float)i / mine.Length : 0,
                    Y = along == Direction.Vertical ? (float)i / mine.Length : 0,
                });
            }
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            if (laneSounds.IsNotNull())
                laneSounds.MixesChanged -= refresh;
        }

        private partial class Mark : Box, IHasTooltip
        {
            public LocalisableString TooltipText { get; }

            /// <summary>
            /// The colour of the file this is the mark of.
            /// </summary>
            public readonly Colour4 Of;

            public Mark(IEnumerable<int> lanes, Colour4 colour)
            {
                TooltipText = LazerKaiHitsoundStrings.LanesMixed(string.Join(@" + ", lanes.Select(l => l + 1)));
                Colour = Of = colour;
            }
        }
    }
}
