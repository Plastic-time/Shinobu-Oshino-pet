using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ShinobuPet
{
    internal static class Tests
    {
        private static int assertions;
        private static Exception failure;
        [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr h, int message, IntPtr w, IntPtr l);
        [DllImport("user32.dll")] private static extern int GetGuiResources(IntPtr process, int flags);
        [DllImport("user32.dll")] private static extern int GetWindowLong(IntPtr window, int index);

        private static void Check(bool value, string name)
        {
            if (!value) throw new Exception(name);
            assertions++;
        }

        [STAThread]
        private static int Main()
        {
            try
            {
                UnitTests();
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                UiTests();
                if (failure != null) throw failure;
                Console.WriteLine("PASS: " + assertions + " assertions; native window, alpha rendering, input, scaling, resource cleanup and settings verified.");
                return 0;
            }
            catch (Exception ex) { Console.Error.WriteLine("FAIL: " + ex.Message); return 1; }
        }

        private static void UnitTests()
        {
            Check(Motion.LookIndex(Point.Empty, new Point(0, -100)) == 0, "up");
            Check(Motion.LookIndex(Point.Empty, new Point(100, 0)) == 4, "right");
            Check(Motion.LookIndex(Point.Empty, new Point(0, 100)) == 8, "down");
            Check(Motion.LookIndex(Point.Empty, new Point(-100, 0)) == 12, "left");
            Check(Motion.LookIndex(Point.Empty, new Point(-1, -100)) == 0, "angle wrap");
            Check(Motion.LookIndex(Point.Empty, new Point(10, 10)) == -1, "pointer dead zone");
            Check(Motion.FrameAt(4, 0) == 0 && Motion.FrameAt(4, 140) == 1, "jump takeoff timing");
            Check(Motion.FrameAt(4, 839) == 4 && Motion.FrameAt(4, 840) == -1, "jump ends on time");
            Rectangle negativeMonitor = new Rectangle(-1920, 0, 1920, 1040);
            Check(Motion.Clamp(new Point(-2500, 2000), new Size(240, 260), negativeMonitor) == new Point(-1920, 780), "negative monitor bounds");
            Check(Motion.Clamp(new Point(10, 20), new Size(240, 260), new Rectangle(0, 0, 100, 100)) == Point.Empty, "undersized working area");
            string path = Path.Combine(Path.GetTempPath(), "ShinobuPet-test-" + Guid.NewGuid().ToString("N"), "settings.txt");
            try
            {
                Preferences defaults = Preferences.Load(path);
                Check(defaults.Scale == 1.25 && defaults.Follow && !defaults.HasPosition, "first run defaults");
                defaults.X = -1500; defaults.Y = 330; defaults.Scale = 2; defaults.Paused = true;
                Check(defaults.Save(path), "save settings");
                Preferences loaded = Preferences.Load(path);
                Check(loaded.X == -1500 && loaded.Y == 330 && loaded.Scale == 2 && loaded.Paused && loaded.HasPosition, "settings roundtrip");
                loaded.Scale = 1.5; Check(loaded.Save(path) && Preferences.Load(path).Scale == 1.5, "atomic replacement");
                File.WriteAllText(path, "scale=NaN\nx=-2147483648\ny=bad\nfollow=bad\npaused=bad\nunknown=value\n");
                loaded = Preferences.Load(path);
                Check(loaded.Scale == 1.25 && !loaded.HasPosition && loaded.Follow && !loaded.Paused, "corrupt settings recovery");
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
                if (Directory.Exists(Path.GetDirectoryName(path))) Directory.Delete(Path.GetDirectoryName(path));
            }
        }

        private static void UiTests()
        {
            using (Form canvas = new Form())
            using (Timer timer = new Timer { Interval = 100 })
            {
                canvas.Text = "Shinobu Pet · visual test";
                canvas.FormBorderStyle = FormBorderStyle.None;
                canvas.StartPosition = FormStartPosition.CenterScreen;
                canvas.ClientSize = new Size(840, 500);
                canvas.BackColor = Color.FromArgb(245, 242, 236);
                canvas.TopMost = true;
                canvas.Paint += delegate(object sender, PaintEventArgs e)
                {
                    using (Font title = new Font("Segoe UI", 26, FontStyle.Bold))
                    using (Font body = new Font("Segoe UI", 12))
                    using (SolidBrush ink = new SolidBrush(Color.FromArgb(48, 62, 55)))
                    {
                        e.Graphics.DrawString("Shinobu Oshino", title, ink, 38, 42);
                        e.Graphics.DrawString("Windows desktop companion", body, ink, 42, 97);
                        e.Graphics.DrawString("Click to wave\nDouble-click to jump\nDrag to move\nRight-click for settings", body, ink, 42, 220);
                        e.Graphics.DrawString("Offline  /  No account  /  Stays where you put her", body, ink, 42, 442);
                    }
                };
                PetForm pet = null;
                int step = 0, ticks = 0;
                IntPtr foreground = IntPtr.Zero;
                Stopwatch phase = Stopwatch.StartNew();
                canvas.Shown += delegate
                {
                    timer.Start();
                };
                timer.Tick += delegate
                {
                    try
                    {
                        if (pet == null)
                        {
                            foreground = GetForegroundWindow();
                            pet = new PetForm(new Preferences { Follow = false }, false);
                            pet.Show();
                            Check(GetForegroundWindow() == foreground, "pet does not steal focus");
                            pet.SetScale(2);
                            pet.Location = new Point(canvas.Left + 430, canvas.Top + 58);
                            return;
                        }
                        if (step == 0)
                        {
                            int style = GetWindowLong(pet.Handle, -20);
                            Check((style & 0x08080080) == 0x08080080, "layered, non-activating tool window");
                            Check(!pet.ShowInTaskbar && pet.TopMost, "taskbar and topmost defaults");
                            using (FileStream iconOutput = File.Create(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "pet.ico"))) pet.Icon.Save(iconOutput);
                            using (Bitmap capture = new Bitmap(canvas.Width, canvas.Height))
                            {
                                using (Graphics graphics = Graphics.FromImage(capture)) graphics.CopyFromScreen(canvas.Location, Point.Empty, capture.Size);
                                Check(capture.GetPixel(431, 59).ToArgb() == canvas.BackColor.ToArgb(), "transparent corner passes through");
                                int nonBackground = 0;
                                for (int y = 60; y < 440; y += 5)
                                    for (int x = 465; x < 780; x += 5)
                                        if (capture.GetPixel(x, y).ToArgb() != canvas.BackColor.ToArgb()) nonBackground++;
                                Check(nonBackground > 200, "native pet pixels visible");
                                capture.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "desktop-preview.png"), ImageFormat.Png);
                            }
                            for (int row = 0; row < 11; row++)
                            {
                                int count = row < 9 ? Motion.Durations[row].Length : 8;
                                for (int col = 0; col < count; col++)
                                {
                                    Bitmap frame = pet.GetFrame(row, col);
                                    Check(frame.GetPixel(0, 0).A == 0, "frame has transparent padding");
                                    pet.Present(row, col);
                                }
                            }
                            pet.Present(0, 0);
                            SendMessage(pet.Handle, 0x0201, new IntPtr(1), new IntPtr((180 << 16) | 192));
                            SendMessage(pet.Handle, 0x0202, IntPtr.Zero, new IntPtr((180 << 16) | 192));
                            phase.Restart(); step++;
                        }
                        else if (step == 1 && phase.ElapsedMilliseconds >= SystemInformation.DoubleClickTime + 100)
                        {
                            Check(pet.Action == 3, "native single click waves");
                            SendMessage(pet.Handle, 0x0203, new IntPtr(1), new IntPtr((180 << 16) | 192));
                            SendMessage(pet.Handle, 0x0202, IntPtr.Zero, new IntPtr((180 << 16) | 192));
                            Check(pet.Action == 4, "native double click jumps");
                            phase.Restart(); step++;
                        }
                        else if (step == 2 && phase.ElapsedMilliseconds > 1100)
                        {
                            Check(pet.Action == -1 && pet.CurrentFrame == 0, "jump returns to idle");
                            Point old = pet.Location;
                            Point pointer = new Point(old.X + 192, old.Y + 180);
                            pet.BeginDrag(pointer);
                            pet.DragTo(new Point(pointer.X - 25, pointer.Y - 20));
                            pet.EndDrag();
                            Check(pet.Location == new Point(old.X - 25, old.Y - 20), "drag follows pointer without jumping");
                            pet.BeginDrag(pointer);
                            pet.DragTo(new Point(-99999, -99999)); pet.EndDrag();
                            Check(Screen.FromPoint(pet.Location).WorkingArea.Contains(pet.Bounds), "drag clamps at monitor edge");
                            foreach (double scale in new[] { 1.0, 1.25, 1.5, 2.0 })
                            {
                                pet.SetScale(scale);
                                Check(pet.Size == new Size((int)(192 * scale), (int)(208 * scale)), "scaled window matches bitmap");
                            }
                            int before = GetGuiResources(Process.GetCurrentProcess().Handle, 0);
                            for (int i = 0; i < 250; i++) pet.Present(3, i % 4);
                            int after = GetGuiResources(Process.GetCurrentProcess().Handle, 0);
                            Check(after - before <= 3, "GDI render handles released");
                            pet.SetPaused(true); phase.Restart(); step++;
                        }
                        else if (step == 3 && phase.ElapsedMilliseconds > 400)
                        {
                            Check(pet.Settings.Paused && pet.Action == -1, "pause remains in effect");
                            pet.MoveToPrimary();
                            Check(Screen.PrimaryScreen.WorkingArea.Contains(pet.Bounds), "recover to primary screen");
                            SendMessage(pet.Handle, 0x0205, IntPtr.Zero, new IntPtr((180 << 16) | 192));
                            Check(pet.PetMenu.Visible, "native right click opens menu");
                            pet.PetMenu.Items[pet.PetMenu.Items.Count - 1].PerformClick();
                            Check(pet.IsDisposed, "window and tray disposed on exit");
                            timer.Stop(); canvas.Close();
                        }
                        if (++ticks > 150) throw new Exception("UI test timed out");
                    }
                    catch (Exception ex) { failure = ex; timer.Stop(); if (pet != null) pet.Dispose(); canvas.Close(); }
                };
                Application.Run(canvas);
                if (pet != null) pet.Dispose();
            }
        }
    }
}
