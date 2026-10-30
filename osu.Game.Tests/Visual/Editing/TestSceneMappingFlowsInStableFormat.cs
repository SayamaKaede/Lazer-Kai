// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Extensions;
using osu.Framework.Screens;
using osu.Framework.Testing;
using osu.Game.Audio;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Database;
using osu.Game.Graphics.UserInterface;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Localisation;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Screens.Edit;
using osu.Game.Screens.Edit.BeatmapFiles;
using osu.Game.Screens.Edit.Hitsounds;
using osu.Game.Screens.Edit.Timing;
using osu.Game.Tests.Resources;
using osuTK;

namespace osu.Game.Tests.Visual.Editing
{
    /// <summary>
    /// Goes through a mapper's work in a set kept in stable's format, in the order it is done.
    /// </summary>
    public partial class TestSceneMappingFlowsInStableFormat : StableFormatConversionTestScene
    {
        private const string difficulty_file = @"Soleily - Renatus (Gamu) [Hard].osu";

        private BeatmapInfo imported = null!;

        public override void SetUpSteps()
        {
            CreateInitialBeatmap = () =>
            {
                Game.LocalConfig.SetValue(OsuSetting.StableCompatibility, true);

                var importedSet = Game.BeatmapManager.Import(new ImportTask(TestResources.GetTestBeatmapForImport())).GetResultSafely();
                imported = importedSet!.Value.Beatmaps.Single(b => b.Path == difficulty_file);

                return Game.BeatmapManager.GetWorkingBeatmap(imported);
            };

            base.SetUpSteps();
        }

        [Test]
        public void TestADifficultyMadeFromNothingIsKeptInStableFormat()
        {
            Editor? before = null;

            AddAssert("this one is kept in stable's format", () => Editor.KeptStableVersion.Value, () => Is.Not.Null);

            AddStep("make a difficulty from nothing", () => (before = Editor).CreateNewDifficulty(new OsuRuleset().RulesetInfo));
            AddUntilStep("wait for dialog", () => Game.ChildrenOfType<CreateNewDifficultyDialog>().SingleOrDefault()?.IsLoaded == true);
            AddStep("with no objects", () => Game.ChildrenOfType<CreateNewDifficultyDialog>().Single().PerformOkAction());
            AddUntilStep("the editor opens it", () => Editor != before && Editor?.ReadyForUse == true);
            AddAssert("it is a new one", () => EditorBeatmap.BeatmapInfo.ID, () => Is.Not.EqualTo(imported.ID));

            AddAssert("its file is lazer's", () => FirstLine(SavedFile()), () => Does.Contain("v128"));
            AddAssert("it is not kept in stable's format", () => Editor.KeptStableVersion.Value, () => Is.Null);
            AddAssert("no hitsound difficulty can be made from it", () => Editor.CanMakeHitsoundDifficulty.Value, () => Is.False);

            Convert();

            AddAssert("its file is stable's", () => FirstLine(SavedFile()), () => Does.Not.Contain("v128"));
            AddAssert("it is kept in stable's format", () => Editor.KeptStableVersion.Value, () => Is.Not.Null);
            AddAssert("a hitsound difficulty can be made from it", () => Editor.CanMakeHitsoundDifficulty.Value);
        }

