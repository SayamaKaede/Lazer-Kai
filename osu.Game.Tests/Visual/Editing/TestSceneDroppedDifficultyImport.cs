// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Extensions;
using osu.Framework.Testing;
using osu.Game.Beatmaps;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Osu;
using osu.Game.Screens.Edit;
using osu.Game.Skinning;
using osu.Game.Storyboards;
using osu.Game.Tests.Beatmaps.IO;
using osuTK.Graphics;
using osuTK.Input;

namespace osu.Game.Tests.Visual.Editing
{
    /// <summary>
    /// A `.osu` dropped on the editor going into the set being edited rather than being imported on its own.
    /// </summary>
    public partial class TestSceneDroppedDifficultyImport : EditorTestScene
    {
        protected override Ruleset CreateEditorRuleset() => new OsuRuleset();

        // what ends up on disk and in realm is the whole point of the feature, so the real ones are needed.
        protected override bool IsolateSavingFromDatabase => false;

        [Resolved]
        private OsuGameBase game { get; set; } = null!;

        [Resolved]
        private BeatmapManager beatmaps { get; set; } = null!;

        private BeatmapSetInfo importedBeatmapSet = null!;
        private string dropDirectory = null!;
        private int written;

        protected override WorkingBeatmap CreateWorkingBeatmap(IBeatmap beatmap, Storyboard storyboard = null!)
            => beatmaps.GetWorkingBeatmap(importedBeatmapSet.Beatmaps.First());

        public override void SetUpSteps()
        {
            AddStep("import test beatmap", () => importedBeatmapSet = BeatmapImportHelper.LoadOszIntoOsu(game, virtualTrack: true).GetResultSafely());
            AddStep("prepare drop directory", () =>
            {
                dropDirectory = Path.Combine(Path.GetTempPath(), $"lazer-kai-drop-{Guid.NewGuid()}");
                Directory.CreateDirectory(dropDirectory);
            });

            base.SetUpSteps();
        }

        [TearDownSteps]
        public void CleanUpDropDirectory()
        {
            AddStep("remove drop directory", () =>
            {
                if (Directory.Exists(dropDirectory))
                    Directory.Delete(dropDirectory, true);
            });
        }

        [Test]
        public void TestNewNameIsAddedToSet()
        {
            int countBefore = 0;
            string path = null!;

            AddStep("count difficulties", () => countBefore = Beatmap.Value.BeatmapSetInfo.Beatmaps.Count);
            AddStep("write file", () => path = write("Dropped Extra", currentRuleset, objectCount: 3));

            AddStep("drop", () => Editor.ImportDroppedDifficulties(new[] { path }));

            AddUntilStep("difficulty added to set", () => Beatmap.Value.BeatmapSetInfo.Beatmaps.Count, () => Is.EqualTo(countBefore + 1));
            AddAssert("added under the dropped name", () => Beatmap.Value.BeatmapSetInfo.Beatmaps.Select(b => b.DifficultyName), () => Does.Contain("Dropped Extra"));
            AddAssert("added difficulty holds the dropped objects",
                () => objectsIn(Beatmap.Value.BeatmapSetInfo.Beatmaps.Single(b => b.DifficultyName == "Dropped Extra").ID), () => Is.EqualTo(3));
        }

        [Test]
        public void TestExistingNameReplacesThatDifficulty()
        {
            int countBefore = 0;
            Guid replacedID = Guid.Empty;
            string path = null!;

            AddStep("write a file over a difficulty other than the open one", () =>
            {
                countBefore = Beatmap.Value.BeatmapSetInfo.Beatmaps.Count;

                var replaced = Beatmap.Value.BeatmapSetInfo.Beatmaps.First(b => b.ID != EditorBeatmap.BeatmapInfo.ID);
                replacedID = replaced.ID;

                path = write(replaced.DifficultyName, replaced.Ruleset.OnlineID, objectCount: 5);
            });

            AddStep("drop", () => Editor.ImportDroppedDifficulties(new[] { path }));

            AddUntilStep("difficulty holds the dropped objects", () => objectsIn(replacedID), () => Is.EqualTo(5));
            AddAssert("nothing was added", () => Beatmap.Value.BeatmapSetInfo.Beatmaps.Count, () => Is.EqualTo(countBefore));
            AddAssert("no dialog was shown", () => DialogOverlay.CurrentDialog, () => Is.Null);
        }

        [Test]
        public void TestOpenDifficultyAsksFirstAndReloads()
        {
            Guid openID = Guid.Empty;
            string path = null!;

            AddStep("write file over the open difficulty", () =>
            {
                openID = EditorBeatmap.BeatmapInfo.ID;
                path = write(EditorBeatmap.BeatmapInfo.DifficultyName, currentRuleset, objectCount: 7);
            });

            AddStep("drop", () => Editor.ImportDroppedDifficulties(new[] { path }));

            AddUntilStep("asked first", () => DialogOverlay.CurrentDialog, () => Is.InstanceOf<OverwriteCurrentDifficultyDialog>());
            AddAssert("nothing written yet", () => objectsIn(openID), () => Is.Not.EqualTo(7));

            AddStep("confirm", () => InputManager.Key(Key.Number1));

            AddUntilStep("editor reopens on the new contents", () => Editor?.ReadyForUse == true && EditorBeatmap.HitObjects.Count == 7);
        }

