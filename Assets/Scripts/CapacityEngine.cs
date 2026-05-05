using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using CapacityPlanner;

public static class CapacityEngine
{

    public static Dictionary<string, int> BuildRuleMap(List<SessionRule> rules)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var rule in rules)
            map[rule.sessionType] = rule.patientsPerSession;
        return map;
    }

    public static DateTime GetSessionDate(DateTime weekStart, DayOfWeek day)
    {

        int offset = ((int)day - (int)DayOfWeek.Monday + 7) % 7;
        return weekStart.AddDays(offset);
    }

    public static bool IsUnavailable(
        string clinician,
        DateTime date,
        List<Unavailability> unavailability)
    {
        return unavailability.Any(u =>
            string.Equals(u.clinician, clinician, StringComparison.OrdinalIgnoreCase)
            && u.CoversDate(date));
    }

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

        var sessions = timetable.Where(t =>
            string.Equals(t.clinician, clinician, StringComparison.OrdinalIgnoreCase));

        foreach (var session in sessions)
        {

            int patients = ruleMap.ContainsKey(session.sessionType)
                ? ruleMap[session.sessionType]
                : 0;

            planned += patients;

            DateTime sessionDate = GetSessionDate(weekStart, session.day);
            if (IsUnavailable(clinician, sessionDate, unavailability))
                lost += patients;
        }

        int adjusted = planned - lost;


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


    public static float CapacityVariance(int planned, int adjusted)
    {
        if (planned == 0) return 0f;
        return ((float)(adjusted - planned) / planned) * 100f;
    }


    public static float DeliveryVariance(int adjusted, int actual)
    {
        if (adjusted == 0) return 0f;
        return ((float)(actual - adjusted) / adjusted) * 100f;
    }

    public static ControlLimit GetLimits(string clinician, List<ControlLimit> limits)
    {
        var specific = limits.FirstOrDefault(l =>
            string.Equals(l.clinician, clinician, StringComparison.OrdinalIgnoreCase));
        if (specific != null) return specific;

        var global = limits.FirstOrDefault(l => l.clinician == null);
        if (global != null) return global;


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


    public static List<DashboardRow> BuildDashboard(
        DateTime weekStart,
        PlannerData data)
    {

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


            var actual = data.actuals.FirstOrDefault(a =>
                string.Equals(a.clinician, clinician, StringComparison.OrdinalIgnoreCase)
                && a.WeekStartDateTime == weekStart);

            int delivered = actual?.patientsDelivered ?? -1;
            float capVariance = CapacityVariance(cap.planned, cap.adjusted);
            float delVariance = delivered >= 0
                ? DeliveryVariance(cap.adjusted, delivered)
                : float.NaN;

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

public class CapacityResult
{
    public string clinician;
    public DateTime weekStart;
    public int planned;
    public int adjusted;
    public int lost;
    public int extra;     
    public int total =>    
        adjusted + extra;
}

public class DashboardRow
{
    public string clinician;
    public int planned;
    public int adjusted;
    public int actual;         
    public float capacityVariance; 
    public float deliveryVariance; 
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