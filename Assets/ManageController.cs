using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using CapacityPlanner;

public class ManageController : MonoBehaviour
{
    private UIDocument _doc;
    private PlannerData _data;

    void Start()
    {
        _doc = GetComponent<UIDocument>();
        _data = DataManager.Instance.Data;
    }

    void OnEnable()
    {
        if (DataManager.Instance == null) return;
        _data = DataManager.Instance.Data;
        BuildAll();
    }

    void BuildAll()
    {
        var root = _doc.rootVisualElement;
        root.style.position = Position.Absolute;
        root.style.top = 0;
        root.style.left = 0;
        root.style.right = 0;
        root.style.bottom = 0;

        WireAddClinician(root);
        WireRemoveClinician(root);
        WireUnavailability(root);
        WireActuals(root);
        RefreshUnavailList(root);
    }

    // ─────────────────────────────────────────────
    // ADD CLINICIAN
    // ─────────────────────────────────────────────
    void WireAddClinician(VisualElement root)
    {
        var nameField = root.Q<TextField>("new-clinician-name");
        var btn = root.Q<Button>("add-clinician-btn");

        btn.clicked += () =>
        {
            var name = nameField.value.Trim();
            if (string.IsNullOrEmpty(name))
            {
                ShowFeedback(btn, "Please enter a name", false);
                return;
            }

            bool exists = _data.timetable.Any(t =>
                string.Equals(t.clinician, name, StringComparison.OrdinalIgnoreCase));
            if (exists)
            {
                ShowFeedback(btn, "Clinician already exists", false);
                return;
            }

            // Add a default Monday AM Clinic session so they appear in the grid
            _data.timetable.Add(new TimetableEntry(
                name, DayOfWeek.Monday, SessionTime.AM, "Clinic"));

            PlannerSaveLoad.WriteAudit($"Added clinician: {name}");
            PlannerEvents.DataChanged();

            nameField.value = "";
            RefreshAllDropdowns(root);
            ShowFeedback(btn, $"{name} added!", true);
        };
    }

    // ─────────────────────────────────────────────
    // REMOVE CLINICIAN
    // ─────────────────────────────────────────────
    void WireRemoveClinician(VisualElement root)
    {
        var dropdown = root.Q<DropdownField>("remove-clinician-dropdown");
        var btn = root.Q<Button>("remove-clinician-btn");

        RefreshDropdown(dropdown, GetClinicians());

        btn.clicked += () =>
        {
            var name = dropdown.value;
            if (string.IsNullOrEmpty(name) || name == "Select...")
            {
                ShowFeedback(btn, "Please select a clinician", false);
                return;
            }

            // Remove all their timetable entries
            _data.timetable.RemoveAll(t =>
                string.Equals(t.clinician, name, StringComparison.OrdinalIgnoreCase));

            // Remove their unavailability entries
            _data.unavailability.RemoveAll(u =>
                string.Equals(u.clinician, name, StringComparison.OrdinalIgnoreCase));

            // Remove their actuals
            _data.actuals.RemoveAll(a =>
                string.Equals(a.clinician, name, StringComparison.OrdinalIgnoreCase));

            PlannerSaveLoad.WriteAudit($"Removed clinician: {name}");
            PlannerEvents.DataChanged();

            RefreshAllDropdowns(root);
            RefreshUnavailList(root);
            ShowFeedback(btn, $"{name} removed", true);
        };
    }

    // ─────────────────────────────────────────────
    // ADD UNAVAILABILITY
    // ─────────────────────────────────────────────
    void WireUnavailability(VisualElement root)
    {
        var clinicianDrop = root.Q<DropdownField>("unavail-clinician");
        var reasonDrop = root.Q<DropdownField>("unavail-reason");
        var startField = root.Q<TextField>("unavail-start");
        var endField = root.Q<TextField>("unavail-end");
        var btn = root.Q<Button>("unavail-btn");

        RefreshDropdown(clinicianDrop, GetClinicians());
        RefreshDropdown(reasonDrop, new List<string> { "Leave", "Sick", "OnCall" });

        // Default dates to current week
        startField.value = DateTime.Now.ToString("yyyy-MM-dd");
        endField.value = DateTime.Now.ToString("yyyy-MM-dd");

        btn.clicked += () =>
        {
            var clinician = clinicianDrop.value;
            var reason = reasonDrop.value;
            var start = startField.value.Trim();
            var end = endField.value.Trim();

            // Validate
            if (string.IsNullOrEmpty(clinician) || clinician == "Select...")
            { ShowFeedback(btn, "Please select a clinician", false); return; }

            if (!DateTime.TryParse(start, out DateTime startDate))
            { ShowFeedback(btn, "Invalid start date — use YYYY-MM-DD", false); return; }

            if (!DateTime.TryParse(end, out DateTime endDate))
            { ShowFeedback(btn, "Invalid end date — use YYYY-MM-DD", false); return; }

            if (endDate < startDate)
            { ShowFeedback(btn, "End date must be after start date", false); return; }

            if (!Enum.TryParse(reason, out UnavailabilityReason reasonEnum))
                reasonEnum = UnavailabilityReason.Leave;

            _data.unavailability.Add(new Unavailability(
                clinician, start, end, reasonEnum));

            PlannerSaveLoad.WriteAudit(
                $"Added unavailability: {clinician} {reason} {start} to {end}");
            PlannerEvents.DataChanged();

            RefreshUnavailList(root);
            ShowFeedback(btn, "Unavailability added!", true);
        };
    }

