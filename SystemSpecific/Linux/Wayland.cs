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
        string defaultFileName = "")
    {
        return ShowDialogPortalAsync(
            ctx,
            action,
            filter,
            filterIndex,
            initialDirectory,
            defaultFileName)
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
        string defaultFileName)
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
                defaultFileName);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SummerGUI] FileDialog-Portal-Fehler: {ex.Message}");
            return null; // Cancel/Abbruch — App läuft weiter
        }
    }

    private static async Task<string> ShowDialogPortalInnerAsync(
        IGUIContext ctx,
        int action,
        string filter,
        int filterIndex,
        string initialDirectory,
        string defaultFileName)
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

        // Leeres Token (Compositor ohne zxdg_exporter_v2, z.B. GNOME/Mutter, Weston)
        // → leere parent_window: gültig, Dialog ohne Eltern-Bezug. Kein "wayland:"-Prefix
        // auf leeren String — das wäre ein kaputt gebildetes Token.
        string formattedToken = string.IsNullOrEmpty(parentWindowToken)
            ? ""
            : $"wayland:{parentWindowToken}";

        ObjectPath requestPath =
            await chooser.OpenFileAsync(
                formattedToken,
                "Datei öffnen",
                options);

        var request =
            connection.CreateProxy<IRequest>(
                "org.freedesktop.portal.Desktop",
                requestPath);

        var tcs = new TaskCompletionSource<string>();

        await request.WatchResponseAsync(
        responseData =>
        {
            var (response, results) = responseData;

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
}