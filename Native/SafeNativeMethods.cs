using System;
using System.Drawing;
using System.Windows.Forms;

namespace Gestures.Native
{

    /// <summary>
    ///   Managed wrapper around native methods.
    /// </summary>
    /// 
    public static class SafeNativeMethods
    {

        /// <summary>
        ///   Extends the window frame into the client area.
        /// </summary>
        /// 
        /// <param name="window">
        ///   The handle to the window in which the frame will
        ///   be extended into the client area.</param>
        /// <param name="margins">
        ///   A pointer to a <see cref="ThemeMargins"/> structure that describes
        ///   the margins to use when extending the frame into the client area.</param>
        ///   
        public static void ExtendAeroGlassIntoClientArea(IWin32Window window, ThemeMargins margins)
        {
            if (window == null) throw new ArgumentNullException("window");
            NativeMethods.DwmExtendFrameIntoClientArea(window.Handle, ref margins);
        }

        /// <summary>
        ///   Obtains a value that indicates whether Desktop
        ///   Window Manager (DWM) composition is enabled. 
        /// </summary>
        /// 
        public static bool IsAeroEnabled
        {
            get { return NativeMethods.DwmIsCompositionEnabled(); }
        }

    }
}