        /// <summary>
        /// Copies the hitsound difficulty with "always apply" on, then keeps mapping: a slider's velocity is changed, a
        /// circle is replaced with a slider of its own velocity, and the file saved says each slider's velocity and the
        /// hitsound difficulty's volume at every sound.
        /// </summary>
        [TestCase(true)]
        [TestCase(false)]
        public void TestMappingOnAfterCopyingTheHitsoundDifficulty(bool alwaysApplyOn)
        {
            double changed = 0;
            double replaced = 0;
            byte[] hitsoundFile = null!;
            Dictionary<double, double> velocities = null!;

            AddStep("note a slider and a circle", () =>
            {
                changed = EditorBeatmap.HitObjects.OfType<Slider>().ToArray()[3].StartTime;
                replaced = EditorBeatmap.HitObjects.OfType<HitCircle>().ToArray()[12].StartTime;
            });

            AddStep("switch to the hitsound screen", () => Editor.Mode.Value = EditorScreenMode.Hitsounds);
            makeAHitsoundDifficulty();
            AddStep("give its first circle a clap from drum", () =>
            {
                var circle = EditorBeatmap.HitObjects.OfType<HitCircle>().First();
                HitsoundEditing.SetHitsound(new[] { new HitPoint(circle, null, circle.StartTime) },
                    new PointHitsound(HitSampleInfo.BANK_SOFT, HitSampleInfo.BANK_DRUM, new[] { HitSampleInfo.HIT_CLAP }));
                EditorBeatmap.Update(circle);
            });
            AddStep("save", () => Editor.Save());
            AddStep("read its file", () => hitsoundFile = SavedFile());

            switchBack();
            AddStep("switch to the hitsound screen", () => Editor.Mode.Value = EditorScreenMode.Hitsounds);
            AddUntilStep("the hitsound screen is loaded", () => Editor.ChildrenOfType<HitsoundPanel>().SingleOrDefault()?.IsLoaded == true);
            AddStep("turn always apply on or leave it off", () => alwaysApply.Current.Value = alwaysApplyOn);
            AddStep("switch to compose", () => Editor.Mode.Value = EditorScreenMode.Compose);

            AddStep("change the slider's velocity to 0.6x", () =>
            {
                var slider = objectAt(changed);
                ((Slider)slider).SliderVelocityMultiplier = 0.6;
                EditorBeatmap.Update(slider);
            });
            AddStep("replace the circle with a 0.7x slider", () =>
            {
                var circle = objectAt(replaced);
                EditorBeatmap.Remove(circle);
                EditorBeatmap.Add(new Slider
                {
                    StartTime = replaced,
                    Position = ((HitCircle)circle).Position,
                    SliderVelocityMultiplier = 0.7,
                    Path = new SliderPath(new[] { new PathControlPoint(Vector2.Zero, PathType.LINEAR), new PathControlPoint(new Vector2(60, 0)) }),
                    // the sounds of the circle it replaces, as placing takes the sounds of the object before.
                    Samples = circle.Samples.ToList(),
                });
            });
            AddWaitStep("let always apply go through", 2);
            AddStep("note the velocities", () => velocities = EditorBeatmap.HitObjects.OfType<Slider>().ToDictionary(s => s.StartTime, s => s.SliderVelocityMultiplier));
            AddAssert("saved in stable's format", () => Editor.Save());

            AddAssert("the file says each slider's velocity", () =>
            {
                var lines = SavedTimingLines.Read(SavedFile());
                return velocities.Where(v => Math.Abs(lines.Last(l => l.Time <= v.Key).SliderVelocity - v.Value) > 0.001).Select(v => $"{v.Key}: {v.Value}").ToArray();
            }, () => Is.Empty);

            AddAssert("the file has the hitsound difficulty's volume at every sound", () =>
            {
                var lines = SavedTimingLines.Read(SavedFile());
                var hitsoundLines = SavedTimingLines.Read(hitsoundFile);
                return EditorBeatmap.HitObjects.SelectMany(h => HitPoints.Of(h, true)).Select(p => p.Time).Distinct()
                                    .Where(t => volumeAt(lines, t) != volumeAt(hitsoundLines, t))
                                    .Select(t => $"{t}: {volumeAt(lines, t)} / {volumeAt(hitsoundLines, t)}").ToArray();
            }, () => Is.Empty);
        }

