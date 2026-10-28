// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osu.Framework.Logging;
using osu.Framework.Platform;
using osu.Framework.Testing;
using osu.Game.Input;
using osuTK.Input;

namespace osu.Game.Screens.Edit
{
    /// <summary>
    /// Writes down what is done in the editor while it is open, one line of JSON each, for working on this game: every
    /// change with how the file changed, and every click, key and scroll with what it was over (2026-10-28).
    /// </summary>
    /// <remarks>
    /// The file is written in full once, as it was opened, and after that only the lines each change takes out and puts
    /// in, so that an hour of mapping comes to a few megabytes rather than a copy of the file for every change. Undo and
    /// redo are written down the same way, marked as such.
    ///
    /// The mouse is written down where it is pressed and let go, and not as it moves: where an object was put is in the
    /// change itself.
    /// </remarks>
    public partial class EditorOperationLog : Component
    {
        /// <summary>
        /// Directory the logs are written under, relative to the game's storage.
        /// </summary>
        public const string DIRECTORY = "lazer-kai-logs";

        private readonly EditorBeatmap editorBeatmap;
        private readonly EditorChangeHandler changeHandler;
        private readonly EditorClock clock;

        [Resolved]
        private Storage storage { get; set; } = null!;

        [Resolved]
        private OperationInputWatcher? inputWatcher { get; set; }

        private StreamWriter? writer;
        private string[] lastLines = Array.Empty<string>();

        /// <summary>
        /// Where this log is being written, relative to the game's storage.
        /// </summary>
        public string? Path { get; private set; }

        public EditorOperationLog(EditorBeatmap editorBeatmap, EditorChangeHandler changeHandler, EditorClock clock)
        {
            this.editorBeatmap = editorBeatmap;
            this.changeHandler = changeHandler;
            this.clock = clock;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            try
            {
                Path = System.IO.Path.Combine(DIRECTORY, $"editor-{DateTime.Now.ToString("yyyyMMdd-HHmmss-fff", CultureInfo.InvariantCulture)}.jsonl");
                writer = new StreamWriter(storage.GetFullPath(Path, true), false, new UTF8Encoding(false)) { AutoFlush = true };
            }
            catch (Exception e)
            {
                Logger.Error(e, "The editor's operation log could not be opened.");
                writer = null;
                return;
            }

            lastLines = linesOf(changeHandler.CurrentState);

            write(new Dictionary<string, object?>
            {
                ["kind"] = "open",
                ["difficulty"] = editorBeatmap.BeatmapInfo.DifficultyName,
                ["ruleset"] = editorBeatmap.BeatmapInfo.Ruleset.ShortName,
                ["file"] = lastLines,
            });

            changeHandler.OnStateChange += stateChanged;

            if (inputWatcher != null)
                inputWatcher.Received += inputReceived;
        }

        private void stateChanged()
        {
            string[] lines = linesOf(changeHandler.CurrentState);

            write(new Dictionary<string, object?>
            {
                ["kind"] = changeHandler.LastStep switch
                {
                    -1 => "undo",
                    1 => "redo",
                    _ => "change",
                },
                ["hunks"] = Diff(lastLines, lines).Select(h => new Dictionary<string, object>
                {
                    ["line"] = h.Line,
                    ["removed"] = h.Removed,
                    ["added"] = h.Added,
                }).ToArray(),
            });

            lastLines = lines;
        }

        /// <summary>
        /// One place where a file changed: the lines taken out from <see cref="Line"/> of the file before, and those put in.
        /// </summary>
        public readonly record struct Hunk(int Line, string[] Removed, string[] Added);

        /// <summary>
        /// The places where <paramref name="before"/> and <paramref name="after"/> differ, each on its own.
        /// </summary>
        /// <remarks>
        /// Placing one object changes the file in two places far apart, its green line and its object, and what lies
        /// between is not part of the change (2026-10-28: written down as one, a change came to 10 KB). The shortest edit
        /// between the two (Myers), run only over what lies between the lines both start and end with.
        /// </remarks>
        public static List<Hunk> Diff(string[] before, string[] after)
        {
            int start = 0;
            while (start < before.Length && start < after.Length && before[start] == after[start])
                start++;

            int end = 0;
            while (end < before.Length - start && end < after.Length - start && before[^(end + 1)] == after[^(end + 1)])
                end++;

            string[] a = before[start..(before.Length - end)];
            string[] b = after[start..(after.Length - end)];

            var hunks = new List<Hunk>();

            if (a.Length == 0 && b.Length == 0)
                return hunks;

            // which line of a and b each step is at, read back from the end: -1 for a line taken out, 1 for one put in, 0 for one both have.
            var steps = shortestEdit(a, b);

            int x = 0, y = 0;
            int i = 0;

            while (i < steps.Count)
            {
                if (steps[i] == 0)
                {
                    x++;
                    y++;
                    i++;
                    continue;
                }

                int line = start + x;
                var removed = new List<string>();
                var added = new List<string>();

                while (i < steps.Count && steps[i] != 0)
                {
                    if (steps[i] < 0)
                        removed.Add(a[x++]);
                    else
                        added.Add(b[y++]);

                    i++;
                }

                hunks.Add(new Hunk(line, removed.ToArray(), added.ToArray()));
            }

            return hunks;
        }

