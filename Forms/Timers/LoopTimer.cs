using System;

namespace SummerGUI;

public class LoopTimer
{
    private int startTick;
    private readonly int interval;

    // interval in Millisekunden
    public LoopTimer(int interval)
    {
        this.interval = interval;
        Reset();
    }

    public void Reset()
    {
        startTick = Environment.TickCount;
    }

    // Gibt true zurück, sobald das Intervall verstrichen ist
    public bool HasElapsed()
    {
        int now = Environment.TickCount;
        // overflow-sichere Differenz
        return unchecked(now - startTick) >= interval;
    }

    // Gibt true zurück und resettet automatisch
    public bool CheckAndReset()
    {
        int now = Environment.TickCount;
        if (unchecked(now - startTick) >= interval)
        {
            startTick = now;
            return true;
        }
        return false;
    }

    // Optionale Methode: Zeitdifferenz
    public int Elapsed => unchecked(Environment.TickCount - startTick);
}
