// PlannerSaveLoad.cs
// Handles saving and loading PlannerData to disk
// and exporting a CSV for Excel

using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using CapacityPlanner;

public static class PlannerSaveLoad
{
    // Where the save file lives on disk
    // Application.persistentDataPath is a safe folder Unity
    // can always write to on any platform
    private static string SavePath =>
        Path.Combine(Application.persistentDataPath, "planner_data.json");

    private static string AuditPath =>
        Path.Combine(Application.persistentDataPath, "audit_log.txt");

    // ─────────────────────────────────────────────
    // SAVE — converts PlannerData to JSON and writes to disk
    // ─────────────────────────────────────────────
    public static void Save(PlannerData data)
    {
        try
        {
            string json = JsonUtility.ToJson(data, prettyPrint: true);
            File.WriteAllText(SavePath, json);
            WriteAudit("Data saved");
            Debug.Log("Saved to: " + SavePath);
        }
        catch (Exception e)
        {
            Debug.LogError("Save failed: " + e.Message);
        }
    }

    // ─────────────────────────────────────────────
    // LOAD — reads JSON from disk and returns PlannerData
    // Returns null if no save file exists yet
    // ─────────────────────────────────────────────
    public static PlannerData Load()
    {
        try
        {
            if (!File.Exists(SavePath))
            {
                Debug.Log("No save file found, using defaults");
                return null;
            }

            string json = File.ReadAllText(SavePath);
            var data = JsonUtility.FromJson<PlannerData>(json);
            WriteAudit("Data loaded");
            Debug.Log("Loaded from: " + SavePath);
            return data;
        }
        catch (Exception e)
        {
            Debug.LogError("Load failed: " + e.Message);
            return null;
        }
    }

    // ─────────────────────────────────────────────
    // EXPORT CSV — writes a spreadsheet anyone can
    // open in Excel or Google Sheets
    // ─────────────────────────────────────────────
    public static void ExportCSV(List<DashboardRow> rows, DateTime weekStart)
    {
        try
        {
            string exportPath = Path.Combine(
                Application.persistentDataPath,
                $"capacity_export_{weekStart:yyyy-MM-dd}.csv");

            using (var writer = new StreamWriter(exportPath))
            {
                // Header row
                writer.WriteLine(
                    "Clinician,Planned,Adjusted,Lost,Cap Variance %," +
                    "Cap Status,Actual,Del Variance %,Del Status");

                // One row per clinician
                foreach (var row in rows)
                {
                    string delVariance = float.IsNaN(row.deliveryVariance)
                        ? "N/A"
                        : row.deliveryVariance.ToString("F1");

                    writer.WriteLine(
                        $"{row.clinician}," +
                        $"{row.planned}," +
                        $"{row.adjusted}," +
                        $"{row.planned - row.adjusted}," +
                        $"{row.capacityVariance:F1}," +
                        $"{row.capacityStatus}," +
                        $"{(row.actual >= 0 ? row.actual.ToString() : "N/A")}," +
                        $"{delVariance}," +
                        $"{row.deliveryStatus}");
                }
            }

            WriteAudit($"CSV exported for week {weekStart:yyyy-MM-dd}");
            Debug.Log("CSV exported to: " + exportPath);
        }
        catch (Exception e)
        {
            Debug.LogError("CSV export failed: " + e.Message);
        }
    }

    // ─────────────────────────────────────────────
    // AUDIT LOG — appends a timestamped entry
    // ─────────────────────────────────────────────
    public static void WriteAudit(string message)
    {
        try
        {
            string entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
            File.AppendAllText(AuditPath, entry + Environment.NewLine);
        }
        catch { /* silently fail — audit log is non-critical */ }
    }

    // ─────────────────────────────────────────────
    // Helper — tells you where files are saved
    // Useful so the manager knows where to find the CSV
    // ─────────────────────────────────────────────
    public static string GetSaveFolder()
    {
        return Application.persistentDataPath;
    }
}