        /// <summary>
        /// The steps from <paramref name="a"/> to <paramref name="b"/>, in order: -1 takes a line of a out, 1 puts a line of b
        /// in, 0 keeps a line both have.
        /// </summary>
        private static List<int> shortestEdit(string[] a, string[] b)
        {
            int n = a.Length, m = b.Length, max = n + m, offset = max + 1;
            int[] v = new int[2 * max + 3];
            var trace = new List<int[]>();

            for (int d = 0; d <= max; d++)
            {
                trace.Add((int[])v.Clone());

                for (int k = -d; k <= d; k += 2)
                {
                    int x = k == -d || (k != d && v[offset + k - 1] < v[offset + k + 1]) ? v[offset + k + 1] : v[offset + k - 1] + 1;
                    int y = x - k;

                    while (x < n && y < m && a[x] == b[y])
                    {
                        x++;
                        y++;
                    }

                    v[offset + k] = x;

                    if (x >= n && y >= m)
                        return backtrack(trace, offset, n, m);
                }
            }

            throw new InvalidOperationException("No edit found.");
        }

        private static List<int> backtrack(List<int[]> trace, int offset, int n, int m)
        {
            var steps = new List<int>();
            int x = n, y = m;

            for (int d = trace.Count - 1; d > 0; d--)
            {
                int[] v = trace[d];
                int k = x - y;
                int previousK = k == -d || (k != d && v[offset + k - 1] < v[offset + k + 1]) ? k + 1 : k - 1;
                int previousX = v[offset + previousK];
                int previousY = previousX - previousK;

                while (x > previousX && y > previousY)
                {
                    steps.Add(0);
                    x--;
                    y--;
                }

                steps.Add(x == previousX ? 1 : -1);
                x = previousX;
                y = previousY;
            }

            while (x > 0 && y > 0)
            {
                steps.Add(0);
                x--;
                y--;
            }

            steps.Reverse();
            return steps;
        }

        private void inputReceived(UIEvent e)
        {
            switch (e)
            {
                case MouseDownEvent down:
                    write(new Dictionary<string, object?>
                    {
                        ["kind"] = "press",
                        ["button"] = down.Button.ToString(),
                        ["x"] = Math.Round(down.ScreenSpaceMousePosition.X),
                        ["y"] = Math.Round(down.ScreenSpaceMousePosition.Y),
                        ["keys"] = modifiersOf(e),
                        ["over"] = describeHovered(),
                    });
                    break;

                case MouseUpEvent up:
                    write(new Dictionary<string, object?>
                    {
                        ["kind"] = "release",
                        ["button"] = up.Button.ToString(),
                        ["x"] = Math.Round(up.ScreenSpaceMousePosition.X),
                        ["y"] = Math.Round(up.ScreenSpaceMousePosition.Y),
                    });
                    break;

                case ScrollEvent scroll:
                    string over = describeHovered();
                    string keys = modifiersOf(e);

                    // a turn of the wheel comes as many events: those over the same thing in quick succession are one.
                    if (pendingScroll != null && Time.Current - pendingScrollAt < scroll_merge_time
                                              && (string?)pendingScroll["over"] == over && (string?)pendingScroll["keys"] == keys)
                    {
                        pendingScroll["delta"] = Math.Round((double)pendingScroll["delta"]! + scroll.ScrollDelta.Y, 2);
                        pendingScroll["count"] = (int)pendingScroll["count"]! + 1;
                    }
                    else
                    {
                        flushScroll();
                        pendingScroll = stamped(new Dictionary<string, object?>
                        {
                            ["kind"] = "scroll",
                            ["delta"] = Math.Round((double)scroll.ScrollDelta.Y, 2),
                            ["count"] = 1,
                            ["keys"] = keys,
                            ["over"] = over,
                        });
                    }

                    pendingScrollAt = Time.Current;
                    break;
            }
        }

        private const double scroll_merge_time = 500;

        private Dictionary<string, object?>? pendingScroll;
        private double pendingScrollAt;

        private void flushScroll()
        {
            if (pendingScroll == null)
                return;

            var entry = pendingScroll;
            pendingScroll = null;
            writeLine(entry);
        }

        private readonly HashSet<Key> keysDown = new HashSet<Key>();

