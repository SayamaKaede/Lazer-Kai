// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Screens.Edit;
using osuTK;

namespace osu.Game.Rulesets.Osu.Edit
{
    /// <summary>
    /// The circles a stream is being shown as, kept in step with a plan as it changes.
    /// </summary>
    /// <remarks>
    /// Real objects in the beatmap rather than something drawn to look like them: a stream is judged on how it
    /// reads, and an outline of one reads differently from the thing itself.
    ///
    /// They are also held on screen whatever the current time is. A stream is longer than the moment being looked
    /// at, and the point of seeing it is seeing all of it -- the far end fading out is exactly the part that needs
    /// adjusting.
    /// </remarks>
    public class StreamPreview
    {
        private readonly EditorBeatmap beatmap;
        private readonly HitObjectComposer? composer;

        private readonly List<HitCircle> circles = new List<HitCircle>();

        public IReadOnlyList<HitCircle> Circles => circles;

        private readonly List<StreamConversion.Note> placed = new List<StreamConversion.Note>();

        /// <summary>
        /// When and where each circle was last put, which is what says how far it has been moved since.
        /// </summary>
        /// <remarks>
        /// Not where the plan had it: the circles are put on whole coordinates while those are kept. Measured from the
        /// plan, every circle that was cut to a whole coordinate had been moved by what was cut off, with nobody having
        /// touched it (2026-10-12).
        /// </remarks>
        public IReadOnlyList<StreamConversion.Note> Placed => placed;

        public StreamPreview(EditorBeatmap beatmap, HitObjectComposer? composer)
        {
            this.beatmap = beatmap;
            this.composer = composer;
        }

        /// <summary>
        /// Brings what is shown into line with <paramref name="plan"/>, moving the circles already there rather
        /// than replacing them, so that nothing flickers while a slider is being dragged about.
        /// </summary>
        /// <remarks>
        /// <paramref name="source"/> is the slider the stream is laid along, which its sound and combo come from.
        /// </remarks>
        /// <remarks>
        /// <paramref name="newComboEvery"/> is how many circles apart a new combo starts, or 0 for none but the first.
        /// </remarks>
        public void Update(Slider source, IReadOnlyList<StreamConversion.Note> plan, int newComboEvery = 0)
        {
            if (circles.Count > plan.Count)
            {
                var extra = circles.GetRange(plan.Count, circles.Count - plan.Count);

                remove(extra);
                circles.RemoveRange(plan.Count, circles.Count - plan.Count);
            }

            var added = new List<HitCircle>();

            // on whole coordinates, as every other position is (2026-10-05): a stream laid along a curve lands on fractions.
            Vector2 whole(Vector2 position) => (composer as OsuHitObjectComposer)?.RoundPositionIfAsked(position) ?? position;

            placed.Clear();

            for (int i = 0; i < plan.Count; i++)
            {
                var put = new StreamConversion.Note(plan[i].Time, whole(plan[i].Position));

                placed.Add(put);

                if (i < circles.Count)
                {
                    circles[i].StartTime = put.Time;
                    circles[i].Position = put.Position;
                    continue;
                }

                added.Add(new HitCircle
                {
                    StartTime = put.Time,
                    Position = put.Position,
                    Samples = source.HeadCircle.Samples.Select(s => s.With()).ToList(),
                });
            }

            circles.AddRange(added);

            beatmap.AddRange(added);

            // set on every pass, not only on the ones just made: the spacing can change with the circles staying
            // put. The beatmap has to be told about each change -- the combo numbers and colours are worked out
            // there, so writing the flag alone left the run looking exactly as it did before.
            for (int i = 0; i < circles.Count; i++)
            {
                bool starts = i == 0 ? source.NewCombo : newComboEvery > 0 && i % newComboEvery == 0;

                if (circles[i].NewCombo == starts)
                    continue;

                circles[i].NewCombo = starts;
                beatmap.Update(circles[i]);
            }

            HoldOnScreen();
        }

        /// <summary>
        /// Whether the beatmap has stopped holding any of the circles -- deleted from under the panel, from the
        /// playfield or the timeline or a key.
        /// </summary>
        public bool AnythingWasTakenAway() => circles.Any(c => !beatmap.HitObjects.Contains(c));

        /// <summary>
        /// Forgets the circles the beatmap no longer holds, so that the next pass builds them again.
        /// </summary>
        /// <remarks>
        /// Only those. The ones it still holds are moved to where they belong by the next pass, as they are on any other.
        /// Forgetting all of them left those in the beatmap with nothing holding them, and the next pass laid a whole
        /// run down over them: one circle deleted from a run of six left eleven (2026-10-12).
        /// </remarks>
        public void Forget() => circles.RemoveAll(c => !beatmap.HitObjects.Contains(c));

        /// <summary>
        /// Says again that every circle is to stay drawn.
        /// </summary>
        /// <remarks>
        /// Said every frame rather than once on the way in. An object added this frame has nothing in the playfield
        /// to say it about yet, so saying it only then reaches nothing and the far end of the stream fades out.
        /// </remarks>
        public void HoldOnScreen()
        {
            foreach (var circle in circles)
                composer?.KeepAlive(circle, true);
        }

        /// <summary>
        /// Takes the circles out again, for a stream that was not kept.
        /// </summary>
        public void Clear()
        {
            remove(circles);
            circles.Clear();
            placed.Clear();
        }

        /// <summary>
        /// Lets go of the circles without removing them, for a stream that was kept. They stop being held on
        /// screen and go back to being ordinary objects.
        /// </summary>
        public void Release()
        {
            foreach (var circle in circles)
                composer?.KeepAlive(circle, false);

            circles.Clear();
            placed.Clear();
        }

        private void remove(List<HitCircle> toRemove)
        {
            foreach (var circle in toRemove)
                composer?.KeepAlive(circle, false);

            beatmap.RemoveRange(toRemove);
        }
    }
}