    // ─────────────────────────────────────────────
    // ENTER ACTUALS
    // ─────────────────────────────────────────────
    void WireActuals(VisualElement root)
    {
        var clinicianDrop = root.Q<DropdownField>("actual-clinician");
        var weekField = root.Q<TextField>("actual-week");
        var patientsField = root.Q<TextField>("actual-patients");
        var btn = root.Q<Button>("actual-btn");

        RefreshDropdown(clinicianDrop, GetClinicians());
        weekField.value = "2026-05-04";

        btn.clicked += () =>
        {
            var clinician = clinicianDrop.value;
            var weekStr = weekField.value.Trim();
            var patientsStr = patientsField.value.Trim();

            if (string.IsNullOrEmpty(clinician) || clinician == "Select...")
            { ShowFeedback(btn, "Please select a clinician", false); return; }

            if (!DateTime.TryParse(weekStr, out _))
            { ShowFeedback(btn, "Invalid date — use YYYY-MM-DD", false); return; }

            if (!int.TryParse(patientsStr, out int patients) || patients < 0)
            { ShowFeedback(btn, "Please enter a valid number", false); return; }

            // Update existing entry or add new one
            var existing = _data.actuals.FirstOrDefault(a =>
                string.Equals(a.clinician, clinician, StringComparison.OrdinalIgnoreCase)
                && a.weekStart == weekStr);

            if (existing != null)
                existing.patientsDelivered = patients;
            else
                _data.actuals.Add(new ActualActivity(clinician, weekStr, patients));

            PlannerSaveLoad.WriteAudit(
                $"Actuals entered: {clinician} week {weekStr} = {patients} patients");
            PlannerEvents.DataChanged();

            patientsField.value = "";
            ShowFeedback(btn, $"Saved {patients} patients for {clinician}", true);
        };
    }

    // ─────────────────────────────────────────────
    // UNAVAILABILITY LIST — shows current entries
    // with a delete button on each
    // ─────────────────────────────────────────────
    void RefreshUnavailList(VisualElement root)
    {
        var list = root.Q<VisualElement>("unavail-list");
        list.Clear();

        if (_data.unavailability.Count == 0)
        {
            var empty = new Label("No unavailability entries");
            empty.AddToClassList("unavail-text");
            list.Add(empty);
            return;
        }

        for (int i = 0; i < _data.unavailability.Count; i++)
        {
            int captured = i;
            var u = _data.unavailability[i];

            var row = new VisualElement();
            row.AddToClassList("unavail-row");

            var text = new Label(
                $"{u.clinician} — {u.reason} — {u.startDate} to {u.endDate}");
            text.AddToClassList("unavail-text");

            var deleteBtn = new Button(() =>
            {
                _data.unavailability.RemoveAt(captured);
                PlannerSaveLoad.WriteAudit(
                    $"Removed unavailability: {u.clinician} {u.reason} {u.startDate}");
                PlannerEvents.DataChanged();
                RefreshUnavailList(root);
            });
            deleteBtn.text = "Remove";
            deleteBtn.AddToClassList("unavail-delete");

            row.Add(text);
            row.Add(deleteBtn);
            list.Add(row);
        }
    }

    // ─────────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────────
    List<string> GetClinicians()
    {
        return _data.timetable
            .Select(t => t.clinician)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(c => c)
            .ToList();
    }

    void RefreshDropdown(DropdownField dropdown, List<string> options)
    {
        var choices = new List<string> { "Select..." };
        choices.AddRange(options);
        dropdown.choices = choices;
        dropdown.value = "Select...";
    }

    void RefreshAllDropdowns(VisualElement root)
    {
        var clinicians = GetClinicians();
        RefreshDropdown(root.Q<DropdownField>("remove-clinician-dropdown"), clinicians);
        RefreshDropdown(root.Q<DropdownField>("unavail-clinician"), clinicians);
        RefreshDropdown(root.Q<DropdownField>("actual-clinician"), clinicians);
    }

    void ShowFeedback(VisualElement anchor, string message, bool success)
    {
        // Remove old feedback
        var old = anchor.parent.Q<Label>("feedback-label");
        if (old != null) anchor.parent.Remove(old);

        var label = new Label(message);
        label.name = "feedback-label";
        label.AddToClassList(success ? "feedback-ok" : "feedback-err");
        anchor.parent.Add(label);
    }
}