        /// <summary>
        /// A copy of a difficulty starts with its green lines and objects, and from there each is saved on its own.
        /// </summary>
        [Test]
        public void TestACopiedDifficultyIsSavedOnItsOwn()
        {
            byte[] original = null!;
            byte[] copied = null!;
            double changed = 0;
            Guid copyId = Guid.Empty;
            Editor? before = null;

            AddStep("read the file", () => original = SavedFile());
            AddStep("note a slider", () => changed = EditorBeatmap.HitObjects.OfType<Slider>().ToArray()[3].StartTime);

            AddStep("make a copy", () => (before = Editor).CreateNewDifficulty(new OsuRuleset().RulesetInfo));
            AddUntilStep("wait for dialog", () => Game.ChildrenOfType<CreateNewDifficultyDialog>().SingleOrDefault()?.IsLoaded == true);
            AddStep("as a copy", () => Game.ChildrenOfType<CreateNewDifficultyDialog>().Single().Buttons.ElementAt(1).TriggerClick());
            AddUntilStep("the editor opens it", () => Editor != before && Editor?.ReadyForUse == true);
            AddStep("note it", () => copyId = EditorBeatmap.BeatmapInfo.ID);
            AddAssert("it is a new one", () => copyId, () => Is.Not.EqualTo(imported.ID));
            AddAssert("saved in stable's format", () => Editor.Save());
            AddStep("read its file", () => copied = SavedFile());

            AddAssert("it is kept in stable's format", () => Editor.KeptStableVersion.Value, () => Is.Not.Null);
            AddAssert("its green lines are the original's", () => SavedTimingLines.Read(copied).Select(l => l.Raw), () => Is.EqualTo(SavedTimingLines.Read(original).Select(l => l.Raw)));
            AddAssert("its objects are the original's", () => sectionOf(copied, "[HitObjects]"), () => Is.EqualTo(sectionOf(original, "[HitObjects]")));

            AddStep("change the slider's velocity to 0.6x", () =>
            {
                var slider = objectAt(changed);
                ((Slider)slider).SliderVelocityMultiplier = 0.6;
                EditorBeatmap.Update(slider);
            });
            AddAssert("saved", () => Editor.Save());
            AddStep("read its file", () => copied = SavedFile());
            AddAssert("its file says 0.6x", () => SavedTimingLines.Read(copied).Last(l => l.Time <= changed).SliderVelocity, () => Is.EqualTo(0.6).Within(0.001));

            switchBack();
            AddAssert("the original's file is as it was", () => SavedFile(), () => Is.EqualTo(original));
            AddAssert("its slider is as it was", () => ((Slider)objectAt(changed)).SliderVelocityMultiplier, () => Is.Not.EqualTo(0.6).Within(0.001));

            AddStep("change the slider's velocity to 0.8x here", () =>
            {
                var slider = objectAt(changed);
                ((Slider)slider).SliderVelocityMultiplier = 0.8;
                EditorBeatmap.Update(slider);
            });
            AddAssert("saved", () => Editor.Save());
            AddAssert("the file says 0.8x", () => SavedTimingLines.Read(SavedFile()).Last(l => l.Time <= changed).SliderVelocity, () => Is.EqualTo(0.8).Within(0.001));

            AddStep("switch to the copy", () => Editor.SwitchToDifficulty(Game.BeatmapManager.QueryBeatmap(b => b.ID == copyId)!));
            AddUntilStep("the editor opens it", () => Editor?.ReadyForUse == true && EditorBeatmap.BeatmapInfo.ID == copyId);
            AddAssert("the copy's file is as it was", () => SavedFile(), () => Is.EqualTo(copied));
            AddAssert("its slider is 0.6x", () => ((Slider)objectAt(changed)).SliderVelocityMultiplier, () => Is.EqualTo(0.6).Within(0.001));
        }

        // moving the whole of this difficulty after copying leaves the hitsound difficulty where it was, and its sounds are
        // lost here (with "always apply" on at once, otherwise on copying again). Left as it is (2026-10-29): the
        // hitsound difficulty is moved by hand as well.

