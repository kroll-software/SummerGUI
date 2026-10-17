using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Diagnostics;
using OpenTK;
using OpenTK.Platform;
using OpenTK.Core.Native;
using OpenTK.Core.Platform;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Tmds.DBus;
using WaylandDotnet;
using WaylandDotnet.Stable;
using WaylandDotnet.Unstable;
using WaylandDotnet.Staging;
using KS.Foundation;

namespace SummerGUI.SystemSpecific.Linux;

[DBusInterface("org.freedesktop.portal.FileChooser")]
public interface IFileChooser : IDBusObject
{
    Task<ObjectPath> OpenFileAsync(
        string parentWindow,
        string title,
        IDictionary<string, object> options);

    // Tmds.DBus streift "Async" → D-Bus-Methode "SaveFile" (Signatur identisch zu OpenFile)
    Task<ObjectPath> SaveFileAsync(
        string parentWindow,
        string title,
        IDictionary<string, object> options);
}

[DBusInterface("org.freedesktop.portal.Request")]
public interface IRequest : IDBusObject
{
    Task<IDisposable> WatchResponseAsync(
        Action<(uint response, IDictionary<string, object> results)> handler);
}


public static class Wayland
{
    [DllImport("libglfw.so.3", EntryPoint = "glfwGetWaylandToplevel", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetWaylandToplevel(IntPtr windowHandle);    
    
    public static void MakeWindowModal(NativeWindow child, NativeWindow parent)
    {        
        if (GLFW.GetPlatform() != Platform.Wayland)
            throw new InvalidOperationException("This method is only supported on Wayland.");

        unsafe
        {
            IntPtr wlDisplayPtr = GLFW.GetWaylandDisplay();

            IntPtr wlChildSurfacePtr =
                GLFW.GetWaylandWindow((Window*)child.WindowPtr);

            IntPtr wlParentSurfacePtr =
                GLFW.GetWaylandWindow((Window*)parent.WindowPtr);

            var display = new WlDisplay(wlDisplayPtr);

            var childSurface = new WlSurface(
                wlChildSurfacePtr,
                display);

            var parentSurface = new WlSurface(
                wlParentSurfacePtr,
                display);


            // ----------------------------------------------------
            // Registry
            // ----------------------------------------------------

            var registry = display.GetRegistry();

            ZxdgExporterV2 exporter = null;
            ZxdgImporterV2 importer = null;
            XdgWmDialogV1 dialog = null;
            XdgWmBase baseInterface = null;

            registry.OnGlobal += (name, interfaceName, version) =>
            {
                //Console.WriteLine($"Registry: {name}: {interfaceName} v{version}");

                if (interfaceName == ZxdgExporterV2.InterfaceName)
                {
                    exporter = registry.Bind<ZxdgExporterV2>(
                        name,
                        Math.Min(version, (uint)ZxdgExporterV2.InterfaceVersion));
                }

                if (interfaceName == ZxdgImporterV2.InterfaceName)
                {
                    importer = registry.Bind<ZxdgImporterV2>(
                        name,
                        Math.Min(version, (uint)ZxdgImporterV2.InterfaceVersion));
                }
                
                if (interfaceName == XdgWmDialogV1.InterfaceName)                
                {
                    dialog = registry.Bind<XdgWmDialogV1>(
                        name,
                        Math.Min(version, (uint)XdgWmDialogV1.InterfaceVersion));
                }
                
                if (interfaceName == XdgWmBase.InterfaceName)
                {
                    baseInterface = registry.Bind<XdgWmBase>(
                        name,
                        Math.Min(version, (uint)XdgWmBase.InterfaceVersion));
                }
            };

            display.Roundtrip();

            if (exporter == null)
                throw new InvalidOperationException(
                    "Wayland compositor does not support zxdg_exporter_v2.");

            if (importer == null)
                throw new InvalidOperationException(
                    "Wayland compositor does not support zxdg_importer_v2.");

            // ----------------------------------------------------
            // Export parent
            // ----------------------------------------------------

            string handle = null;

            var exported = exporter.ExportToplevel(parentSurface);

            var tcs = new TaskCompletionSource<string>();

            exported.OnHandle += h =>
            {
                handle = h;
                tcs.TrySetResult(h);
            };

            display.Roundtrip();

            handle = tcs.Task.GetAwaiter().GetResult();

            // ----------------------------------------------------
            // Import again
            // ----------------------------------------------------

            var imported = importer.ImportToplevel(handle);

            // ----------------------------------------------------
            // Parent -> Child
            // ----------------------------------------------------

            imported.SetParentOf(childSurface);

            display.Roundtrip();

            // ----------------------------------------------------                        
            // Console.WriteLine($"GLFW Version: {GLFW.GetVersionString()}");

            IntPtr nativeChildToplevelPtr = IntPtr.Zero;
                        
            try
            {
                nativeChildToplevelPtr = GetWaylandToplevel((IntPtr)child.WindowPtr);
            }
            catch (Exception ex)
            {
                ex.LogError();                
            }

            if (nativeChildToplevelPtr != IntPtr.Zero)
            {
                XdgToplevel childToplevel = XdgToplevel.Create(nativeChildToplevelPtr, display);            

                var Dlg = dialog.GetXdgDialog(childToplevel);
                Dlg.SetModal();

                display.Roundtrip();
            }                     
        }
    }

    public static string ShowDialogPortal(
        IGUIContext ctx,
        int action,
        string filter = "",
        int filterIndex = 1,
        string initialDirectory = "",
        string defaultFileName = "",
        string caption = "")
    {
        return ShowDialogPortalAsync(
            ctx,
            action,
            filter,
            filterIndex,
            initialDirectory,
            defaultFileName,
            caption)
            .GetAwaiter()
            .GetResult();
    }

    private static async Task<string> GetParentWindowToken(IGUIContext ctx)
    {
        TaskCompletionSource<string> tcs = new TaskCompletionSource<string>();        

        unsafe
        {
            IntPtr wlDisplayPtr = GLFW.GetWaylandDisplay();
            IntPtr wlSurfacePtr = GLFW.GetWaylandWindow((Window*)ctx.GlWindow.WindowPtr);            

            // Vorhandene Wayland-Objekte kapseln            
            var display = new WlDisplay(wlDisplayPtr);            
    
            var registry = display.GetRegistry();

            uint exporterName = 0;
            uint exporterVersion = 1;

            registry.OnGlobal += (name, iface, version) =>
            {
                //Console.WriteLine($"{name}: {iface} v{version}");

                if (iface == ZxdgExporterV2.InterfaceName)
                {
                    exporterName = name;
                    exporterVersion = Math.Min(version, (uint)WaylandDotnet.Unstable.ZxdgExporterV2.InterfaceVersion);

                    //Console.WriteLine($"Found exporter: name={name} version={version}");
                }
            };

            display.Roundtrip();

            // GNOME/Mutter und viele Compositoren (u.a. Weston) werfen KEIN zxdg_exporter_v2
            // aus (GNOME nutzt das interne Portal ohne xdg-foreign). Das ist gültig:
            // Das XDG-Desktop-Portal erlaubt ein leeres parent_window. Dann ist der
            // Dialog nicht an ein Elternfenster gekoppelt. Kein Throw — ein Throw hier
            // war der uncatchte Crash bei FileOpen/FileNew. (2026-10-05)
            if (exporterName == 0)
                return "";

            var exporter =
                registry.Bind<ZxdgExporterV2>(
                    exporterName,
                    exporterVersion);

            //Console.WriteLine("Exporter successfully bound.");

            var surface = new WlSurface(wlSurfacePtr, display);
            var exported = exporter.ExportToplevel(surface);           

            exported.OnHandle += handle =>
            {
                //Console.WriteLine($"\tHandle: {handle}");
                tcs.TrySetResult(handle);
            };

            display.Roundtrip();            
        }

        // Sicherheits-Timeout: Manche Compositoren wirbeln zxdg_exporter_v2 aus,
        // liefern aber nie ein handle. Ohne Timeout würde `await tcs.Task` ewig
        // blockieren → App freeze. Fallback ist dann ein leerer Token (kein Parent),
        // mit dem das Portal trotzdem korrekt funktioniert. (2026-10-05)
        var finished = await Task.WhenAny(tcs.Task, Task.Delay(2000));
        if (finished == tcs.Task)
            return await tcs.Task;

        return "";
    }
    

    private static async Task<string> ShowDialogPortalAsync(
        IGUIContext ctx,
        int action,
        string filter,
        int filterIndex,
        string initialDirectory,
        string defaultFileName,
        string caption = "")
    {
        // Defensive: JEDER Fehler im Portal-Pfad (fehlendes Backend, D-Bus-Abbruch,
        // Timeout) darf die Anwendung NICHT abstürzen lassen. Der Callers erwartet
        // null bei Abbruch/Cancel. Früher flog jede Exception uncaught bis in den
        // GUI-Thread → "crasht sofort" ohne Fehlermeldung. (2026-10-05)
        try
        {
            return await ShowDialogPortalInnerAsync(
                ctx,
                action,
                filter,
                filterIndex,
                initialDirectory,
                defaultFileName,
                caption);
        }
        catch (Exception ex)
        {
            // Volldump (Typ, Nachricht, innere Ausnahmen, Stack) — zur Diagnose
            Console.WriteLine($"[SummerGUI] FileDialog-Portal-Fehler: {ex}");
            return null; // Cancel/Abbruch — App läuft weiter
        }
    }

    private static async Task<string> ShowDialogPortalInnerAsync(
        IGUIContext ctx,
        int action,
        string filter,
        int filterIndex,
        string initialDirectory,
        string defaultFileName,
        string caption = "")
    {
        string parentWindowToken = await GetParentWindowToken(ctx);
        //Console.WriteLine($"\tparentWindowToken: {parentWindowToken}");
        
        var connection =
            new Connection(Address.Session);

        await connection.ConnectAsync();

        var chooser =
            connection.CreateProxy<IFileChooser>(
                "org.freedesktop.portal.Desktop",
                "/org/freedesktop/portal/desktop");

        var options = new Dictionary<string, object>
        {
            // WICHTIG: Dem Portal explizit sagen, dass dieser Dialog modal sein soll!
            { "modal", true }
        };

        if (!string.IsNullOrEmpty(initialDirectory))
        {
            // Portal erwartet einen Byte-Array-String mit Null-Terminierung für Pfade
            byte[] pathBytes = System.Text.Encoding.UTF8.GetBytes(initialDirectory + "\0");
            options["current_folder"] = pathBytes;
        }

        if (action == SystemDialogs.GTK_FILE_CHOOSER_ACTION_SAVE &&
            !string.IsNullOrEmpty(defaultFileName))
        {
            // Portal-Spec: "current-name" ist Typ s (String) — NICHT Byte-Array.
            // (ay wird mit InvalidArgument "Expected type 's' ... got 'ay'" abgelehnt. 2026-10-17)
            string name = defaultFileName;

            // Anzeige: Ruft man das Save-Dialog mit Namen OHNE Extension an
            // (Name + Filter, Extension wird vom Caller nach dem Dialog
            // nachgereicht), zeigt das Portal den Namen ohne Extension —
            // verwirrend. Extension wird dann aus dem gewählten Filter
            // vorbefüllt, falls keine gesetzt ist. (2026-10-17)
            if (string.IsNullOrEmpty(System.IO.Path.GetExtension(name)))
            {
                string ext = FirstFilterExtension(filter, filterIndex);
                if (ext != null)
                    name += ext;
            }

            options["current_name"] = name;
        }

        // Filter: Das Portal kennt keine Filter-Dropdowns, aber die Option "mime_types" (as)
        // für OpenFile/SaveFile. Beste Anstrengung: bekannte Extension → MIME-Typ,
        // unbekannte (z.B. *.trn) werden übersprungen → dann kein Filter, alle Dateien sichtbar. (2026-10-17)
        if (action != SystemDialogs.GTK_FILE_CHOOSER_ACTION_SELECT_FOLDER)
        {
            List<string> mimes = FilterToMimeTypes(filter, filterIndex);
            if (mimes.Count > 0)
                options["mime_types"] = mimes.ToArray();
        }

        // Leeres Token (Compositor ohne zxdg_exporter_v2, z.B. GNOME/Mutter, Weston)
        // → leere parent_window: gültig, Dialog ohne Eltern-Bezug. Kein "wayland:"-Prefix
        // auf leeren String — das wäre ein kaputt gebildetes Token.
        string formattedToken = string.IsNullOrEmpty(parentWindowToken)
            ? ""
            : $"wayland:{parentWindowToken}";

        // Fallback-Caption pro Aktion (nur wenn der Caller keine Caption übergibt)
        string title = caption;
        if (string.IsNullOrEmpty(title))
        {
            if (action == SystemDialogs.GTK_FILE_CHOOSER_ACTION_SAVE)
                title = "Save File";
            else if (action == SystemDialogs.GTK_FILE_CHOOSER_ACTION_SELECT_FOLDER)
                title = "Select Folder";
            else
                title = "Open File";
        }

        ObjectPath requestPath;
        switch (action)
        {
            case SystemDialogs.GTK_FILE_CHOOSER_ACTION_SAVE:
                requestPath = await chooser.SaveFileAsync(formattedToken, title, options);
                break;

            case SystemDialogs.GTK_FILE_CHOOSER_ACTION_SELECT_FOLDER:
                // WICHTIG: Das Portal kennt KEINE "PickFolder"-Methode
                // (Portal 1.21.1 beantwortet mit UnknownMethod!).
                // Folder-Selektion = OpenFile mit der Option "directory"
                // (FileChooser-Interface v3 = xdg-desktop-portal >= 1.7.1).
                // Die Antwort kommt identisch: results.uris mit genau einer Ordner-URI.
                options["directory"] = true;
                requestPath = await chooser.OpenFileAsync(formattedToken, title, options);
                break;

            default:
                requestPath = await chooser.OpenFileAsync(formattedToken, title, options);
                break;
        }

        var request =
            connection.CreateProxy<IRequest>(
                "org.freedesktop.portal.Desktop",
                requestPath);

        var tcs = new TaskCompletionSource<string>();

        await request.WatchResponseAsync(
        responseData =>
        {
            var (response, results) = responseData;

            // DIAGNOSTIK (2026-10-17, Save/Open-Unterscheidung): exaktes Portal-Ergebnis
            // machen. Erwartete Werte: response 0=Success 1=Reject 2=Cancel; results: uris (as)
            var diag = new System.Text.StringBuilder($"[SummerGUI] Portal-Response {response}");
            if (results != null)
                foreach (var kv in results)
                    diag.Append(' ').Append(kv.Key).Append('=').Append(kv.Value?.GetType().Name);
            else
                diag.Append(" results=<null>");
            Console.WriteLine(diag);

            try
            {
                if (response != 0)
                {
                    tcs.TrySetResult(null);
                    return;
                }

                if (!results.TryGetValue("uris", out object uriObj))
                {
                    tcs.TrySetResult(null);
                    return;
                }

                if (uriObj is string[] uris &&
                    uris.Length > 0)
                {
                    string path =
                        new Uri(uris[0]).LocalPath;

                    tcs.TrySetResult(path);
                    return;
                }

                tcs.TrySetResult(null);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        });

        return await tcs.Task;
    }

    /***
    Filter-Translation für das XDG-Desktop-Portal:
    SummerGUI-Filter-String ("Name|*.ext|Name2|*.ext2") → Portal-Option "mime_types" (as).
    Nur Extensions mit eindeutigem, in der GIO-Datenbank vorhandenem MIME-Typ werden
    übersetzt; alles andere bleibt unfilteriert (Dialog zeigt alle Dateien).
    Das Portal bietet kein Filter-Dropdown — das ist eine Portal-Begrenzung, die für
    jede Anwendung unter diesem Backend gleich aussieht. (2026-10-17)
    ***/
    private static readonly Dictionary<string, string> KnownMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        { "txt",   "text/plain" },
        { "log",   "text/plain" },
        { "csv",   "text/csv" },
        { "xml",   "text/xml" },
        { "json",  "application/json" },
        { "md",    "text/markdown" },
        { "pdf",   "application/pdf" },
        { "png",   "image/png" },
        { "jpg",   "image/jpeg" },
        { "jpeg",  "image/jpeg" },
        { "gif",   "image/gif" },
        { "bmp",   "image/bmp" },
        { "svg",   "image/svg+xml" },
        { "mp3",   "audio/mpeg" },
        { "wav",   "audio/wav" },
        { "cs",    "text/x-csharp" },
        { "zip",   "application/zip" },
    };

    private static List<string> FilterToMimeTypes(string filter, int filterIndex)
    {
        var result = new List<string>();
        if (string.IsNullOrEmpty(filter))
            return result;

        string[] parts = filter.Split('|');
        if (parts.Length < 2)
            return result;

        // filterIndex ist 1-basiert → Pattern-Teil liegt auf Index (idx*2)-1
        int patternIdx = (Math.Max(1, filterIndex) * 2) - 1;
        if (patternIdx >= parts.Length)
            patternIdx = parts.Length - 1;

        foreach (string entry in parts[patternIdx].Split(';'))
        {
            string ext = entry.Trim().TrimStart('*').TrimStart('.');
            if (ext.Length == 0)
                continue;

            if (KnownMimeTypes.TryGetValue(ext, out string mime) && !result.Contains(mime))
                result.Add(mime);
        }

        return result;
    }

    /**
     Die erste Extension DES GEWÄHLTEN Filters (1-basierter Index), inkl. Punkt
     (z.B. ".trn"). Rückt null, falls der Filter unlesbar ist.
     Filter-Format: "Name (*.trn)|*.trn" oder mehrere: "A|*.a;*.b|B|*.c". (2026-10-17)
     ***/
    private static string FirstFilterExtension(string filter, int filterIndex)
    {
        if (string.IsNullOrEmpty(filter))
            return null;

        string[] parts = filter.Split('|');
        if (parts.Length < 2)
            return null;

        // filterIndex ist 1-basiert → Pattern-Teil liegt auf Index (idx*2)-1
        int patternIdx = (Math.Max(1, filterIndex) * 2) - 1;
        if (patternIdx >= parts.Length)
            patternIdx = parts.Length - 1;

        foreach (string entry in parts[patternIdx].Split(';'))
        {
            string ext = entry.Trim().TrimStart('*');
            if (ext.Length == 0)
                continue;
            if (!ext.StartsWith('.'))
                ext = "." + ext;
            return ext;
        }

        return null;
    }
}
