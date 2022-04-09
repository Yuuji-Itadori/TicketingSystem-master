using System;
using System.Text;
using System.Linq;
using TicketingSystem.Data;
using TicketingSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;


namespace TicketingSystem.Controllers
{
    public partial class HomeController : Controller
    {
        #region "ViewDataKeys"
        public static readonly string TitleKey = "Title";
        public static readonly string LoginErrorMessageKey = "LoginErrorMessage";
        public static readonly string SignupErrorMessageKey = "SignupErrorMessage";
        public static readonly string LoadRightClassKey = "LoadRight";
        public static readonly string TicketResponseTimeKey = "TicketResponseTime";

        public static readonly string TicketTypeNameKey = "TicketTypeName";
        public static readonly string TicketSummaryKey = "TicketSummary";
        public static readonly string TicketSeverityKey = "TicketSeverity";
        public static readonly string TicketServiceKey = "TicketService";
        public static readonly string TicketNotesKey = "TicketNotes";
        public static readonly string TicketLocationKey = "TicketLocation";
        public static readonly string TicketIDKey = "TicketID";
        public static readonly string TicketDetailsKey = "TicketDetails";
        public static readonly string TicketClientNameKey = "TicketClientName";
        public static readonly string TicketStatus = "TicketStatus";
        public static readonly string TicketLogKey = "TicketLog";

        public static readonly string UsernameKey = "Username";
        public static readonly string UserTypeKey = "UserType";
        public static readonly string IsPrimaryUserKey = "IsPrimaryUser";
        public static readonly string IsSecondaryUserKey = "IsSecondaryUser";

        public static readonly string OpenTicketsKey = "OpenTickets";
        public static readonly string InProgressTicketsKey = "InProgressTickets";
        public static readonly string UserTicketsKey = "UserTickets";
        public static readonly string ClosedTicketsKey = "ClosedTickets";
        public static readonly string AllTicketsKey = "AllTickets";

        public static readonly string AllManagersKey = "AllManagers";
        public static readonly string SelectedTicketTypeKey = "SelectedTicketTypeId";
        public static readonly string AllServicesKey = "AllServices";
        public static readonly string AllLocationsKey = "AllLocations";

        public static readonly string ServiceNameKey = "ServiceName";
        public static readonly string LocationNameKey = "LocationName";
        public static readonly string HandlerNameKey = "HandlerName";

        public static readonly string UnaprovedTicketsKey = "unaprovedTickets";

        public static readonly string RespondedTicketsKey = "Responded";
        public static readonly string AssignedTicketsKey = "Assigned";
        public static readonly string ResolvedTicketsKey = "Resolved";
        public static readonly string activeTabKey = "activeTab";

        public static readonly string TicketStatusIdKey = "ticketStatusId";
        #endregion



        #region "Login"
        public IActionResult Login()
        {
            ViewData[LoginErrorMessageKey] = TempData[LoginErrorMessageKey];
            ViewData[SignupErrorMessageKey] = TempData[SignupErrorMessageKey];
            ViewData[LoadRightClassKey] = (TempData[SignupErrorMessageKey] != null);

            return View();
        }

        [HttpPost]
        public IActionResult Login(string username, string password)
        {
            ViewData[LoadRightClassKey] = false;
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ViewData[LoginErrorMessageKey] = "Username and or password, not provided";
                return View();
            }

            password = Crypto.Encrypt(password);
            Staff user = GetUserFromLogin(username, password);

            if (user == null)
            {
                ViewData[LoginErrorMessageKey] = "The username and or password is incorrect";
                return View();
            }

            HttpContext.Session.SetString(SessionKeyUsername, username);
            HttpContext.Session.SetInt32(SessionKeyUserType, (int)user.CurrentType);

            if (user.EmailAddress == null && user.ContactNumber == null) return RedirectToAction("ContactDetails");

            string action;
            switch (user.CurrentType)
            {
                case Staff.Type.General: action = "TicketTypePicker"; break;
                case Staff.Type.Handler: action = "TicketHandler"; break;
                case Staff.Type.Manager: action = "TicketTypePicker"; break; // May change to approvals or statistics
                default: return View();
            }
            return RedirectToAction(action);
        }
        #endregion