        /// <summary>
        /// Brings in a hitsound difficulty from another set of the same song, as one made earlier, and copies it here.
        /// </summary>
        [Test]
        public void TestCopyingAHitsoundDifficultyBroughtInFromAnotherSet()
        {
            double clapped = 0;
            byte[] hitsoundFile = null!;
            Guid otherSet = Guid.Empty;
            Editor? before = null;

            AddStep("note a circle with no additions", () => clapped = EditorBeatmap.HitObjects.OfType<HitCircle>().First(c => c.Samples.All(s => s.Name == HitSampleInfo.HIT_NORMAL)).StartTime);
            AddStep("make another set with a hitsound difficulty of this one", () =>
            {
                string[] lines = System.Text.Encoding.UTF8.GetString(SavedFile()).Replace("\r\n", "\n").Split('\n');
                bool inObjects = false;
                bool clapGiven = false;

                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i];

                    if (line.StartsWith("Version:", StringComparison.Ordinal))
                        lines[i] = "Version:Hitsounds";
                    else if (line.StartsWith("BeatmapID:", StringComparison.Ordinal))
                        lines[i] = "BeatmapID:0";
                    else if (line.StartsWith("BeatmapSetID:", StringComparison.Ordinal))
                        lines[i] = "BeatmapSetID:-1";
                    else if (line.Trim() == "[HitObjects]")
                        inObjects = true;
                    else if (inObjects && !clapGiven && line.Split(',') is { Length: > 5 } fields && fields[2] == ((int)clapped).ToString())
                    {
                        fields[4] = "8";
                        lines[i] = string.Join(',', fields);
                        clapGiven = true;
                    }
                }

                hitsoundFile = System.Text.Encoding.UTF8.GetBytes(string.Join("\r\n", lines));

                var stream = new System.IO.MemoryStream();

                using (var zip = new System.IO.Compression.ZipArchive(stream, System.IO.Compression.ZipArchiveMode.Create, true))
                using (var entry = zip.CreateEntry("Soleily - Renatus (Gamu) [Hitsounds].osu").Open())
                    entry.Write(hitsoundFile);

                stream.Seek(0, System.IO.SeekOrigin.Begin);
                otherSet = Game.BeatmapManager.Import(new ImportTask(stream, "earlier.osz")).GetResultSafely()!.ID;
            });

            AddStep("open the screen", () => Editor.OpenImportFromAnotherSet());
            AddUntilStep("the sets of the game are listed", () => importScreen?.IsLoaded == true && importScreen.Import.SetsListed);
            AddStep("choose the other set", () => importScreen!.Import.SetRows.Single(r => r.Set.ID == otherSet).TriggerClick());
            AddUntilStep("what it has is shown", () => importScreen!.Import.DifficultyRows.Any());
            AddStep("tick the hitsound difficulty", () => importScreen!.Import.DifficultyRows.Single(r => r.Difficulty.DifficultyName == "Hitsounds").Box.Current.Value = true);
            AddStep("bring in", () => importScreen!.Import.ImportButton.TriggerClick());
            AddUntilStep("brought in", () => importScreen!.Import.Changed);
            AddStep("leave", () =>
            {
                before = Editor;
                importScreen!.Exit();
            });
            AddUntilStep("the editor opens the set again", () => Editor != before && Editor?.ReadyForUse == true);
            AddAssert("on this difficulty", () => EditorBeatmap.BeatmapInfo.ID, () => Is.EqualTo(imported.ID));

