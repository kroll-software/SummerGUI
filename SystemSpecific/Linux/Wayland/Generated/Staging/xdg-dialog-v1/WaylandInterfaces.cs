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
    public static WlInterface* XdgWmDialogV1 = AllocateInterface();
    public static WlInterface* XdgDialogV1 = AllocateInterface();


    /// <summary>
    /// Interface: xdg_wm_dialog_v1
    /// Version: 1
    /// Requests: 2, Events: 0
    /// </summary>
    [ModuleInitializer]
    public static void CreateXdgWmDialogV1Interface()
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
            Name = Utf8StringMarshaller.ConvertToUnmanaged("get_xdg_dialog"),
            Signature = Utf8StringMarshaller.ConvertToUnmanaged("no"),
            Types = (WlInterface**)CreateTypesArray([XdgDialogV1, XdgToplevel])
        };

        var iface = new WlInterface
        {
            Name = Utf8StringMarshaller.ConvertToUnmanaged("xdg_wm_dialog_v1"),
            Version = 1,
            MethodCount = 2,
            Methods = requests,
            EventCount = 0,
            Events = (WlMessage*)IntPtr.Zero
        };

        Marshal.StructureToPtr(iface, (IntPtr)XdgWmDialogV1, false);
        Interfaces.Add("xdg_wm_dialog_v1", (IntPtr)XdgWmDialogV1);
    }


    /// <summary>
    /// Interface: xdg_dialog_v1
    /// Version: 1
    /// Requests: 3, Events: 0
    /// </summary>
    [ModuleInitializer]
    public static void CreateXdgDialogV1Interface()
    {
        // Request signatures
        var requests = (WlMessage*)Marshal.AllocHGlobal(sizeof(WlMessage) * 3);
        requests[0] = new WlMessage
        {
            Name = Utf8StringMarshaller.ConvertToUnmanaged("destroy"),
            Signature = Utf8StringMarshaller.ConvertToUnmanaged(""),
            Types = (WlInterface**)IntPtr.Zero
        };
        requests[1] = new WlMessage
        {
            Name = Utf8StringMarshaller.ConvertToUnmanaged("set_modal"),
            Signature = Utf8StringMarshaller.ConvertToUnmanaged(""),
            Types = (WlInterface**)IntPtr.Zero
        };
        requests[2] = new WlMessage
        {
            Name = Utf8StringMarshaller.ConvertToUnmanaged("unset_modal"),
            Signature = Utf8StringMarshaller.ConvertToUnmanaged(""),
            Types = (WlInterface**)IntPtr.Zero
        };

        var iface = new WlInterface
        {
            Name = Utf8StringMarshaller.ConvertToUnmanaged("xdg_dialog_v1"),
            Version = 1,
            MethodCount = 3,
            Methods = requests,
            EventCount = 0,
            Events = (WlMessage*)IntPtr.Zero
        };

        Marshal.StructureToPtr(iface, (IntPtr)XdgDialogV1, false);
        Interfaces.Add("xdg_dialog_v1", (IntPtr)XdgDialogV1);
    }

}
