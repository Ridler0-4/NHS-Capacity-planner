using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using CapacityPlanner;

public class DashboardController : MonoBehaviour
{
    private DateTime _weekStart = new DateTime(2025, 1, 20);

    private UIDocument _doc;
    private PlannerData _data;

    void Start()
    {
        _doc = GetComponent<UIDocument>();
        _data = DataManager.Instance.Data;
        PlannerEvents.OnDataChanged += () =>
        {
            if (gameObject.activeInHierarchy)
                BuildDashboard();
        };
        // OnEnable handles the actual build
    }

    void OnDestroy()
    {
        // Always unsubscribe to avoid memory leaks
        PlannerEvents.OnDataChanged -= BuildDashboard;
    }

    void BuildDashboard()
    {
        var root = _doc.rootVisualElement;

        root.style.position = Position.Absolute;
        root.style.top = 0;
        root.style.left = 0;
        root.style.right = 0;
        root.style.bottom = 0;

        root.Q<Label>("week-label").text =
            "Week of " + _weekStart.ToString("dd MMM yyyy");

        var rows = CapacityEngine.BuildDashboard(_weekStart, _data);

        BuildMetricStrip(root, rows);
        BuildTable(root, rows);
    }

    void BuildMetricStrip(VisualElement root, List<DashboardRow> rows)
    {
        var strip = root.Q<VisualElement>("metric-strip");
        strip.Clear();

        int totalPlanned = 0;
        int totalAdjusted = 0;
        int totalActual = 0;
        int alerts = 0;

        foreach (var r in rows)
        {
            totalPlanned += r.planned;
            totalAdjusted += r.adjusted;
            if (r.actual >= 0) totalActual += r.actual;
            if (r.capacityStatus == CapacityStatus.Red ||
                r.deliveryStatus == CapacityStatus.Red) alerts++;
        }

        strip.Add(MakeMetricCard("Planned", totalPlanned.ToString(), "patients"));
        strip.Add(MakeMetricCard("Adjusted", totalAdjusted.ToString(), "after leave"));
        strip.Add(MakeMetricCard("Delivered", totalActual.ToString(), "patients"));
        strip.Add(MakeMetricCard("Alerts", alerts.ToString(),
            alerts > 0 ? "red status" : "all clear"));
    }

    VisualElement MakeMetricCard(string label, string value, string sub)
    {
        var card = new VisualElement();
        card.AddToClassList("metric-card");

        var lbl = new Label(label);
        lbl.AddToClassList("metric-label");

        var val = new Label(value);
        val.AddToClassList("metric-value");

        var subLbl = new Label(sub);
        subLbl.AddToClassList("metric-sub");

        card.Add(lbl);
        card.Add(val);
        card.Add(subLbl);
        return card;
    }

    void BuildTable(VisualElement root, List<DashboardRow> rows)
    {
        var body = root.Q<VisualElement>("table-body");
        body.Clear();

        foreach (var row in rows)
            body.Add(BuildTableRow(row));
    }

    VisualElement BuildTableRow(DashboardRow row)
    {
        var tr = new VisualElement();
        tr.AddToClassList("table-row");

        var name = new Label(row.clinician);
        name.AddToClassList("col-name");
        tr.Add(name);

        tr.Add(NumCell(row.planned.ToString()));
        tr.Add(NumCell(row.adjusted.ToString()));
        tr.Add(NumCell(FormatVariance(row.capacityVariance)));
        tr.Add(BadgeCell(row.capacityStatus));
        tr.Add(NumCell(row.actual >= 0 ? row.actual.ToString() : "-"));
        tr.Add(NumCell(float.IsNaN(row.deliveryVariance)
            ? "-" : FormatVariance(row.deliveryVariance)));
        tr.Add(BadgeCell(row.deliveryStatus));

        return tr;
    }

    VisualElement NumCell(string text)
    {
        var cell = new VisualElement();
        cell.AddToClassList("col-num");
        cell.Add(new Label(text));
        return cell;
    }

    VisualElement BadgeCell(CapacityStatus status)
    {
        var cell = new VisualElement();
        cell.AddToClassList("col-badge");

        var badge = new Label();
        badge.AddToClassList("badge");

        switch (status)
        {
            case CapacityStatus.Green:
                badge.text = "Green";
                badge.AddToClassList("badge-green");
                break;
            case CapacityStatus.Amber:
                badge.text = "Amber";
                badge.AddToClassList("badge-amber");
                break;
            case CapacityStatus.Red:
                badge.text = "Red";
                badge.AddToClassList("badge-red");
                break;
            case CapacityStatus.AboveUpper:
                badge.text = "Above limit";
                badge.AddToClassList("badge-above");
                break;
            default:
                badge.text = "No data";
                badge.AddToClassList("badge-nodata");
                break;
        }

        cell.Add(badge);
        return cell;
    }

    string FormatVariance(float v)
    {
        if (float.IsNaN(v)) return "-";
        return (v >= 0 ? "+" : "") + v.ToString("F1") + "%";
    }
    void OnEnable()
    {
        if (DataManager.Instance != null)
            BuildDashboard();
    }

}