            AddStep("switch to the hitsound screen", () => Editor.Mode.Value = EditorScreenMode.Hitsounds);
            AddUntilStep("the hitsound screen is loaded", () => Editor.ChildrenOfType<HitsoundPanel>().SingleOrDefault()?.IsLoaded == true);
            AddAssert("the one brought in is offered to copy from", () => Editor.ChildrenOfType<HitsoundPanel>().Single().ChildrenOfType<FormDropdown<string>>().Single().Current.Value, () => Is.EqualTo("Hitsounds"));
            AddAssert("the circle has no clap yet", () => claps(objectAt(clapped)), () => Is.False);
            AddStep("copy", () => Editor.ChildrenOfType<HitsoundPanel>().Single().ChildrenOfType<RoundedButton>().Single(b => b.Text == LazerKaiHitsoundStrings.CopyFromButton).TriggerClick());
            AddAssert("the circle claps", () => claps(objectAt(clapped)));
            AddAssert("saved in stable's format", () => Editor.Save());
            AddAssert("the file has the clap", () => sectionOf(SavedFile(), "[HitObjects]").First(l => l.Split(',')[2] == ((int)clapped).ToString()).Split(',')[4], () => Is.EqualTo("8"));
            AddAssert("the file has the hitsound difficulty's volume at every sound", () =>
            {
                var lines = SavedTimingLines.Read(SavedFile());
                var hitsoundLines = SavedTimingLines.Read(hitsoundFile);
                return EditorBeatmap.HitObjects.SelectMany(h => HitPoints.Of(h, true)).Select(p => p.Time).Distinct()
                                    .Where(t => volumeAt(lines, t) != volumeAt(hitsoundLines, t))
                                    .Select(t => $"{t}: {volumeAt(lines, t)} / {volumeAt(hitsoundLines, t)}").ToArray();
            }, () => Is.Empty);
        }

        /// <summary>
        /// Turns a slider into a stream after the hitsound difficulty is copied.
        /// </summary>
        [TestCase(true)]
        [TestCase(false)]
        public void TestTurningASliderIntoAStreamAfterCopyingTheHitsoundDifficulty(bool alwaysApplyOn)
        {
            double start = 0;
            string[] headSounds = null!;
            byte[] hitsoundFile = null!;

            AddStep("switch to the hitsound screen", () => Editor.Mode.Value = EditorScreenMode.Hitsounds);
            makeAHitsoundDifficulty();
            AddStep("save", () => Editor.Save());
            AddStep("read its file", () => hitsoundFile = SavedFile());

            switchBack();
            AddStep("switch to the hitsound screen", () => Editor.Mode.Value = EditorScreenMode.Hitsounds);
            AddUntilStep("the hitsound screen is loaded", () => Editor.ChildrenOfType<HitsoundPanel>().SingleOrDefault()?.IsLoaded == true);
            AddStep("copy", () => Editor.ChildrenOfType<HitsoundPanel>().Single().ChildrenOfType<RoundedButton>().Single(b => b.Text == LazerKaiHitsoundStrings.CopyFromButton).TriggerClick());
            AddStep("turn always apply on or leave it off", () => alwaysApply.Current.Value = alwaysApplyOn);
            AddStep("switch to compose", () => Editor.Mode.Value = EditorScreenMode.Compose);
            AddUntilStep("compose is loaded", () => Editor.ChildrenOfType<osu.Game.Rulesets.Edit.HitObjectComposer>().SingleOrDefault()?.IsLoaded == true);

            AddStep("select a slider", () =>
            {
                var slider = EditorBeatmap.HitObjects.OfType<Slider>().ToArray()[3];
                start = slider.StartTime;
                headSounds = slider.Samples.Select(s => s.Name).OrderBy(n => n).ToArray();
                ((EditorClock)Editor.Dependencies.Get(typeof(EditorClock))).Seek(start);
                EditorBeatmap.SelectedHitObjects.Add(slider);
            });
            AddStep("turn it into a stream", () =>
            {
                InputManager.PressKey(osuTK.Input.Key.LControl);
                InputManager.PressKey(osuTK.Input.Key.LShift);
                InputManager.Key(osuTK.Input.Key.F);
                InputManager.ReleaseKey(osuTK.Input.Key.LShift);
                InputManager.ReleaseKey(osuTK.Input.Key.LControl);
            });
            AddUntilStep("the panel is up", () => Editor.ChildrenOfType<osu.Game.Rulesets.Osu.Edit.StreamConversionPanel>().SingleOrDefault()?.IsLoaded == true);
            AddStep("take the conversion", () => Editor.ChildrenOfType<osu.Game.Rulesets.Osu.Edit.StreamConversionPanel>().Single().ChildrenOfType<RoundedButton>()
                                                        .Single(b => b.Text.Equals(EditorStrings.ConvertToStream)).TriggerClick());
            AddUntilStep("it is a stream", () => objectAt(start) is HitCircle);
            AddWaitStep("let always apply go through", 2);

            AddAssert("the first circle sounds as the head did", () => objectAt(start).Samples.Select(s => s.Name).OrderBy(n => n), () => Is.EqualTo(headSounds));
            AddAssert("saved in stable's format", () => Editor.Save());
            AddAssert("the file has the hitsound difficulty's volume at every sound", () =>
            {
                var lines = SavedTimingLines.Read(SavedFile());
                var hitsoundLines = SavedTimingLines.Read(hitsoundFile);
                return EditorBeatmap.HitObjects.SelectMany(h => HitPoints.Of(h, true)).Select(p => p.Time).Distinct()
                                    .Where(t => volumeAt(lines, t) != volumeAt(hitsoundLines, t))
                                    .Select(t => $"{t}: {volumeAt(lines, t)} / {volumeAt(hitsoundLines, t)}").ToArray();
            }, () => Is.Empty);
        }

        /// <summary>
        /// Changes the BPM of the first red line after mapping, moving the objects with it as the timing screen does with
        /// "adjust existing objects" on. Each object keeps the volume and each slider the velocity it had.
        /// </summary>
        [Test]
        public void TestChangingTheBPMAfterMapping()
        {
            (int volume, double velocity)[] before = null!;
            double newBeatLength = 0;

            AddStep("note each object's volume and velocity", () =>
            {
                var lines = SavedTimingLines.Read(SavedFile());
                before = EditorBeatmap.HitObjects.Select(h => (volumeAt(lines, h.StartTime), lines.Last(l => l.Time <= h.StartTime).SliderVelocity)).ToArray();
            });

            AddStep("make the first red line 1% faster", () =>
            {
                var timing = EditorBeatmap.ControlPointInfo.TimingPoints.First();
                double oldBeatLength = timing.BeatLength;
                timing.BeatLength = newBeatLength = oldBeatLength / 1.01;

                EditorBeatmap.BeginChange();
                TimingSectionAdjustments.SetHitObjectBPM(EditorBeatmap, timing, oldBeatLength);
                EditorBeatmap.UpdateAllHitObjects();
                EditorBeatmap.EndChange();
            });
            AddAssert("saved in stable's format", () => Editor.Save());

            AddAssert("the file's first red line is faster", () => SavedTimingLines.Read(SavedFile()).First(l => l.Uninherited).BeatLength, () => Is.EqualTo(newBeatLength).Within(0.001));
            AddAssert("the file's objects are where the editor's are", () => sectionOf(SavedFile(), "[HitObjects]").Select(l => int.Parse(l.Split(',')[2])),
                () => Is.EqualTo(EditorBeatmap.HitObjects.Select(h => (int)h.StartTime)));
            AddAssert("each object keeps its volume", () =>
            {
                var lines = SavedTimingLines.Read(SavedFile());
                var objects = EditorBeatmap.HitObjects.ToArray();
                return Enumerable.Range(0, objects.Length).Where(i => volumeAt(lines, objects[i].StartTime) != before[i].volume)
                                 .Select(i => $"{objects[i].StartTime}: {volumeAt(lines, objects[i].StartTime)} / {before[i].volume}").ToArray();
            }, () => Is.Empty);
            AddAssert("each slider keeps its velocity", () =>
            {
                var lines = SavedTimingLines.Read(SavedFile());
                var objects = EditorBeatmap.HitObjects.ToArray();
                return Enumerable.Range(0, objects.Length).Where(i => objects[i] is Slider && Math.Abs(lines.Last(l => l.Time <= objects[i].StartTime).SliderVelocity - before[i].velocity) > 0.001)
                                 .Select(i => $"{objects[i].StartTime}: {lines.Last(l => l.Time <= objects[i].StartTime).SliderVelocity} / {before[i].velocity}").ToArray();
            }, () => Is.Empty);
        }

        /// <summary>
        /// Hitsounds the beatmap in an osu!mania difficulty, as the user does, turns that into the hitsound difficulty
        /// and copies it here.
        /// </summary>
        [Test]
        public void TestCopyingAHitsoundDifficultyMadeInOsuMania()
        {
            double clapped = 0;
            double whistled = 0;
            BeatmapInfo mania = null!;
            Editor? before = null;

            AddStep("note two circles", () =>
            {
                // ones with no additions, so that what the copy gives them is seen.
                var circles = EditorBeatmap.HitObjects.OfType<HitCircle>().Where(c => c.Samples.All(s => s.Name == HitSampleInfo.HIT_NORMAL)).ToArray();
                clapped = circles[0].StartTime;
                whistled = circles[1].StartTime;
            });
            AddStep("give the beatmap an osu!mania difficulty with a clap and a whistle there", () =>
            {
                var written = new List<string>();
                bool inObjects = false;

                foreach (string line in System.Text.Encoding.UTF8.GetString(SavedFile()).Replace("\r\n", "\n").Split('\n'))
                {
                    string trimmed = line.Trim();

                    if (trimmed.StartsWith('['))
                        inObjects = trimmed == "[HitObjects]";

                    if (inObjects && trimmed.Length > 0 && !trimmed.StartsWith('['))
                        continue;

                    if (trimmed.StartsWith("Mode:", StringComparison.Ordinal))
                        written.Add("Mode: 3");
                    else if (trimmed.StartsWith("Version:", StringComparison.Ordinal))
                        written.Add("Version:Hitsounds-mania");
                    else
                        written.Add(line);

                    if (trimmed == "[HitObjects]")
                    {
                        written.Add($"64,192,{(int)clapped},1,8,0:0:0:0:");
                        written.Add($"64,192,{(int)whistled},1,2,0:0:0:0:");
                    }
                }

                mania = Game.BeatmapManager.AddDifficultyFromFile(EditorBeatmap.BeatmapInfo.BeatmapSet!, Game.Beatmap.Value,
                    System.Text.Encoding.UTF8.GetBytes(string.Join("\r\n", written))).BeatmapInfo;
            });

            AddStep("open it", () => (before = Editor).SwitchToDifficulty(mania));
            AddUntilStep("opened", () => Editor != before && Editor?.ReadyForUse == true);
            AddStep("switch to the hitsound screen", () => Editor.Mode.Value = EditorScreenMode.Hitsounds);
            AddUntilStep("the hitsound screen is loaded", () => Editor.ChildrenOfType<HitsoundPanel>().SingleOrDefault()?.IsLoaded == true);
            AddStep("turn it into a hitsound difficulty", () => Editor.ChildrenOfType<HitsoundPanel>().Single().ConvertManiaButton.TriggerClick());
            AddUntilStep("done", () => EditorBeatmap.BeatmapInfo.BeatmapSet!.Beatmaps.Any(b => b.Ruleset.ShortName == "osu" && b.DifficultyName == HitsoundDifficultyConverter.CONVERTED_NAME));

            switchBack();
            AddStep("switch to the hitsound screen", () => Editor.Mode.Value = EditorScreenMode.Hitsounds);
            AddUntilStep("the hitsound screen is loaded", () => Editor.ChildrenOfType<HitsoundPanel>().SingleOrDefault()?.IsLoaded == true);
            AddAssert("the hitsound difficulty is offered to copy from", () => Editor.ChildrenOfType<HitsoundPanel>().Single().ChildrenOfType<FormDropdown<string>>().Single().Current.Value,
                () => Is.EqualTo(HitsoundDifficultyConverter.CONVERTED_NAME));
            AddAssert("neither has its addition yet", () => !claps(objectAt(clapped)) && objectAt(whistled).Samples.All(s => s.Name != HitSampleInfo.HIT_WHISTLE));
            AddStep("copy", () => Editor.ChildrenOfType<HitsoundPanel>().Single().ChildrenOfType<RoundedButton>().Single(b => b.Text == LazerKaiHitsoundStrings.CopyFromButton).TriggerClick());
            AddAssert("the first circle claps", () => claps(objectAt(clapped)));
            AddAssert("the second whistles", () => objectAt(whistled).Samples.Any(s => s.Name == HitSampleInfo.HIT_WHISTLE));
            AddAssert("saved in stable's format", () => Editor.Save());
            AddAssert("the file has the clap and the whistle", () => new[] { clapped, whistled }.Select(t => sectionOf(SavedFile(), "[HitObjects]").First(l => l.Split(',')[2] == ((int)t).ToString()).Split(',')[4]),
                () => Is.EqualTo(new[] { "8", "2" }));
        }

        private ImportFromAnotherSetScreen? importScreen => Game.ScreenStack.CurrentScreen as ImportFromAnotherSetScreen;

        private static bool claps(HitObject hitObject) => hitObject.Samples.Any(s => s.Name == HitSampleInfo.HIT_CLAP);

        private static string[] sectionOf(byte[] file, string name) =>
            System.Text.Encoding.UTF8.GetString(file).Replace("\r\n", "\n").Split('\n')
                  .SkipWhile(l => l.Trim() != name).Skip(1)
                  .TakeWhile(l => !l.Trim().StartsWith('['))
                  .Where(l => l.Trim().Length > 0)
                  .ToArray();

        [Test]
        public void TestAVelocityChangedWithoutAHitsoundDifficultyIsSaved()
        {
            double changed = 0;

            AddStep("note a slider", () => changed = EditorBeatmap.HitObjects.OfType<Slider>().ToArray()[3].StartTime);
            AddStep("change its velocity to 0.6x", () =>
            {
                var slider = objectAt(changed);
                ((Slider)slider).SliderVelocityMultiplier = 0.6;
                EditorBeatmap.Update(slider);
            });
            AddStep("save", () => Editor.Save());
            AddAssert("the file says 0.6x", () => SavedTimingLines.Read(SavedFile()).Last(l => l.Time <= changed).SliderVelocity, () => Is.EqualTo(0.6).Within(0.001));
        }

        private static int volumeAt(IReadOnlyList<SavedTimingLine> lines, double time) => lines.Last(l => l.Time <= time + 5).Volume;

        private HitObject objectAt(double time) => EditorBeatmap.HitObjects.First(h => Math.Abs(h.StartTime - time) < 1);

        private FormCheckBox alwaysApply => Editor.ChildrenOfType<HitsoundPanel>().Single().ChildrenOfType<FormCheckBox>().Single(c => c.Caption == LazerKaiHitsoundStrings.AlwaysCopyFrom);

        private void makeAHitsoundDifficulty()
        {
            Editor? before = null;

            AddStep("make a hitsound difficulty", () => (before = Editor).MakeHitsoundDifficulty(true));
            AddUntilStep("the editor opens it", () => Editor != before && Editor?.ReadyForUse == true);
        }

        private void switchBack()
        {
            AddStep("switch back", () => Editor.SwitchToDifficulty(Game.BeatmapManager.QueryBeatmap(b => b.ID == imported.ID)!));
            AddUntilStep("the editor opens it", () => Editor?.ReadyForUse == true && EditorBeatmap.BeatmapInfo.ID == imported.ID);
        }
    }
}
