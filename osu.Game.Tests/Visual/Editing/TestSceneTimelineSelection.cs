// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Testing;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Graphics.UserInterface;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Osu.Edit;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Screens.Edit.Compose.Components.Timeline;
using osu.Game.Tests.Beatmaps;
using osuTK;
using osuTK.Input;

namespace osu.Game.Tests.Visual.Editing
{
    public partial class TestSceneTimelineSelection : EditorTestScene
    {
        protected override Ruleset CreateEditorRuleset() => new OsuRuleset();

        protected override IBeatmap CreateBeatmap(RulesetInfo ruleset)
        {
            var beatmap = new TestBeatmap(ruleset, false);
            beatmap.ControlPointInfo.Add(0, new TimingControlPoint { BeatLength = 500 });
            return beatmap;
        }

        private TimelineBlueprintContainer blueprintContainer
            => Editor.ChildrenOfType<TimelineBlueprintContainer>().First();

        private Vector2 getPosition(HitObject hitObject) =>
            blueprintContainer.SelectionBlueprints.First(s => s.Item == hitObject).ScreenSpaceDrawQuad.Centre;

        private Vector2 getMiddlePosition(HitObject hitObject1, HitObject hitObject2) =>
            (getPosition(hitObject1) + getPosition(hitObject2)) / 2;

        private void moveMouseToObject(Func<HitObject> targetFunc)
        {
            AddStep("move mouse to object", () =>
            {
                var hitObject = targetFunc();
                InputManager.MoveMouseTo(getPosition(hitObject));
            });
        }

        [Test]
        public void TestContextMenuWithObjectBehind()
        {
            TimelineHitObjectBlueprint blueprint;

            AddStep("add object", () =>
            {
                EditorBeatmap.Add(new HitCircle { StartTime = 3000 });
            });

            AddStep("enter slider placement", () =>
            {
                InputManager.Key(Key.Number3);
                InputManager.MoveMouseTo(ScreenSpaceDrawQuad.Centre);
            });

            AddStep("start conflicting slider", () =>
            {
                InputManager.Click(MouseButton.Left);

                blueprint = this.ChildrenOfType<TimelineHitObjectBlueprint>().First();
                InputManager.MoveMouseTo(blueprint.ScreenSpaceDrawQuad.TopLeft - new Vector2(10, 0));
            });

            AddStep("end conflicting slider", () =>
            {
                InputManager.Click(MouseButton.Right);
            });

            AddStep("click object", () =>
            {
                InputManager.Key(Key.Number1);
                blueprint = this.ChildrenOfType<TimelineHitObjectBlueprint>().First();
                InputManager.MoveMouseTo(blueprint, new Vector2(-1, 0));
                InputManager.Click(MouseButton.Left);
            });

            AddStep("right click", () => InputManager.Click(MouseButton.Right));
            AddAssert("context menu open", () => this.ChildrenOfType<OsuContextMenu>().Any(m => m.State == MenuState.Open));
        }

        [Test]
        public void TestNudgeSelection()
        {
            HitCircle[] addedObjects = null;

            AddStep("add hitobjects", () => EditorBeatmap.AddRange(addedObjects = new[]
            {
                new HitCircle { StartTime = 500 },
                new HitCircle { StartTime = 1000, Position = new Vector2(100) },
                new HitCircle { StartTime = 1500, Position = new Vector2(200) },
                new HitCircle { StartTime = 2000, Position = new Vector2(300) },
            }));

            AddStep("select objects", () => EditorBeatmap.SelectedHitObjects.AddRange(addedObjects));

            AddStep("nudge forwards", () => InputManager.Key(Key.K));
            AddAssert("objects moved forwards in time", () => addedObjects[0].StartTime > 500);

            AddStep("nudge backwards", () => InputManager.Key(Key.J));
            AddAssert("objects reverted to original position", () => addedObjects[0].StartTime == 500);
        }

        [Test]
        public void TestBasicSelect()
        {
            var addedObject = new HitCircle { StartTime = 500 };
            AddStep("add hitobject", () => EditorBeatmap.Add(addedObject));

            moveMouseToObject(() => addedObject);
            AddStep("left click", () => InputManager.Click(MouseButton.Left));

            AddAssert("hitobject selected", () => EditorBeatmap.SelectedHitObjects.Single() == addedObject);

            var addedObject2 = new HitCircle
            {
                StartTime = 1000,
                Position = new Vector2(100),
            };

            AddStep("add one more hitobject", () => EditorBeatmap.Add(addedObject2));
            AddAssert("selection unchanged", () => EditorBeatmap.SelectedHitObjects.Single() == addedObject);

            moveMouseToObject(() => addedObject2);
            AddStep("left click", () => InputManager.Click(MouseButton.Left));
            AddAssert("hitobject selected", () => EditorBeatmap.SelectedHitObjects.Single() == addedObject2);
        }

