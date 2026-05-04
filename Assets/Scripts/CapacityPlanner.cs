// DataModels.cs
// Place this file in Assets/Scripts/Models/
// No MonoBehaviour needed — these are pure data containers.

using System;
using System.Collections.Generic;

namespace CapacityPlanner
{
    // ─────────────────────────────────────────────
    // 1. TIMETABLE ENTRY — baseline weekly plan
    //    One row = one clinician session slot
    // ─────────────────────────────────────────────
    [Serializable]
    public class TimetableEntry
    {
        public string clinician;        // e.g. "Dr. Matthews"
        public DayOfWeek day;           // Mon–Sun (uses built-in C# enum)
        public SessionTime time;        // AM or PM
        public string sessionType;      // e.g. "Clinic", "Theatre", "Admin"

        public TimetableEntry() { }

        public TimetableEntry(string clinician, DayOfWeek day, SessionTime time, string sessionType)
        {
            this.clinician = clinician;
            this.day = day;
            this.time = time;
            this.sessionType = sessionType;
        }
    }

    // ─────────────────────────────────────────────
    // 2. SESSION RULE — capacity value per session type
    //    Drives all planned capacity calculations
    // ─────────────────────────────────────────────
    [Serializable]
    public class SessionRule
    {
        public string sessionType;          // Must match TimetableEntry.sessionType exactly
        public int patientsPerSession;   // e.g. Clinic = 8, Theatre = 4, Admin = 0

        public SessionRule() { }

        public SessionRule(string sessionType, int patientsPerSession)
        {
            this.sessionType = sessionType;
            this.patientsPerSession = patientsPerSession;
        }
    }

    // ─────────────────────────────────────────────
    // 3. UNAVAILABILITY — date-ranged override
    //    Zeros out any sessions falling in this range
    // ─────────────────────────────────────────────
    [Serializable]
    public class Unavailability
    {
        public string clinician;  // Must match TimetableEntry.clinician exactly
        public string startDate;  // ISO 8601: "2025-01-20"
        public string endDate;    // ISO 8601: "2025-01-24"
        public UnavailabilityReason reason; // Leave | Sick | OnCall

        // Parsed helpers — not serialized, computed on demand
        public DateTime StartDateTime => DateTime.Parse(startDate);
        public DateTime EndDateTime => DateTime.Parse(endDate);

        public Unavailability() { }

        public Unavailability(string clinician, string startDate, string endDate, UnavailabilityReason reason)
        {
            this.clinician = clinician;
            this.startDate = startDate;
            this.endDate = endDate;
            this.reason = reason;
        }

        /// <summary>Returns true if the given calendar date falls within this block.</summary>
        public bool CoversDate(DateTime date)
        {
            return date >= StartDateTime && date <= EndDateTime;
        }
    }

    // ─────────────────────────────────────────────
    // 4. ACTUAL ACTIVITY — real outcome per week
    //    Compared against adjusted capacity
    // ─────────────────────────────────────────────
    [Serializable]
    public class ActualActivity
    {
        public string clinician;          // Must match TimetableEntry.clinician exactly
        public string weekStart;          // ISO 8601: "2025-01-20" (always a Monday)
        public int patientsDelivered;  // Raw count entered by the user

        // Parsed helper
        public DateTime WeekStartDateTime => DateTime.Parse(weekStart);

        public ActualActivity() { }

        public ActualActivity(string clinician, string weekStart, int patientsDelivered)
        {
            this.clinician = clinician;
            this.weekStart = weekStart;
            this.patientsDelivered = patientsDelivered;
        }
    }
    [Serializable]
    public class ControlLimit
    {
        public string clinician;        // null = applies to everyone
        public float upperLimitPct;    // e.g. +10f  = flag if 10% over plan
        public float lowerAmberPct;    // e.g. -5f   = amber below this
        public float lowerRedPct;      // e.g. -10f  = red below this

        public ControlLimit() { }

        public ControlLimit(string clinician, float upper, float amber, float red)
        {
            this.clinician = clinician;
            this.upperLimitPct = upper;
            this.lowerAmberPct = amber;
            this.lowerRedPct = red;
        }
    }

    // ─────────────────────────────────────────────
    // 6. ADDITIONAL SESSION — extra sessions added
    //    outside the normal timetable to cover gaps
    // ─────────────────────────────────────────────
    [Serializable]
    public class AdditionalSession
    {
        public string clinician;
        public string date;           // ISO 8601: "2025-01-22"
        public SessionTime time;        // AM or PM
        public string sessionType;    // "Clinic", "Theatre" etc
        public string reason;         // e.g. "Covering Dr Daniels leave"
        public bool isExtra = true; // always true — marks it as additional

