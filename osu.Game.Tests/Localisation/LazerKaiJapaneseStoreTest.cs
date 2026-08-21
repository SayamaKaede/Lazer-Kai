// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using osu.Framework.Localisation;
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
