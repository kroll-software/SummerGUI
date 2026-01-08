using System;

namespace SummerGUI.Timers;

public class CursorBlinker
{
    private readonly LoopTimer timer;
    public bool IsVisible { get; private set; } = true;

    public CursorBlinker(int blinkRateMs = 500)
    {
        timer = new LoopTimer(blinkRateMs);
    }

    // Im Render- oder Update-Loop aufrufen
    public bool Update()
    {
        if (timer.CheckAndReset())
        {
            IsVisible = !IsVisible;
            return true; // signalisiert: neu zeichnen
        }
        return false;
    }
}
