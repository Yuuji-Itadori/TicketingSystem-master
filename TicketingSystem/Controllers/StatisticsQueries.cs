using System;
using System.Text;
using System.Linq;
using TicketingSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;


namespace TicketingSystem.Controllers
{
    public partial class HomeController : Controller
    {
        #region "Old"
        /// <summary>
        /// Gets the average completion time for the given ticket type.
        /// </summary>
        /// <param name="ticketType">The ticket type to query.</param>
        /// <param name="dateOfEarliest">Optional earliest date of ticket submission to query from.</param>
        /// <param name="ticketHandlerId">Optional ticket handler identification number. All used if none provided.</param>
        /// <returns>Average timespan for completion of the given ticket type.</returns>
        private TimeSpan GetAverageTicketCompletionTime(Ticket.Type ticketType, DateTime? dateOfEarliest = null, int? ticketHandlerId = null)
        {
            IEnumerable<Ticket> tickets = GetAllTicketsOfStatus(Ticket.Status.Resolved)
                .Select(t => t.ticket)
                .Where(t => t.RequestType == ticketType);

            if (dateOfEarliest != null) tickets = tickets.Where(t => t.SubmissionDate >= dateOfEarliest);
            if (ticketHandlerId != null)
            {
                var id = (int)ticketHandlerId;
                tickets = from t in tickets
                          join su in _context.StaffToTickets on t.ID equals su.TicketID
                          into allTickets
                          from sua in allTickets.DefaultIfEmpty()
                          where t.PrimaryUserID == id || sua.UserID == id
                          select (t);
            }

            IEnumerable<double> spansInSeconds = tickets.Select(t => ((DateTime)t.CompletionDate - t.SubmissionDate).TotalSeconds);
            if (spansInSeconds.Count() > 0)
                return TimeSpan.FromSeconds(spansInSeconds.Average());
            else
                return TimeSpan.MinValue;
        }

        /// <summary>
        /// Gets the count of late tickets for each month, for the current year.
        /// </summary>
        /// <param name="ticketType">The type of ticket to query against.</param>
        /// <param name="ticketHandlerId">Optional ticket handler identification number. All used if none provided.</param>
        /// <returns>Collection of tuples containing the month number, and the quantity of late tickets for the month.</returns>
        private IEnumerable<(int Month, int LateCount)> GetLateCountForEachMonth(Ticket.Type ticketType, int? ticketHandlerId = null)
        {
            IEnumerable<Ticket> completedLateTickets = GetAllTicketsOfStatus(Ticket.Status.Resolved)
                .Select(t => t.ticket)
                .Where(t => t.RequestType == ticketType
                    && (DateTime)t.CompletionDate > t.EstimatedServiceRecoveryOrResolutionDateTime
                    && t.CompletionDate.Value.Year == DateTime.Now.Year);

            if (ticketHandlerId != null)
            {
                var id = (int)ticketHandlerId;
                completedLateTickets = from t in completedLateTickets
                                       join su in _context.StaffToTickets on t.ID equals su.TicketID
                                       into allTickets
                                       from sua in allTickets.DefaultIfEmpty()
                                       where t.PrimaryUserID == id || sua.UserID == id
                                       select (t);
            }

            var stats = new List<(int, int)>();
            for (int m = 1; m <= 12; m++)
                stats.Add((m, completedLateTickets.Where(t => ((DateTime)t.CompletionDate).Month == m).Count()));

            return stats;
        }

