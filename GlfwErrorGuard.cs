using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace SummerGUI
{
	/// <summary>
	/// Process-wide helper: a single global GLFW error callback is installed once,
	/// so every app benefits (instead of each Main having to maintain its own copy).
	///
	/// Wayland does not implement some GLFW features:
	///   - glfwGetWindowPos     → "The platform does not provide the window position"
	///   - glfwSetWindowPos     → "The platform does not support setting the window position"
	///   - glfwSetWindowOpacity → "The platform does not support setting the window opacity"
	/// OpenTK (GlWindow.ClientRectangle / ClientPosition / Position) uses exactly these calls;
	/// each access under Wayland emits an error, and from OpenTK's default callback
	/// that becomes a GLFWException. This framework has cleaned up the relevant spots
	/// (ClientSize instead of ClientRectangle, no Position Get/Set under Wayland,
	/// Opacity fade only under X11). This guard is the safety net:
	/// "Feature/PlatformUnavailable" is allowed and logged, while all other
	/// GLFW errors remain hard exceptions (for developer feedback).
	///
	/// Note: environment variables (XDG_*, WAYLAND_DISPLAY, DBUS_SESSION_BUS_ADDRESS)
	/// are the launcher's responsibility (shell, systemd, VSCode launch.json) —
	/// the framework must never rewrite them from the inside. A pure X11 session
	/// is left entirely alone; the app runs on the X11 stack there.
	/// </summary>
	public static class GlfwErrorGuard
	{
		private static bool _installed;

		/// <summary>
		/// Installs the global GLFW error callback exactly once.
		/// Must be called before the first NativeWindow/GLFW.Init (idempotent).
		/// </summary>
		/// <param name="settings">The unmodified NativeWindowSettings (returned for the base call).</param>
		public static NativeWindowSettings EnsureInstalled(NativeWindowSettings settings)
		{
			if (_installed)
				return settings;

			lock (SummerGUIWindow.SyncObject)
			{
				if (_installed)
					return settings;

				GLFWProvider.SetErrorCallback((errorCode, description) =>
				{
					string msg = description ?? "";
					bool benignWayland =
						errorCode == ErrorCode.FeatureUnavailable ||
						errorCode == ErrorCode.PlatformUnavailable ||
						msg.IndexOf("opacity", StringComparison.OrdinalIgnoreCase) >= 0 ||
						msg.IndexOf("window position", StringComparison.OrdinalIgnoreCase) >= 0;

					if (benignWayland)
					{
						System.Diagnostics.Debug.WriteLine($"[SummerGUI Wayland Note] {errorCode}: {msg}");
						return;
					}

					throw new GLFWException($"GLFW Error {errorCode}: {msg}");
				});

				_installed = true;
			}

			return settings;
		}
	}
}
