using System;
using System.Collections.Generic;

namespace CapacityPlanner
{
    [Serializable]
    public class TimetableEntry
    {
        public string clinician;        
        public DayOfWeek day;           
        public SessionTime time;        
        public string sessionType;      

        public TimetableEntry() { }

        public TimetableEntry(string clinician, DayOfWeek day, SessionTime time, string sessionType)
        {
            this.clinician = clinician;
            this.day = day;
            this.time = time;
            this.sessionType = sessionType;
        }
    }


    [Serializable]
    public class SessionRule
    {
        public string sessionType;          
        public int patientsPerSession;  

        public SessionRule() { }

        public SessionRule(string sessionType, int patientsPerSession)
        {
            this.sessionType = sessionType;
            this.patientsPerSession = patientsPerSession;
        }
    }

    [Serializable]
    public class Unavailability
    {
        public string clinician; 
        public string startDate;  
        public string endDate;   
        public UnavailabilityReason reason; 


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

       
        public bool CoversDate(DateTime date)
        {
            return date >= StartDateTime && date <= EndDateTime;
        }
    }


    [Serializable]
    public class ActualActivity
    {
        public string clinician;          
        public string weekStart;        
        public int patientsDelivered;  
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
        public string clinician;       
        public float upperLimitPct;    
        public float lowerAmberPct;   
        public float lowerRedPct;     

        public ControlLimit() { }

        public ControlLimit(string clinician, float upper, float amber, float red)
        {
            this.clinician = clinician;
            this.upperLimitPct = upper;
            this.lowerAmberPct = amber;
            this.lowerRedPct = red;
        }
    }


    [Serializable]
    public class AdditionalSession
    {
        public string clinician;
        public string date;          
        public SessionTime time;       
        public string sessionType;    
        public string reason;        
        public bool isExtra = true;

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

    [Serializable]
    public class PlannerData
    {
        public List<TimetableEntry> timetable = new List<TimetableEntry>();
        public List<SessionRule> sessionRules = new List<SessionRule>();
        public List<Unavailability> unavailability = new List<Unavailability>();
        public List<ActualActivity> actuals = new List<ActualActivity>();
        public List<ControlLimit> controlLimits = new List<ControlLimit>();
        public List<AdditionalSession> additionalSessions = new List<AdditionalSession>();


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
                new ActualActivity("Dr. Matthews",  "2026-05-04", 68),
                new ActualActivity("Dr. Jameson", "2026-05-04", 52),
                new ActualActivity("Dr. Daniels",   "2026-05-04", 10),
            };
        }
    }
}