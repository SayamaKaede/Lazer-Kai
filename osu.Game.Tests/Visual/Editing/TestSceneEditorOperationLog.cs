// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Platform;
using osu.Framework.Testing;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Configuration;
using osu.Game.Input;
using osu.Game.Overlays.Settings.Sections.LazerKai;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Screens.Edit;
using osu.Game.Storyboards;
using osuTK;
using osuTK.Input;

namespace osu.Game.Tests.Visual.Editing
{
    /// <summary>
    /// The editor's operation log, for working on this game: what it writes down, and how much (2026-10-28).
    /// </summary>
    public partial class TestSceneEditorOperationLog : EditorTestScene
    {
        protected override Ruleset CreateEditorRuleset() => new OsuRuleset();

        protected override bool IsolateSavingFromDatabase => false;

        protected override WorkingBeatmap CreateWorkingBeatmap(IBeatmap beatmap, Storyboard? storyboard = null) => new DummyWorkingBeatmap(Audio, null);

        [Resolved]
        private OsuConfigManager config { get; set; } = null!;

        // the game's own sits above this scene's input manager, which the steps' input does not get past.
        private readonly OperationInputWatcher inputWatcher = new OperationInputWatcher();

        protected override IReadOnlyDependencyContainer CreateChildDependencies(IReadOnlyDependencyContainer parent)
        {
            var dependencies = new DependencyContainer(base.CreateChildDependencies(parent));
            dependencies.Cache(inputWatcher);
            return dependencies;
        }

        [BackgroundDependencyLoader]
        private void load() => Add(inputWatcher);

        public override void SetUpSteps()
        {
            AddStep("turn the log on", () => config.SetValue(OsuSetting.EditorOperationLog, true));
            base.SetUpSteps();
            AddStep("make new beatmap unique", () => EditorBeatmap.Metadata.Title = Guid.NewGuid().ToString());
        }

        [Test]
        public void TestChangesUndoKeysAndClicksAreWrittenDown()
        {
            AddAssert("a log is written", () => Editor.OperationLog?.Path, () => Is.Not.Null);

            AddStep("put a red line", () => EditorBeatmap.ControlPointInfo.Add(0, new TimingControlPoint { BeatLength = 500 }));
            AddStep("place a circle", () => EditorBeatmap.Add(new HitCircle { StartTime = 1000, Position = new Vector2(100) }));
            AddStep("click the middle of the screen", () =>
            {
                InputManager.MoveMouseTo(Editor.ScreenSpaceDrawQuad.Centre);
                InputManager.Click(MouseButton.Left);
            });
            // a step each: what is held is read once a frame, and a key let go in the frame it was pressed in is not.
            AddStep("hold ctrl", () => InputManager.PressKey(Key.ControlLeft));
            AddStep("press z", () => InputManager.PressKey(Key.Z));
            AddStep("let go of z", () => InputManager.ReleaseKey(Key.Z));
            AddStep("let go of ctrl", () => InputManager.ReleaseKey(Key.ControlLeft));

            AddUntilStep("the circle is gone", () => EditorBeatmap.HitObjects.Count == 0);

            AddAssert("opened with the file", () => entries().First()["kind"]!.ToString(), () => Is.EqualTo("open"));
            AddAssert("the circle is put in", () => entries().Any(e => e["kind"]!.ToString() == "change"
                                                                       && linesIn(e, "added").Any(l => l.StartsWith("100,100,1000,", StringComparison.Ordinal))));
            AddAssert("the click is written down", () => entries().Any(e => e["kind"]!.ToString() == "press" && e["button"]!.ToString() == "Left"));
            AddAssert("ctrl+z is written down", () => entries().Any(e => e["kind"]!.ToString() == "key" && e["key"]!.ToString() == "Z" && e["keys"]!.ToString() == "Ctrl"));
            AddAssert("the undo takes the circle out", () => entries().Any(e => e["kind"]!.ToString() == "undo"
                                                                              && linesIn(e, "removed").Any(l => l.StartsWith("100,100,1000,", StringComparison.Ordinal))));
        }

