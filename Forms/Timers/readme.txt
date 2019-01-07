DotNet Timers don't work very well together with the SummerGUIWindow Dispatcher.
So we have some custom timer implementations here, which are based on System.Threading.Tasks

You should use these whenever possible.

SummerGUI Timers use actions (delegates) instead of events. 
They are threadsafe under all conditions, safe for memory leaks
and have a very low demand on resources without interrupting the running thread.
