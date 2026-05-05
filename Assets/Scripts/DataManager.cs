using UnityEngine;
using CapacityPlanner;

public class DataManager : MonoBehaviour
{
    public static DataManager Instance { get; private set; }
    public PlannerData Data { get; private set; }

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Try to load saved data first
        // Fall back to defaults if no save file exists
        Data = PlannerSaveLoad.Load() ?? CreateDefaults();

        // Auto-save whenever data changes
        PlannerEvents.OnDataChanged += () => PlannerSaveLoad.Save(Data);
        {
            Debug.Log("Data changed — saving...");
            PlannerSaveLoad.Save(Data);
        }
        ;
    }

    PlannerData CreateDefaults()
    {
        var data = new PlannerData();
        data.LoadDefaults();
        PlannerSaveLoad.Save(data); // save defaults immediately
        return data;
    }

    // Call this from anywhere to manually save
    public void Save() => PlannerSaveLoad.Save(Data);
}