        #region "Signup"
        [HttpPost]
        public IActionResult Signup(string username, string password, string confirmPassword)
        {
            // Return if field not provided.
            foreach (string value in new string[] { username, password, confirmPassword })
                if (string.IsNullOrEmpty(value))
                {
                    TempData[SignupErrorMessageKey] = "Username and or password, not provided";
                    return RedirectToAction("Login");
                }

            // Return if password and confirmation password are not equal, or if the specified username is taken.
            bool userExist = UserExists(username);
            bool noPassMatch = !password.Equals(confirmPassword);
            if (userExist || noPassMatch)
            {
                if (userExist) TempData[SignupErrorMessageKey] = "The username is already taken";
                if (noPassMatch) TempData[SignupErrorMessageKey] = "The entered passwords, do not match";
                return RedirectToAction("Login");
            }

            var userAccess = Staff.Type.General;
            password = Crypto.Encrypt(password);
            _context.Staff.Add(new Staff()
            {
                Name = username,
                Password = password,
                //CurrentBranch = Staff.Branch.Unset,
                //CurrentDepartment = Staff.Department.Unset,
                CurrentType = userAccess
            });
            _context.SaveChanges();

            HttpContext.Session.SetString(SessionKeyUsername, username);
            HttpContext.Session.SetInt32(SessionKeyUserType, (int)userAccess);
            return RedirectToAction("ContactDetails");
        }

        public IActionResult FinalizeAccount() => throw new NotImplementedException();
        #endregion



        public IActionResult ContactDetails()
        {
            if (!IsCurrentUserValid()) return RedirectToAction("Login");
            SetCommonViewData();
            return View();
        }

        [HttpPost]
        public IActionResult UpdateStaffContactDetails(string phoneNum, string email)
        {
            string username = HttpContext.Session.GetString(SessionKeyUsername);
            Staff user = GetUserByExactName(username);
            user.EmailAddress = email;
            user.ContactNumber = phoneNum;
            _context.SaveChanges();

            string action;
            switch (user.CurrentType)
            {
                case Staff.Type.General: action = "TicketTypePicker"; break;
                case Staff.Type.Handler: action = "TicketHandler"; break;
                case Staff.Type.Manager: action = "TicketTypePicker"; break; // May change to approvals or statistics
                default: action = "Login"; break;
            }
            return RedirectToAction(action);
        }

        public IActionResult Logout()
        {
            HttpContext.Session.SetString(SessionKeyUsername, string.Empty);
            HttpContext.Session.SetInt32(SessionKeyUserType, (int)Staff.Type.None);
            return RedirectToAction("Login");
        }



        #region "Ticket Handling"
        public IActionResult TicketHandler()
        {
            if (!IsCurrentUserValid()) return RedirectToAction("Login");

            string username = HttpContext.Session.GetString(SessionKeyUsername);
            int userId = GetUserByExactName(username).ID;

            ViewData[OpenTicketsKey] = GetAllTicketsOfStatus(Ticket.Status.Open);
            ViewData[UserTicketsKey] = GetTicketsAssignedForUser(userId);
            ViewData[AllTicketsKey] = GetAllTicketsWithClientName().Where(t => t.ticket.CurrentStatus != Ticket.Status.Unapproved);

            SetCommonViewData();
            return View();
        }

