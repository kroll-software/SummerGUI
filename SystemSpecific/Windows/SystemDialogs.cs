using System;
using System.Runtime.InteropServices;
using System.Text;
using OpenTK.Windowing.Desktop;

namespace SummerGUI.SystemSpecific.Windows
{
    internal static class Win32Native
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct OPENFILENAME
        {
            public int lStructSize;
            public IntPtr hwndOwner;
            public IntPtr hInstance;
            public string lpstrFilter;
            public string lpstrCustomFilter;
            public int nMaxCustFilter;
            public int nFilterIndex;
            public string lpstrFile;
            public int nMaxFile;
            public string lpstrFileTitle;
            public int nMaxFileTitle;
            public string lpstrInitialDir;
            public string lpstrTitle;
            public int Flags;
            public short nFileOffset;
            public short nFileExtension;
            public string lpstrDefExt;
            public IntPtr lCustData;
            public IntPtr lpfnHook;
            public string lpTemplateName;
            public IntPtr pvReserved;
            public int dwReserved;
            public int FlagsEx;
        }

        [DllImport("comdlg32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern bool GetOpenFileName(ref OPENFILENAME ofn);

        [DllImport("comdlg32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern bool GetSaveFileName(ref OPENFILENAME ofn);

        // Für den FolderBrowser nutzen wir die Shell32 (modernere Variante siehe unten)
        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SHBrowseForFolder(ref BROWSEINFO bi);

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        public static extern bool SHGetPathFromIDList(IntPtr pidl, StringBuilder pszPath);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct BROWSEINFO
        {
            public IntPtr hwndOwner;
            public IntPtr pidlRoot;
            public string pszDisplayName;
            public string lpszTitle;
            public int ulFlags;
            public IntPtr lpfn;
            public IntPtr lParam;
            public int iImage;
        }

        public const int OFN_OVERWRITEPROMPT = 0x00000002;
        public const int OFN_PATHMUSTEXIST = 0x00000800;
        public const int OFN_FILEMUSTEXIST = 0x00001000;
        public const int OFN_NOCHANGEDIR = 0x00000008;
        
        public const int BIF_RETURNONLYFSDIRS = 0x0001;
        public const int BIF_NEWDIALOGSTYLE = 0x0040;
    }

    public class SystemDialogs
    {
        // Hilfsmethode, um das HWND aus dem GLFW NativeWindow zu extrahieren
        private unsafe IntPtr GetHWND(NativeWindow window)
        {
            // Unter Windows liefert GLFW direkt das HWND über eine spezifische Funktion
            return OpenTK.Windowing.GraphicsLibraryFramework.GLFW.GetWin32Window(window.WindowPtr);
        }

        // Hilfsmethode für das Windows-Filterformat (Ersetzt '|' durch '\0')
        private string FormatFilter(string filter)
        {
            if (string.IsNullOrEmpty(filter)) return null;
            return filter.Replace('|', '\0') + "\0\0";
        }

        public unsafe string OpenFileDialog(
            IGUIContext ctx, 
            string caption = "Open File",
            string filter = "", 
            int filterIndex = 1, 
            string initialDirectory = "",
            bool restoreDirectory = false)
        {
            var ofn = new Win32Native.OPENFILENAME();
            ofn.lStructSize = Marshal.SizeOf(ofn);
            ofn.hwndOwner = GetHWND(ctx.GlWindow);
            ofn.lpstrFilter = FormatFilter(filter);
            ofn.nFilterIndex = filterIndex;
            ofn.lpstrFile = new string(new char[1024]);
            ofn.nMaxFile = ofn.lpstrFile.Length;
            ofn.lpstrTitle = caption;
            ofn.lpstrInitialDir = initialDirectory;
            
            ofn.Flags = Win32Native.OFN_PATHMUSTEXIST | Win32Native.OFN_FILEMUSTEXIST;
            if (restoreDirectory) ofn.Flags |= Win32Native.OFN_NOCHANGEDIR;

            if (Win32Native.GetOpenFileName(ref ofn))
            {
                return ofn.lpstrFile;
            }
            return string.Empty;
        }

        public unsafe string SaveFileDialog(
            IGUIContext ctx, 
            string caption = "Save File",
            string defaultFileName = "", 
            string filter = "", 
            int filterIndex = 1, 
            string initialDirectory = "",
            bool restoreDirectory = false)
        {
            var ofn = new Win32Native.OPENFILENAME();
            ofn.lStructSize = Marshal.SizeOf(ofn);
            ofn.hwndOwner = GetHWND(ctx.GlWindow);
            ofn.lpstrFilter = FormatFilter(filter);
            ofn.nFilterIndex = filterIndex;
            
            // Default Filename vorbesetzen
            string fileName = defaultFileName.PadRight(1024, '\0');
            ofn.lpstrFile = fileName;
            ofn.nMaxFile = 1024;
            
            ofn.lpstrTitle = caption;
            ofn.lpstrInitialDir = initialDirectory;
            
            ofn.Flags = Win32Native.OFN_PATHMUSTEXIST | Win32Native.OFN_OVERWRITEPROMPT;
            if (restoreDirectory) ofn.Flags |= Win32Native.OFN_NOCHANGEDIR;

            if (Win32Native.GetSaveFileName(ref ofn))
            {
                return ofn.lpstrFile;
            }
            return string.Empty;
        }

        public unsafe string SelectFolderDialog(
            IGUIContext ctx, 
            string caption = "Select Folder",            
            string initialDirectory = "",
            bool restoreDirectory = false)
        {
            var bi = new Win32Native.BROWSEINFO();
            bi.hwndOwner = GetHWND(ctx.GlWindow);
            bi.lpszTitle = caption;
            bi.ulFlags = Win32Native.BIF_RETURNONLYFSDIRS | Win32Native.BIF_NEWDIALOGSTYLE;

            IntPtr pidl = Win32Native.SHBrowseForFolder(ref bi);
            if (pidl != IntPtr.Zero)
            {
                StringBuilder sb = new StringBuilder(1024);
                if (Win32Native.SHGetPathFromIDList(pidl, sb))
                {
                    return sb.ToString();
                }
            }
            return string.Empty;
        }
    }
}