        /// <summary>
        /// Gets the staff with the most late tickets.
        /// </summary>
        /// <param name="ticketType">The ticket type to query against.</param>
        /// <param name="dateOfEarliest">Optional earliest submission date to query against.</param>
        /// <param name="ticketHandlerId">Optional ticket handler identification number.</param>
        /// <returns>Collection of staff with the most late tickets. One if handlerId provided, otherwise maximum 10, if not.</returns>
        private IEnumerable<(int StaffId, int LateCount)> GetStaffWithMostLates(Ticket.Type ticketType, DateTime? dateOfEarliest = null, int? ticketHandlerId = null)
        {
            bool handlerProvided = (ticketHandlerId != null);
            IEnumerable<Ticket> completedLateTickets = GetAllTicketsOfStatus(Ticket.Status.Resolved)
                .Select(a => a.ticket)
                .Where(t => t.RequestType == ticketType
                    && t.CompletionDate > t.EstimatedResponseDateTime);

            if (dateOfEarliest != null) completedLateTickets = completedLateTickets.Where(t => t.SubmissionDate >= dateOfEarliest);
            IEnumerable<(int primaryUserId, int secondaryUserId)> ids = from t in completedLateTickets
                                                                        join su in _context.StaffToTickets on t.ID equals su.TicketID
                                                                        into allTickets
                                                                        from sua in allTickets.DefaultIfEmpty()
                                                                        select ((int)t.PrimaryUserID, sua.UserID);
            if (handlerProvided) ids = ids.Where(i => i.primaryUserId == ticketHandlerId || i.secondaryUserId == ticketHandlerId);

            var stats = new List<(int staffId, int lateCount)>();
            foreach (int id in ids.Select(i => i.primaryUserId).Concat(ids.Select(i => i.secondaryUserId).Distinct()))
                stats.Add((id, ids.Where(i => i.primaryUserId == id || i.secondaryUserId == id).Count()));
            stats.OrderBy(s => s.lateCount);

            if (stats.Count() > 10)
                return stats.GetRange(0, 10);
            else
                return stats;
        }
        #endregion



        /// <summary>
        /// Gets the first day of the month a year ago, excluding the month equal to the current month.
        /// E.g. 15/04/2021 yeilds 01/05/2020. Time is always 00:00:00
        /// </summary>
        private DateTime GetLastTwelveMonthStart()
        {
            var now = DateTime.Now;
            var startDate = new DateTime(now.Year - 1, now.Month, 1).AddMonths(1);
            return startDate;
        }

        /// <summary>
        /// Gets the first day of the month as a DateTime. Time is always 00:00:00
        /// </summary>
        private DateTime GetFirstDayOfMonth()
        {
            var now = DateTime.Now;
            return new DateTime(now.Year, now.Month, 1);
        }

        /// <summary>
        /// Gets the DateTime of the first day of the current week. Time is always 00:00:00
        /// </summary>
        private DateTime GetStartOfWeek()
        {
            var now = DateTime.Now;
            var startOfWeek = now.AddDays(-((int)now.DayOfWeek));
            startOfWeek = new DateTime(startOfWeek.Year, startOfWeek.Month, startOfWeek.Day);
            return startOfWeek;
        }



        /// <returns>The total quantity of all tickets.</returns>
        private int TotalTicketQuantity() 
            => _context.Tickets.Count();

        /// <returns>The quantity of tickets that are currently active.</returns>
        private int TotalActiveTickets()
            => _context.Tickets.Where(t => t.CurrentStatus > Ticket.Status.Denied && 
                                           t.CurrentStatus < Ticket.Status.Resolved)
                               .Count();

        /// <returns>The quantity of tickets that were completed within the past week.</returns>
        private int TotalCompletedTicketsThisWeek()
            => _context.Tickets.Where(t => t.CompletedByUserID != null)
                               .Where(t => (DateTime)t.CompletionDate >= GetStartOfWeek())
                               .Count();

        /// <returns>Total amount of tickets completed.</returns>
        private int TotalCompletedTickets() 
            => _context.Tickets.Where(t => t.CurrentStatus == Ticket.Status.Resolved).Count();


        /// <param name="onlyHighSeverity">Filter results to include only tickets with high severity.</param>
        /// <returns>Collection of ticket types, with the quantity of tickets for that type.</returns>
        private IEnumerable<(Ticket.Type TicketType, int Quantity)> QuantityPerType(bool onlyHighSeverity)
        {
            int QuantityOfTicketsForType(Ticket.Type ticketType)
                => _context.Tickets.Where(t => t.RequestType == ticketType &&
                                               (onlyHighSeverity ? t.SeverityLevel >= Ticket.Severity.High : true))
                                   .Count();

            var stats = new List<(Ticket.Type, int)>();
            for (int i = 0; i <= (int)Ticket.Type.ITSupportService; i++)
            {
                var type = (Ticket.Type)i;
                stats.Add((type, QuantityOfTicketsForType(type)));
            }
            return stats;
        }

