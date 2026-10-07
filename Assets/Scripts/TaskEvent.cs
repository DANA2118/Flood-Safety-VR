using System;

public static class TaskEvent
{
    public static event Action<string> TaskCompleted;

    public static void Complete(string taskId)
    {
        TaskCompleted?.Invoke(taskId);
    }
}
