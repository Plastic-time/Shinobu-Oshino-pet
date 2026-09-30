using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.Win32;

namespace ShinobuPet
{
    internal sealed class PetForm : Form
    {
        private readonly Preferences preferences;
        private readonly bool persist;
        private readonly Bitmap atlas;
        private readonly Dictionary<int, Bitmap> frames = new Dictionary<int, Bitmap>();
        private readonly Timer clock = new Timer { Interval = 40 };
        private readonly Timer clickDelay = new Timer { Interval = SystemInformation.DoubleClickTime };
        private readonly Stopwatch elapsed = Stopwatch.StartNew();
        private readonly NotifyIcon tray;
        private readonly Icon petIcon;
        private readonly ContextMenuStrip menu = new ContextMenuStrip();
        private ToolStripMenuItem followItem, pauseItem, topItem;
        private int action = -1, shownFrame = -1;
        private double actionStart, nextIdle = 4500, lastPointerChange = -10000;
        private Point lastPointer, downAt, downLocation;
        private bool held, dragged, doubleClicked;
        private bool disposed;

        internal PetForm(Preferences settings, bool saveSettings)
        {
            preferences = settings;
            persist = saveSettings;
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Shinobu.spritesheet.png"))
            {
                if (stream == null) throw new InvalidDataException("Missing sprite resource");
                using (Bitmap original = new Bitmap(stream)) atlas = new Bitmap(original);
            }
            if (atlas.Width != 1536 || atlas.Height != 2288) throw new InvalidDataException("Invalid sprite dimensions");
            AutoScaleMode = AutoScaleMode.None;
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            Text = "忍野忍 · Shinobu Oshino Pet";
            TopMost = preferences.Topmost;
            Cursor = Cursors.Hand;
            Size = FrameSize;
            if (preferences.HasPosition)
            {
                Point saved = new Point(preferences.X, preferences.Y);
                Location = Motion.Clamp(saved, Size, Screen.FromPoint(saved).WorkingArea);
            }
            else MoveToPrimary();
            using (Bitmap iconFrame = atlas.Clone(new Rectangle(48, 8, 104, 136), PixelFormat.Format32bppArgb))
            {
                IntPtr rawIcon = iconFrame.GetHicon();
                try { using (Icon borrowed = Icon.FromHandle(rawIcon)) petIcon = (Icon)borrowed.Clone(); }
                finally { Native.DestroyIcon(rawIcon); }
            }
            Icon = petIcon;
            BuildMenu();
            tray = new NotifyIcon { Icon = petIcon, Text = "忍野忍 · 右键打开菜单", ContextMenuStrip = menu, Visible = true };
            tray.DoubleClick += delegate { MoveToPrimary(); };
            clock.Tick += delegate { TickFrame(); };
            clickDelay.Tick += delegate { clickDelay.Stop(); Play(3); };
            SystemEvents.DisplaySettingsChanged += DisplayChanged;
        }