        [Test]
        public void TestCancellingLeavesTheOpenDifficultyAlone()
        {
            string path = null!;
            int objectsBefore = 0;

            AddStep("write file over the open difficulty", () =>
            {
                objectsBefore = EditorBeatmap.HitObjects.Count;
                path = write(EditorBeatmap.BeatmapInfo.DifficultyName, currentRuleset, objectCount: 7);
            });

            AddStep("drop", () => Editor.ImportDroppedDifficulties(new[] { path }));

            AddUntilStep("asked first", () => DialogOverlay.CurrentDialog, () => Is.InstanceOf<OverwriteCurrentDifficultyDialog>());
            AddStep("cancel", () => InputManager.Key(Key.Number2));

            AddUntilStep("dialog gone", () => DialogOverlay.CurrentDialog == null);
            AddAssert("still on the same objects", () => EditorBeatmap.HitObjects.Count, () => Is.EqualTo(objectsBefore));
        }

        /// <summary>
        /// A difficulty is written back out from its beatmap, its skin and its storyboard, and reading only the
        /// first of those from the dropped file left the combo colours behind.
        /// </summary>
        [Test]
        public void TestComboColoursComeAcross()
        {
            Guid replacedID = Guid.Empty;
            string path = null!;

            AddStep("write a file carrying combo colours", () =>
            {
                var replaced = Beatmap.Value.BeatmapSetInfo.Beatmaps.First(b => b.ID != EditorBeatmap.BeatmapInfo.ID);
                replacedID = replaced.ID;

                path = write(replaced.DifficultyName, replaced.Ruleset.OnlineID, objectCount: 5, comboColours: distinctive);
            });

            AddStep("drop", () => Editor.ImportDroppedDifficulties(new[] { path }));

            AddUntilStep("difficulty replaced", () => objectsIn(replacedID), () => Is.EqualTo(5));
            AddAssert("colours came across", () => coloursIn(replacedID), () => Is.EquivalentTo(distinctive));
        }

        /// <summary>
        /// The other half of it: a file with no colours of its own must not pick up the default set on the way
        /// in, which would write colours into a difficulty that never had any.
        /// </summary>
        [Test]
        public void TestNoColoursStaysNoColours()
        {
            Guid replacedID = Guid.Empty;
            string path = null!;

            AddStep("write a file with no colours", () =>
            {
                var replaced = Beatmap.Value.BeatmapSetInfo.Beatmaps.First(b => b.ID != EditorBeatmap.BeatmapInfo.ID);
                replacedID = replaced.ID;

                path = write(replaced.DifficultyName, replaced.Ruleset.OnlineID, objectCount: 5);
            });

            AddStep("drop", () => Editor.ImportDroppedDifficulties(new[] { path }));

            AddUntilStep("difficulty replaced", () => objectsIn(replacedID), () => Is.EqualTo(5));
            AddAssert("no colours were invented", () => coloursIn(replacedID), () => Is.Null);
        }

        /// <summary>
        /// Anything that is not a `.osu` is left for the game to import in the ordinary way.
        /// </summary>
        [Test]
        public void TestOtherExtensionsAreNotTaken()
        {
            Assert.That(Screens.Edit.Editor.IsDroppedDifficulty(@"C:\somewhere\set.osz"), Is.False);
            Assert.That(Screens.Edit.Editor.IsDroppedDifficulty(@"C:\somewhere\skin.osk"), Is.False);
            Assert.That(Screens.Edit.Editor.IsDroppedDifficulty(@"C:\somewhere\Artist - Title (Creator) [Extra].osu"), Is.True);
            Assert.That(Screens.Edit.Editor.IsDroppedDifficulty(@"C:\somewhere\SHOUTING.OSU"), Is.True);
        }

        private int currentRuleset => EditorBeatmap.BeatmapInfo.Ruleset.OnlineID;

        /// <summary>
        /// Colours no beatmap would pick, so that finding them proves they came from the dropped file rather
        /// than having been in the set all along.
        /// </summary>
        private static readonly Color4[] distinctive =
        {
            new Color4(254, 1, 2, 255),
            new Color4(1, 254, 3, 255),
            new Color4(2, 3, 254, 255),
        };

