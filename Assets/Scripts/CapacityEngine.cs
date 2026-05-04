// CapacityEngine.cs
// Place in Assets/Scripts/
// Pure static class — no MonoBehaviour, no UI.
// Every method takes data in, returns numbers out.

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using CapacityPlanner;

public static class CapacityEngine
{
    // ─────────────────────────────────────────────
    // STEP 1 — Build a lookup: sessionType → patients
    // e.g. { "Clinic": 8, "Theatre": 4, "Admin": 0 }
    // ─────────────────────────────────────────────
    public static Dictionary<string, int> BuildRuleMap(List<SessionRule> rules)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var rule in rules)
            map[rule.sessionType] = rule.patientsPerSession;
        return map;
    }

    // ─────────────────────────────────────────────
    // STEP 2 — Work out the date of a timetable slot
    // e.g. "Dr Ahmed, Monday AM" in week of 2025-01-20
    //       → DateTime 2025-01-20
    // ─────────────────────────────────────────────
    public static DateTime GetSessionDate(DateTime weekStart, DayOfWeek day)
    {
        // weekStart is always a Monday
        // DayOfWeek.Monday = 1, Tuesday = 2 ... Sunday = 0
        int offset = ((int)day - (int)DayOfWeek.Monday + 7) % 7;
        return weekStart.AddDays(offset);
    }

    // ─────────────────────────────────────────────
    // STEP 3 — Is a clinician unavailable on a date?
    // ─────────────────────────────────────────────
    public static bool IsUnavailable(
        string clinician,
        DateTime date,
        List<Unavailability> unavailability)
    {
        return unavailability.Any(u =>
            string.Equals(u.clinician, clinician, StringComparison.OrdinalIgnoreCase)
            && u.CoversDate(date));
    }

    // ─────────────────────────────────────────────
    // STEP 4 — Calculate capacity for one clinician
    //          for one week
    // ─────────────────────────────────────────────
    public static CapacityResult Calculate(
       string clinician,
       DateTime weekStart,
       List<TimetableEntry> timetable,
       List<SessionRule> sessionRules,
       List<Unavailability> unavailability,
       List<AdditionalSession> additionalSessions = null)
    {
        additionalSessions = additionalSessions ?? new List<AdditionalSession>();
        var ruleMap = BuildRuleMap(sessionRules);

        int planned = 0;
        int lost = 0;

        // Look at every session this clinician has in the timetable
        var sessions = timetable.Where(t =>
            string.Equals(t.clinician, clinician, StringComparison.OrdinalIgnoreCase));

        foreach (var session in sessions)
        {
            // How many patients does this session type give us?
            int patients = ruleMap.ContainsKey(session.sessionType)
                ? ruleMap[session.sessionType]
                : 0;

            planned += patients;

            // Does leave cancel this session?
            DateTime sessionDate = GetSessionDate(weekStart, session.day);
            if (IsUnavailable(clinician, sessionDate, unavailability))
                lost += patients;
        }

        int adjusted = planned - lost;

        // Add any extra sessions for this clinician this week
        int extra = 0;
        var weekEnd = weekStart.AddDays(6);
        var extraRuleMap = BuildRuleMap(sessionRules);
        foreach (var s in additionalSessions)
        {
            if (!string.Equals(s.clinician, clinician, StringComparison.OrdinalIgnoreCase)) continue;
            if (s.SessionDate < weekStart || s.SessionDate > weekEnd) continue;
            extra += extraRuleMap.ContainsKey(s.sessionType) ? extraRuleMap[s.sessionType] : 0;
        }

        return new CapacityResult
        {
            clinician = clinician,
            weekStart = weekStart,
            planned = planned,
            adjusted = adjusted,
            lost = lost
        };
    }

    // ─────────────────────────────────────────────
    // STEP 5 — Variance calculations
    // ─────────────────────────────────────────────

    // How much capacity did we lose to leave? (negative = loss)
    public static float CapacityVariance(int planned, int adjusted)
    {
        if (planned == 0) return 0f;
        return ((float)(adjusted - planned) / planned) * 100f;
    }

    // How did actual delivery compare to adjusted capacity?
    public static float DeliveryVariance(int adjusted, int actual)
    {
        if (adjusted == 0) return 0f;
        return ((float)(actual - adjusted) / adjusted) * 100f;
    }

    // ─────────────────────────────────────────────
    // STEP 6 — Traffic light status
    // ─────────────────────────────────────────────
    public static ControlLimit GetLimits(string clinician, List<ControlLimit> limits)
    {
        var specific = limits.FirstOrDefault(l =>
            string.Equals(l.clinician, clinician, StringComparison.OrdinalIgnoreCase));
        if (specific != null) return specific;

        var global = limits.FirstOrDefault(l => l.clinician == null);
        if (global != null) return global;

        // Safety net if no limits defined
        return new ControlLimit(null, 10f, -5f, -10f);
    }

    public static CapacityStatus GetCapacityStatus(float variancePct, ControlLimit limits)
    {
        if (variancePct > limits.upperLimitPct) return CapacityStatus.AboveUpper;
        if (variancePct >= limits.lowerAmberPct) return CapacityStatus.Green;
        if (variancePct >= limits.lowerRedPct) return CapacityStatus.Amber;
        return CapacityStatus.Red;
    }

    public static CapacityStatus GetDeliveryStatus(float variancePct, ControlLimit limits)
    {
        if (variancePct > limits.upperLimitPct) return CapacityStatus.AboveUpper;
        if (variancePct >= limits.lowerAmberPct) return CapacityStatus.Green;
        if (variancePct >= limits.lowerRedPct) return CapacityStatus.Amber;
        return CapacityStatus.Red;
    }

    // ─────────────────────────────────────────────
    // STEP 7 — Run everything for ALL clinicians
    //          Returns one DashboardRow per clinician
    // ─────────────────────────────────────────────
    public static List<DashboardRow> BuildDashboard(
        DateTime weekStart,
        PlannerData data)
    {
        // Get unique clinician names from the timetable
        var clinicians = data.timetable
            .Select(t => t.clinician)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(c => c)
            .ToList();

        var rows = new List<DashboardRow>();

        foreach (var clinician in clinicians)
        {
            var cap = Calculate(
                clinician, weekStart,
                data.timetable, data.sessionRules, data.unavailability);

            // Find actual delivery for this clinician + week
            var actual = data.actuals.FirstOrDefault(a =>
                string.Equals(a.clinician, clinician, StringComparison.OrdinalIgnoreCase)
                && a.WeekStartDateTime == weekStart);

            int delivered = actual?.patientsDelivered ?? -1; // -1 = no data
            float capVariance = CapacityVariance(cap.planned, cap.adjusted);
            float delVariance = delivered >= 0
                ? DeliveryVariance(cap.adjusted, delivered)
                : float.NaN;

            // Look up the right limits for this clinician
            var limits = GetLimits(clinician, data.controlLimits);

            rows.Add(new DashboardRow
            {
                clinician = clinician,
                planned = cap.planned,
                adjusted = cap.adjusted,
                actual = delivered,
                capacityVariance = capVariance,
                deliveryVariance = delVariance,
                capacityStatus = GetCapacityStatus(capVariance, limits),
                deliveryStatus = delivered >= 0
                    ? GetDeliveryStatus(delVariance, limits)
                    : CapacityStatus.NoData
            });
        }

        return rows;
    }
}

// ─────────────────────────────────────────────
// RESULT TYPES
// These are simple containers for output data.
// Add these at the bottom of the same file.
// ─────────────────────────────────────────────

public class CapacityResult
{
    public string clinician;
    public DateTime weekStart;
    public int planned;
    public int adjusted;
    public int lost;
    public int extra;      // patients from additional sessions
    public int total =>    // adjusted + extra = real available capacity
        adjusted + extra;
}

public class DashboardRow
{
    public string clinician;
    public int planned;
    public int adjusted;
    public int actual;           // -1 if no data entered
    public float capacityVariance; // negative = loss
    public float deliveryVariance; // NaN if no actual data
    public CapacityStatus capacityStatus;
    public CapacityStatus deliveryStatus;
}

public enum CapacityStatus
{
    AboveUpper,
    Green,
    Amber,
    Red,
    NoData
}