        [Test]
        public void TestMultiSelect()
        {
            var addedObjects = new[]
            {
                new HitCircle { StartTime = 500 },
                new HitCircle { StartTime = 1000, Position = new Vector2(100) },
                new HitCircle { StartTime = 1500, Position = new Vector2(200) },
                new HitCircle { StartTime = 2000, Position = new Vector2(300) },
            };

            AddStep("add hitobjects", () => EditorBeatmap.AddRange(addedObjects));

            moveMouseToObject(() => addedObjects[0]);
            AddStep("click first", () => InputManager.Click(MouseButton.Left));

            AddAssert("hitobject selected", () => EditorBeatmap.SelectedHitObjects.Single() == addedObjects[0]);

            AddStep("hold control", () => InputManager.PressKey(Key.ControlLeft));

            moveMouseToObject(() => addedObjects[1]);
            AddStep("click second", () => InputManager.Click(MouseButton.Left));
            AddAssert("2 hitobjects selected", () => EditorBeatmap.SelectedHitObjects.Count == 2 && EditorBeatmap.SelectedHitObjects.Contains(addedObjects[1]));

            moveMouseToObject(() => addedObjects[2]);
            AddStep("click third", () => InputManager.Click(MouseButton.Left));
            AddAssert("3 hitobjects selected", () => EditorBeatmap.SelectedHitObjects.Count == 3 && EditorBeatmap.SelectedHitObjects.Contains(addedObjects[2]));

            moveMouseToObject(() => addedObjects[1]);
            AddStep("click second", () => InputManager.Click(MouseButton.Left));
            AddAssert("2 hitobjects selected", () => EditorBeatmap.SelectedHitObjects.Count == 2 && !EditorBeatmap.SelectedHitObjects.Contains(addedObjects[1]));

            AddStep("release control", () => InputManager.ReleaseKey(Key.ControlLeft));
        }

        [Test]
        public void TestBasicDeselect()
        {
            var addedObject = new HitCircle { StartTime = 500 };
            AddStep("add hitobject", () => EditorBeatmap.Add(addedObject));

            moveMouseToObject(() => addedObject);
            AddStep("left click", () => InputManager.Click(MouseButton.Left));

            AddAssert("hitobject selected", () => EditorBeatmap.SelectedHitObjects.Single() == addedObject);

            AddStep("click away", () =>
            {
                InputManager.MoveMouseTo(Editor.ChildrenOfType<Timeline>().First().ScreenSpaceDrawQuad.TopLeft + new Vector2(5));
                InputManager.Click(MouseButton.Left);
            });

            AddAssert("selection lost", () => EditorBeatmap.SelectedHitObjects.Count == 0);
        }

        [Test]
        public void TestQuickDelete()
        {
            var addedObject = new HitCircle
            {
                StartTime = 0,
            };

            AddStep("add hitobject", () => EditorBeatmap.Add(addedObject));

            moveMouseToObject(() => addedObject);

            AddStep("hold shift", () => InputManager.PressKey(Key.ShiftLeft));
            AddStep("right click", () => InputManager.Click(MouseButton.Right));
            AddStep("release shift", () => InputManager.ReleaseKey(Key.ShiftLeft));

            AddAssert("no hitobjects in beatmap", () => EditorBeatmap.HitObjects.Count == 0);
        }

        /// <summary>
        /// The handle on a slider's end takes hold of its object, which took hold of every button with it: a quick
        /// delete over the end selected the slider and stopped there, leaving no way to delete it from that spot.
        /// </summary>
        [Test]
        public void TestQuickDeleteOverASliderEnd()
        {
            var addedObject = new Slider
            {
                StartTime = 0,
                Path = new SliderPath(new[]
                {
                    new PathControlPoint(new Vector2(0), PathType.LINEAR),
                    new PathControlPoint(new Vector2(100, 0)),
                })
            };

            AddStep("add slider", () => EditorBeatmap.Add(addedObject));

            AddStep("move to the end handle", () => InputManager.MoveMouseTo(Editor.ChildrenOfType<TimelineHitObjectBlueprint.DragArea>().Last()));
            AddStep("middle click", () => InputManager.Click(MouseButton.Middle));

            AddAssert("no hitobjects in beatmap", () => EditorBeatmap.HitObjects.Count, () => Is.Zero);
        }