        /// <remarks>
        /// Written again in lazer's format, an osu!mania note's position was taken for its column (2026-11-02): one at
        /// x 14 of 18 columns came out in the fifteenth, and every one further right in the last.
        /// </remarks>
        [Test]
        public void TestManiaNotesStayInTheirColumns()
        {
            string path = null!;
            int[] positions = { 14, 42, 71, 99, 497 };

            AddStep("write an 18 key file in lazer's format", () =>
            {
                path = Path.Combine(dropDirectory, $"drop-{written++}.osu");

                var contents = new StringBuilder();

                contents.AppendLine(@"osu file format v128");
                contents.AppendLine();
                contents.AppendLine(@"[General]");
                contents.AppendLine(@"Mode: 3");
                contents.AppendLine();
                contents.AppendLine(@"[Metadata]");
                contents.AppendLine(@"Version:Hitsounds (mania)");
                contents.AppendLine();
                contents.AppendLine(@"[Difficulty]");
                contents.AppendLine(@"CircleSize:18");
                contents.AppendLine();
                contents.AppendLine(@"[TimingPoints]");
                contents.AppendLine(@"0,500,4,2,0,100,1,0");
                contents.AppendLine();
                contents.AppendLine(@"[HitObjects]");

                for (int i = 0; i < positions.Length; i++)
                    contents.AppendLine($@"{positions[i]},192,{1000 + i * 500},1,0,0:0:0:0:");

                File.WriteAllText(path, contents.ToString());
            });

            AddStep("drop", () => Editor.ImportDroppedDifficulties(new[] { path }));

            AddUntilStep("difficulty added to set", () => Beatmap.Value.BeatmapSetInfo.Beatmaps.Any(b => b.DifficultyName == "Hitsounds (mania)"));
            AddAssert("the notes are where they were", () =>
            {
                var info = Beatmap.Value.BeatmapSetInfo.Beatmaps.Single(b => b.DifficultyName == "Hitsounds (mania)");
                return beatmaps.GetWorkingBeatmap(info, true).Beatmap.HitObjects.Select(h => (int)((IHasXPosition)h).X);
            }, () => Is.EqualTo(positions));
        }

        private IReadOnlyList<Color4>? coloursIn(Guid difficultyID)
        {
            var info = Beatmap.Value.BeatmapSetInfo.Beatmaps.Single(b => b.ID == difficultyID);

            // read back through the skin, which is where the game looks for them and where the encoder wrote
            // them from. `LegacyBeatmapSkin` does not stand in the default set, so nothing here is invented.
            return beatmaps.GetWorkingBeatmap(info, true).Skin
                           .GetConfig<GlobalSkinColours, IReadOnlyList<Color4>>(GlobalSkinColours.ComboColours)?.Value;
        }

        /// <summary>
        /// Read back through the beatmap manager rather than off the editor, so that what is asserted is what
        /// ended up on disk.
        /// </summary>
        private int objectsIn(Guid difficultyID)
        {
            var info = Beatmap.Value.BeatmapSetInfo.Beatmaps.Single(b => b.ID == difficultyID);
            return beatmaps.GetWorkingBeatmap(info, true).Beatmap.HitObjects.Count;
        }

        /// <summary>
        /// Writes out a difficulty to be dropped, with a countable number of objects in it so that the one that
        /// ends up in the set can be told apart from the one that was there before.
        /// </summary>
        private string write(string difficultyName, int rulesetId, int objectCount, Color4[]? comboColours = null)
        {
            // the name and mode inside the file decide where it lands, so the filename only has to be unique.
            string path = Path.Combine(dropDirectory, $"drop-{written++}.osu");

            var contents = new StringBuilder();

            contents.AppendLine(@"osu file format v14");
            contents.AppendLine();
            contents.AppendLine(@"[General]");
            contents.AppendLine($@"Mode: {rulesetId}");
            contents.AppendLine();
            contents.AppendLine(@"[Metadata]");
            contents.AppendLine(@"Title:Dropped");
            contents.AppendLine(@"Artist:Dropped");
            contents.AppendLine(@"Creator:Dropped");
            contents.AppendLine($@"Version:{difficultyName}");
            contents.AppendLine();
            contents.AppendLine(@"[Difficulty]");
            contents.AppendLine(@"HPDrainRate:5");
            contents.AppendLine(@"CircleSize:4");
            contents.AppendLine(@"OverallDifficulty:6");
            contents.AppendLine(@"ApproachRate:7");
            contents.AppendLine(@"SliderMultiplier:1.4");
            contents.AppendLine(@"SliderTickRate:1");
            contents.AppendLine();
            contents.AppendLine(@"[TimingPoints]");
            contents.AppendLine(@"0,500,4,2,0,100,1,0");
            contents.AppendLine();

            if (comboColours != null)
            {
                contents.AppendLine(@"[Colours]");

                for (int i = 0; i < comboColours.Length; i++)
                {
                    var colour = comboColours[i];
                    contents.AppendLine(FormattableString.Invariant($"Combo{1 + i} : {(byte)(colour.R * 255)},{(byte)(colour.G * 255)},{(byte)(colour.B * 255)}"));
                }

                contents.AppendLine();
            }

            contents.AppendLine(@"[HitObjects]");

            for (int i = 0; i < objectCount; i++)
                contents.AppendLine($@"100,100,{1000 + i * 500},1,0,0:0:0:0:");

            File.WriteAllText(path, contents.ToString());

            return path;
        }
    }
}