        internal Size FrameSize { get { return new Size((int)(192 * preferences.Scale), (int)(208 * preferences.Scale)); } }
        internal int CurrentFrame { get { return shownFrame; } }
        internal int Action { get { return action; } }
        internal Preferences Settings { get { return preferences; } }
        internal ContextMenuStrip PetMenu { get { return menu; } }
        protected override bool ShowWithoutActivation { get { return true; } }
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x00080000 | 0x00000080 | 0x08000000; // Layered, tool window, no activate.
                return cp;
            }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            Present(0, 0);
            clock.Start();
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x0021) { m.Result = new IntPtr(3); return; } // MA_NOACTIVATE
            base.WndProc(ref m);
        }

        private void BuildMenu()
        {
            ToolStripMenuItem title = new ToolStripMenuItem("忍野忍 · Shinobu Oshino") { Enabled = false };
            menu.Items.Add(title);
            menu.Items.Add("挥挥手", null, delegate { Play(3); });
            menu.Items.Add("跳一下", null, delegate { Play(4); });
            ToolStripMenuItem actions = new ToolStripMenuItem("其他动作");
            string[] names = { "眨眨眼", "向右原地跑", "向左原地跑", "挥手", "跳跃", "有点失落", "等你回来", "认真思考", "检查一下" };
            for (int row = 0; row < names.Length; row++)
            {
                int chosen = row;
                actions.DropDownItems.Add(names[row], null, delegate { Play(chosen); });
            }
            menu.Items.Add(actions);
            menu.Items.Add(new ToolStripSeparator());
            followItem = new ToolStripMenuItem("视线跟随鼠标") { Checked = preferences.Follow, CheckOnClick = true };
            followItem.Click += delegate { preferences.Follow = followItem.Checked; Save(); };
            menu.Items.Add(followItem);
            pauseItem = new ToolStripMenuItem("暂停动画") { Checked = preferences.Paused, CheckOnClick = true };
            pauseItem.Click += delegate { SetPaused(pauseItem.Checked); };
            menu.Items.Add(pauseItem);
            topItem = new ToolStripMenuItem("显示在最前面") { Checked = preferences.Topmost, CheckOnClick = true };
            topItem.Click += delegate { preferences.Topmost = topItem.Checked; TopMost = preferences.Topmost; Save(); };
            menu.Items.Add(topItem);
            ToolStripMenuItem size = new ToolStripMenuItem("大小");
            foreach (double scale in new[] { 1.0, 1.25, 1.5, 2.0 })
            {
                double chosen = scale;
                ToolStripMenuItem item = new ToolStripMenuItem((int)(scale * 100) + "%") { Checked = scale == preferences.Scale };
                item.Click += delegate
                {
                    SetScale(chosen);
                    foreach (ToolStripMenuItem entry in size.DropDownItems) entry.Checked = entry == item;
                };
                size.DropDownItems.Add(item);
            }
            menu.Items.Add(size);
            menu.Items.Add("回到主屏幕", null, delegate { MoveToPrimary(); Save(); });
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("操作说明", null, delegate
            {
                MessageBox.Show("单击：挥手\n双击：跳跃\n按住左键：拖动\n右键：动作与设置\n托盘图标双击：回到主屏幕\n\n默认固定在原地，偶尔眨眼。移动鼠标时会看向你。\n关闭此程序：右键菜单 → 退出\n\n独立离线运行，无需账号。设置仅保存在本机。\nv2.0.0 · 非官方同人桌宠", "忍野忍 · 操作说明", MessageBoxButtons.OK, MessageBoxIcon.Information);
            });
            menu.Items.Add("退出", null, delegate { Close(); });
        }

        internal void Play(int row)
        {
            if (row < 0 || row >= Motion.Durations.Length) throw new ArgumentOutOfRangeException("row");
            clickDelay.Stop();
            preferences.Paused = false;
            pauseItem.Checked = false;
            action = row;
            actionStart = elapsed.Elapsed.TotalMilliseconds;
            Present(row, 0);
        }

        private void TickFrame()
        {
            if (preferences.Paused || held || menu.Visible) return;
            double now = elapsed.Elapsed.TotalMilliseconds;
            Point pointer = Cursor.Position;
            if (pointer != lastPointer) { lastPointer = pointer; lastPointerChange = now; }
            if (action >= 0)
            {
                int frame = Motion.FrameAt(action, now - actionStart);
                if (frame >= 0) { Present(action, frame); return; }
                action = -1;
                nextIdle = now + 5500;
            }
            if (now >= nextIdle) { Play(0); return; }
            if (preferences.Follow && now - lastPointerChange < 1800)
            {
                Point eye = new Point(Left + Width / 2, Top + (int)(85 * preferences.Scale));
                int index = Motion.LookIndex(eye, pointer);
                if (index >= 0) { Present(9 + index / 8, index % 8); return; }
            }
            Present(0, 0);
        }

        internal Bitmap GetFrame(int row, int column)
        {
            int key = row * 8 + column;
            Bitmap bitmap;
            if (frames.TryGetValue(key, out bitmap)) return bitmap;
            bitmap = new Bitmap(FrameSize.Width, FrameSize.Height, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.CompositingMode = CompositingMode.SourceCopy;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                using (ImageAttributes attributes = new ImageAttributes())
                {
                    attributes.SetWrapMode(WrapMode.TileFlipXY);
                    g.DrawImage(atlas, new Rectangle(Point.Empty, bitmap.Size), column * 192, row * 208, 192, 208, GraphicsUnit.Pixel, attributes);
                }
            }
            frames.Add(key, bitmap);
            return bitmap;
        }

        internal void Present(int row, int column)
        {
            int key = row * 8 + column;
            if (!IsHandleCreated || shownFrame == key) return;
            Native.Present(Handle, GetFrame(row, column), Location);
            shownFrame = key;
        }

        internal void SetScale(double scale)
        {
            if (scale != 1 && scale != 1.25 && scale != 1.5 && scale != 2) throw new ArgumentOutOfRangeException("scale");
            Point feet = new Point(Left + Width / 2, Top + Height);
            preferences.Scale = scale;
            foreach (Bitmap bitmap in frames.Values) bitmap.Dispose();
            frames.Clear();
            Size = FrameSize;
            Location = Motion.Clamp(new Point(feet.X - Width / 2, feet.Y - Height), Size, Screen.FromPoint(feet).WorkingArea);
            int frame = Math.Max(shownFrame, 0);
            shownFrame = -1;
            Present(frame / 8, frame % 8);
            Save();
        }

        internal void SetPaused(bool paused)
        {
            preferences.Paused = paused;
            pauseItem.Checked = paused;
            action = -1;
            nextIdle = elapsed.Elapsed.TotalMilliseconds + 5500;
            if (!paused) Present(0, 0);
            Save();
        }

        internal void MoveToPrimary()
        {
            Rectangle area = Screen.PrimaryScreen.WorkingArea;
            Location = Motion.Clamp(new Point(area.Right - Width - 60, area.Bottom - Height - 24), Size, area);
        }

        private void DisplayChanged(object sender, EventArgs e)
        {
            if (disposed || !IsHandleCreated) return;
            try
            {
                BeginInvoke((Action)delegate
                {
                    if (disposed) return;
                    Location = Motion.Clamp(Location, Size, Screen.FromPoint(Location).WorkingArea);
                    Save();
                });
            }
            catch (InvalidOperationException) { }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;
            BeginDrag(Cursor.Position);
            doubleClicked = e.Clicks >= 2;
            if (doubleClicked) Play(4);
        }

        internal void BeginDrag(Point pointer)
        {
            clickDelay.Stop();
            downAt = pointer;
            downLocation = Location;
            held = true;
            dragged = false;
            Capture = true;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (held) DragTo(Cursor.Position);
        }

        internal void DragTo(Point pointer)
        {
            if (!held) return;
            int dx = pointer.X - downAt.X, dy = pointer.Y - downAt.Y;
            Size threshold = SystemInformation.DragSize;
            if (Math.Abs(dx) > threshold.Width / 2 || Math.Abs(dy) > threshold.Height / 2) dragged = true;
            if (dragged)
                Location = Motion.Clamp(new Point(downLocation.X + dx, downLocation.Y + dy), Size, Screen.FromPoint(pointer).WorkingArea);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button == MouseButtons.Right) { clickDelay.Stop(); menu.Show(Cursor.Position); return; }
            if (e.Button != MouseButtons.Left || !held) return;
            bool click = !dragged && !doubleClicked;
            EndDrag();
            if (click) clickDelay.Start();
            doubleClicked = false;
        }

        internal void EndDrag()
        {
            held = false;
            Capture = false;
            Save();
        }

        protected override void OnMouseCaptureChanged(EventArgs e)
        {
            base.OnMouseCaptureChanged(e);
            if (!Capture && held) { held = false; dragged = false; doubleClicked = false; }
        }

        private void Save()
        {
            preferences.X = Left;
            preferences.Y = Top;
            preferences.HasPosition = true;
            if (persist) preferences.Save(Preferences.DefaultPath);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            Save();
            base.OnFormClosing(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !disposed)
            {
                disposed = true;
                SystemEvents.DisplaySettingsChanged -= DisplayChanged;
                clock.Dispose();
                clickDelay.Dispose();
                tray.Visible = false;
                tray.Dispose();
                menu.Dispose();
                petIcon.Dispose();
                foreach (Bitmap frame in frames.Values) frame.Dispose();
                frames.Clear();
                atlas.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
