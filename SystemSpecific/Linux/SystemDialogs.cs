using System;
using System.Runtime.InteropServices;
using KS.Foundation;
using OpenTK;
using OpenTK.Platform;

namespace SummerGUI.SystemSpecific.Linux
{
	public class SystemDialogs
	{
		const string GtkLib = "libgtk-3.so";	// "gtk-3"

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

		public SystemDialogs ()
		{			
		}

		public string OpenFileDialog(string caption, INativeWindow parentWindow)
		{
			IntPtr parent = IntPtr.Zero;

			//IntPtr display = parentWindow.WindowInfo.Handle;

			object display = KS.Foundation.ReflectionUtils.GetPropertyValue (parentWindow.WindowInfo, "Display");
			IntPtr disp_pointer = new IntPtr (display.SafeInt());

			object RootWindow = KS.Foundation.ReflectionUtils.GetPropertyValue (parentWindow.WindowInfo, "FBConfig");
			IntPtr root_pointer = new IntPtr (RootWindow.SafeInt());

			parent = root_pointer;


			//parent = parentWindow.WindowInfo.Handle

			//XSetTransientForHint

			//var info = (OpenTK.Platform.X11.X11WindowInfo)parentWindow.WindowInfo;

			//parent = parentWindow.WindowInfo.Handle;

			//OpenTK.Platform.Utilities.

			/***
			Utilities.CreateX11WindowInfo(IntPtr.Zero, 0,
				parentWindow.WindowInfo.Handle,
				IntPtr.Zero,
				IntPtr.Zero
			);
			***/
			//Utilities.CreateX11WindowInfo(
			//IntPtr GtkWindow = (this.GlWindow.WindowInfo as OpenTK.Platform.X11.).Visual;

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

			try {
				gtk_init(0, IntPtr.Zero);
				//gtk_init(0, parent);

				dialog = gtk_file_chooser_dialog_new (title, parent, (int)action,
					button1,
					response1,
					button2,
					response2,
					IntPtr.Zero);

				//XReparentWindow(disp_pointer, dialog, parentWindow.WindowInfo.Handle, 0, 0);

				// set_transient_for()
				// gtk_window_set_modal()

				int ret = gtk_dialog_run (dialog);
				if (ret == (int)GtkResponseType.GTK_RESPONSE_ACCEPT) {
					result = gtk_file_chooser_get_filename (dialog);
				}

				if (dialog != IntPtr.Zero) {
					gtk_widget_destroy (dialog);
					while (gtk_events_pending () != 0)
						gtk_main_iteration ();
				}	
			} catch (Exception ex) {
				ex.LogError ();
				return String.Empty;
			} finally {
				Marshal.FreeHGlobal (title);
				Marshal.FreeHGlobal (button1);
				Marshal.FreeHGlobal (button2);	
			}				

			if (result == IntPtr.Zero)
				return String.Empty;

			string filename = result.ToAnsiString ();
			Marshal.FreeHGlobal (result);
			return filename;
		}
	}
}

