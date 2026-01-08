using System;

namespace SummerGUI.Timers;

public class DeltaTimer
{
    public int Delta { get; private set; }
    private int lastTick;

    public DeltaTimer()
    {
        lastTick = Environment.TickCount;
    }

    public void Update()
    {
        int now = Environment.TickCount;
        Delta = unchecked(now - lastTick);
        lastTick = now;
    }
}