        /// <summary>
        /// Keys are read from what is held each frame rather than from the events: a shortcut the game takes before
        /// anything else sees it, as Ctrl+Z is, never reaches <see cref="OperationInputWatcher"/>.
        /// </summary>
        protected override void Update()
        {
            base.Update();

            if (writer == null)
                return;

            if (pendingScroll != null && Time.Current - pendingScrollAt >= scroll_merge_time)
                flushScroll();

            var keyboard = GetContainingInputManager()?.CurrentState.Keyboard;

            if (keyboard == null)
                return;

            foreach (var key in keyboard.Keys)
            {
                if (!keysDown.Add(key))
                    continue;

                write(new Dictionary<string, object?>
                {
                    ["kind"] = "key",
                    ["key"] = key.ToString(),
                    ["keys"] = string.Concat(keyboard.ControlPressed ? "Ctrl+" : "", keyboard.AltPressed ? "Alt+" : "", keyboard.ShiftPressed ? "Shift+" : "").TrimEnd('+'),
                });
            }

            foreach (var key in keysDown.Where(k => !keyboard.Keys.IsPressed(k)).ToArray())
            {
                keysDown.Remove(key);
                write(new Dictionary<string, object?>
                {
                    ["kind"] = "key up",
                    ["key"] = key.ToString(),
                });
            }
        }

        private static string modifiersOf(UIEvent e)
            => string.Concat(e.ControlPressed ? "Ctrl+" : "", e.AltPressed ? "Alt+" : "", e.ShiftPressed ? "Shift+" : "").TrimEnd('+');

        /// <summary>
        /// The few drawables in front under the mouse, by type, with the first text found among them: enough to tell a
        /// button or menu item from the playfield.
        /// </summary>
        private string describeHovered()
        {
            var inputManager = GetContainingInputManager();

            if (inputManager == null)
                return string.Empty;

            // what covers the whole screen comes first, the game's own containers among it, and says nothing of what was
            // under the mouse (2026-10-28: every click was "PlatformActionContainer").
            float screen = inputManager.ScreenSpaceDrawQuad.Area;
            // nor do the cursor, which follows the mouse, or what has no size of its own, as this log's watcher has none.
            var hovered = inputManager.HoveredDrawables
                                      .Where(d => d is not OperationInputWatcher && d is not CursorContainer && d.Parent is not CursorContainer)
                                      .Where(d => d.ScreenSpaceDrawQuad.Area > 1 && d.ScreenSpaceDrawQuad.Area < screen / 2)
                                      .Take(3).ToArray();

            string types = string.Join(" < ", hovered.Select(d => d.GetType().Name));
            string? text = null;

            // a part of a button, as its sounds are, holds no text itself: the button around it does.
            foreach (var drawable in hovered)
            {
                text = textOf(drawable);

                if (string.IsNullOrEmpty(text) && drawable.Parent is Drawable parent && parent.ScreenSpaceDrawQuad.Area < screen / 2)
                    text = textOf(parent);

                if (!string.IsNullOrEmpty(text))
                    break;
            }

            return string.IsNullOrEmpty(text) ? types : $"{types} \"{text}\"";
        }

        private static string? textOf(Drawable drawable)
        {
            if (drawable is IHasText hasText)
                return hasText.Text.ToString();

            if (drawable is not CompositeDrawable composite)
                return null;

            // only what is small enough to be one control: a screen's worth of text says nothing of what was clicked.
            if (composite.DrawWidth * composite.DrawHeight > 400 * 100)
                return null;

            return composite.ChildrenOfType<SpriteText>().Select(s => s.Text.ToString()).FirstOrDefault(t => !string.IsNullOrEmpty(t));
        }

        /// <summary>
        /// Writes <paramref name="entry"/> down as of now, after any scroll waiting to be written.
        /// </summary>
        private void write(Dictionary<string, object?> entry)
        {
            flushScroll();
            writeLine(stamped(entry));
        }

        /// <summary>
        /// <paramref name="entry"/> with the time of day and the time in the track it happened at in front.
        /// </summary>
        private Dictionary<string, object?> stamped(Dictionary<string, object?> entry)
        {
            var line = new Dictionary<string, object?>
            {
                ["at"] = DateTime.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture),
                ["time"] = Math.Round(clock.CurrentTime),
            };

            foreach (var (key, value) in entry)
                line[key] = value;

            return line;
        }

        private void writeLine(Dictionary<string, object?> line)
        {
            if (writer == null)
                return;

            try
            {
                writer.WriteLine(JsonConvert.SerializeObject(line, Formatting.None));
            }
            catch (Exception e)
            {
                Logger.Error(e, "The editor's operation log could not be written to, and is closed.");
                writer.Dispose();
                writer = null;
            }
        }

        private static string[] linesOf(byte[]? state)
            => state == null ? Array.Empty<string>() : Encoding.UTF8.GetString(state).Replace("\r\n", "\n").Split('\n');

        protected override void Dispose(bool isDisposing)
        {
            changeHandler.OnStateChange -= stateChanged;

            if (inputWatcher != null)
                inputWatcher.Received -= inputReceived;

            flushScroll();
            writer?.Dispose();
            writer = null;

            base.Dispose(isDisposing);
        }
    }
}