        [Test]
        public void TestRangeSelect()
        {
            var addedObjects = new[]
            {
                new HitCircle { StartTime = 500 },
                new HitCircle { StartTime = 1000, Position = new Vector2(100) },
                new HitCircle { StartTime = 1500, Position = new Vector2(200) },
                new HitCircle { StartTime = 2000, Position = new Vector2(300) },
                new HitCircle { StartTime = 2500, Position = new Vector2(400) },
            };

            AddStep("add hitobjects", () => EditorBeatmap.AddRange(addedObjects));

            moveMouseToObject(() => addedObjects[1]);
            AddStep("click second", () => InputManager.Click(MouseButton.Left));

            AddAssert("hitobject selected", () => EditorBeatmap.SelectedHitObjects.Single() == addedObjects[1]);

            AddStep("hold shift", () => InputManager.PressKey(Key.ShiftLeft));

            moveMouseToObject(() => addedObjects[3]);
            AddStep("click fourth", () => InputManager.Click(MouseButton.Left));
            assertSelectionIs(addedObjects.Skip(1).Take(3));

            moveMouseToObject(() => addedObjects[0]);
            AddStep("click first", () => InputManager.Click(MouseButton.Left));
            assertSelectionIs(addedObjects.Take(2));

            AddStep("clear selection", () => EditorBeatmap.SelectedHitObjects.Clear());
            AddStep("release shift", () => InputManager.ReleaseKey(Key.ShiftLeft));

            moveMouseToObject(() => addedObjects[0]);
            AddStep("click first", () => InputManager.Click(MouseButton.Left));
            assertSelectionIs(addedObjects.Take(1));

            AddStep("hold ctrl", () => InputManager.PressKey(Key.ControlLeft));
            moveMouseToObject(() => addedObjects[2]);
            AddStep("click third", () => InputManager.Click(MouseButton.Left));
            assertSelectionIs(new[] { addedObjects[0], addedObjects[2] });

            AddStep("hold shift", () => InputManager.PressKey(Key.ShiftLeft));
            moveMouseToObject(() => addedObjects[4]);
            AddStep("click fifth", () => InputManager.Click(MouseButton.Left));
            assertSelectionIs(addedObjects.Except(new[] { addedObjects[1] }));

            moveMouseToObject(() => addedObjects[0]);
            AddStep("click first", () => InputManager.Click(MouseButton.Left));
            assertSelectionIs(addedObjects);

            AddStep("clear selection", () => EditorBeatmap.SelectedHitObjects.Clear());
            moveMouseToObject(() => addedObjects[0]);
            AddStep("click first", () => InputManager.Click(MouseButton.Left));
            assertSelectionIs(addedObjects.Take(1));

            moveMouseToObject(() => addedObjects[1]);
            AddStep("click first", () => InputManager.Click(MouseButton.Left));
            assertSelectionIs(addedObjects.Take(2));

            moveMouseToObject(() => addedObjects[2]);
            AddStep("click first", () => InputManager.Click(MouseButton.Left));
            assertSelectionIs(addedObjects.Take(3));

            AddStep("release keys", () =>
            {
                InputManager.ReleaseKey(Key.ControlLeft);
                InputManager.ReleaseKey(Key.ShiftLeft);
            });
        }

        [Test]
        public void TestRangeSelectAfterExternalSelection()
        {
            var addedObjects = new[]
            {
                new HitCircle { StartTime = 500 },
                new HitCircle { StartTime = 1000, Position = new Vector2(100) },
                new HitCircle { StartTime = 1500, Position = new Vector2(200) },
                new HitCircle { StartTime = 2000, Position = new Vector2(300) },
            };

            AddStep("add hitobjects", () => EditorBeatmap.AddRange(addedObjects));

            AddStep("select all without mouse", () => EditorBeatmap.SelectedHitObjects.AddRange(EditorBeatmap.HitObjects));
            assertSelectionIs(addedObjects);

            AddStep("hold down shift", () => InputManager.PressKey(Key.ShiftLeft));

            moveMouseToObject(() => addedObjects[1]);
            AddStep("click second object", () => InputManager.Click(MouseButton.Left));
            assertSelectionIs(addedObjects);

            moveMouseToObject(() => addedObjects[3]);
            AddStep("click fourth object", () => InputManager.Click(MouseButton.Left));
            assertSelectionIs(addedObjects.Skip(1));

            AddStep("release shift", () => InputManager.ReleaseKey(Key.ShiftLeft));
        }

