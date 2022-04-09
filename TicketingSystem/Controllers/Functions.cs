using System;
using System.Linq;
using TicketingSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;


namespace TicketingSystem.Controllers
{
    public partial class HomeController : Controller
    {
        /// <summary>
        /// Sets the current username, and user type according to the HttpContext values.
        /// </summary>
        private void SetCommonViewData()
        {
            ViewData[UsernameKey] = HttpContext.Session.GetString(SessionKeyUsername);
            ViewData[UserTypeKey] = HttpContext.Session.GetInt32(SessionKeyUserType);
        }

        public bool IsCurrentUserValid()
        {
            string currentUser = HttpContext.Session.GetString(SessionKeyUsername);
            int? currentUserType = HttpContext.Session.GetInt32(SessionKeyUserType);

            bool areFieldsValid = !(string.IsNullOrEmpty(currentUser) || currentUserType == null);
            return areFieldsValid && (GetUserByExactName(currentUser) != null);
        }

        /// <summary>
        /// Indicates if an account with the specified username exists (Case insensitve).
        /// </summary>
        /// <param name="username">The username of the user.</param>
        public bool UserExists(string username)
            => (_context.Staff.Where(u => u.Name.Equals(username)).Count() > 0);

        /// <summary>
        /// Retrieves an instance of User, if one exists, based on the provided username (Case sensitive).
        /// </summary>
        /// <param name="username">The username of the user.</param>
        public Staff GetUserByExactName(string username)
            => _context.Staff.ToArray().Where(u =>
                u.Name.Equals(username, StringComparison.Ordinal)).FirstOrDefault();

        /// <summary>
        /// Retrieves an instance of User, if one exists, based on the provided username and passowrd (Case sensitive).
        /// </summary>
        /// <param name="username">The username of the user.</param>
        /// <param name="password">The encrypted password of the user.</param>
        public Staff GetUserFromLogin(string username, string password)
            => _context.Staff.ToArray().Where(u =>
                u.Name.Equals(username, StringComparison.Ordinal) &&
                u.Password.Equals(password, StringComparison.Ordinal)).FirstOrDefault();

        /// <summary>
        /// Gets the ID number of a User, if one exists, based on the provided username (Case sensitive). 
        /// </summary>
        /// <param name="username">The username of the user.</param>
        public int? GetUserIdFromExactName(string username)
            => GetUserByExactName(username)?.ID;


        /// <summary>
        /// Retrieves all tickets assigned to the user, either as primary or secondary, regardless of completion state.
        /// </summary>
        /// <param name="userId">The identification number of the user.</param>
        private IEnumerable<(Ticket ticket, string staffName)> GetTicketsAssignedForUser(int userId)
        {
            IEnumerable<(Ticket ticket, string staffName)> all = GetAllTicketsWithClientName();
            IEnumerable<(Ticket, string)> primary = all
                .Where(t => t.ticket.PrimaryUserID == userId);
            IEnumerable<(Ticket, string)> secondary = (from ids in _context.StaffToTickets.ToArray()
                                                        join a in all on ids.TicketID equals a.ticket.ID
                                                        where ids.UserID == userId
                                                        select (a));
            return primary.Union(secondary);
        }

        private Ticket GetTicketById(int TicketID)
            => (_context.Tickets.Where(t => t.ID == TicketID).FirstOrDefault());

        private string GetLocationNameForTicket(int ticketID)
            => (from t in _context.Tickets.ToArray()
                where t.ID == ticketID
                join l in _context.Locations on t.LocationID equals l.ID
                select (l.Name)).FirstOrDefault();

        private string GetServiceNameForTicket(int ticketID)
            => (from t in _context.Tickets.ToArray()
                where t.ID == ticketID
                join s in _context.Services on t.ServiceID equals s.ID
                select (s.Name)).FirstOrDefault();

