using System;
using System.Drawing;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace SummerGUI;

public class KeyPressEventArgs : System.EventArgs
{
    public char KeyChar { get; }
    /// <summary>
    /// The full text produced by the input event. For BMP characters this is 1 char;
    /// for astral (BMP-exceeding) emoji / CJK it is a UTF-16 surrogate pair (2 chars).
    /// Use this for insertion — never <see cref="KeyChar"/> alone.
    /// </summary>
    public string Text { get; }

    public KeyPressEventArgs(char keyChar)
    {
        KeyChar = keyChar;
        Text = keyChar.ToString();
    }

    public KeyPressEventArgs(string text)
    {
        if (String.IsNullOrEmpty(text))
            text = "\0";
        Text = text;
        KeyChar = text[0];
    }
}

public class MouseWheelEventArgs : System.EventArgs
{
    // Die Eigenschaft, die Ihre internen Widgets erwarten		
    public float X { get; } 
    public float Y { get; } 
    public OpenTK.Mathematics.Vector2 Offset { get; } 
    public float OffsetX { get; } 
    public float OffsetY { get; } 
    public PointF Position
    {
        get{
            return new PointF(X, Y); 
        }
    }
    
    public MouseWheelEventArgs(float x, float y, OpenTK.Mathematics.Vector2 offset, float offsetX, float offsetY)
    {
        X = x;
        Y = y;
        Offset = offset;
        OffsetX = offsetX;
        OffsetY = offsetY;
    }
    // ... andere Eigenschaften, falls nötig (z.B. Handled)
}

public class MouseButtonEventArgs : System.EventArgs
{
    // Die Eigenschaft, die Ihre internen Widgets erwarten
    public float X { get; } 
    public float Y { get; } 

    public PointF Position 
    {
        get
        {
            return new PointF(X, Y);
        }		
    } 

    public MouseButton Button { get; } 
    
    public MouseButtonEventArgs(float x, float y, MouseButton button)
    {
        X = x;
        Y = y;
        Button = button;
    }
    // ... andere Eigenschaften, falls nötig (z.B. Handled)
}


public static class ModifierKeys
{
    private static bool m_LeftControl = false;
    private static bool m_RightControl = false;
    private static bool m_LeftShift = false;
    private static bool m_RightShift = false;
    private static bool m_LeftAlt = false;
    private static bool m_RightAlt = false;

    private static bool m_CapsLock = false;

    public static bool ControlPressed { 
        get
        {
            return m_LeftControl || m_RightControl;
        }
    }
    public static bool ShiftPressed{ 
        get
        {
            bool on = m_LeftShift || m_RightShift;
            if (m_CapsLock)
                on = !on;
            return on;
        }		
    }

    public static bool ShiftPressedWithoutCapslock{ 
        get
        {				
            return m_LeftShift || m_RightShift;
        }		
    }

    public static bool AltPressed 
    { 
        get {
            return m_LeftAlt || m_RightAlt;
        }
    }
    
    // *** Control ***
    public static void OnLeftControlPressed()
    {
        m_LeftControl = true;
    }
    public static void OnLeftControlReleased()
    {
        m_LeftControl = false;
    }

    public static void OnRightControlPressed()
    {
        m_RightControl = true;
    }
    public static void OnRightControlReleased()
    {
        m_RightControl = false;
    }

    // *** Shift ***
    
    public static void OnLeftShiftPressed()
    {
        m_LeftShift = true;
    }
    public static void OnLeftShiftReleased()
    {
        m_LeftShift = false;
    }

    public static void OnRightShiftPressed()
    {
        m_RightShift = true;
    }
    public static void OnRightShiftReleased()
    {
        m_RightShift = false;
    }

    // *** CapsLock ***

    public static void OnCapsLockPressed()
    {
        m_CapsLock = !m_CapsLock;
    }
    public static void OnCapsLockReleased()
    {
        //m_CapsLock = false;
    }

    // *** Alt ***
            
    public static void OnLeftAltPressed()
    {
        m_LeftAlt = true;
    }
    public static void OnLeftAltReleased()
    {
        m_LeftAlt = false;
    }

    public static void OnRightAltPressed()
    {
        m_RightAlt = true;
    }
    public static void OnRightAltReleased()
    {
        m_RightAlt = false;
    }
}

