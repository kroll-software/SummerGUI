namespace SummerGUI
{
	/// <summary>
	/// Synchroner Frame-Flush für Menü-Execution-Pfade (Close-before-Execute).
	/// </summary>
	/// <remarks>
	/// Der Click-Handler eines Menüpunkts darf den GUI-Thread blockieren
	/// (z. B. nativer Datei-Dialog: Wayland-Portal via D-Bus, GTK
	/// gtk_dialog_run auf X11). Solange der Thread blockiert, läuft die
	/// Frame-Schleife des Fensters nicht weiter — ein Invalidate() bleibt
	/// unsichtbar, das zuletzt gerenderte Frame (Dropdown noch offen)
	/// bleibt auf dem Screen, bis der Dialog geschlossen wird.
	/// <para>
	/// In-App-Dialoge (z. B. About / ColorPicker, ChildFormWindow) sind
	/// nicht betroffen: ihre eigene Event-Loop rendert das Parent-Fenster
	/// mit (ChildFormWindow.OnDispatchUpdateAndRenderFrame →
	/// ParentWindow.OnDispatchUpdateAndRenderFrame). Deshalb schlossen
	/// diese Dialoge das Dropdown sichtbar, Native-Dialoge nicht.
	/// </para>
	/// <para>
	/// Solution: genau einen Frame (Update + Paint + SwapBuffers)
	/// synchron rendern, nachdem das Menü geschlossen wurde und bevor
	/// der blockierende Handler aufgerufen wird.
	/// </para>
	/// </remarks>
	internal static class MenuSyncFrame
	{
		/// <summary>
		/// Rendert genau einen Frame synchron auf dem übergebenen Fenster.
		/// </summary>
		/// <param name="window">
		/// Das zu rendernde Fenster. MUSS vor OnClose()/Dispose() per
		/// widget.ParentWindow erfasst werden: nach dem Dispose ist Root == null
		/// und ParentWindow liefert null (Widget.CleanupManagedResources).
		/// Null verträglich (No-op).
		/// </param>
		public static void Flush(SummerGUIWindow window)
		{
			if (window == null)
				return;

			// Run() liefert 30 fps → TargetRenderPeriod ≈ 1/30s: ein Dispatch
			// < 33ms nach dem letzten Frame würde den Render-Frame überspringen.
			// 0.0 = "Frame unbedingt" (MaxFrequency), danach wiederherstellen.
			double oldPeriod = window.TargetRenderPeriod;
			try {
				window.TargetRenderPeriod = 0.0;
				window.OnDispatchUpdateAndRenderFrame ();
			} finally {
				window.TargetRenderPeriod = oldPeriod;
			}
		}
	}
}
