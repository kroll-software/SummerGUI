# XDG Foreign V2

<p class="breadcrumb"><a href="https://github.com/IrishBruse/WaylandDotnet/blob/main/WaylandDotnet">WaylandDotnet</a> <img src="assets/arrow.svg" class="breadcrumb-arrow" alt="" /> <a href="https://github.com/IrishBruse/WaylandDotnet/blob/main/WaylandDotnet/Protocols/Unstable">Unstable</a> <img src="assets/arrow.svg" class="breadcrumb-arrow" alt="" /> <a href="https://github.com/IrishBruse/WaylandDotnet/blob/main/WaylandDotnet/Protocols/Unstable/xdg-foreign-unstable-v2/">XdgForeignUnstableV2</a></p>

---

<h2 class="decleration interface">
    <a href="#/Protocols/Unstable/xdg-foreign-unstable-v2/?id=zxdgexporterv2" id="zxdgexporterv2">
        <span class="codicon codicon-symbol-interface"></span>
        ZxdgExporterV2
    </a>
    <span class="pill">version 1</span>
</h2>

Interface for exporting surfaces


A global interface used for exporting surfaces that can later be imported
using xdg_importer.


<h3 class="decleration request" title="Destroy request">
    <a href="#/Protocols/Unstable/xdg-foreign-unstable-v2/?id=zxdgexporterv2_destroy" id="zxdgexporterv2_destroy">
        <span class="codicon codicon-symbol-method method"></span>
        ZxdgExporterV2.<span class="method">Destroy</span>
    </a>
    <span class="pill destructor">Type: destructor</span>
</h3>

```csharp
void Destroy()
```


**Destroy the xdg_exporter object**

Notify the compositor that the xdg_exporter object will no longer be
used.

<h3 class="decleration request" title="ExportToplevel request">
    <a href="#/Protocols/Unstable/xdg-foreign-unstable-v2/?id=zxdgexporterv2_exporttoplevel" id="zxdgexporterv2_exporttoplevel">
        <span class="codicon codicon-symbol-method method"></span>
        ZxdgExporterV2.<span class="method">ExportToplevel</span>
    </a>
</h3>

```csharp
ZxdgExportedV2 ExportToplevel(WlSurface surface)
```

| Argument | Type | Description |
| --- | --- | --- |
| id | new_id | The new xdg_exported object |
| surface | object | The surface to export |

**Export a toplevel surface**

The export_toplevel request exports the passed surface so that it can later be
imported via xdg_importer. When called, a new xdg_exported object will
be created and xdg_exported.handle will be sent immediately. See the
corresponding interface and event for details.

A surface may be exported multiple times, and each exported handle may
be used to create an xdg_imported multiple times. Only xdg_toplevel
equivalent surfaces may be exported, otherwise an invalid_surface
protocol error is sent.

<h3 class="decleration enum" title="Error enum">
    <a href="#/Protocols/Unstable/xdg-foreign-unstable-v2/?id=zxdgexporterv2_error_enum" id="zxdgexporterv2_error_enum">
        <span class="codicon codicon-symbol-enum enum"></span>
        ZxdgExporterV2.<span class="enum">Error</span>
    </a>
</h3>

```csharp
public enum Error
```

Error values


These errors can be emitted in response to invalid xdg_exporter
requests.


| Value | Integer | Description |
| --- | --- | --- |
| InvalidSurface | 0 | Surface is not an xdg_toplevel |
<h2 class="decleration interface">
    <a href="#/Protocols/Unstable/xdg-foreign-unstable-v2/?id=zxdgimporterv2" id="zxdgimporterv2">
        <span class="codicon codicon-symbol-interface"></span>
        ZxdgImporterV2
    </a>
    <span class="pill">version 1</span>
</h2>

Interface for importing surfaces


A global interface used for importing surfaces exported by xdg_exporter.
With this interface, a client can create a reference to a surface of
another client.


<h3 class="decleration request" title="Destroy request">
    <a href="#/Protocols/Unstable/xdg-foreign-unstable-v2/?id=zxdgimporterv2_destroy" id="zxdgimporterv2_destroy">
        <span class="codicon codicon-symbol-method method"></span>
        ZxdgImporterV2.<span class="method">Destroy</span>
    </a>
    <span class="pill destructor">Type: destructor</span>
</h3>

```csharp
void Destroy()
```


**Destroy the xdg_importer object**

Notify the compositor that the xdg_importer object will no longer be
used.

<h3 class="decleration request" title="ImportToplevel request">
    <a href="#/Protocols/Unstable/xdg-foreign-unstable-v2/?id=zxdgimporterv2_importtoplevel" id="zxdgimporterv2_importtoplevel">
        <span class="codicon codicon-symbol-method method"></span>
        ZxdgImporterV2.<span class="method">ImportToplevel</span>
    </a>
</h3>

```csharp
ZxdgImportedV2 ImportToplevel(string handle)
```

| Argument | Type | Description |
| --- | --- | --- |
| id | new_id | The new xdg_imported object |
| handle | string | The exported surface handle |

**Import a toplevel surface**

The import_toplevel request imports a surface from any client given a handle
retrieved by exporting said surface using xdg_exporter.export_toplevel.
When called, a new xdg_imported object will be created. This new object
represents the imported surface, and the importing client can
manipulate its relationship using it. See xdg_imported for details.