        public DateTime SessionDate => DateTime.Parse(date);

        public AdditionalSession() { }

        public AdditionalSession(string clinician, string date, SessionTime time,
                                 string sessionType, string reason)
        {
            this.clinician = clinician;
            this.date = date;
            this.time = time;
            this.sessionType = sessionType;
            this.reason = reason;
        }
    }

    // ─────────────────────────────────────────────
    // SUPPORTING ENUMS
    // ─────────────────────────────────────────────
    public enum SessionTime
    {
        AM,
        PM
    }

    public enum UnavailabilityReason
    {
        Leave,
        Sick,
        OnCall
    }

    // ─────────────────────────────────────────────
    // DATA STORE — single source of truth
    //    Attach to a persistent GameObject (e.g. "DataManager")
    //    All other scripts read from this, never store their own copies.
    // ─────────────────────────────────────────────
    [Serializable]
    public class PlannerData
    {
        public List<TimetableEntry> timetable = new List<TimetableEntry>();
        public List<SessionRule> sessionRules = new List<SessionRule>();
        public List<Unavailability> unavailability = new List<Unavailability>();
        public List<ActualActivity> actuals = new List<ActualActivity>();
        public List<ControlLimit> controlLimits = new List<ControlLimit>();
        public List<AdditionalSession> additionalSessions = new List<AdditionalSession>();


        /// <summary>Seed with sample data so you have something to work with immediately.</summary>
        public void LoadDefaults()
        {
            controlLimits = new List<ControlLimit>
            {
                new ControlLimit(null, 10f, -5f, -10f),
                
            }
            ;

            timetable = new List<TimetableEntry>
            {
                new TimetableEntry("Dr. Matthews",  DayOfWeek.Monday,    SessionTime.AM, "Clinic"),
                new TimetableEntry("Dr. Matthews",  DayOfWeek.Monday,    SessionTime.PM, "Theatre"),
                new TimetableEntry("Dr. Matthews",  DayOfWeek.Tuesday,   SessionTime.AM, "Clinic"),
                new TimetableEntry("Dr. Matthews",  DayOfWeek.Wednesday, SessionTime.AM, "Clinic"),
                new TimetableEntry("Dr. Matthews",  DayOfWeek.Thursday,  SessionTime.AM, "Clinic"),
                new TimetableEntry("Dr. Matthews",  DayOfWeek.Thursday,  SessionTime.PM, "Theatre"),
                new TimetableEntry("Dr. Matthews",  DayOfWeek.Friday,    SessionTime.AM, "Clinic"),

                new TimetableEntry("Dr. Jameson", DayOfWeek.Monday,    SessionTime.AM, "Clinic"),
                new TimetableEntry("Dr. Jameson", DayOfWeek.Monday,    SessionTime.PM, "Clinic"),
                new TimetableEntry("Dr. Jameson", DayOfWeek.Tuesday,   SessionTime.AM, "Theatre"),
                new TimetableEntry("Dr. Jameson", DayOfWeek.Wednesday, SessionTime.AM, "Clinic"),
                new TimetableEntry("Dr. Jameson", DayOfWeek.Friday,    SessionTime.AM, "Clinic"),

                new TimetableEntry("Dr. Daniels",   DayOfWeek.Monday,    SessionTime.AM, "Clinic"),
                new TimetableEntry("Dr. Daniels",   DayOfWeek.Tuesday,   SessionTime.AM, "Clinic"),
                new TimetableEntry("Dr. Daniels",   DayOfWeek.Tuesday,   SessionTime.PM, "Theatre"),
                new TimetableEntry("Dr. Daniels",   DayOfWeek.Friday,    SessionTime.AM, "Clinic"),
            };

            sessionRules = new List<SessionRule>
            {
                new SessionRule("Clinic",   8),
                new SessionRule("Theatre",  4),
                new SessionRule("Admin",    0),
                new SessionRule("On Call",  2),
                new SessionRule("Research", 0),
            };

            unavailability = new List<Unavailability>
            {
                new Unavailability("Dr. Daniels", "2025-01-20", "2025-01-24", UnavailabilityReason.Leave),
            };

            actuals = new List<ActualActivity>
            {
                new ActualActivity("Dr. Matthews",  "2025-01-20", 68),
                new ActualActivity("Dr. Jameson", "2025-01-20", 52),
                new ActualActivity("Dr. Daniels",   "2025-01-20", 10),
            };
        }
    }
}