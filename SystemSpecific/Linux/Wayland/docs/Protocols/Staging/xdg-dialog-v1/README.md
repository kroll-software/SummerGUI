# XDG Dialog

<p class="breadcrumb"><a href="https://github.com/IrishBruse/WaylandDotnet/blob/main/WaylandDotnet">WaylandDotnet</a> <img src="assets/arrow.svg" class="breadcrumb-arrow" alt="" /> <a href="https://github.com/IrishBruse/WaylandDotnet/blob/main/WaylandDotnet/Protocols/Staging">Staging</a> <img src="assets/arrow.svg" class="breadcrumb-arrow" alt="" /> <a href="https://github.com/IrishBruse/WaylandDotnet/blob/main/WaylandDotnet/Protocols/Staging/xdg-dialog-v1/">XdgDialogV1</a></p>

---

<h2 class="decleration interface">
    <a href="#/Protocols/Staging/xdg-dialog-v1/?id=xdgwmdialogv1" id="xdgwmdialogv1">
        <span class="codicon codicon-symbol-interface"></span>
        XdgWmDialogV1
    </a>
    <span class="pill">version 1</span>
</h2>

Create dialogs related to other toplevels


The xdg_wm_dialog_v1 interface is exposed as a global object allowing
to register surfaces with a xdg_toplevel role as "dialogs" relative to
another toplevel.

The compositor may let this relation influence how the surface is
placed, displayed or interacted with.

Warning! The protocol described in this file is currently in the testing
phase. Backward compatible changes may be added together with the
corresponding interface version bump. Backward incompatible changes can
only be done by creating a new major version of the extension.


<h3 class="decleration request" title="Destroy request">
    <a href="#/Protocols/Staging/xdg-dialog-v1/?id=xdgwmdialogv1_destroy" id="xdgwmdialogv1_destroy">
        <span class="codicon codicon-symbol-method method"></span>
        XdgWmDialogV1.<span class="method">Destroy</span>
    </a>
    <span class="pill destructor">Type: destructor</span>
</h3>

```csharp
void Destroy()
```


**Destroy the dialog manager object**

Destroys the xdg_wm_dialog_v1 object. This does not affect
the xdg_dialog_v1 objects generated through it.

<h3 class="decleration request" title="GetXdgDialog request">
    <a href="#/Protocols/Staging/xdg-dialog-v1/?id=xdgwmdialogv1_getxdgdialog" id="xdgwmdialogv1_getxdgdialog">
        <span class="codicon codicon-symbol-method method"></span>
        XdgWmDialogV1.<span class="method">GetXdgDialog</span>
    </a>
</h3>

```csharp
XdgDialogV1 GetXdgDialog(XdgToplevel toplevel)
```

| Argument | Type | Description |
| --- | --- | --- |
| id | new_id |  |
| toplevel | object |  |

**Create a dialog object**

Creates a xdg_dialog_v1 object for the given toplevel. See the interface
description for more details.

Compositors must raise an already_used error if clients attempt to
create multiple xdg_dialog_v1 objects for the same xdg_toplevel.

<h3 class="decleration enum" title="Error enum">
    <a href="#/Protocols/Staging/xdg-dialog-v1/?id=xdgwmdialogv1_error_enum" id="xdgwmdialogv1_error_enum">
        <span class="codicon codicon-symbol-enum enum"></span>
        XdgWmDialogV1.<span class="enum">Error</span>
    </a>
</h3>

```csharp
public enum Error
```

| Value | Integer | Description |
| --- | --- | --- |
| AlreadyUsed | 0 | The xdg_toplevel object has already been used to create a xdg_dialog_v1 |
<h2 class="decleration interface">
    <a href="#/Protocols/Staging/xdg-dialog-v1/?id=xdgdialogv1" id="xdgdialogv1">
        <span class="codicon codicon-symbol-interface"></span>
        XdgDialogV1
    </a>
    <span class="pill">version 1</span>
</h2>

Dialog object


A xdg_dialog_v1 object is an ancillary object tied to a xdg_toplevel. Its
purpose is hinting the compositor that the toplevel is a "dialog" (e.g. a
temporary window) relative to another toplevel (see
xdg_toplevel.set_parent). If the xdg_toplevel is destroyed, the xdg_dialog_v1
becomes inert.

Through this object, the client may provide additional hints about
the purpose of the secondary toplevel. This interface has no effect
on toplevels that are not attached to a parent toplevel.


<h3 class="decleration request" title="Destroy request">
    <a href="#/Protocols/Staging/xdg-dialog-v1/?id=xdgdialogv1_destroy" id="xdgdialogv1_destroy">
        <span class="codicon codicon-symbol-method method"></span>
        XdgDialogV1.<span class="method">Destroy</span>
    </a>
    <span class="pill destructor">Type: destructor</span>
</h3>

```csharp
void Destroy()
```


**Destroy the dialog object**

Destroys the xdg_dialog_v1 object. If this object is destroyed
before the related xdg_toplevel, the compositor should unapply its
effects.

<h3 class="decleration request" title="SetModal request">
    <a href="#/Protocols/Staging/xdg-dialog-v1/?id=xdgdialogv1_setmodal" id="xdgdialogv1_setmodal">
        <span class="codicon codicon-symbol-method method"></span>
        XdgDialogV1.<span class="method">SetModal</span>
    </a>
</h3>

```csharp
void SetModal()
```


**Mark dialog as modal**

Hints that the dialog has "modal" behavior. Modal dialogs typically
require to be fully addressed by the user (i.e. closed) before resuming
interaction with the parent toplevel, and may require a distinct
presentation.

Clients must implement the logic to filter events in the parent
toplevel on their own.

Compositors may choose any policy in event delivery to the parent
toplevel, from delivering all events unfiltered to using them for
internal consumption.

<h3 class="decleration request" title="UnsetModal request">
    <a href="#/Protocols/Staging/xdg-dialog-v1/?id=xdgdialogv1_unsetmodal" id="xdgdialogv1_unsetmodal">
        <span class="codicon codicon-symbol-method method"></span>
        XdgDialogV1.<span class="method">UnsetModal</span>
    </a>
</h3>

```csharp
void UnsetModal()
```


**Mark dialog as not modal**

Drops the hint that this dialog has "modal" behavior. See
xdg_dialog_v1.set_modal for more details.