        [Test]
        public void TestBasicDragSelection()
        {
            var addedObjects = new[]
            {
                new HitCircle { StartTime = 0 },
                new HitCircle { StartTime = 500, Position = new Vector2(100) },
                new HitCircle { StartTime = 1000, Position = new Vector2(200) },
                new HitCircle { StartTime = 1500, Position = new Vector2(300) },
            };
            AddStep("add hitobjects", () => EditorBeatmap.AddRange(addedObjects));

            AddStep("move mouse", () => InputManager.MoveMouseTo(getMiddlePosition(addedObjects[0], addedObjects[1])));
            AddStep("mouse down", () => InputManager.PressButton(MouseButton.Left));

            AddStep("drag to select", () => InputManager.MoveMouseTo(getMiddlePosition(addedObjects[2], addedObjects[3])));
            assertSelectionIs(new[] { addedObjects[1], addedObjects[2] });

            AddStep("drag to deselect", () => InputManager.MoveMouseTo(getMiddlePosition(addedObjects[1], addedObjects[2])));
            assertSelectionIs(new[] { addedObjects[1] });

            AddStep("mouse up", () => InputManager.ReleaseButton(MouseButton.Left));
            assertSelectionIs(new[] { addedObjects[1] });
        }

        [Test]
        public void TestFastDragSelection()
        {
            var addedObjects = new[]
            {
                new HitCircle { StartTime = 0 },
                new HitCircle { StartTime = 500 },
                new HitCircle { StartTime = 20000, Position = new Vector2(100) },
                new HitCircle { StartTime = 31000, Position = new Vector2(200) },
                new HitCircle { StartTime = 60000, Position = new Vector2(300) },
            };

            AddStep("add hitobjects", () => EditorBeatmap.AddRange(addedObjects));

            AddStep("move mouse", () => InputManager.MoveMouseTo(getMiddlePosition(addedObjects[0], addedObjects[1])));
            AddStep("mouse down", () => InputManager.PressButton(MouseButton.Left));
            AddStep("start drag", () => InputManager.MoveMouseTo(getPosition(addedObjects[1])));

            AddStep("jump editor clock", () => EditorClock.Seek(30000));
            AddStep("jump editor clock", () => EditorClock.Seek(60000));
            AddStep("end drag", () => InputManager.ReleaseButton(MouseButton.Left));
            assertSelectionIs(addedObjects.Skip(1));
            // only what is on screen has a blueprint now, so not every selected object has one (see TestSceneLargeSelection).
            // what is on screen does, and shows as selected.
            AddUntilStep("the object on screen has a blueprint", () => blueprintContainer.SelectionBlueprints.Any(b => b.Item == addedObjects[4]));
            AddAssert("every blueprint of a selected object shows it", () => blueprintContainer.SelectionBlueprints
                                                                                     .Where(b => EditorBeatmap.SelectedHitObjects.Contains(b.Item))
                                                                                     .All(b => b.IsSelected));
        }

        /// <summary>
        /// A long stretch selected on the timeline is pasted whole. Part of it went missing on osu! (reported 2026-10-22).
        /// </summary>
        [TestCase(false)]
        [TestCase(true)]
        public void TestALongStretchSelectedOnTheTimelineIsPastedWhole(bool playing)
        {
            const double spacing = 80;

            var addedObjects = Enumerable.Range(0, 110).Select(i => (HitObject)(i % 10 == 9
                ? new Spinner { StartTime = 1000 + i * spacing, EndTime = 1000 + i * spacing + 40 }
                : i % 2 == 0
                    ? new HitCircle { StartTime = 1000 + i * spacing, Position = new Vector2(i % 400, 100), NewCombo = i % 4 == 0 }
                    : new Slider
                    {
                        StartTime = 1000 + i * spacing,
                        Position = new Vector2(i % 400, 200),
                        RepeatCount = i % 3,
                        SliderVelocityMultiplier = 0.5 + i % 4 * 0.25,
                        Path = new SliderPath(i % 3 == 0 ? PathType.BEZIER : PathType.PERFECT_CURVE, new[] { Vector2.Zero, new Vector2(40, -30), new Vector2(80, 0) })
                    })).ToArray();

            AddStep("add hitobjects", () => EditorBeatmap.AddRange(addedObjects));
            AddStep("go to the start", () => EditorClock.Seek(1000));

            AddStep("move mouse before the first", () => InputManager.MoveMouseTo(getPosition(addedObjects[0]) - new Vector2(20, 0)));
            AddStep("mouse down", () => InputManager.PressButton(MouseButton.Left));
            AddStep("start drag", () => InputManager.MoveMouseTo(getPosition(addedObjects[1])));

            if (playing)
            {
                AddStep("play", () => EditorClock.Start());
                AddUntilStep("past the last", () => EditorClock.CurrentTime, () => Is.GreaterThan(addedObjects.Last().GetEndTime() + 500));
                AddStep("stop", () => EditorClock.Stop());
            }
            else
            {
                // a little at a time, the way a wheel moves it.
                AddRepeatStep("scroll forward", () => EditorClock.Seek(EditorClock.CurrentTime + 100), 100);
            }

            AddStep("end drag", () => InputManager.ReleaseButton(MouseButton.Left));

            AddAssert("everything was selected", () => EditorBeatmap.SelectedHitObjects.Count, () => Is.EqualTo(addedObjects.Length));

            AddStep("copy", () => Editor.Copy());
            AddStep("go back to the start", () => EditorClock.Seek(0));
            AddStep("paste", () => Editor.Paste());

            AddAssert("everything was pasted", () => EditorBeatmap.HitObjects.Count, () => Is.EqualTo(addedObjects.Length * 2));
        }