        public IActionResult TicketDetails(int ticketId)
        {
            if (!IsCurrentUserValid()) return RedirectToAction("Login");

            Ticket ticket = GetTicketById(ticketId);
            bool isPrimaryUser = false;
            bool isSecondaryUser = false;
            if (ticket == null) return RedirectToAction("TicketHandler");

            if (ticket.CurrentStatus > Ticket.Status.Open)
            {
                var currentUserId = (int)GetUserIdFromExactName(HttpContext.Session.GetString(SessionKeyUsername));
                isPrimaryUser = (ticket.PrimaryUserID == currentUserId);
                isSecondaryUser = (_context.StaffToTickets.Where(st => st.UserID == currentUserId && st.TicketID == ticketId).FirstOrDefault() != null);
            }

            string serviceName = GetServiceNameForTicket(ticketId);
            string locationName = GetLocationNameForTicket(ticketId);

            ViewData[TicketTypeNameKey] = ticket.RequestType;
            ViewData[TicketIDKey] = ticketId;
            ViewData[TicketSeverityKey] = ticket.SeverityLevel;
            ViewData[TicketClientNameKey] = GetClientNameForTicket(ticketId);
            ViewData[TicketSummaryKey] = (string.IsNullOrEmpty(ticket.Summary) ? string.Empty : ticket.Summary);
            ViewData[TicketDetailsKey] = (string.IsNullOrEmpty(ticket.Details) ? string.Empty : ticket.Details);
            ViewData[TicketNotesKey] = (string.IsNullOrEmpty(ticket.Notes) ? string.Empty : ticket.Notes);
            ViewData[TicketStatus] = ticket.CurrentStatus;
            ViewData[TicketLogKey] = (string.IsNullOrEmpty(ticket.Log) ? string.Empty : ticket.Log);
            ViewData[TicketServiceKey] = (string.IsNullOrEmpty(serviceName) ? string.Empty : serviceName);
            ViewData[TicketLocationKey] = (string.IsNullOrEmpty(locationName) ? string.Empty : locationName);

            ViewData[IsPrimaryUserKey] = isPrimaryUser;
            ViewData[IsSecondaryUserKey] = isSecondaryUser;

            SetCommonViewData();
            return View();
        }

        public IActionResult CompleteTicket(int ticketId)
        {
            if (!IsCurrentUserValid()) return RedirectToAction("Login");

            string username = HttpContext.Session.GetString(SessionKeyUsername);
            int userId = GetUserByExactName(username).ID;

            Ticket ticket = GetTicketById(ticketId);
            if (ticket == null) return RedirectToAction("TicketHandler");
            if (ticket.PrimaryUserID != userId) return RedirectToAction("TicketHandler");

            ticket.CurrentStatus = Ticket.Status.Resolved;
            ticket.CompletedByUserID = userId;
            ticket.CompletionDate = DateTime.Now;

            ticket.AddToLog($"The ticket status has changed to {Ticket.GetTextForStatus(Ticket.Status.Resolved)}");
            ticket.AddToLog(userId, username, "Marked the ticket as resolved");
            _context.SaveChanges();

            return RedirectToAction("TicketDetails", "Home", new { ticketId });
        }

        public IActionResult AssignSelfToTicket(int ticketId)
        {
            if (!IsCurrentUserValid()) return RedirectToAction("Login");

            string username = HttpContext.Session.GetString(SessionKeyUsername);
            int userId = GetUserByExactName(username).ID;

            Ticket ticket = GetTicketById(ticketId);
            if (ticket == null) return RedirectToAction("TicketHandler");
            if (ticket.ClientID == userId) return RedirectToAction("TicketHandler");

            if (ticket.PrimaryUserID != null)
            {
                _context.StaffToTickets.Add(new StaffToTicketLink()
                {
                    TicketID = ticketId,
                    UserID = userId
                });
                ticket.AddToLog(userId, username, "Added themself as the secondary handler");
            }
            else
            {
                ticket.CurrentStatus = Ticket.Status.Assigned;
                ticket.AssignmentDate = DateTime.Now;
                ticket.AddToLog($"The ticket status has changed to {Ticket.GetTextForStatus(Ticket.Status.Assigned)}");
                ticket.PrimaryUserID = userId;
                ticket.AddToLog(userId, username, "Added themself as a primary handler");
            }
            _context.SaveChanges();

            return RedirectToAction("TicketDetails", "Home", new { ticketId });
        }

        public IActionResult TicketApproval()
        {
            ViewData[UnaprovedTicketsKey] = GetAllTicketsOfStatus(Ticket.Status.Unapproved);
            SetCommonViewData();
            return View();
        }

        public IActionResult ApproveTicket(int ticketId, Ticket.Status approvalState)
        {
            if (!IsCurrentUserValid()) return RedirectToAction("Login");

            if (approvalState > Ticket.Status.Open) RedirectToAction("TicketApproval");
            Ticket ticket = GetTicketById(ticketId);
            ticket.CurrentStatus = approvalState;
            _context.SaveChanges();
            return RedirectToAction("TicketApproval");
        }
        #endregion


