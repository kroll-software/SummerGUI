# StatusBar

`SummerGUI.Forms.Status.StatusBar` ist der horizontale Statusbalken am
unternen Rand eines `ApplicationWindow`. Er nimmt beliebig viele
Statuspanels auf, die nebeneinander angeordnet werden — ganz im
Geiste des Frameworks: *so einfach wie möglich, so kompliziert
wie notwendig.*

## Grundprinzipien

1. **Reihenfolge + Docking bestimmen die Position.**
2. **Größe wird pro Panel gesteuert** — entweder über
   `IStatusPanel.PanelWidth` / `.Fill` oder über den Content
   (Shrink-to-Content, Standard).
3. **Panels verteilen sich intelligent über die Breite:**
   starre Panels bekommen ihren Content, gewichtete Panels
   (`Fill`/Bruchteil) bekommen den freien Platz — **aber nie mehr
   als ihr Content** (eine Quote ist eine Obergrenze, kein Zwang).
   Bei Platzmangel verliert das **längste** Panel zuerst; kurze Panels
   (z. B. `"Ready."`) bleiben ganzzäh.
4. **Niemals eine Überlappung; kein Panel wird je breiter als die
   Gesamtbreite.** Selbst ein einzelnes Statuspanel schrumpft auf
   die Fensterbreite.
5. **Ageschnittener Text bekommt ein automatisches Tooltip**
   (Standardverhalten von `TextWidget`).

## Position und Reihenfolge

Die Reihenfolge (von links nach rechts) ist:

| # | Gruppe       | Quelle                                   | Beispiel                              |
|---|--------------|------------------------------------------|---------------------------------------|
| 1 | DefaultPanel | immer vorhanden, `Docking.Left`          | `"Ready."`                            |
| 2 | Left-Panels  | `Docking.Left`, ChildCollection-Reihenfolge | `StatusTextPanel("a", Docking.Left)` |
| 3 | Fill-Panels  | `Docking.Fill` oder `Docking.None`, ChildCollection-Reihenfolge | `StatusTextPanel(...).Fill = true`   |
| 4 | Right-Panels | `Docking.Right`, ChildCollection-Reihenfolge | Diagnostics (Layout/Paint)             |

Der **`DefaultPanel`** ist immer vorhanden, immer linksbündig und
immer an erster Position. Er kann nicht entfernt und nicht von
andere Panels verdrängt werden.

## `IStatusPanel`

Jedes Statuspanel, das seine Breite selbst steuern will,
implementiert `IStatusPanel`:

```csharp
public interface IStatusPanel
{
    // 0      = shrink-to-content
    // (0..1] = BRUCHTEIL des Restplatzes (0.6 = 60 %)
    // > 1    = FESTE Pixelbreite
    float PanelWidth { get; set; }

    // true  = Panel wächst auf vollen Restplatz
    // false = nur PanelWidth wirkt
    bool  Fill { get; set; }
}
```

`StatusTextPanel` und `StatusProgressPanel` implementieren
`IStatusPanel` bereits.

### PanelWidth-Semantik

| Wert                | Bedeutung                                              |
|---------------------|--------------------------------------------------------|
| `0`                 | **Shrink-to-Content** — die Panel-Breite = Text-Breite (Voreinstellung) |
| `0 < x ≤ 1`         | **Obergrenze / Anteil** des Restplatzes. z. B. `0.6` = *„bis zu 60 %“* — das Panel wächst maximal auf `min(Content, 60 %)`, bleibt bei kurzem Text also knapp |
| `x > 1`             | **Feste Pixelbreite**. z. B. `200` = 200 Pixel |

* Negative Werte sind **nicht** erlaubt (`ArgumentOutOfRangeException`).
* `Fill = true` ist gleichbedeutend mit `PanelWidth = 1` (wächst auf
  vollen Restplatz, **maximal** aber bis zum eigenen Content).
* Mehrere gewichtete Panels teilen den freien Platz nach Gewicht —
  jede Breite bleibt aber unter ihrem jeweiligen Content gedeckelt
  (keine leeren, aufgeblähten Zellen).

### `Fill`

`Fill = true` bedeutet: *dieses Panel wächst, um Platz aufzufüllen.*
Mehrerer `Fill`-Panels teilen den Restplatz gewichtet nach
`PanelWidth`.

### `Docking`

* `Docking.Left`  — Panel ist an die linke Gruppe angebunden.
* `Docking.Right` — Panel ist an die rechte Gruppe angebunden
  (Reihenfolge: vom rechten Rand aus).
* `Docking.Fill`  oder `Docking.None` — Panel wächst im Zentrum.

## Layout-Strategie

```
[0] [1] [2] [3] [4]
```

1. `DefaultPanel` wird zuerst links angeordnet.
2. `Docking.Left`-Panels in ChildCollection-Reihenfolge.
3. `Docking.Fill`/`Docking.None`-Panels im Zentrum (wachsen).
4. `Docking.Right`-Panels vom rechten Rand aus.

Pro Panel berechnet der Layout-Algorithmus (Cap-Modell):

```
content_i = (PanelWidth_i > 1) ? PanelWidth_i : Content-Breite_i
weight_i  = Fill? 1 : (0 < PanelWidth_i ≤ 1 ? PanelWidth_i : 0)
demand_i  = clamp(content_i, min_i, max_i)
cap_i     = weight_i > 0 ? clamp(weight_i * budget, min_i, max_i)
                          : demand_i            // starres Panel
target_i  = min(demand_i, cap_i)

// Platzmangel: vom längsten Panel abtragen (bis min_i)
// Breite_i = target_i  (pathologisch: 0 = wird nicht gezeichnet)
```

