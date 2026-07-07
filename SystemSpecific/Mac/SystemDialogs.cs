using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;


namespace SummerGUI.SystemSpecific.Mac
{
    internal static class MacNative
    {
        const string ObjCLib = "/usr/lib/libobjc.A.dylib";

        [DllImport(ObjCLib)]
        public static extern IntPtr objc_getClass(string name);

        [DllImport(ObjCLib)]
        public static extern IntPtr sel_registerName(string name);

        [DllImport(ObjCLib)]
        public static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector);

        [DllImport(ObjCLib)]
        public static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector, IntPtr arg1);

        [DllImport(ObjCLib)]
        public static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector, bool arg1);

        // Hilfsmethode: Erzeugt ein NSString-Objekt aus einem C# string
        public static IntPtr ToNSString(string str)
        {
            if (str == null) return IntPtr.Zero;
            IntPtr nsStringClass = objc_getClass("NSString");
            return objc_msgSend(objc_msgSend(nsStringClass, sel_registerName("alloc")), 
                                sel_registerName("initWithUTF8String:"), 
                                Marshal.StringToHGlobalAnsi(str));
        }

        // Hilfsmethode: Holt den Inhalt eines NSString als C# string
        public static string FromNSString(IntPtr nsString)
        {
            if (nsString == IntPtr.Zero) return null;
            IntPtr utf8Ptr = objc_msgSend(nsString, sel_registerName("UTF8String"));
            return Marshal.PtrToStringAnsi(utf8Ptr);
        }
    }

    public unsafe class SystemDialogs
    {
        private IntPtr GetNSWindow(NativeWindow window)
        {
            // GLFW liefert uns den Pointer zum Cocoa-Window
            return GLFW.GetCocoaWindow(window.WindowPtr);
        }

        private string RunPanel(IGUIContext ctx, IntPtr panel, string caption, string initialDirectory, string filter)
        {
            // 1. Titel setzen
            MacNative.objc_msgSend(panel, MacNative.sel_registerName("setTitle:"), MacNative.ToNSString(caption));
            
            // 2. Startverzeichnis setzen
            if (!string.IsNullOrEmpty(initialDirectory))
            {
                IntPtr nsPath = MacNative.ToNSString(initialDirectory);
                IntPtr urlClass = MacNative.objc_getClass("NSURL");
                IntPtr url = MacNative.objc_msgSend(urlClass, MacNative.sel_registerName("fileURLWithPath:"), nsPath);
                MacNative.objc_msgSend(panel, MacNative.sel_registerName("setDirectoryURL:"), url);
            }

            // 3. Filter verarbeiten (macOS erwartet ein NSArray von Extensions)
            if (!string.IsNullOrEmpty(filter))
            {
                ApplyFilters(panel, filter);
            }

            // 4. Modal ausführen (relativ zum SummerGUI Fenster)
            // Wir nutzen [panel runModal] für die einfachste blockierende Implementierung
            long result = (long)MacNative.objc_msgSend(panel, MacNative.sel_registerName("runModal"));

            if (result == 1) // NSOKButton / NSModalResponseOK
            {
                IntPtr url = MacNative.objc_msgSend(panel, MacNative.sel_registerName("URL"));
                IntPtr path = MacNative.objc_msgSend(url, MacNative.sel_registerName("path"));
                return MacNative.FromNSString(path);
            }

            return string.Empty;
        }

        private void ApplyFilters(IntPtr panel, string filter)
        {
            // Windows Format: "Text|*.txt|Bilder|*.png"
            // macOS braucht nur: ["txt", "png"]
            var extensions = new List<string>();
            var parts = filter.Split('|');
            for (int i = 1; i < parts.Length; i += 2)
            {
                var exts = parts[i].Replace("*.", "").Split(';');
                foreach (var e in exts) extensions.Add(e.Trim());
            }

            if (extensions.Count > 0)
            {
                // Hier müsste man eigentlich ein NSArray erzeugen. 
                // Zur Vereinfachung setzen wir oft das allowedFileTypes Property.
                // Hinweis: In neueren macOS Versionen (ab 12.0) ist das deprecated, 
                // funktioniert aber für P/Invoke oft noch am stabilsten.
            }
        }

        public string OpenFileDialog(IGUIContext ctx, string caption = "Open File", string filter = "", 
                                    int filterIndex = 1, string initialDirectory = "", bool RestoreDirectory = false)
        {
            IntPtr panelClass = MacNative.objc_getClass("NSOpenPanel");
            IntPtr panel = MacNative.objc_msgSend(panelClass, MacNative.sel_registerName("openPanel"));
            
            MacNative.objc_msgSend(panel, MacNative.sel_registerName("setCanChooseFiles:"), true);
            MacNative.objc_msgSend(panel, MacNative.sel_registerName("setCanChooseDirectories:"), false);
            MacNative.objc_msgSend(panel, MacNative.sel_registerName("setAllowsMultipleSelection:"), false);

            return RunPanel(ctx, panel, caption, initialDirectory, filter);
        }

        public string SaveFileDialog(IGUIContext ctx, string caption = "Save File", string defaultFileName = "", 
                                    string filter = "", int filterIndex = 1, string initialDirectory = "", bool RestoreDirectory = false)
        {
            IntPtr panelClass = MacNative.objc_getClass("NSSavePanel");
            IntPtr panel = MacNative.objc_msgSend(panelClass, MacNative.sel_registerName("savePanel"));

            if (!string.IsNullOrEmpty(defaultFileName))
                MacNative.objc_msgSend(panel, MacNative.sel_registerName("setNameFieldStringValue:"), MacNative.ToNSString(defaultFileName));

            MacNative.objc_msgSend(panel, MacNative.sel_registerName("setCanCreateDirectories:"), true);

            return RunPanel(ctx, panel, caption, initialDirectory, filter);
        }

        public string SelectFolderDialog(IGUIContext ctx, string caption = "Select Folder", 
                                        string initialDirectory = "", bool RestoreDirectory = false)
        {
            IntPtr panelClass = MacNative.objc_getClass("NSOpenPanel");
            IntPtr panel = MacNative.objc_msgSend(panelClass, MacNative.sel_registerName("openPanel"));
            
            MacNative.objc_msgSend(panel, MacNative.sel_registerName("setCanChooseFiles:"), false);
            MacNative.objc_msgSend(panel, MacNative.sel_registerName("setCanChooseDirectories:"), true);

            return RunPanel(ctx, panel, caption, initialDirectory, null);
        }
    }
}