<h2 class="decleration interface">
    <a href="#/Protocols/Unstable/xdg-foreign-unstable-v2/?id=zxdgexportedv2" id="zxdgexportedv2">
        <span class="codicon codicon-symbol-interface"></span>
        ZxdgExportedV2
    </a>
    <span class="pill">version 1</span>
</h2>

An exported surface handle


An xdg_exported object represents an exported reference to a surface. The
exported surface may be referenced as long as the xdg_exported object not
destroyed. Destroying the xdg_exported invalidates any relationship the
importer may have established using xdg_imported.


<h3 class="decleration request" title="Destroy request">
    <a href="#/Protocols/Unstable/xdg-foreign-unstable-v2/?id=zxdgexportedv2_destroy" id="zxdgexportedv2_destroy">
        <span class="codicon codicon-symbol-method method"></span>
        ZxdgExportedV2.<span class="method">Destroy</span>
    </a>
    <span class="pill destructor">Type: destructor</span>
</h3>

```csharp
void Destroy()
```


**Unexport the exported surface**

Revoke the previously exported surface. This invalidates any
relationship the importer may have set up using the xdg_imported created
given the handle sent via xdg_exported.handle.

<h3 class="decleration event" title="Handle event">
    <a href="#/Protocols/Unstable/xdg-foreign-unstable-v2/?id=onzxdgexportedv2_handle" id="onzxdgexportedv2_handle">
        <span class="codicon codicon-symbol-event event"></span>
        ZxdgExportedV2.<span class="event">OnHandle</span>
    </a>
</h3>

```csharp
void HandleHandler(string handle)
```

| Argument | Type | Description |
| --- | --- | --- |
| handle | string | The exported surface handle |

**The exported surface handle**

The handle event contains the unique handle of this exported surface
reference. It may be shared with any client, which then can use it to
import the surface by calling xdg_importer.import_toplevel. A handle
may be used to import the surface multiple times.

<h2 class="decleration interface">
    <a href="#/Protocols/Unstable/xdg-foreign-unstable-v2/?id=zxdgimportedv2" id="zxdgimportedv2">
        <span class="codicon codicon-symbol-interface"></span>
        ZxdgImportedV2
    </a>
    <span class="pill">version 1</span>
</h2>

An imported surface handle


An xdg_imported object represents an imported reference to surface exported
by some client. A client can use this interface to manipulate
relationships between its own surfaces and the imported surface.


<h3 class="decleration request" title="Destroy request">
    <a href="#/Protocols/Unstable/xdg-foreign-unstable-v2/?id=zxdgimportedv2_destroy" id="zxdgimportedv2_destroy">
        <span class="codicon codicon-symbol-method method"></span>
        ZxdgImportedV2.<span class="method">Destroy</span>
    </a>
    <span class="pill destructor">Type: destructor</span>
</h3>

```csharp
void Destroy()
```


**Destroy the xdg_imported object**

Notify the compositor that it will no longer use the xdg_imported
object. Any relationship that may have been set up will at this point
be invalidated.

<h3 class="decleration request" title="SetParentOf request">
    <a href="#/Protocols/Unstable/xdg-foreign-unstable-v2/?id=zxdgimportedv2_setparentof" id="zxdgimportedv2_setparentof">
        <span class="codicon codicon-symbol-method method"></span>
        ZxdgImportedV2.<span class="method">SetParentOf</span>
    </a>
</h3>

```csharp
void SetParentOf(WlSurface surface)
```

| Argument | Type | Description |
| --- | --- | --- |
| surface | object | The child surface |

**Set as the parent of some surface**

Set the imported surface as the parent of some surface of the client.
The passed surface must be an xdg_toplevel equivalent, otherwise an
invalid_surface protocol error is sent. Calling this function sets up
a surface to surface relation with the same stacking and positioning
semantics as xdg_toplevel.set_parent.

<h3 class="decleration event" title="Destroyed event">
    <a href="#/Protocols/Unstable/xdg-foreign-unstable-v2/?id=onzxdgimportedv2_destroyed" id="onzxdgimportedv2_destroyed">
        <span class="codicon codicon-symbol-event event"></span>
        ZxdgImportedV2.<span class="event">OnDestroyed</span>
    </a>
</h3>

```csharp
void DestroyedHandler()
```


**The imported surface handle has been destroyed**

The imported surface handle has been destroyed and any relationship set
up has been invalidated. This may happen for various reasons, for
example if the exported surface or the exported surface handle has been
destroyed, if the handle used for importing was invalid.

<h3 class="decleration enum" title="Error enum">
    <a href="#/Protocols/Unstable/xdg-foreign-unstable-v2/?id=zxdgimportedv2_error_enum" id="zxdgimportedv2_error_enum">
        <span class="codicon codicon-symbol-enum enum"></span>
        ZxdgImportedV2.<span class="enum">Error</span>
    </a>
</h3>

```csharp
public enum Error
```

Error values


These errors can be emitted in response to invalid xdg_imported
requests.


| Value | Integer | Description |
| --- | --- | --- |
| InvalidSurface | 0 | Surface is not an xdg_toplevel |