        [HttpPost]
        public IActionResult UpdateAdditionalInformation(int ticketId, string[] additionalInformation)
        {
            if (!IsCurrentUserValid()) return RedirectToAction("Login");

            string username = HttpContext.Session.GetString(SessionKeyUsername);
            int? currentUserId = GetUserIdFromExactName(username);
            var test = GetClientNameForTicket(ticketId);
            Ticket ticket = null;
            string redirectLocation;
            if ((Staff.Type)HttpContext.Session.GetInt32(SessionKeyUserType) == Staff.Type.Handler && GetClientNameForTicket(ticketId) != username)
            {
                ticket = GetTicketsAssignedForUser((int)currentUserId)
                    .Select(t => t.ticket)
                    .Where(t => t.ID == ticketId)
                    .FirstOrDefault();
                redirectLocation = "TicketDetails";
                if (ticket == null || additionalInformation.Length < 1)
                    return RedirectToAction(redirectLocation, "Home", ticketId);

                if (ticket.CurrentStatus != Ticket.Status.Responded)
                {
                    ticket.CurrentStatus = Ticket.Status.Responded;
                    ticket.AddToLog($"Ticket status updated to {Ticket.GetTextForStatus(Ticket.Status.Responded)}");
                }
            }
            else if ((Staff.Type)HttpContext.Session.GetInt32(SessionKeyUserType) == Staff.Type.Manager)
            {
                ticket = GetTicketById(ticketId);
                redirectLocation = "TicketDetails";
                if (ticket == null || additionalInformation.Length < 1)
                    return RedirectToAction(redirectLocation, "Home", ticketId);
            }
            else
            {
                ticket = GetUserSubmittedTickets((int)currentUserId)
                   .Where(t => t.ID == ticketId)
                   .FirstOrDefault();

                redirectLocation = "TicketViewer";
                if (ticket == null || additionalInformation.Length < 1)
                    return RedirectToAction(redirectLocation, "Home", ticketId);
            }
            if (ticket.CurrentStatus == Ticket.Status.Resolved || ticket.CurrentStatus == Ticket.Status.Denied)
                return RedirectToAction(redirectLocation, "Home", ticketId);

            var builder = new StringBuilder();
            foreach (string line in additionalInformation)
                builder.Append(line);
            ticket.Notes += $"[{DateTime.Now:dd/MM/yy hh:mm:ss}] {username}, {currentUserId}: {builder}\n";
            ticket.AddToLog((int)currentUserId, username, "Added information");
            _context.SaveChanges();

            return RedirectToAction(redirectLocation, "Home", new { ticketId });
        }


        #region "Ticket Submitter Viewing"
        public IActionResult MyTickets(int? activeTab = null)
        {
            if (!IsCurrentUserValid()) return RedirectToAction("Login");

            string username = HttpContext.Session.GetString(SessionKeyUsername);
            int userId = GetUserByExactName(username).ID;

            ViewData[UnaprovedTicketsKey] = GetSubmittedTicketForUserOfStatus(userId, Ticket.Status.Unapproved);
            ViewData[OpenTicketsKey] = GetSubmittedTicketForUserOfStatus(userId, Ticket.Status.Open);
            ViewData[RespondedTicketsKey] = GetSubmittedTicketForUserOfStatus(userId, Ticket.Status.Responded);
            ViewData[AssignedTicketsKey] = GetSubmittedTicketForUserOfStatus(userId, Ticket.Status.Assigned);
            ViewData[ResolvedTicketsKey] = GetSubmittedTicketForUserOfStatus(userId, Ticket.Status.Resolved);
            ViewData[activeTabKey] = activeTab;

            SetCommonViewData();
            return View();
        }

