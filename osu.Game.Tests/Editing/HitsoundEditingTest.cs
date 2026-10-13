// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.IO;
using System.Linq;
using NUnit.Framework;
using osu.Game.Audio;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.Formats;
using osu.Game.Rulesets.Mania;
using osu.Game.Rulesets.Mania.Beatmaps;
using osu.Game.Rulesets.Mania.Objects;
using osu.Game.Rulesets.Objects.Legacy;
using osu.Game.Screens.Edit.Hitsounds;

namespace osu.Game.Tests.Editing
{
    /// <summary>
    /// What the buttons of the hitsound screen do to the sounds of a point.
    /// </summary>
    [TestFixture]
    public class HitsoundEditingTest
    {
        /// <summary>
        /// A sound that is a file of its own, as the notes of a difficulty hitsounded in stable's osu!mania are, stays
        /// that file whatever is asked of it. An addition made from it was the file again rather than an addition: a
        /// note with two hit sounds, which cannot be written (DOTNET-G, 2026-10-13).
        /// </summary>
        [Test]
        public void TestAnAdditionGivenToASoundFromAFileIsAnAddition()
        {
            var samples = HitsoundEditing.WithHitsound(new HitSampleInfo[] { new ConvertHitObjectParser.FileHitSampleInfo(@"drum-hitnormal", 70) },
                new PointHitsound(HitSampleInfo.BANK_NORMAL, HitSampleInfo.BANK_SOFT, new[] { HitSampleInfo.HIT_WHISTLE, HitSampleInfo.HIT_CLAP }));

            Assert.That(samples.Select(s => s.Name), Is.EqualTo(new[] { HitSampleInfo.HIT_NORMAL, HitSampleInfo.HIT_WHISTLE, HitSampleInfo.HIT_CLAP }));
            Assert.That(samples[0], Is.InstanceOf<ConvertHitObjectParser.FileHitSampleInfo>());
            Assert.That(samples.Skip(1).Select(s => (s.Bank, s.Volume)), Is.All.EqualTo((HitSampleInfo.BANK_SOFT, 70)));
            Assert.That(samples.Skip(1), Is.All.Not.InstanceOf<ConvertHitObjectParser.FileHitSampleInfo>());
        }

        /// <summary>
        /// The same for one put on with its own button.
        /// </summary>
        [Test]
        public void TestAnAdditionPutOnASoundFromAFileIsAnAddition()
        {
            var note = new Note { Samples = { new ConvertHitObjectParser.FileHitSampleInfo(@"drum-hitnormal", 70) } };

            HitsoundEditing.SetAddition(HitPoints.Of(note, false).ToList(), HitSampleInfo.HIT_CLAP, true, HitSampleInfo.BANK_SOFT);

            Assert.That(note.Samples.Select(s => s.Name), Is.EqualTo(new[] { HitSampleInfo.HIT_NORMAL, HitSampleInfo.HIT_CLAP }));
            Assert.That(note.Samples[1], Is.Not.InstanceOf<ConvertHitObjectParser.FileHitSampleInfo>());
            Assert.That((note.Samples[1].Bank, note.Samples[1].Volume), Is.EqualTo((HitSampleInfo.BANK_SOFT, 70)));
        }

        /// <summary>
        /// The same for one the object makes for itself, as the buttons of the compose screen have it do.
        /// </summary>
        [Test]
        public void TestAnAdditionAnObjectMakesFromASoundFromAFileIsAnAddition()
        {
            var note = new Note { Samples = { new ConvertHitObjectParser.FileHitSampleInfo(@"drum-hitnormal", 70) } };

            var addition = note.CreateHitSampleInfo(HitSampleInfo.HIT_CLAP);

            Assert.That(addition.Name, Is.EqualTo(HitSampleInfo.HIT_CLAP));
            Assert.That(addition, Is.Not.InstanceOf<ConvertHitObjectParser.FileHitSampleInfo>());
            Assert.That(addition.Volume, Is.EqualTo(70));
        }

        /// <summary>
        /// And the note can be written.
        /// </summary>
        [Test]
        public void TestANoteGivenAnAdditionCanBeWritten()
        {
            var note = new Note { Samples = { new ConvertHitObjectParser.FileHitSampleInfo(@"drum-hitnormal", 70) } };

            HitsoundEditing.SetHitsound(HitPoints.Of(note, false).ToList(), new PointHitsound(HitSampleInfo.BANK_NORMAL, HitSampleInfo.BANK_SOFT, new[] { HitSampleInfo.HIT_WHISTLE }));

            var beatmap = new ManiaBeatmap(new StageDefinition(4))
            {
                BeatmapInfo = { Ruleset = new ManiaRuleset().RulesetInfo },
                HitObjects = { note },
            };

            using var writer = new StringWriter();

            Assert.DoesNotThrow(() => new LegacyBeatmapEncoder(beatmap, null, null).Encode(writer));
            Assert.That(writer.ToString(), Does.Contain(@"drum-hitnormal"));
        }
    }
}
