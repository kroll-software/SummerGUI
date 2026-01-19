using System;
using System.Runtime.InteropServices;
using System.Text;
using OpenTK;
using OpenTK.Platform;
using OpenTK.Core.Native;
using OpenTK.Core.Platform;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace SummerGUI.SystemSpecific.Linux
{
    internal static class GtkNative
    {
        const string GtkLib = "libgtk-3.so.0";
        const string GdkLib = "libgdk-3.so.0";

        [DllImport(GtkLib)]
        public static extern void gtk_init(int argc, IntPtr argv);

        [DllImport(GtkLib)]
        public static extern IntPtr gtk_file_chooser_dialog_new(string title, IntPtr parent, int action, 
            string button1, int response1, string button2, int response2, IntPtr terminator);

        [DllImport(GtkLib)]
        public static extern int gtk_dialog_run(IntPtr dialog);

        [DllImport(GtkLib)]
        public static extern IntPtr gtk_file_chooser_get_filename(IntPtr dialog);

        [DllImport(GtkLib)]
        public static extern void gtk_widget_destroy(IntPtr dialog);

        [DllImport(GtkLib)]
        public static extern void gtk_main_iteration_do(bool blocking);

        [DllImport(GtkLib)]
        public static extern bool gtk_events_pending();

        [DllImport(GtkLib)]
        public static extern IntPtr gtk_widget_get_window(IntPtr widget);

        [DllImport(GtkLib)]
        public static extern void gtk_widget_realize(IntPtr widget);

        [DllImport(GdkLib)]
        public static extern IntPtr gdk_x11_window_get_xid(IntPtr gdkWindow);

        [DllImport("libgtk-3.so.0")]
        public static extern IntPtr gtk_file_filter_new();

        [DllImport("libgtk-3.so.0")]
        public static extern void gtk_file_filter_set_name(IntPtr filter, string name);

        [DllImport("libgtk-3.so.0")]
        public static extern void gtk_file_filter_add_pattern(IntPtr filter, string pattern);

        [DllImport("libgtk-3.so.0")]
        public static extern void gtk_file_chooser_add_filter(IntPtr chooser, IntPtr filter);

        [DllImport("libgtk-3.so.0")]
        public static extern void gtk_file_chooser_set_filter(IntPtr chooser, IntPtr filter);

        [DllImport("libgtk-3.so.0")]
        public static extern void gtk_file_chooser_set_current_folder(IntPtr chooser, string name);

        [DllImport("libgtk-3.so.0")]
        public static extern void gtk_file_chooser_set_do_overwrite_confirmation(IntPtr chooser, int setting);

        [DllImport("libgtk-3.so.0")]
        public static extern void gtk_window_present(IntPtr window);

        // Setzt den vorgeschlagenen Dateinamen (nur den Namen, nicht den Pfad!)
        [DllImport("libgtk-3.so.0")]
        public static extern void gtk_file_chooser_set_current_name(IntPtr chooser, string name);

        // Ermöglicht/Erzwingt das Erstellen von Ordnern
        [DllImport("libgtk-3.so.0")]
        public static extern void gtk_file_chooser_set_create_folders(IntPtr chooser, int setting);        

        // Wichtig für die Speicherfreigabe von Strings, die GTK erstellt hat
        [DllImport("libglib-2.0.so.0")]
        public static extern void g_free(IntPtr mem);
    }    

    public enum GtkResponseType
    {
        None = -1,
        Reject = -2,
        Accept = -3,
        DeleteEvent = -4,
        OK = -5,
        Cancel = -6,
        Close = -7,
        Yes = -8,
        No = -9,
        Apply = -10,
        Help = -11
    }

    public class SystemDialogs
    {
        private static bool IsInitialized = false;

        private static void EnsureGtkInit()
        {
            if (!IsInitialized)
            {
                // GTK initialisieren (0 Argumente)
                GtkNative.gtk_init(0, IntPtr.Zero);
                IsInitialized = true;
            }
        }

        private static int GTK_FILE_CHOOSER_ACTION_OPEN = 0;
        private static int GTK_FILE_CHOOSER_ACTION_SAVE = 1; // Buttons: Verwende "_Speichern" statt "_Öffnen".
        private static int GTK_FILE_CHOOSER_ACTION_SELECT_FOLDER = 2;   //Buttons: Verwende "_Auswählen".
        private static string GTK_RESPONSE_CANCEL = "_Cancel";
        private static string GTK_RESPONSE_OPEN = "_Open";
        private static string GTK_RESPONSE_SAVE = "_Save";
        private static string GTK_RESPONSE_SELECT = "_Select";        
        
        public string OpenFileDialog(
            IGUIContext ctx, 
            string caption = "Open File",
            string filter = "", 
            int filterIndex = 1, 
            string initialDirectory = "",
            bool restoreDirectory = false)
        {
            EnsureGtkInit();

            // Wir erstellen den Dialog ohne explizites Parent-Handle, 
            // da GLFW-Handles (X11) nicht ohne weiteres als GtkWindow-Pointer akzeptiert werden.
            IntPtr dialog = GtkNative.gtk_file_chooser_dialog_new(
                caption, 
                IntPtr.Zero, 
                GTK_FILE_CHOOSER_ACTION_OPEN,
                GTK_RESPONSE_CANCEL, -6, // GTK_RESPONSE_CANCEL
                GTK_RESPONSE_OPEN, -5,    // GTK_RESPONSE_OK
                IntPtr.Zero
            );
            
            return ShowDialog(ctx, dialog, GTK_FILE_CHOOSER_ACTION_OPEN, filter, filterIndex, initialDirectory);
        }

        public string SaveFileDialog(
            IGUIContext ctx, 
            string caption = "Save File",
            string defaultFileName = "", // Neu
            string filter = "", 
            int filterIndex = 1, 
            string initialDirectory = "",
            bool restoreDirectory = false)
        {
            EnsureGtkInit();

            // Wir erstellen den Dialog ohne explizites Parent-Handle, 
            // da GLFW-Handles (X11) nicht ohne weiteres als GtkWindow-Pointer akzeptiert werden.
            IntPtr dialog = GtkNative.gtk_file_chooser_dialog_new(
                caption, 
                IntPtr.Zero, 
                GTK_FILE_CHOOSER_ACTION_SAVE,
                GTK_RESPONSE_CANCEL, -6,
                GTK_RESPONSE_SAVE, -5,
                IntPtr.Zero
            );
            
            return ShowDialog(ctx, dialog, GTK_FILE_CHOOSER_ACTION_SAVE, filter, filterIndex, initialDirectory, defaultFileName);
        }

        public string SelectFolderDialog(
            IGUIContext ctx, 
            string caption = "Select Folder",            
            string initialDirectory = "",
            bool restoreDirectory = false)
        {
            EnsureGtkInit();

            // Wir erstellen den Dialog ohne explizites Parent-Handle, 
            // da GLFW-Handles (X11) nicht ohne weiteres als GtkWindow-Pointer akzeptiert werden.
            IntPtr dialog = GtkNative.gtk_file_chooser_dialog_new(
                caption, 
                IntPtr.Zero, 
                GTK_FILE_CHOOSER_ACTION_SELECT_FOLDER,
                GTK_RESPONSE_CANCEL, -6,
                GTK_RESPONSE_SELECT, -5,
                IntPtr.Zero
            );
            
            return ShowDialog(ctx, dialog, GTK_FILE_CHOOSER_ACTION_SELECT_FOLDER, null, 0, initialDirectory);
        }

        private unsafe string ShowDialog(
            IGUIContext ctx,             
            IntPtr dialog, 
            int action,
            string filter = "", 
            int filterIndex = 1, 
            string initialDirectory = "",
            string defaultFileName = "")
        {
            // --- Verzeichnis setzen ---
            if (!string.IsNullOrEmpty(initialDirectory))
            {
                // Linux nutzt /, Windows nutzt \. GTK kommt meist mit / klar.
                string linuxPath = initialDirectory.Replace('\\', '/');
                GtkNative.gtk_file_chooser_set_current_folder(dialog, linuxPath);
            }

            // --- Filter parsen (Windows Syntax: "Name|*.ext|Name2|*.ext2") ---
            if (!string.IsNullOrEmpty(filter))
            {
                string[] parts = filter.Split('|');
                IntPtr selectedFilterPtr = IntPtr.Zero;
                int currentFilterIdx = 1;

                for (int i = 0; i < parts.Length - 1; i += 2)
                {
                    string name = parts[i];
                    string patterns = parts[i + 1];

                    IntPtr gtkFilter = GtkNative.gtk_file_filter_new();
                    GtkNative.gtk_file_filter_set_name(gtkFilter, name);

                    // Unterstützung für mehrere Extensions pro Filter (mit ; getrennt)
                    string[] extensions = patterns.Split(';');
                    foreach (var ext in extensions)
                    {
                        GtkNative.gtk_file_filter_add_pattern(gtkFilter, ext.Trim());
                    }

                    GtkNative.gtk_file_chooser_add_filter(dialog, gtkFilter);

                    // Den vom User gewünschten FilterIndex (1-basiert) merken
                    if (currentFilterIdx == filterIndex)
                    {
                        selectedFilterPtr = gtkFilter;
                    }
                    currentFilterIdx++;
                }

                // Initialen Filter setzen
                if (selectedFilterPtr != IntPtr.Zero)
                    GtkNative.gtk_file_chooser_set_filter(dialog, selectedFilterPtr);
            }

            // --- Spezifische Einstellungen für Save-Dialoge ---
            // Wir prüfen, welche Action der Dialog hat
            //int action = GtkNative.gtk_file_chooser_get_action(dialog); 
            
            // Falls du den GetAction-Import nicht hast, kannst du auch einen bool isSave übergeben
            if (action == (int)GTK_FILE_CHOOSER_ACTION_SAVE)
            {
                // 1. Überschreiben-Warnung aktivieren
                GtkNative.gtk_file_chooser_set_do_overwrite_confirmation(dialog, 1);
                
                // 2. Erstellen von Ordnern erlauben
                GtkNative.gtk_file_chooser_set_create_folders(dialog, 1);
                
                // 3. Dateinamen vorgeben (z.B. "Unbenannt.txt")
                if (!string.IsNullOrEmpty(defaultFileName))
                {
                    GtkNative.gtk_file_chooser_set_current_name(dialog, defaultFileName);
                }
            }

            // 2. WICHTIG: Den Dialog realisieren, damit er eine XID bekommt
            GtkNative.gtk_widget_realize(dialog);
            GtkNative.gtk_window_present(dialog);   // Neu, zur Robustheit

            // 3. X11 XID des GTK-Dialogs holen
            IntPtr gdkWin = GtkNative.gtk_widget_get_window(dialog);
            IntPtr dialogXid = GtkNative.gdk_x11_window_get_xid(gdkWin);

            // 4. Parent-Fenster XID holen (aus deinem NativeWindow)
            var parentWindow = ctx.GlWindow;
            IntPtr parentXid = (IntPtr)GLFW.GetX11Window((Window*)parentWindow.WindowPtr);
            IntPtr display = GLFW.GetX11Display();

            if (dialogXid != IntPtr.Zero && parentXid != IntPtr.Zero)
            {
                // Nutze deine bestehende X11-Logik
                X11Interface.XSetTransientForHint(display, dialogXid, parentXid);
                
                // Modal-State setzen (hier nutzen wir deine Logik für den Dialog)
                ApplyModalStateDirect(display, dialogXid, parentXid, true);
                
                // In der Taskleiste verstecken
                ApplyHideFromTaskbarDirect(display, dialogXid);
            }

            string resultPath = null;
            int result = GtkNative.gtk_dialog_run(dialog);
            if (result == (int)GtkResponseType.OK)
            {
                IntPtr filenamePtr = GtkNative.gtk_file_chooser_get_filename(dialog);
                if (filenamePtr != IntPtr.Zero)
                {
                    resultPath = Marshal.PtrToStringAnsi(filenamePtr);
                    GtkNative.g_free(filenamePtr);
                }
            }

            GtkNative.gtk_widget_destroy(dialog);
            
            // Main-Loop kurz abarbeiten für sauberes Schließen
            while (GtkNative.gtk_events_pending())
                GtkNative.gtk_main_iteration_do(false);

            return resultPath;
        }

        public static void ApplyModalStateDirect(IntPtr display, IntPtr childXid, IntPtr parentXid, bool isModal)
        {
            if (display == IntPtr.Zero || childXid == IntPtr.Zero) return;

            // 1. Transient Hint setzen (Basis für die Fenster-Hierarchie)
            if (parentXid != IntPtr.Zero)
            {
                X11Interface.XSetTransientForHint(display, childXid, parentXid);
            }

            // 2. Atoms für den Modal-Zustand holen
            IntPtr wmStateAtom = X11Interface.XInternAtom(display, "_NET_WM_STATE", false);
            IntPtr modalAtom = X11Interface.XInternAtom(display, "_NET_WM_STATE_MODAL", false);
            IntPtr rootWindow = X11Interface.XRootWindow(display, 0);

            // 3. ClientMessage Event vorbereiten (EWMH Standard)
            X11Interface.XEvent ev = new X11Interface.XEvent();
            ev.type = 33; // ClientMessage
            ev.xclient.window = childXid;
            ev.xclient.message_type = wmStateAtom;
            ev.xclient.format = 32;
            ev.xclient.data0 = (IntPtr)(isModal ? 1 : 0); // 1 = Add, 0 = Remove
            ev.xclient.data1 = modalAtom;
            ev.xclient.data2 = IntPtr.Zero;
            ev.xclient.data3 = (IntPtr)2; // Source: Application

            // 4. Event an das Root-Window senden, damit der Window-Manager reagiert
            IntPtr eventMask = (IntPtr)(1L << 19 | 1L << 20); // SubstructureNotify | SubstructureRedirect
            X11Interface.XSendEvent(display, rootWindow, false, eventMask, ref ev);

            X11Interface.XFlush(display);
        }

        public static void ApplyHideFromTaskbarDirect(IntPtr display, IntPtr xid)
        {
            IntPtr wmState = X11Interface.XInternAtom(display, "_NET_WM_STATE", false);
            IntPtr skipTaskbar = X11Interface.XInternAtom(display, "_NET_WM_STATE_SKIP_TASKBAR", false);
            
            // Wir nutzen denselben Mechanismus wie oben, nur mit einem anderen Atom
            IntPtr rootWindow = X11Interface.XRootWindow(display, 0);
            X11Interface.XEvent ev = new X11Interface.XEvent();
            ev.type = 33;
            ev.xclient.window = xid;
            ev.xclient.message_type = wmState;
            ev.xclient.format = 32;
            ev.xclient.data0 = (IntPtr)1; // Add
            ev.xclient.data1 = skipTaskbar;
            ev.xclient.data3 = (IntPtr)2;

            X11Interface.XSendEvent(display, rootWindow, false, (IntPtr)(1L << 19 | 1L << 20), ref ev);
            X11Interface.XFlush(display);
        }

        private static void SendWindowManagerEvent(IntPtr display, IntPtr xid, IntPtr messageType, IntPtr action, IntPtr atom1)
        {
            IntPtr root = X11Interface.XRootWindow(display, 0);
            X11Interface.XEvent ev = new X11Interface.XEvent();
            ev.type = 33; // ClientMessage
            ev.xclient.window = xid;
            ev.xclient.message_type = messageType;
            ev.xclient.format = 32;
            ev.xclient.data0 = action; 
            ev.xclient.data1 = atom1;
            ev.xclient.data3 = (IntPtr)2; // Source: App

            IntPtr mask = (IntPtr)(1L << 19 | 1L << 20); // SubstructureNotify | SubstructureRedirect
            X11Interface.XSendEvent(display, root, false, mask, ref ev);
            X11Interface.XFlush(display);
        }
    }
}