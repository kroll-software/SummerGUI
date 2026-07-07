namespace WaylandDotnet.Internal;

#nullable enable
#pragma warning disable CS1591
#pragma warning disable CS0108
#pragma warning disable CA2255
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

public static unsafe partial class WaylandInterfaces
{
    public static WlInterface* ZxdgExporterV2 = AllocateInterface();
    public static WlInterface* ZxdgImporterV2 = AllocateInterface();
    public static WlInterface* ZxdgExportedV2 = AllocateInterface();
    public static WlInterface* ZxdgImportedV2 = AllocateInterface();


    /// <summary>
    /// Interface: zxdg_exporter_v2
    /// Version: 1
    /// Requests: 2, Events: 0
    /// </summary>
    [ModuleInitializer]
    public static void CreateZxdgExporterV2Interface()
    {
        // Request signatures
        var requests = (WlMessage*)Marshal.AllocHGlobal(sizeof(WlMessage) * 2);
        requests[0] = new WlMessage
        {
            Name = Utf8StringMarshaller.ConvertToUnmanaged("destroy"),
            Signature = Utf8StringMarshaller.ConvertToUnmanaged(""),
            Types = (WlInterface**)IntPtr.Zero
        };
        requests[1] = new WlMessage
        {
            Name = Utf8StringMarshaller.ConvertToUnmanaged("export_toplevel"),
            Signature = Utf8StringMarshaller.ConvertToUnmanaged("no"),
            Types = (WlInterface**)CreateTypesArray([ZxdgExportedV2, WlSurface])
        };

        var iface = new WlInterface
        {
            Name = Utf8StringMarshaller.ConvertToUnmanaged("zxdg_exporter_v2"),
            Version = 1,
            MethodCount = 2,
            Methods = requests,
            EventCount = 0,
            Events = (WlMessage*)IntPtr.Zero
        };

        Marshal.StructureToPtr(iface, (IntPtr)ZxdgExporterV2, false);
        Interfaces.Add("zxdg_exporter_v2", (IntPtr)ZxdgExporterV2);
    }


    /// <summary>
    /// Interface: zxdg_importer_v2
    /// Version: 1
    /// Requests: 2, Events: 0
    /// </summary>
    [ModuleInitializer]
    public static void CreateZxdgImporterV2Interface()
    {
        // Request signatures
        var requests = (WlMessage*)Marshal.AllocHGlobal(sizeof(WlMessage) * 2);
        requests[0] = new WlMessage
        {
            Name = Utf8StringMarshaller.ConvertToUnmanaged("destroy"),
            Signature = Utf8StringMarshaller.ConvertToUnmanaged(""),
            Types = (WlInterface**)IntPtr.Zero
        };
        requests[1] = new WlMessage
        {
            Name = Utf8StringMarshaller.ConvertToUnmanaged("import_toplevel"),
            Signature = Utf8StringMarshaller.ConvertToUnmanaged("ns"),
            Types = (WlInterface**)CreateTypesArray([ZxdgImportedV2, (WlInterface*)IntPtr.Zero])
        };

        var iface = new WlInterface
        {
            Name = Utf8StringMarshaller.ConvertToUnmanaged("zxdg_importer_v2"),
            Version = 1,
            MethodCount = 2,
            Methods = requests,
            EventCount = 0,
            Events = (WlMessage*)IntPtr.Zero
        };

        Marshal.StructureToPtr(iface, (IntPtr)ZxdgImporterV2, false);
        Interfaces.Add("zxdg_importer_v2", (IntPtr)ZxdgImporterV2);
    }


    /// <summary>
    /// Interface: zxdg_exported_v2
    /// Version: 1
    /// Requests: 1, Events: 1
    /// </summary>
    [ModuleInitializer]
    public static void CreateZxdgExportedV2Interface()
    {
        // Request signatures
        var requests = (WlMessage*)Marshal.AllocHGlobal(sizeof(WlMessage) * 1);
        requests[0] = new WlMessage
        {
            Name = Utf8StringMarshaller.ConvertToUnmanaged("destroy"),
            Signature = Utf8StringMarshaller.ConvertToUnmanaged(""),
            Types = (WlInterface**)IntPtr.Zero
        };

        // Event signatures
        var events = (WlMessage*)Marshal.AllocHGlobal(sizeof(WlMessage) * 1);
        events[0] = new WlMessage
        {
            Name = Utf8StringMarshaller.ConvertToUnmanaged("handle"),
            Signature = Utf8StringMarshaller.ConvertToUnmanaged("s"),
            Types = (WlInterface**)CreateTypesArray([(WlInterface*)IntPtr.Zero])
        };

        var iface = new WlInterface
        {
            Name = Utf8StringMarshaller.ConvertToUnmanaged("zxdg_exported_v2"),
            Version = 1,
            MethodCount = 1,
            Methods = requests,
            EventCount = 1,
            Events = events
        };

        Marshal.StructureToPtr(iface, (IntPtr)ZxdgExportedV2, false);
        Interfaces.Add("zxdg_exported_v2", (IntPtr)ZxdgExportedV2);
    }


    /// <summary>
    /// Interface: zxdg_imported_v2
    /// Version: 1
    /// Requests: 2, Events: 1
    /// </summary>
    [ModuleInitializer]
    public static void CreateZxdgImportedV2Interface()
    {
        // Request signatures
        var requests = (WlMessage*)Marshal.AllocHGlobal(sizeof(WlMessage) * 2);
        requests[0] = new WlMessage
        {
            Name = Utf8StringMarshaller.ConvertToUnmanaged("destroy"),
            Signature = Utf8StringMarshaller.ConvertToUnmanaged(""),
            Types = (WlInterface**)IntPtr.Zero
        };
        requests[1] = new WlMessage
        {
            Name = Utf8StringMarshaller.ConvertToUnmanaged("set_parent_of"),
            Signature = Utf8StringMarshaller.ConvertToUnmanaged("o"),
            Types = (WlInterface**)CreateTypesArray([WlSurface])
        };

        // Event signatures
        var events = (WlMessage*)Marshal.AllocHGlobal(sizeof(WlMessage) * 1);
        events[0] = new WlMessage
        {
            Name = Utf8StringMarshaller.ConvertToUnmanaged("destroyed"),
            Signature = Utf8StringMarshaller.ConvertToUnmanaged(""),
            Types = (WlInterface**)IntPtr.Zero
        };

        var iface = new WlInterface
        {
            Name = Utf8StringMarshaller.ConvertToUnmanaged("zxdg_imported_v2"),
            Version = 1,
            MethodCount = 2,
            Methods = requests,
            EventCount = 1,
            Events = events
        };

        Marshal.StructureToPtr(iface, (IntPtr)ZxdgImportedV2, false);
        Interfaces.Add("zxdg_imported_v2", (IntPtr)ZxdgImportedV2);
    }

}