        [Test]
        public void TestDragSelectionDuringPlacement()
        {
            var addedObjects = new[]
            {
                new Slider
                {
                    StartTime = 300,
                    Path = new SliderPath([
                        new PathControlPoint(),
                        new PathControlPoint(new Vector2(200)),
                    ])
                },
            };
            AddStep("add hitobjects", () => EditorBeatmap.AddRange(addedObjects));

            AddStep("seek to 700", () => EditorClock.Seek(700));
            AddStep("select spinner placement tool", () =>
            {
                InputManager.Key(Key.Number4);
                InputManager.MoveMouseTo(this.ChildrenOfType<OsuHitObjectComposer>().Single());
            });
            AddStep("begin spinner placement", () => InputManager.Click(MouseButton.Left));
            AddStep("seek to 1500", () => EditorClock.Seek(1500));

            AddStep("start dragging", () =>
            {
                var blueprintQuad = blueprintContainer.SelectionBlueprints[1].ScreenSpaceDrawQuad;
                var dragStartPos = (blueprintQuad.TopLeft + blueprintQuad.BottomLeft) / 2 - new Vector2(30, 0);
                InputManager.MoveMouseTo(dragStartPos);
                InputManager.PressButton(MouseButton.Left);
            });

            AddStep("select entire object", () =>
            {
                var blueprintQuad = blueprintContainer.SelectionBlueprints[1].ScreenSpaceDrawQuad;
                var dragStartPos = (blueprintQuad.TopRight + blueprintQuad.BottomRight) / 2 + new Vector2(30, 0);
                InputManager.MoveMouseTo(dragStartPos);
            });
            AddStep("end drag", () => InputManager.ReleaseButton(MouseButton.Left));

            AddUntilStep("hitobject selected", () => EditorBeatmap.SelectedHitObjects, () => NUnit.Framework.Contains.Item(addedObjects[0]));
            AddAssert("placement committed", () => EditorBeatmap.HitObjects, () => Has.Count.EqualTo(2));
        }

        [Test]
        public void TestBreakRemoval()
        {
            var addedObjects = new[]
            {
                new HitCircle { StartTime = 0 },
                new HitCircle { StartTime = 5000 },
            };

            AddStep("add hitobjects", () => EditorBeatmap.AddRange(addedObjects));
            AddAssert("beatmap has one break", () => EditorBeatmap.Breaks, () => Has.Count.EqualTo(1));

            AddStep("move mouse to break", () => InputManager.MoveMouseTo(this.ChildrenOfType<TimelineBreak>().Single()));
            AddStep("right click", () => InputManager.Click(MouseButton.Right));

            AddStep("move mouse to delete menu item", () => InputManager.MoveMouseTo(this.ChildrenOfType<OsuContextMenu>().First(m => m.State == MenuState.Open)
                                                                                         .ChildrenOfType<DrawableOsuMenuItem>().First()));
            AddStep("click", () => InputManager.Click(MouseButton.Left));

            AddAssert("beatmap has no breaks", () => EditorBeatmap.Breaks, () => Is.Empty);
            AddAssert("break piece went away", () => this.ChildrenOfType<TimelineBreak>().Count(), () => Is.Zero);
        }

        private void assertSelectionIs(IEnumerable<HitObject> hitObjects)
            => AddAssert("correct hitobjects selected", () => EditorBeatmap.SelectedHitObjects.OrderBy(h => h.StartTime).SequenceEqual(hitObjects));
    }
}