        /// <param name="onlyHighSeverity">Filter results to include only tickets with high severity.</param>
        /// <returns>A collection of ticket types, with the average time in hours taken to complete.</returns>
        private IEnumerable<(Ticket.Type TicketType, double AverageCompletionTime)> AverageCompletionTimePerType(bool onlyHighSeverity)
        {
            double AverageCompletionTime(Ticket.Type ticketType)
                => TimeSpan.FromSeconds(_context.Tickets.Where(t => t.RequestType == ticketType &&
                                                                    t.CompletedByUserID != null &&
                                                                    (onlyHighSeverity ? t.SeverityLevel >= Ticket.Severity.High : true))
                                                        .ToArray()
                                                        .Select(t => ((DateTime)t.CompletionDate - t.SubmissionDate).TotalSeconds)
                                                        .DefaultIfEmpty()
                                                        .Average()).TotalHours;

            var stats = new List<(Ticket.Type, double)>();
            for (int i = 0; i <= (int)Ticket.Type.ITSupportService; i++)
            {
                var type = (Ticket.Type)i;
                stats.Add((type, AverageCompletionTime(type)));
            }
            return stats;
        }
    
        /// <param name="onlyHighSeverity">Filter results to include only tickets with high severity.</param>
        /// <returns>A collection of months, with the quantity of tickets received.</returns>
        private IEnumerable<(int Month, int TicketCount)> ReceivedTicketsPerMonth(bool onlyHighSeverity)
        {
            DateTime startDateOfLastYear = GetLastTwelveMonthStart();

            int TicketCountForMonth(int month)
                => _context.Tickets.Where(t => t.SubmissionDate.Month == month &&
                                               t.SubmissionDate >= startDateOfLastYear &&
                                               (onlyHighSeverity ? t.SeverityLevel >= Ticket.Severity.High : true))
                                   .Count();

            var stats = new List<(int, int)>();
            for (int i = startDateOfLastYear.Month; i != DateTime.Now.Month; i++)
            {
                i = (i > 12 ? 1 : i);
                stats.Add((i, TicketCountForMonth(i)));
            }
            int currentMonth = DateTime.Now.Month;
            stats.Add((currentMonth, TicketCountForMonth(currentMonth)));
            return stats;
        }

        /// <returns>The average time in hours of tickets being unassigned, per type.</returns>
        private IEnumerable<(Ticket.Type TicketType, double AverageTimeUnassigned)> AverageTicketUnassignedTimePerType()
        {
            double AverageForType(Ticket.Type ticketType)
                => TimeSpan.FromSeconds(_context.Tickets.Where(t => t.CurrentStatus > Ticket.Status.Open &&
                                                                    t.RequestType == ticketType &&
                                                                    t.PrimaryUserID != null)
                                                        .ToArray()
                                                        .Select(t => ((DateTime)t.AssignmentDate - t.SubmissionDate).TotalSeconds)
                                                        .DefaultIfEmpty()
                                                        .Average()).TotalHours;

            var stats = new List<(Ticket.Type, double)>();
            for (int i = 0; i <= (int)Ticket.Type.ITSupportService; i++)
            {
                var type = (Ticket.Type)i;
                stats.Add((type, AverageForType(type)));
            }
            return stats;
        }

        /// <returns>A collection of weekdays and the quantity completed.</returns>
        private IEnumerable<(DayOfWeek, int Count)> TotalCompletedTicketsThisWeekPerDay()
        {
            DateTime firstDayOfWeek = GetStartOfWeek();
            var stats = new List<(DayOfWeek, int)>();

            for (int i = 0; i <= 6; i++)
            {
                var day = (DayOfWeek)i;
                int count = _context.Tickets.Where(t => t.CompletedByUserID != null)
                                            .ToArray()
                                            .Select(t => (DateTime)t.CompletionDate)
                                            .Where(cd => cd >= firstDayOfWeek &&
                                                cd.DayOfWeek == day)
                                            .Count();
                stats.Add((day, count));
            }

            return stats;
        }

        /// <returns>A collection of months and the count of tickets late.</returns>
        private IEnumerable<(int Month, int TicketCount)> GetLateTicketsPerMonth()
        {
            DateTime startDateOfLastYear = GetLastTwelveMonthStart();

            int LateTicketCountForMonth(int month)
                => _context.Tickets.Where(t => t.SubmissionDate.Month == month &&
                                               t.SubmissionDate >= startDateOfLastYear &&
                                               (t.CompletedByUserID == null ? 
                                                    t.EstimatedServiceRecoveryOrResolutionDateTime < DateTime.Now : 
                                                    t.EstimatedServiceRecoveryOrResolutionDateTime < (DateTime)t.CompletionDate))
                                   .Count();

            var stats = new List<(int, int)>();
            for (int i = startDateOfLastYear.Month; i != DateTime.Now.Month; i++)
            {
                i = (i > 12 ? 1 : i);
                stats.Add((i, LateTicketCountForMonth(i)));
            }
            int currentMonth = DateTime.Now.Month;
            stats.Add((currentMonth, LateTicketCountForMonth(currentMonth)));
            return stats;
        }