        /// <summary>
        /// A click is written down with what was clicked, not the containers covering the whole screen in front of it
        /// (2026-10-28: every click in a real log said "PlatformActionContainer").
        /// </summary>
        [Test]
        public void TestAClickSaysWhatWasClicked()
        {
            AddStep("click the file menu", () =>
            {
                InputManager.MoveMouseTo(Editor.ChildrenOfType<SpriteText>().First(t => t.Text.ToString() == "File"));
                InputManager.Click(MouseButton.Left);
            });
            AddAssert("written down as the file menu", () => entries().Last(e => e["kind"]!.ToString() == "press")["over"]!.ToString(), () => Does.Contain("\"File\""));
        }

        /// <summary>
        /// Not a check so much as a measure: how much a change adds to the log, for how big an hour of mapping comes to.
        /// </summary>
        [Test]
        public void TestHowMuchAChangeTakes()
        {
            long before = 0;

            AddStep("put a red line", () => EditorBeatmap.ControlPointInfo.Add(0, new TimingControlPoint { BeatLength = 500 }));
            AddStep("note the size", () => before = new FileInfo(fullPath()).Length);
            // sliders of a velocity each, so that each also puts in a green line, far above it in the file: as placing does
            // in a mapped difficulty, where a change came to 10 KB when the two were written down as one (2026-10-28).
            AddRepeatStep("place a slider", () =>
            {
                int i = EditorBeatmap.HitObjects.Count;
                EditorBeatmap.Add(new Slider
                {
                    StartTime = 1000 + i * 250,
                    Position = new Vector2(i % 400, i % 300),
                    SliderVelocityMultiplier = 0.5 + i % 10 * 0.1,
                    Path = new SliderPath(PathType.LINEAR, new[] { Vector2.Zero, new Vector2(50, 0) }),
                });
            }, 300);
            AddAssert("one change each", () => entries().Count(e => e["kind"]!.ToString() == "change"), () => Is.GreaterThanOrEqualTo(300));
            AddAssert("say how much each took", () =>
            {
                long each = (new FileInfo(fullPath()).Length - before) / 300;
                TestContext.Progress.WriteLine($"operation log: {each} bytes a change");
                return each;
            }, () => Is.LessThan(1000));
        }

        [Test]
        public void TestTwoPlacesApartAreTwoHunks()
        {
            string[] before = Enumerable.Range(0, 300).Select(i => $"line {i}").ToArray();
            string[] after = before.ToArray();
            after[10] = "changed 10";
            after = after.Take(290).Append("new").Concat(after.Skip(290)).ToArray();

            var hunks = EditorOperationLog.Diff(before, after);

            Assert.That(hunks.Select(h => (h.Line, string.Join('|', h.Removed), string.Join('|', h.Added))),
                Is.EqualTo(new[] { (10, "line 10", "changed 10"), (290, "", "new") }));
        }

        [Test]
        public void TestNothingChangedIsNoHunk()
            => Assert.That(EditorOperationLog.Diff(new[] { "a", "b" }, new[] { "a", "b" }), Is.Empty);

        [Test]
        public void TestEverythingChanged()
        {
            var hunks = EditorOperationLog.Diff(new[] { "a", "b" }, new[] { "c" });
            Assert.That(hunks.Select(h => (h.Line, string.Join('|', h.Removed), string.Join('|', h.Added))), Is.EqualTo(new[] { (0, "a|b", "c") }));
        }

        [TestCase("20261018.0", true)]
        [TestCase("20261027 e25f59", false)]
        [TestCase("local debug", false)]
        public void TestOnlyADistributedVersionIsTakenForOne(string version, bool distributed)
            => Assert.That(DevelopmentSettings.IsDistributedVersion(version), Is.EqualTo(distributed));

        private string fullPath() => ((Storage)Editor.Dependencies.Get(typeof(Storage))).GetFullPath(Editor.OperationLog!.Path!);

        private static string[] linesIn(JObject entry, string which)
            => entry["hunks"]!.SelectMany(h => h[which]!.Select(l => l.ToString())).ToArray();

        private JObject[] entries()
        {
            using var stream = new FileStream(fullPath(), FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(stream);

            return reader.ReadToEnd().Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(JObject.Parse).ToArray();
        }
    }
}
