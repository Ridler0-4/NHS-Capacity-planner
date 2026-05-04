using CapacityPlanner;
using System;
using UnityEngine;

public class DataManager : MonoBehaviour
{
    public static DataManager Instance { get; private set; }

    public PlannerData Data { get; private set; }

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        Data = new PlannerData();
        Data.LoadDefaults();


        var weekStart = new DateTime(2025, 1, 20);
        var rows = CapacityEngine.BuildDashboard(weekStart, Data);

        foreach (var row in rows)
        {
            Debug.Log($"{row.clinician} | " +
                      $"Planned: {row.planned} | " +
                      $"Adjusted: {row.adjusted} | " +
                      $"Actual: {row.actual} | " +
                      $"Cap: {row.capacityVariance:F1}% {row.capacityStatus} | " +
                      $"Del: {row.deliveryVariance:F1}% {row.deliveryStatus}");
        }
    }

}