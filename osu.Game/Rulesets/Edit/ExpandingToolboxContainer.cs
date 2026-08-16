// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Containers;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Input.Events;
using osu.Framework.Utils;
using osu.Game.Configuration;
using osu.Game.Graphics.Containers;
using osu.Game.Screens.Edit;
using osuTK;

namespace osu.Game.Rulesets.Edit
{
    public partial class ExpandingToolboxContainer : ExpandingContainer
    {
        protected override double HoverExpansionDelay => 250;

        protected override bool ExpandOnHover => expandOnHover;

        private readonly Bindable<bool> contractSidebars = new Bindable<bool>();

        /// <summary>
        /// When set, this toolbox stays expanded regardless of the contracting behaviour applied to the others.
        /// </summary>
        public readonly Bindable<bool> AlwaysExpanded = new Bindable<bool>();

        private bool expandOnHover;
        private OffsetMaintainingScrollContainer scrollContainer = null!;

        [Resolved]
        private Editor? editor { get; set; }

        public ExpandingToolboxContainer(float contractedWidth, float expandedWidth)
            : base(contractedWidth, expandedWidth)
        {
            RelativeSizeAxes = Axes.Y;

            FillFlow.Spacing = new Vector2(5);
            FillFlow.Padding = new MarginPadding { Vertical = 5 };

            Expanded.Value = true;
        }

        /// <summary>
        /// Holds groups the user pinned, above the scrolling area so they stay put.
        /// </summary>
        private FillFlowContainer pinnedArea = null!;

        private readonly Dictionary<EditorToolboxGroup, float> originalLayoutPositions = new Dictionary<EditorToolboxGroup, float>();

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config)
        {
            config.BindWith(OsuSetting.EditorContractSidebars, contractSidebars);

            AddInternal(pinnedArea = new FillFlowContainer
            {
                Name = @"Pinned groups",
                Depth = -1,
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = FillFlow.Spacing,
                Padding = new MarginPadding { Top = 5 },
            });
        }

        protected override OsuScrollContainer CreateScrollContainer() => scrollContainer = new OffsetMaintainingScrollContainer();

        protected override void LoadComplete()
        {
            base.LoadComplete();

            var inputManager = GetContainingInputManager();

            if (inputManager != null)
            {
                Expanded.BindValueChanged(_ =>
                {
                    // When state changes from expanded -> collapsed the mouse is no longer within the toolbox so there would be no
                    // hovered children if we used the mouse position directly
                    var position = new Vector2(ScreenSpaceDrawQuad.Centre.X, inputManager.CurrentState.Mouse.Position.Y);

                    scrollContainer.TargetDrawable = Children.FirstOrDefault(it => it.Contains(position));
                });
            }

            foreach (var group in Children.OfType<EditorToolboxGroup>().ToArray())
            {
                // remembered so unpinning can put the group back where it was rather than at the end.
                originalLayoutPositions[group] = FillFlow.GetLayoutPosition(group);

                var captured = group;
                group.Pinned.BindValueChanged(pinned => setPinned(captured, pinned.NewValue));
            }
        }

        private void setPinned(EditorToolboxGroup group, bool pinned)
        {
            if (pinned)
            {
                if (FillFlow.Contains(group))
                {
                    FillFlow.Remove(group, false);
                    pinnedArea.Add(group);
                }
            }
            else if (pinnedArea.Contains(group))
            {
                pinnedArea.Remove(group, false);
                FillFlow.Add(group);
                FillFlow.SetLayoutPosition(group, originalLayoutPositions[group]);
            }
        }

        protected override void Update()
        {
            base.Update();

            // keeps the scrolling groups clear of whatever is pinned above them.
            scrollContainer.Padding = new MarginPadding { Top = pinnedArea.DrawHeight };

            bool requireContracting = !AlwaysExpanded.Value && (contractSidebars.Value || editor?.DrawSize.X / editor?.DrawSize.Y < 1.7f);

            if (expandOnHover != requireContracting)
            {
                expandOnHover = requireContracting;
                Expanded.Value = !expandOnHover;
            }
        }

        protected override bool OnMouseDown(MouseDownEvent e) => true;

        protected override bool OnClick(ClickEvent e) => true;

        private partial class OffsetMaintainingScrollContainer : OsuScrollContainer
        {
            private Drawable? targetDrawable;
            private float targetPosition;

            public Drawable? TargetDrawable
            {
                get => targetDrawable;
                set
                {
                    targetDrawable = value;

                    if (value != null)
                        targetPosition = ToLocalSpace(value.ScreenSpaceDrawQuad.TopLeft).Y;
                }
            }

            protected override void UpdateAfterChildren()
            {
                if (targetDrawable != null)
                {
                    float currentPosition = ToLocalSpace(targetDrawable.ScreenSpaceDrawQuad.TopLeft).Y;

                    if (!Precision.AlmostEquals(targetPosition, currentPosition))
                    {
                        double offset = currentPosition - targetPosition;

                        double scrollTarget = Math.Clamp(Current + offset, 0, ScrollableExtent);

                        ScrollTo(scrollTarget, false, double.PositiveInfinity);
                    }
                }

                base.UpdateAfterChildren();
            }

            protected override void OnUserScroll(double value, bool animated = true, double? distanceDecay = null)
            {
                targetDrawable = null;

                base.OnUserScroll(value, animated, distanceDecay);
            }
        }
    }
}
