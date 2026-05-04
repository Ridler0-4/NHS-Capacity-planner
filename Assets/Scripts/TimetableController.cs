using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using CapacityPlanner;

public class TimetableController : MonoBehaviour
{
    private static readonly string[] Days = { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };
    private static readonly string[] Times = { "AM", "PM" };
    private static readonly string[] SessionTypes =
        { "Clinic", "Theatre", "Admin", "On Call", "Research", "" };

    //change this later with a date picker
    private DateTime _weekStart = new DateTime(2026, 4, 30);

    private UIDocument _doc;
    private VisualElement _grid;
    private PlannerData _data;

    void Start()
    {
        _doc = GetComponent<UIDocument>();
        _data = DataManager.Instance.Data;


        var sheet = Resources.Load<StyleSheet>("TimetableStyles");

        BuildGrid();
    }

    
    void BuildGrid()
    {
        
        var root = _doc.rootVisualElement;
        root.style.position = Position.Absolute;
        root.style.top = 0;
        root.style.left = 0;
        root.style.right = 0;
        root.style.bottom = 0;
        _grid = root.Q<VisualElement>("grid-container");
        _grid.Clear();


        root.Q<Label>("week-label").text = "Week of " + _weekStart.ToString("dd MMM yyyy");

        // Get unique clinicians in alphabetical order
        var clinicians = _data.timetable
            .Select(t => t.clinician)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(c => c)
            .ToList();


        BuildHeaderRow();

        foreach (var clinician in clinicians)
        {
            foreach (var time in Times)
            {
                BuildClinicianRow(clinician, time);
            }
        }
    }

    
    void BuildHeaderRow()
    {
        var row = new VisualElement();
        row.AddToClassList("grid-row");

        // Blank corner cell
        var corner = new VisualElement();
        corner.AddToClassList("grid-header-cell");
        corner.style.width = 120;
        row.Add(corner);

        // Day labels
        foreach (var day in Days)
        {
            var cell = new VisualElement();
            cell.AddToClassList("grid-header-cell");
            cell.Add(new Label(day));
            row.Add(cell);
        }

        _grid.Add(row);
    }
    
    void BuildClinicianRow(string clinician, string time)
    {
        var row = new VisualElement();
        row.AddToClassList("grid-row");

        var nameCell = new VisualElement();
        nameCell.AddToClassList("clinician-cell");

        if (time == "AM")
        {

            var nameLabel = new Label(clinician);
            nameLabel.AddToClassList("clinician-name");
            nameCell.Add(nameLabel);
        }

        var timeLabel = new Label(time);
        timeLabel.AddToClassList("clinician-time");
        nameCell.Add(timeLabel);
        row.Add(nameCell);

        foreach (var dayStr in Days)
        {
            var dayEnum = ParseDay(dayStr);
            var timeEnum = time == "AM" ? SessionTime.AM : SessionTime.PM;

            row.Add(BuildSessionCell(clinician, dayEnum, timeEnum));
        }

        _grid.Add(row);
    }

    
    // SESSION CELL — shows session type or Leave
    // Click it to cycle through session types
    
    VisualElement BuildSessionCell(string clinician, DayOfWeek day, SessionTime time)
    {
        var cell = new VisualElement();
        cell.AddToClassList("session-cell");

        // Check for leave first — leave overrides everything
        var sessionDate = CapacityEngine.GetSessionDate(_weekStart, day);
        bool onLeave = CapacityEngine.IsUnavailable(clinician, sessionDate, _data.unavailability);

        // Find existing timetable entry
        var entry = _data.timetable.FirstOrDefault(t =>
            string.Equals(t.clinician, clinician, StringComparison.OrdinalIgnoreCase)
            && t.day == day
            && t.time == time);

        // Build the clickable label
        var label = new Label();
        label.AddToClassList("session-label");

        if (onLeave)
        {
            label.text = "Leave";
            label.AddToClassList("leave");
        }
        else
        {
            SetSessionLabel(label, entry?.sessionType ?? "");
        }

        // Click cycles through session types (only if not on leave)
        if (!onLeave)
        {
            label.RegisterCallback<ClickEvent>(_ =>
            {
                CycleSession(clinician, day, time, label);
            });
            label.style.cursor = new StyleCursor(new UnityEngine.UIElements.Cursor());
        }

        cell.Add(label);
        return cell;
    }

    
    // CYCLE SESSION TYPE on click
    
    void CycleSession(string clinician, DayOfWeek day, SessionTime time, Label label)
    {
        var entry = _data.timetable.FirstOrDefault(t =>
            string.Equals(t.clinician, clinician, StringComparison.OrdinalIgnoreCase)
            && t.day == day
            && t.time == time);

        string current = entry?.sessionType ?? "";
        int idx = Array.IndexOf(SessionTypes, current);
        string next = SessionTypes[(idx + 1) % SessionTypes.Length];

        if (entry == null && next != "")
        {
            // Add a new entry
            _data.timetable.Add(new TimetableEntry(clinician, day, time, next));
        }
        else if (entry != null && next == "")
        {
            // Remove the entry (empty = no session)
            _data.timetable.Remove(entry);
        }
        else if (entry != null)
        {
            entry.sessionType = next;
        }

        // Update just this label — no need to rebuild the whole grid
        SetSessionLabel(label, next);
        PlannerEvents.DataChanged();

    }


    // HELPER — set label text and colour class

    void SetSessionLabel(Label label, string sessionType)
    {
        // Remove all colour classes first
        label.RemoveFromClassList("clinic");
        label.RemoveFromClassList("theatre");
        label.RemoveFromClassList("admin");
        label.RemoveFromClassList("oncall");
        label.RemoveFromClassList("leave");
        label.RemoveFromClassList("empty");

        switch (sessionType)
        {
            case "Clinic": label.text = "Clinic"; label.AddToClassList("clinic"); break;
            case "Theatre": label.text = "Theatre"; label.AddToClassList("theatre"); break;
            case "Admin": label.text = "Admin"; label.AddToClassList("admin"); break;
            case "On Call": label.text = "On Call"; label.AddToClassList("oncall"); break;
            case "Research": label.text = "Research"; label.AddToClassList("admin"); break;
            default: label.text = "—"; label.AddToClassList("empty"); break;
        }
    }

    
    // HELPER — convert "Mon" string to DayOfWeek enum
    
    DayOfWeek ParseDay(string day)
    {
        return day switch
        {
            "Mon" => DayOfWeek.Monday,
            "Tue" => DayOfWeek.Tuesday,
            "Wed" => DayOfWeek.Wednesday,
            "Thu" => DayOfWeek.Thursday,
            "Fri" => DayOfWeek.Friday,
            "Sat" => DayOfWeek.Saturday,
            "Sun" => DayOfWeek.Sunday,
            _ => DayOfWeek.Monday
        };
    }
    void OnEnable()
    {
        if (DataManager.Instance != null)
            BuildGrid();
    }
}