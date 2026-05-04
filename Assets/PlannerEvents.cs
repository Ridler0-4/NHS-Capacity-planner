using System;

public static class PlannerEvents
{
    public static event Action OnDataChanged;

    public static void DataChanged()
    {
        OnDataChanged?.Invoke();
    }
}