// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.IO;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Extensions;
using osu.Framework.Extensions.ObjectExtensions;
using osu.Framework.Platform;
using osu.Framework.Testing;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Database;
using osu.Game.IO.Archives;
using osu.Game.Tests.Resources;
using osu.Game.Tests.Visual;

namespace osu.Game.Tests.Beatmaps.IO
{
    /// <summary>
    /// Writing one difficulty out as a bare <c>.osu</c>, which is what song select's shift-right-click does.
    /// </summary>
    [HeadlessTest]
    public partial class BeatmapFileExportTest : OsuTestScene
    {
        [Resolved]
        private BeatmapManager beatmaps { get; set; } = null!;

        private Storage exports => LocalStorage.GetStorageForDirectory(@"exports");

        [SetUpSteps]
        public void StartFromAnEmptyExportDirectory()
        {
            // the directory outlives a single test, and both tests here read everything in it.
            AddStep("clear what was written before", () =>
            {
                foreach (string file in exports.GetFiles(string.Empty))
                    exports.Delete(file);
            });
        }

        /// <summary>
        /// The two formats have to actually differ. Both roads lead to the same method, and only which exporter it
        /// is asked of decides whether the file is converted back for stable on the way.
        /// </summary>
        [Test]
        public void TestTheFormatSettingDecidesWhatIsWritten()
        {
            IWorkingBeatmap beatmap = null!;

            AddStep("import a beatmap", () => beatmap = importBeatmap());
            AddStep("save it", () => beatmaps.Save((beatmap.BeatmapInfo as BeatmapInfo)!, beatmap.Beatmap));

            AddStep("write it out for stable", () => beatmaps.ExportBeatmapFile((BeatmapInfo)beatmap.BeatmapInfo, BeatmapExportFormat.Stable));
            AddAssert("it came out as v14", () => versionsWritten(), () => Is.EquivalentTo(new[] { 14 }));

            AddStep("write it out for lazer", () => beatmaps.ExportBeatmapFile((BeatmapInfo)beatmap.BeatmapInfo, BeatmapExportFormat.Lazer));
            AddAssert("the second one is what was stored", () => versionsWritten(), () => Is.EquivalentTo(new[] { 14, 128 }));
        }

        /// <summary>
        /// What "for lazer" writes for a beatmap that has never been saved in lazer: the file as imported. Nothing
        /// rewrites it until an editor session saves, so it is not v128 and need not even be v14 -- this one is
        /// a v13 the game reads perfectly well.
        /// </summary>
        [Test]
        public void TestAnImportedBeatmapKeepsTheVersionItArrivedAs()
        {
            IWorkingBeatmap beatmap = null!;

            AddStep("import one that came from stable", () => beatmap = importBeatmap(@"241526 Soleily - Renatus.osz"));
            AddStep("write it out for lazer", () => beatmaps.ExportBeatmapFile((BeatmapInfo)beatmap.BeatmapInfo, BeatmapExportFormat.Lazer));

            AddAssert("it came out as it went in", () => versionsWritten(), () => Is.EquivalentTo(new[] { 13 }));
        }

        /// <summary>
        /// Song select does not hand out the beatmaps realm holds. Its carousel keeps detached copies, and an
        /// export handed one of those wrote an archive with nothing in it and could not find the file behind a
        /// difficulty at all.
        /// </summary>
        [Test]
        public void TestACopyFromSongSelectExportsWhatIsInIt()
        {
            IWorkingBeatmap beatmap = null!;
            BeatmapSetInfo detached = null!;

            AddStep("import a beatmap", () => beatmap = importBeatmap());
            AddStep("save it", () => beatmaps.Save((beatmap.BeatmapInfo as BeatmapInfo)!, beatmap.Beatmap));

            AddStep("take the copy song select would have", () =>
                detached = Realm.Run(r => r.Find<BeatmapSetInfo>(((BeatmapInfo)beatmap.BeatmapInfo).BeatmapSet!.ID)!.Detach()));

            AddStep("write the difficulty out", () => beatmaps.ExportBeatmapFile(detached.Beatmaps.First(), BeatmapExportFormat.Stable));
            AddAssert("the difficulty was written", () => versionsWritten(), () => Is.EquivalentTo(new[] { 14 }));

            AddStep("write the set out", () => beatmaps.Export(detached, BeatmapExportFormat.Stable).WaitSafely());
            AddUntilStep("the archive carries the difficulty", () => filesInExportedArchive(), () => Has.Some.EndsWith(@".osu"));
        }

        /// <summary>What is inside the one archive written out.</summary>
        private string[] filesInExportedArchive()
        {
            string? archive = exports.GetFiles(string.Empty, @"*.osz").SingleOrDefault();

            if (archive == null)
                return [];

            using var stream = exports.GetStream(archive);
            return new ZipArchiveReader(stream).Filenames.ToArray();
        }

        /// <summary>
        /// The format version of every <c>.osu</c> in the export directory. Unordered: the second write lands under
        /// a name the first one pushed it to, and that name does not sort after the first.
        /// </summary>
        private int[] versionsWritten() => exports.GetFiles(string.Empty, @"*.osu").Select(readVersion).ToArray();

        private int readVersion(string file)
        {
            using var stream = exports.GetStream(file);
            using var reader = new StreamReader(stream);

            string header = reader.ReadLine() ?? string.Empty;

            return int.Parse(header.Replace(@"osu file format v", string.Empty).Trim());
        }

        private IWorkingBeatmap importBeatmap(string filename = @"decimal-timing-beatmap.olz")
        {
            var imported = beatmaps.Import(new ImportTask(TestResources.OpenResource($@"Archives/{filename}"), filename)).GetResultSafely();
            return imported.AsNonNull().PerformRead(s => beatmaps.GetWorkingBeatmap(s.Beatmaps[0]));
        }
    }
}
