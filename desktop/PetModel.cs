using System;
using System.Drawing;
using System.Globalization;
using System.IO;

namespace ShinobuPet
{
    internal static class Motion
    {
        internal static readonly int[][] Durations = {
            new[] { 280, 110, 110, 140, 140, 320 },
            new[] { 120, 120, 120, 120, 120, 120, 120, 220 },
            new[] { 120, 120, 120, 120, 120, 120, 120, 220 },
            new[] { 140, 140, 140, 280 },
            new[] { 140, 140, 140, 140, 280 },
            new[] { 140, 140, 140, 140, 140, 140, 140, 240 },
            new[] { 150, 150, 150, 150, 150, 260 },
            new[] { 120, 120, 120, 120, 120, 220 },
            new[] { 150, 150, 150, 150, 150, 280 }
        };

        // Look angles are clockwise, starting above the pet.
        internal static int LookIndex(Point eye, Point pointer)
        {
            double dx = (double)pointer.X - eye.X, dy = (double)pointer.Y - eye.Y;
            if (dx * dx + dy * dy < 28 * 28) return -1;
            double angle = Math.Atan2(dx, -dy) * 180.0 / Math.PI;
            if (angle < 0) angle += 360;
            return ((int)Math.Floor((angle + 11.25) / 22.5)) % 16;
        }

        internal static int FrameAt(int row, double elapsed)
        {
            if (row < 0 || row >= Durations.Length) throw new ArgumentOutOfRangeException("row");
            for (int i = 0; i < Durations[row].Length; i++)
            {
                if (elapsed < Durations[row][i]) return i;
                elapsed -= Durations[row][i];
            }
            return -1;
        }

        internal static Point Clamp(Point point, Size size, Rectangle area)
        {
            return new Point(Math.Max(area.Left, Math.Min(point.X, Math.Max(area.Left, area.Right - size.Width))),
                Math.Max(area.Top, Math.Min(point.Y, Math.Max(area.Top, area.Bottom - size.Height))));
        }
    }

    internal sealed class Preferences
    {
        internal double Scale = 1.25;
        internal bool Follow = true;
        internal bool Topmost = true;
        internal bool Paused;
        internal bool HasPosition;
        internal int X, Y;
        internal static string DefaultPath
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ShinobuOshinoPet", "settings.txt"); }
        }

        internal static Preferences Load(string path)
        {
            Preferences result = new Preferences();
            try
            {
                if (!File.Exists(path) || new FileInfo(path).Length > 4096) return result;
                bool hasX = false, hasY = false;
                foreach (string line in File.ReadAllLines(path))
                {
                    string[] pair = line.Split('=');
                    if (pair.Length != 2) continue;
                    bool flag; int number; double scale;
                    switch (pair[0])
                    {
                        case "scale":
                            if (double.TryParse(pair[1], NumberStyles.Float, CultureInfo.InvariantCulture, out scale) &&
                                (scale == 1 || scale == 1.25 || scale == 1.5 || scale == 2)) result.Scale = scale;
                            break;
                        case "follow": if (bool.TryParse(pair[1], out flag)) result.Follow = flag; break;
                        case "topmost": if (bool.TryParse(pair[1], out flag)) result.Topmost = flag; break;
                        case "paused": if (bool.TryParse(pair[1], out flag)) result.Paused = flag; break;
                        case "x": if (int.TryParse(pair[1], out number) && Math.Abs((long)number) <= 100000) { result.X = number; hasX = true; } break;
                        case "y": if (int.TryParse(pair[1], out number) && Math.Abs((long)number) <= 100000) { result.Y = number; hasY = true; } break;
                    }
                }
                result.HasPosition = hasX && hasY;
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            return result;
        }

        internal bool Save(string path)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
                string body = "scale=" + Scale.ToString(CultureInfo.InvariantCulture) + "\nfollow=" + Follow +
                    "\ntopmost=" + Topmost + "\npaused=" + Paused + "\nx=" + X + "\ny=" + Y + "\n";
                string temporary = path + ".tmp";
                File.WriteAllText(temporary, body);
                if (File.Exists(path)) File.Replace(temporary, path, null);
                else File.Move(temporary, path);
                return true;
            }
            catch (IOException) { return false; }
            catch (UnauthorizedAccessException) { return false; }
        }
    }
}
