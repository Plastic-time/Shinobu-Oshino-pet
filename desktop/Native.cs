using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;

namespace ShinobuPet
{
    internal static class Native
    {
        [StructLayout(LayoutKind.Sequential)] internal struct PointNative
        {
            public int X, Y;
            internal PointNative(int x, int y) { X = x; Y = y; }
        }
        [StructLayout(LayoutKind.Sequential)] internal struct SizeNative
        {
            public int Width, Height;
            internal SizeNative(int width, int height) { Width = width; Height = height; }
        }
        [StructLayout(LayoutKind.Sequential, Pack = 1)] internal struct Blend
        { public byte Operation, Flags, Alpha, Format; }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UpdateLayeredWindow(IntPtr hwnd, IntPtr destination, ref PointNative position,
            ref SizeNative size, IntPtr source, ref PointNative sourcePoint, int key, ref Blend blend, int flags);
        [DllImport("user32.dll")] private static extern IntPtr GetDC(IntPtr hwnd);
        [DllImport("user32.dll")] private static extern int ReleaseDC(IntPtr hwnd, IntPtr dc);
        [DllImport("gdi32.dll")] private static extern IntPtr CreateCompatibleDC(IntPtr dc);
        [DllImport("gdi32.dll")] private static extern bool DeleteDC(IntPtr dc);
        [DllImport("gdi32.dll")] private static extern IntPtr SelectObject(IntPtr dc, IntPtr value);
        [DllImport("gdi32.dll")] private static extern bool DeleteObject(IntPtr value);
        [DllImport("user32.dll")] internal static extern bool DestroyIcon(IntPtr icon);

        internal static void Present(IntPtr window, Bitmap bitmap, Point location)
        {
            IntPtr screen = GetDC(IntPtr.Zero), memory = IntPtr.Zero, pixels = IntPtr.Zero, old = IntPtr.Zero;
            try
            {
                memory = CreateCompatibleDC(screen);
                pixels = bitmap.GetHbitmap(Color.FromArgb(0));
                old = SelectObject(memory, pixels);
                PointNative at = new PointNative(location.X, location.Y), source = new PointNative(0, 0);
                SizeNative size = new SizeNative(bitmap.Width, bitmap.Height);
                Blend blend = new Blend { Alpha = 255, Format = 1 };
                if (!UpdateLayeredWindow(window, screen, ref at, ref size, memory, ref source, 0, ref blend, 2))
                    throw new Win32Exception(Marshal.GetLastWin32Error());
            }
            finally
            {
                if (old != IntPtr.Zero) SelectObject(memory, old);
                if (pixels != IntPtr.Zero) DeleteObject(pixels);
                if (memory != IntPtr.Zero) DeleteDC(memory);
                if (screen != IntPtr.Zero) ReleaseDC(IntPtr.Zero, screen);
            }
        }
    }
}
