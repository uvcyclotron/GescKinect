using System;
using System.CodeDom.Compiler;
using System.Runtime.InteropServices;

namespace Gestures.Native
{

    /// <summary>
    ///   Native Win32 methods.
    /// </summary>
    /// 
    internal static partial class NativeMethods
    {

        [DllImport("dwmapi.dll", PreserveSig = false)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern void DwmExtendFrameIntoClientArea(IntPtr hwnd, ref ThemeMargins margins);

        [DllImport("dwmapi.dll", PreserveSig = false)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DwmIsCompositionEnabled();

    }

    /// <summary>
    ///   MARGINS structure (Windows).
    /// </summary>
    /// 
    /// <remarks>
    ///   Returned by the GetThemeMargins function to define the 
    ///   margins of windows that have visual styles applied.
    ///   
    ///   http://msdn.microsoft.com/en-us/library/windows/desktop/bb773244(v=vs.85).aspx
    /// </remarks>
    /// 
    [StructLayout(LayoutKind.Sequential)]
    [GeneratedCode("PInvoke", "1.0.0.0")]
    public struct ThemeMargins
    {
        /// <summary>
        ///   Width of the left border that retains its size.
        /// </summary>
        /// 
        public int LeftWidth;

        /// <summary>
        ///   Width of the right border that retains its size.
        /// </summary>
        /// 
        public int RightWidth;

        /// <summary>
        ///   Height of the top border that retains its size.
        /// </summary>
        /// 
        public int TopHeight;

        /// <summary>
        ///   Height of the bottom border that retains its size.
        /// </summary>
        /// 
        public int BottomHeight;
    }
}
