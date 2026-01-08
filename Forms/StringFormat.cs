using System;

namespace SummerGUI
{
    // Die Flags, die in den Konstruktoren verwendet werden
    [Flags]
    public enum StringFormatFlags
    {
        NoWrap = 1,
        DisplayFormatControl = 2,
        NoFontFallback = 4,
        MeasureTrailingSpaces = 8,
        NoClip = 16,
        LineLimit = 32,
        NoWrapAndNoClip = NoWrap | NoClip
    }

    // Die tatsächliche StringFormat Klasse
    public class StringFormat
    {
        public StringFormatFlags FormatFlags { get; set; }
        // Fügen Sie hier weitere Eigenschaften hinzu, falls Sie diese verwenden:
        // public StringAlignment Alignment { get; set; }
        // public StringAlignment LineAlignment { get; set; }
        // ...

        // Standardkonstruktor
        public StringFormat() : this(StringFormatFlags.NoWrap) { }

        // Konstruktor mit Flags
        public StringFormat(StringFormatFlags flags)
        {
            FormatFlags = flags;
        }

        // Statische Standardinstanzen, wie sie in System.Drawing existierten
        public static StringFormat GenericDefault
        {
            get { return new StringFormat(StringFormatFlags.LineLimit | StringFormatFlags.NoFontFallback | StringFormatFlags.NoWrap); }
        }

        public static StringFormat GenericTypographic
        {
            get { return new StringFormat(StringFormatFlags.NoFontFallback | StringFormatFlags.DisplayFormatControl); }
        }
    }
}