        /// <returns>A collection of severity levels, and the quantity for the severity for the past month.</returns>
        private IEnumerable<(Ticket.Severity SeverityLevel, int Quantity)> TotalCountPerSeverityLevelForMonth()
        {
            DateTime firstDayOfMonth = GetFirstDayOfMonth();
            var stats = new List<(Ticket.Severity, int)>();

            for (int i = 1; i <= (int)Ticket.Severity.Critical; i++)
            {
                var severity = (Ticket.Severity)i;
                int count = _context.Tickets.Where(t => t.SeverityLevel == severity &&
                                                        t.SubmissionDate >= firstDayOfMonth)
                                            .Count();
                stats.Add((severity, count));
            }
            return stats;
        }

        /// <returns>The average time to complete tickets, per level of severity.</returns>
        private IEnumerable<(Ticket.Severity SeverityLevel, double AverageCompletionTimeInHours)> AverageCompletionTimePerSeverity()
        {
            double AverageCompletionTime(Ticket.Severity ticketSeverity)
                => TimeSpan.FromSeconds(_context.Tickets.Where(t => t.SeverityLevel == ticketSeverity &&
                                                                    t.CompletedByUserID != null)
                                                        .ToArray()
                                                        .Select(t => ((DateTime)t.CompletionDate - t.SubmissionDate).TotalSeconds)
                                                        .DefaultIfEmpty()
                                                        .Average()).TotalHours;

            var stats = new List<(Ticket.Severity, double)>();
            for (int i = 0; i <= (int)Ticket.Severity.Critical; i++)
            {
                var severity = (Ticket.Severity)i;
                stats.Add((severity, AverageCompletionTime(severity)));
            }
            return stats;
        }

        /// <returns>The average time a ticket is left unassigned, per level of severity.</returns>
        private IEnumerable<(Ticket.Severity SeverityLevel, double AverageTimeUnassignedInHours)> AverageTimeUnassignedPerSeverity()
        {
            double AverageForLevel(Ticket.Severity ticketSeverity)
                => TimeSpan.FromSeconds(_context.Tickets.Where(t => t.CurrentStatus > Ticket.Status.Open &&
                                                                    t.SeverityLevel == ticketSeverity &&
                                                                    t.PrimaryUserID != null)
                                                        .ToArray()
                                                        .Select(t => ((DateTime)t.AssignmentDate - t.SubmissionDate).TotalSeconds)
                                                        .DefaultIfEmpty()
                                                        .Average()).TotalHours;

            var stats = new List<(Ticket.Severity, double)>();
            for (int i = 0; i <= (int)Ticket.Severity.Critical; i++)
            {
                var severity = (Ticket.Severity)i;
                stats.Add((severity, AverageForLevel(severity)));
            }
            return stats;
        }


        #region "Pages"
        public IActionResult Statistics()
        {
            if (!IsCurrentUserValid()) return RedirectToAction("Login");

            SetCommonViewData();
            return View();
        }

        public IActionResult StatisticsSlideOne()
        {          
            ViewData["ReceivedPerMonth"] = ReceivedTicketsPerMonth(onlyHighSeverity: false);
            ViewData["QuantityPerType"] = QuantityPerType(onlyHighSeverity: false);
            ViewData["CompletedThisWeek"] = TotalCompletedTicketsThisWeekPerDay();
            ViewData["AvgCompletionPerType"] = AverageCompletionTimePerType(onlyHighSeverity: false);
            ViewData["AvgUnassigned"] = AverageTicketUnassignedTimePerType();

            return View();
        }

        public IActionResult StatisticsSlideTwo()
        {
            ViewData["HighSeverityPerMonth"] = ReceivedTicketsPerMonth(onlyHighSeverity: true);
            ViewData["HighSeverityCountPerType"] = QuantityPerType(onlyHighSeverity: true);
            ViewData["HighSeverityAvgCompletionTimePerType"] = AverageCompletionTimePerType(onlyHighSeverity: true);
            ViewData["AverageCompletionTimePerSeverity"] = AverageCompletionTimePerSeverity();
            ViewData["AverageUnassignedTimeInHoursPerSeverity"] = AverageTimeUnassignedPerSeverity();

            return View();
        }

        public IActionResult StatisticsSlideThree() => View();
        #endregion
    }
}