        private string GetHandlerNameForTicket(int handlerID, int ticketID)
            => (from t in _context.Tickets.ToArray()
                where t.PrimaryUserID == handlerID && t.ID == ticketID
                join s in _context.Staff on t.PrimaryUserID equals s.ID
                select (s.Name)).FirstOrDefault();

        private string GetClientNameForTicket(int ticketID)
            => (from t in _context.Tickets.ToArray()
                where t.ID == ticketID
                join s in _context.Staff on t.ClientID equals s.ID
                select (s.Name)).FirstOrDefault();

        private IEnumerable<(Ticket ticket, string staffName)> GetAllTicketsWithClientName()
            => (from t in _context.Tickets.ToArray()
                join s in _context.Staff on t.ClientID equals s.ID
                select (t, s.Name)).OrderByDescending(x => x.t.SubmissionDate).Take(100);

        private IEnumerable<(Ticket ticket, string primaryHandlerName, string locationName, string serviceName)> GetSubmittedTicketForUserOfStatus(int submitterId, Ticket.Status status)
        {
            var collection = new List<(Ticket, string, string, string)>();
            var tix = GetUserSubmittedTickets(submitterId).Where(t => t.CurrentStatus == status);
            var locationName = string.Empty;
            var serviceName = string.Empty;

            foreach (Ticket ticket in tix)
            {
                locationName = GetLocationNameForTicket(ticket.ID);
                serviceName = GetServiceNameForTicket(ticket.ID);

                collection.Add((ticket,
                    (ticket.PrimaryUserID != null ? GetHandlerNameForTicket((int)ticket.PrimaryUserID, ticket.ID) : string.Empty),
                    (string.IsNullOrEmpty(locationName) ? string.Empty : locationName),
                    (string.IsNullOrEmpty(serviceName) ? string.Empty : serviceName)));
            }
            return collection;
        }

        /// <summary>
        /// Retrieves all tickets, for the specified department, with the specified status type.
        /// </summary>
        /// <param name="status">The status of the desired tickets.</param>
        private IEnumerable<(Ticket ticket, string clientName)> GetAllTicketsOfStatus(Ticket.Status status)
            => GetAllTicketsWithClientName().Where(d => d.ticket.CurrentStatus == status);

        /// <summary>
        /// Gets all of the Users who are Managers
        /// </summary>
        private Staff[] GetManagers()
            => _context.Staff.Where(u => u.CurrentType == Staff.Type.Manager).ToArray();

        /// <summary>
        /// Gets a manager from the provided User ID number.
        /// </summary>
        /// <param name="id">The identification number of the manager.</param>
        private Staff GetManagerById(int id)
            => GetManagers().Where(m => m.ID == id).FirstOrDefault();

        private IEnumerable<Ticket> GetUserSubmittedTickets(int submitterId)
            => _context.Tickets.Where(t => t.ClientID == submitterId).OrderByDescending(x => x.SubmissionDate).Take(100);


        private Ticket CreateTicket(Ticket.Type type, Ticket.Severity severity, Location location, Service service, int clientId, string summary, string details)
        {
            var ticket = new Ticket()
            {
                RequestType = type,
                CurrentStatus = ((type == Ticket.Type.AccessControl || type == Ticket.Type.NonStandardService) ? Ticket.Status.Unapproved : Ticket.Status.Open),
                SeverityLevel = (type == Ticket.Type.ITSupportService ? severity : Ticket.Severity.None),
                SubmissionDate = DateTime.Now,
                ClientID = clientId,
                Summary = summary,
                Details = details
            };

            if (type != Ticket.Type.Procurement) ticket.ServiceID = service?.ID;
            if (!(type == Ticket.Type.PasswordReset || type == Ticket.Type.AccessControl)) ticket.LocationID = location?.ID;

            return ticket;
        }

        public static string GetMonthNameFromIndex(int monthIndex)
        {
            string[] names = { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" };
            monthIndex--;

            if (monthIndex < 0) return names[0];
            else if (monthIndex > 11) return names[11];
            else return names[monthIndex];
        }
    }
}