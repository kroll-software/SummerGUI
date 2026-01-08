using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using KS.Foundation;
using OpenTK;
using OpenTK.Platform;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Mathematics;


namespace SummerGUI.SystemSpecific.Linux
{
	public class SystemDialogs
	{
        const string GtkLib = "libgtk-3.so.0";    // "gtk-3"

        [DllImport (GtkLib, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr gtk_file_chooser_dialog_new(IntPtr title, IntPtr parent, int action, 
			IntPtr btn1,
			int response1,
			IntPtr btn2,
			int response2,
			IntPtr extra
		);

		[DllImport (GtkLib, CallingConvention = CallingConvention.Cdecl)]
		static extern void gtk_init(int argc, IntPtr argv);

		[DllImport (GtkLib, CallingConvention = CallingConvention.Cdecl)]
		static extern int gtk_events_pending();

		[DllImport (GtkLib, CallingConvention = CallingConvention.Cdecl)]
		static extern void gtk_main_iteration();

		[DllImport (GtkLib, CallingConvention = CallingConvention.Cdecl)]
		static extern void gtk_main();

		[DllImport (GtkLib, CallingConvention = CallingConvention.Cdecl)]
		static extern void gtk_widget_destroy(IntPtr dialog);

		[DllImport (GtkLib, CallingConvention = CallingConvention.Cdecl)]
		static extern int gtk_dialog_run (IntPtr dialog);

		[DllImport (GtkLib, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr gtk_file_chooser_get_filename (IntPtr dialog);

        [DllImport(GtkLib, CallingConvention = CallingConvention.Cdecl)]
        static extern void gtk_window_set_modal (IntPtr dialog, int modal);

        [DllImport(GtkLib, CallingConvention = CallingConvention.Cdecl)]
        static extern void gtk_window_set_transient_for (IntPtr window, IntPtr parent);

        [DllImport(GtkLib, CallingConvention = CallingConvention.Cdecl)]
        static extern IntPtr gtk_window_new(int type);

        [DllImport(GtkLib, CallingConvention = CallingConvention.Cdecl)]
        static extern void gtk_window_present(IntPtr window);

        [DllImport(GtkLib, CallingConvention = CallingConvention.Cdecl)]
        static extern void gtk_window_present_with_time(IntPtr window, uint timestamp);

        [DllImport(GtkLib, CallingConvention = CallingConvention.Cdecl)]
        static extern void gtk_window_set_decorated(IntPtr window, int decorated);

        [DllImport(GtkLib, CallingConvention = CallingConvention.Cdecl)]
        static extern IntPtr gtk_widget_get_window(IntPtr widget);

        [DllImport(GtkLib, CallingConvention = CallingConvention.Cdecl)]
        static extern IntPtr gdk_x11_window_get_xid(IntPtr window);


        [DllImport("libX11")]
		extern public static void XReparentWindow (IntPtr x11display, IntPtr x11window, IntPtr x11parentWindow, int x, int y);


        public enum GtkFileChooserAction {
			GTK_FILE_CHOOSER_ACTION_OPEN,
			GTK_FILE_CHOOSER_ACTION_SAVE,
			GTK_FILE_CHOOSER_ACTION_SELECT_FOLDER,
			GTK_FILE_CHOOSER_ACTION_CREATE_FOLDER
		};

		public enum GtkResponseType {
			GTK_RESPONSE_NONE = -1,
			GTK_RESPONSE_REJECT = -2,
			GTK_RESPONSE_ACCEPT = -3,
			GTK_RESPONSE_DELETE_EVENT = -4,
			GTK_RESPONSE_OK = -5,
			GTK_RESPONSE_CANCEL = -6,
			GTK_RESPONSE_CLOSE = -7,
			GTK_RESPONSE_YES = -8,
			GTK_RESPONSE_NO = -9,
			GTK_RESPONSE_APPLY = -10,
			GTK_RESPONSE_HELP = -11
		}

        public enum GtkWindowPosition
        {
            GTK_WIN_POS_NONE,
            GTK_WIN_POS_CENTER,
            GTK_WIN_POS_MOUSE,
            GTK_WIN_POS_CENTER_ALWAYS,
            GTK_WIN_POS_CENTER_ON_PARENT
        }

        public enum GtkWindowType
        {
            GTK_WINDOW_TOPLEVEL,
            GTK_WINDOW_POPUP
        }

		public SystemDialogs ()
		{			
		}

		public string OpenFileDialog(string caption, NativeWindow parentWindow)
		{
            return "";

            IntPtr parent = IntPtr.Zero;
            //IntPtr parent = parentWindow.WindowInfo.Handle;

            //IntPtr parent = (IntPtr)KS.Foundation.ReflectionUtils.GetPropertyValue(parentWindow.WindowInfo, "Parent");
            //IntPtr display = (IntPtr)KS.Foundation.ReflectionUtils.GetPropertyValue(parentWindow.WindowInfo, "Display");
            IntPtr display = IntPtr.Zero;

            GtkFileChooserAction action = GtkFileChooserAction.GTK_FILE_CHOOSER_ACTION_OPEN;

			if (String.IsNullOrEmpty(caption))
				caption = "Open File";
			IntPtr title = Marshal.StringToHGlobalAnsi(caption);

			IntPtr dialog = IntPtr.Zero;
			IntPtr result = IntPtr.Zero;

			IntPtr button1 = Marshal.StringToHGlobalAnsi("_Cancel");
			int response1 = (int)GtkResponseType.GTK_RESPONSE_CANCEL;

			IntPtr button2 = Marshal.StringToHGlobalAnsi("_Open");
			//IntPtr button2 = Marshal.StringToHGlobalAnsi("_Save");
			int response2 = (int)GtkResponseType.GTK_RESPONSE_ACCEPT;

            Task.Run(() =>
            {

            try {
				gtk_init(0, IntPtr.Zero);

                IntPtr dummyGtkWindow = gtk_window_new((int)GtkWindowType.GTK_WINDOW_POPUP);
                parent = dummyGtkWindow;

                //ReflectionUtils.SetPropertyValue(parentWindow.WindowInfo, "Parent", parent);

                dialog = gtk_file_chooser_dialog_new (title, parent, (int)action,
					button1,
					response1,
					button2,
					response2,
					IntPtr.Zero);

                //gtk_window_set_transient_for(dialog, parentWindow.WindowInfo.Handle);

                //XReparentWindow(display, dialog, parentWindow.WindowInfo.Handle, 0, 0);
                //gtk_window_set_modal(dialog, 1);




                    int ret = gtk_dialog_run(dialog);
                    if (ret == (int)GtkResponseType.GTK_RESPONSE_ACCEPT)
                    {
                        result = gtk_file_chooser_get_filename(dialog);
                    }

                    if (dialog != IntPtr.Zero)
                    {
                        gtk_widget_destroy(dialog);
                        dialog = IntPtr.Zero;
                    }

                    if (dummyGtkWindow != IntPtr.Zero)
                    {
                        gtk_widget_destroy(dummyGtkWindow);
                    }

                    while (gtk_events_pending() != 0)
                        gtk_main_iteration();                

            } catch (Exception ex) {
				ex.LogError ();
				//return String.Empty;
			} finally {
				Marshal.FreeHGlobal (title);
				Marshal.FreeHGlobal (button1);
				Marshal.FreeHGlobal (button2);	
			}

                //if (result == IntPtr.Zero)
                //return String.Empty;

                string filename = result.ToAnsiString ();
			    Marshal.FreeHGlobal (result);

            });


            Thread.Sleep(1000);

            IntPtr window = gtk_widget_get_window(dialog);
            IntPtr windowID = gdk_x11_window_get_xid(window);

            X11Interface.XRaiseWindow((IntPtr)display, windowID);
            X11Interface.XSetInputFocus((IntPtr)display, windowID, 0, 0);


            //return filename;
            return String.Empty;
        }
    }
}

