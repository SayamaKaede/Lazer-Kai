// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using osu.Framework.Localisation;
using osu.Game.Extensions;
using osu.Game.Localisation;

namespace osu.Game.Tests.Localisation
{
    [TestFixture]
    public class LazerKaiJapaneseStoreTest
    {
        private static readonly Regex japanese = new Regex(@"[\p{IsHiragana}\p{IsKatakana}\p{IsCJKUnifiedIdeographs}]");

        /// <summary>
        /// Japanese belongs in <see cref="LazerKaiJapaneseStore"/>, never in a string's own fallback.
        /// </summary>
        /// <remarks>
        /// A fallback is what everyone not playing in Japanese sees, so writing Japanese there hands it to the wrong
        /// people — and silently, because it looks right to whoever wrote it.
        /// </remarks>
        [TestCaseSource(nameof(stringClasses))]
        public void TestNoFallbackIsWrittenInJapanese(Type stringClass)
        {
            var offenders = fallbacksIn(stringClass).Where(pair => japanese.IsMatch(pair.Fallback)).ToArray();

            Assert.That(offenders, Is.Empty,
                $"{stringClass.Name} carries Japanese in a fallback: {string.Join(", ", offenders.Select(o => o.Name))}");
        }

        /// <summary>
        /// Every string this fork adds is answered by the store.
        /// </summary>
        /// <remarks>
        /// A string's key is written out twice — once where the string is declared, once in the store — and a
        /// prefix that does not line up between the two fails quietly: the English fallback appears, which reads
        /// as a string nobody has translated yet rather than as a mistake. What is checked is that the store
        /// answers at all, not that the answer is in Japanese, since a name like "Lazer-Kai" reads the same
        /// either way — but it still has to be written down as a decision rather than fallen through to.
        ///
        /// Only the fork's own string classes are covered: its additions to the game's own classes cannot be
        /// told apart from what was already in them.
        /// </remarks>
        [TestCaseSource(nameof(forkStringClasses))]
        public void TestEveryForkStringIsInTheStore(Type stringClass)
        {
            var store = new LazerKaiJapaneseStore(new UntranslatedStore());

            // the stand-in below answers with the lookup itself, so a key that comes back unchanged is one the
            // store passed straight through.
            var missing = keysIn(stringClass).Where(pair => store.Get(pair.Key) == pair.Key).ToArray();

            Assert.That(missing, Is.Empty,
                $"{stringClass.Name} has nothing in the store for: {string.Join(", ", missing.Select(m => $"{m.Name} ({m.Key})"))}");
        }

        /// <summary>
        /// Works out what each string's key is, which also holds the classes to naming them the one way.
        /// </summary>
        /// <remarks>
        /// A <see cref="LocalisableString"/> keeps its key to itself, so it is rebuilt here from the convention
        /// every one of these classes follows: the namespace the game's own resources sit in, the class without
        /// its "Strings", then the property in snake case. A string that names its key some other way is
        /// reported as untranslated, which is what it would be in Japanese anyway unless the store were written
        /// to match — and then the next person would have two conventions to know about.
        /// </remarks>
        private static IEnumerable<(string Name, string Key)> keysIn(Type type)
        {
            string prefix = $@"osu.Game.Resources.Localisation.{type.Name.Replace(@"Strings", string.Empty, StringComparison.Ordinal)}";

            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Static))
            {
                if (property.PropertyType != typeof(LocalisableString))
                    continue;

                yield return (property.Name, $@"{prefix}:{property.Name.ToSnakeCase()}");
            }
        }

        private static IEnumerable<Type> forkStringClasses() => stringClasses().Where(t => t.Name.StartsWith(@"LazerKai", StringComparison.Ordinal));

        /// <summary>
        /// Stands in for the game's own localisations, and answers every lookup with the lookup itself so that
        /// anything the store does not translate is plainly not Japanese.
        /// </summary>
        private class UntranslatedStore : ILocalisationStore
        {
            public CultureInfo EffectiveCulture => CultureInfo.InvariantCulture;

            public string Get(string lookup) => lookup;

            public Task<string> GetAsync(string lookup, CancellationToken cancellationToken = default) => Task.FromResult(lookup);

            public Stream? GetStream(string name) => null;

            public IEnumerable<string> GetAvailableResources() => Array.Empty<string>();

            public void Dispose()
            {
            }
        }

        private static IEnumerable<(string Name, string Fallback)> fallbacksIn(Type type)
        {
            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Static))
            {
                if (property.PropertyType != typeof(LocalisableString))
                    continue;

                if (property.GetValue(null) is not LocalisableString value)
                    continue;

                // the fallback is what a localisable string reads as with no translation behind it.
                yield return (property.Name, value.ToString());
            }
        }

        private static IEnumerable<Type> stringClasses() => typeof(EditorStrings).Assembly
                                                                                 .GetTypes()
                                                                                 .Where(t => t.Namespace == typeof(EditorStrings).Namespace)
                                                                                 .Where(t => t.IsClass && t.IsAbstract && t.IsSealed)
                                                                                 .Where(t => t.Name.EndsWith("Strings", StringComparison.Ordinal))
                                                                                 .OrderBy(t => t.Name);
    }
}
