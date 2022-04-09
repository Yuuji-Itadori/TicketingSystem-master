using System;
using System.ComponentModel.DataAnnotations;

namespace TicketingSystem.Models
{
    public class Ticket
    {
        public enum Type
        {
            PasswordReset = 0,
            AccessControl = 1,
            Procurement = 2,
            StandardService = 3,
            NonStandardService = 4,
            ITSupportService = 5
        }
        public enum Severity
        {
            None = 0,
            Low = 1,
            Medium = 2,
            High = 3,
            Critical = 4
        }
        public enum Status
        {
            Denied = -1,
            Unapproved = 0,
            Open = 1,
            Assigned = 2,
            Responded = 3,
            Resolved = 4
        }

        public static string GetTextForType(Type type)
        {
            var name = string.Empty;
            switch (type)
            {
                case Type.PasswordReset: name = "Password Reset Request"; break;
                case Type.AccessControl: name = "Access Control Request"; break;
                case Type.Procurement: name = "Procurement Request"; break;
                case Type.StandardService: name = "Standard Service Request"; break;
                case Type.NonStandardService: name = "Non-Standard Service Request"; break;
                case Type.ITSupportService: name = "IT Support Service Request"; break;
            }
            return name;
        }
        public static string GetTextForStatus(Status status)
            => Enum.GetName(typeof(Status), status);
        public static string GetTextForSeverity(Severity severity)
            => Enum.GetName(typeof(Severity), severity) + " incident";

        public string TextForType 
            => GetTextForType(RequestType);
        public string TextForSeverity
            => GetTextForSeverity(SeverityLevel);
        public string TextForStatus
            => GetTextForStatus(CurrentStatus);

        public int EstimatedResponseTimeInMinutes
        {
            get
            {
                int amount = 0;
                int baseTime = GetResponseTimeAmountWithUnit().timeAmount;
                if (RequestType == Type.ITSupportService)
                    switch (SeverityLevel)
                    {
                        case Severity.Low: amount = baseTime * 1440; break;
                        case Severity.Medium: amount = baseTime * 60; break;
                        case Severity.High:
                        case Severity.Critical: amount = baseTime; break;
                    }
                else
                    switch (RequestType)
                    {
                        case Type.PasswordReset: amount = baseTime; break;
                        case Type.AccessControl: amount = baseTime * 60; break;
                        case Type.Procurement: amount = baseTime * 1440; break;
                        case Type.StandardService: amount = baseTime * 60; break;
                        case Type.NonStandardService: amount = baseTime * 1440; break;
                    }
                return amount;
            }
        }
        public int EstimatedServiceRecoveryOrResolutionTimeInMinutes
        {
            get
            {
                int amount = 0;
                int baseTime = GetServiceRecoveryOrResolutionTimeAmountWithUnit().timeAmount;
                if (RequestType == Type.ITSupportService)
                    switch (SeverityLevel)
                    {
                        case Severity.Low: amount = baseTime * 1440; break;
                        case Severity.Medium: amount = baseTime * 1440; break;
                        case Severity.High: amount = baseTime * 60; break;
                        case Severity.Critical: amount = baseTime * 60; break;
                    }
                else
                    switch (RequestType)
                    {
                        case Type.PasswordReset: amount = baseTime * 60; break;
                        case Type.AccessControl: amount = baseTime * 1440; break;
                        case Type.Procurement: amount = baseTime * 1440; break;
                        case Type.StandardService: amount = baseTime * 1440; break;
                        case Type.NonStandardService: amount = baseTime * 1440; break;
                    }
                return amount;
            }
        }
        public DateTime EstimatedResponseDateTime
            => SubmissionDate.AddMinutes(EstimatedResponseTimeInMinutes);
        public DateTime EstimatedServiceRecoveryOrResolutionDateTime
            => SubmissionDate.AddMinutes(EstimatedServiceRecoveryOrResolutionTimeInMinutes);
        public (int timeAmount, string timeIncrement) GetResponseTimeAmountWithUnit()
        {
            const string days = "Days";
            const string hours = "Hours";
            const string minutes = "Minutes";

            (int, string) value = (0, string.Empty);
            if (RequestType == Type.ITSupportService)
                switch (SeverityLevel)
                {
                    case Severity.Low: value = (2, days); break;
                    case Severity.Medium: value = (8, hours); break;
                    case Severity.High:
                    case Severity.Critical: value = (15, minutes); break;
                }
            else
                switch (RequestType)
                {
                    case Type.PasswordReset: value = (60, minutes); break;
                    case Type.AccessControl: value = (8, hours); break;
                    case Type.Procurement: value = (5, days); break;
                    case Type.StandardService: value = (8, hours); break;
                    case Type.NonStandardService: value = (8, days); break;
                }
            return value;
        }     
        public (int timeAmount, string timeIncrement) GetServiceRecoveryOrResolutionTimeAmountWithUnit()
        {
            const string days = "Days";
            const string hours = "Hours";

            (int, string) value = (0, string.Empty);
            if (RequestType == Type.ITSupportService)
                switch (SeverityLevel)
                {
                    case Severity.Low: value = (10, days); break;
                    case Severity.Medium: value = (3, days); break;
                    case Severity.High: value = (8, hours); break;
                    case Severity.Critical: value = (6, hours); break;
                }
            else
                switch (RequestType)
                {
                    case Type.PasswordReset: value = (2, hours); break;
                    case Type.AccessControl: value = (2, days); break;
                    case Type.Procurement: value = (14, days); break;
                    case Type.StandardService: value = (3, days); break;
                    case Type.NonStandardService: value = (21, days); break;
                }
            return value;
        }
        public TimeSpan EstimatedTimeLeft
        {
            get
            {
                TimeSpan ts = EstimatedServiceRecoveryOrResolutionDateTime - DateTime.Now;
                if (ts < TimeSpan.Zero) ts = TimeSpan.Zero;
                return ts;
            }
        }

        /// <summary>
        /// Adds a message to the log for the ticket.
        /// </summary>
        /// <param name="userId">The identification number of the user updating the ticket.</param>
        /// <param name="username">The username of the user updating the ticket.</param>
        /// <param name="message">The update message to show in the log.</param>
        public void AddToLog(int userId, string username, string message)
            => Log += $"[{DateTime.Now:dd/MM/yy hh:mm:ss}] {username} ({userId}) : {message}\n";
        /// <summary>
        /// Adds a messahe to the log for the ticket.
        /// </summary>
        /// <param name="message">The message to add to the log.</param>
        public void AddToLog(string message)
            => Log += $"[{DateTime.Now:dd/MM/yy hh:mm:ss}] : {message}\n";

        [Key]
        public int ID { get; set; }

        [EnumDataType(typeof(Type))]
        public Type RequestType { get; set; }

        [EnumDataType(typeof(Status))]
        public Status CurrentStatus { get; set; }

        public int? ServiceID { get; set; }

        public int? LocationID { get; set; }

        [EnumDataType(typeof(Severity))]
        public Severity SeverityLevel { get; set; }

        public DateTime SubmissionDate { get; set; }

        public DateTime? CompletionDate { get; set; }

        public int ClientID { get; set; }

        public string Summary { get; set; }

        public string Details { get; set; }

        public string Notes { get; set; }

        public int? PrimaryUserID { get; set; }

        public int? CompletedByUserID { get; set; } 

        public DateTime? AssignmentDate { get; set; }

        public string Log { get; set; }
    }
}