        public IActionResult TicketViewer(int ticketId)
        {
            if (!IsCurrentUserValid()) return RedirectToAction("Login");

            int? currentUserId = GetUserIdFromExactName(HttpContext.Session.GetString(SessionKeyUsername));
            Ticket ticket = GetUserSubmittedTickets((int)currentUserId)
                .Where(t => t.ID == ticketId).FirstOrDefault();
            if (ticket == null) return RedirectToAction("MyTickets");

            ViewData[TicketDetailsKey] = ticket;
            ViewData[TicketClientNameKey] = GetClientNameForTicket(ticketId);
            ViewData[ServiceNameKey] = ticket.ServiceID != null ? GetServiceNameForTicket(ticket.ID) : string.Empty;
            ViewData[LocationNameKey] = ticket.LocationID != null ? GetLocationNameForTicket(ticket.ID) : string.Empty;
            ViewData[HandlerNameKey] = ticket.PrimaryUserID != null ? GetHandlerNameForTicket((int)ticket.PrimaryUserID, ticket.ID) : "No user assigned";
            ViewData[TicketNotesKey] = (string.IsNullOrEmpty(ticket.Notes) ? string.Empty : ticket.Notes);

            SetCommonViewData();
            return View();
        }
        #endregion


        #region "Ticket Creator"
        public IActionResult TicketTypePicker()
        {
            if (!IsCurrentUserValid()) return RedirectToAction("Login");

            SetCommonViewData();
            return View();
        }


        public IActionResult TicketCreator(int chosenTicketTypeId)
        {
            if (!IsCurrentUserValid()) return RedirectToAction("Login");
            if (!Enum.IsDefined(typeof(Ticket.Type), chosenTicketTypeId))
                return RedirectToAction("TicketTypePicker");

            ViewData[SelectedTicketTypeKey] = chosenTicketTypeId;
            ViewData[AllManagersKey] = GetManagers();
            ViewData[AllLocationsKey] = _context.Locations.ToArray();
            ViewData[AllServicesKey] = _context.Services.ToArray();
            SetCommonViewData();

            return View();
        }

        [HttpPost]
        public IActionResult TicketCreator(int ticketTypeId, int severityLevelId, int locationId, int serviceId, string username, string summary, string details)
        {
            if (!IsCurrentUserValid()) return RedirectToAction("Login");
            if (!Enum.IsDefined(typeof(Ticket.Type), ticketTypeId)) return RedirectToAction("TicketCreator");

            var ticketType = (Ticket.Type)ticketTypeId;
            var severityLevel = (Ticket.Severity)severityLevelId;
            Location location = _context.Locations.Where(l => l.ID == locationId).FirstOrDefault();
            Service service = _context.Services.Where(s => s.ID == serviceId).FirstOrDefault();
            string loggedInUsername = HttpContext.Session.GetString(SessionKeyUsername);

            if (!UserExists(username) && username != null) return RedirectToAction("TicketCreator", "Home", ticketTypeId);
            Ticket ticket = CreateTicket(ticketType, severityLevel, location, service,
                (string.IsNullOrEmpty(username) ? GetUserByExactName(loggedInUsername).ID : GetUserByExactName(username).ID),
                summary, details);
            ticket.AddToLog(GetUserByExactName(loggedInUsername).ID, loggedInUsername, $"Created ticket of type: {Ticket.GetTextForType(ticketType)}");
            _context.Tickets.Add(ticket);
            _context.SaveChanges();

            SetCommonViewData();
            var ResponseTime = ticket.GetResponseTimeAmountWithUnit();
            var responseTimeData = new string[] { Convert.ToString(ResponseTime.timeAmount), ResponseTime.timeIncrement };
            return RedirectToAction("TicketConfirmation", "Home", new { ticketTypeName = ticket.TextForType, responseTimeData, ticketStatusId = (int)ticket.CurrentStatus });
        }

        public IActionResult TicketConfirmation(string ticketTypeName, string[] responseTimeData, int ticketStatusId)
        {
            if (!IsCurrentUserValid()) return RedirectToAction("Login");

            ViewData[TicketTypeNameKey] = ticketTypeName;
            ViewData[TicketResponseTimeKey] = responseTimeData;
            ViewData[TicketStatusIdKey] = ticketStatusId;
            SetCommonViewData();

            return View();
        }
        #endregion
    }
}