| Größe     | Berechnung                                                       |
|-----------|------------------------------------------------------------------|
| `budget`  | `max(0, avail − gaps)`                                            |
| `min_i`   | `max(8, MinSize.Width)` (absolute Untergrenze)                   |
| `max_i`   | `MaxSize.Width` (falls gesetzt)                                   |
| `demand_i`| `clamp(content_i, min_i, max_i)` (wie breit das Panel *will*)     |
| `cap_i`   | `clamp(weight_i × budget, min_i, max_i)` — die **Kap** je Quote   |
| `target_i`| `min(demand_i, cap_i)` — **nie breiter als Content ODER Quote**   |

### Platzverteilung (Intelligent & Überlappungsfrei)

Der Algorithmus garantiert drei Dinge, **immer**:

1. **Kein Panel ist je breiter als die Gesamtbreite.**
2. **Panels überlappen / zeichnen sich nie gegenseitig über.**
3. **Kein Panel wird unnötig groß** — eine Quote ist eine Obergrenze,
   kein Zwang: ein kurzes Panel (`"Ready."` mit 0.6) bleibt knapp,
   statt 60 % der Breite mit Luft auszufüllen.

Verteilungsregeln:

* **Normalfall** (`Σ target_i ≤ budget`): jedes Panel bekommt
  `min(Content, Quote)` — gewichtete Panels dürfen also ihren Anteil
  nur *bis* zu ihrem Content erreichen.
* **Platzmangel** (`Σ target_i > budget`): das **längste** Panel
  (größter Abstand `target_i − min_i`) verliert zuerst Pixel, bis es
  auf sein `MinSize` fällt; dann nimmt das nächste längste Panel die
  Rolle ein. Kurze Panels (kleine Überhöhung) bleiben ganzzäh, lange
  (mit großem Überschuss) werden zuerst mit **Ellipsis** gekürzt —
  bei Hover erscheint automatisch ein Tooltip (Standard von `TextWidget`).
* **Pathologischer Fall** (Fenster kleiner als die Summe aller
  `MinSize`): Mindestbreiten werden von rechts nach links vergeben;
  der Rest bekommt Breite 0 und wird **nicht gezeichnet**.

Jede Breite wird zusätzlich an `MinSize`/`MaxSize` geclampt und es
gilt eine absolute Untergrenze von **8 px** pro Panel.

## Beispiel

```csharp
var bar = window.StatusBar;

// 1) DefaultPanel — immer da, links
bar.DefaultPanel.Text = "Ready.";

// 2) Content-Panel, LEFT
var info = new StatusTextPanel ("info", Docking.Left, "12 Elemente");
bar.AddChild (info);

// 3) max. 60 % des Restplatzes, LEFT — wächst aber nicht über den
//    eigenen Content hinaus (kurzer Text bleibt knapp)
var msg = new StatusTextPanel ("msg", Docking.Left,
    "Dieser Text nimmt maximal 60 % …");
msg.PanelWidth = 0.6f;
bar.AddChild (msg);

// 4) Fixe Breite, LEFT
var fixed = new StatusTextPanel ("fix", Docking.Left, "200 px fix");
fixed.PanelWidth = 200f;   // > 1 => Pixel
bar.AddChild (fixed);

// 5) Fill, CENTER (reihenfolge im Zentrum)
var fill = new StatusTextPanel ("fill", Docking.Fill,
    "Dieses Panel wächst und füllt den Restplatz");
fill.Fill = true;
bar.AddChild (fill);

// 6) Right — vom rechten Rand aus
var right = new StatusTextPanel ("right", Docking.Right, "Status rechts");
bar.AddChild (right);
```

## Diagnostics

`ApplicationWindowDiagnostics` stellt `PulseLayout` und `PulsePaint`
zur Verfügung; deren Textpanels sind `Docking.Right` und
erscheinen in der rechten Gruppe. `StartDiagnostics()` hängt sie
an die `StatusBar` des `ApplicationWindow`.

## Tooltips für abgeschnittene Panels

`StatusTextPanel` erbt von `TextWidget`, das automatisch ein
Tooltip setzt, wenn der text nicht in die Panel-Breite passt.
Kein zusätzliche Code nötig.

## API

| Member                            | Art            | Beschreibung                                        |
|-----------------------------------|----------------|-----------------------------------------------------|
| `DefaultPanel`   | `StatusTextPanel` | Immer vorhanden, links, erste Position.          |
| `ProgressPanel`  | `StatusProgressPanel` | Fortschrittsanzeige (rechts, Content, Max 140 px). |
| `ShowStatus(string, bool)`        | method         | Setzt den Text des `DefaultPanel`.                   |
| `ShowProgress(int)`               | method         | Zeigt den `ProgressPanel` mit Promille.              |
| `ClearStatus()`                   | method         | Setzt `DefaultPanel` zurück auf `ReadyStatusString`. |
| `Add (Widget)` / `Remove (Widget)`    | method | Panels hinzufügen / entfernen (Standard-`Container`-Verhalten). |

## Hinweis für `PanelWidth`

Die Semantik ist bewusst WinForms-nahe, aber nicht 1:1:
es gibt keinen negativen Wert, es gibt keinen `PercentMode`-Schalter,
und `PanelWidth = 0` ist immer shrink-to-Content. `PanelWidth ∈ (0..1]`
ist eine **Obergrenze** („bis zu X %“), kein Zwang: ein kurzes Panel
bleibt knapp, ein langes wächst maximal bis zu seiner Quote.
