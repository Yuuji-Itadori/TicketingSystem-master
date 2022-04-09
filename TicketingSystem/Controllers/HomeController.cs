using System;
using System.Linq;
using System.Diagnostics;
using TicketingSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;


namespace TicketingSystem.Controllers
{
    public partial class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }


        public const string SessionKeyUsername = "_Username";
        public string SessionInfo_Username { get; private set; }
        public const string SessionKeyUserType = "_UserType";
        public int SessionInfo_UserType { get; private set; }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }


        private void MassAddTickets(int quantityToProduce)
        {
            quantityToProduce = (quantityToProduce < 0 ? 1 : quantityToProduce + 1);
            _context.Tickets.RemoveRange(_context.Tickets);
            _context.SaveChanges();

            var rnd = new Random();
            int maxSeverity = Enum.GetNames(typeof(Ticket.Severity)).Length;
            int maxStatus = (int)Ticket.Status.Resolved;
            int maxType = Enum.GetNames(typeof(Ticket.Type)).Length;

            int submitterId = 0;
            var status = Ticket.Status.Unapproved;
            var type = Ticket.Type.PasswordReset;
            var severity = Ticket.Severity.Medium;
            var submissionDate = DateTime.Now;
            DateTime? assignmentDate = null;
            DateTime? completionDate = null;
            int? handlerId = null;
            int? serviceId = null;
            int? locationId = null;
            int? completedByUserId = null;

            int[] submitterIds = _context.Staff.Where(s => s.CurrentType == Staff.Type.General)
                                               .Select(s => s.ID)
                                               .ToArray();
            int[] handlerIds = _context.Staff.Where(s => s.CurrentType == Staff.Type.Manager)
                                             .Select(s => s.ID)
                                             .ToArray();
            int[] locationIds = _context.Locations.Select(l => l.ID)
                                                  .ToArray();
            int[] serviceIds = _context.Services.Select(s => s.ID)
                                                .ToArray();

            // password - services, access control - service, procurement - location, 
            // standard service & non standard & IT support - location + service

            for (int i = 1; i < quantityToProduce; i++)
            {
                submitterId = submitterIds[rnd.Next(0, submitterIds.Length)];
                status = (Ticket.Status)rnd.Next(2, maxStatus + 1);
                type = (Ticket.Type)rnd.Next(0, maxType);
                severity = Ticket.Severity.Medium;
                submissionDate = DateTime.Now.AddMonths(-rnd.Next(0, 12)).AddDays(-rnd.Next(10, 30));
                assignmentDate = null;
                completionDate = null;
                handlerId = null;
                serviceId = null;
                locationId = null;
                completedByUserId = null;

                switch (type)
                {
                    case Ticket.Type.PasswordReset:
                        serviceId = serviceIds[rnd.Next(0, serviceIds.Length)];
                        break;
                    case Ticket.Type.AccessControl:
                        serviceId = serviceIds[rnd.Next(0, serviceIds.Length)];
                        severity = (Ticket.Severity)rnd.Next(1, maxSeverity + 1);
                        status = (Ticket.Status)rnd.Next(-1, maxStatus + 1);
                        break;
                    case Ticket.Type.ITSupportService:
                        locationId = locationIds[rnd.Next(0, locationIds.Length)];
                        serviceId = serviceIds[rnd.Next(0, serviceIds.Length)];
                        break;
                    case Ticket.Type.NonStandardService:
                        locationId = locationIds[rnd.Next(0, locationIds.Length)];
                        serviceId = serviceIds[rnd.Next(0, serviceIds.Length)];
                        severity = (Ticket.Severity)rnd.Next(1, maxSeverity + 1);
                        status = (Ticket.Status)rnd.Next(-1, maxStatus + 1);
                        break;
                    case Ticket.Type.StandardService:
                        locationId = locationIds[rnd.Next(0, locationIds.Length)];
                        serviceId = serviceIds[rnd.Next(0, serviceIds.Length)];
                        status = (Ticket.Status)rnd.Next(-1, maxStatus + 1);
                        break;
                    case Ticket.Type.Procurement:
                        locationId = locationIds[rnd.Next(0, locationIds.Length)];
                        break;
                }

                switch (status)
                {
                    case Ticket.Status.Resolved:
                        handlerId = handlerIds[rnd.Next(0, handlerIds.Length)];
                        assignmentDate = submissionDate.AddDays(rnd.Next(0, 7))
                                                       .AddHours(rnd.Next(0, 24))
                                                       .AddMinutes(rnd.Next(0, 60));
                        completedByUserId = handlerId;
                        completionDate = assignmentDate.Value.AddDays(rnd.Next(0, 14))
                                                             .AddHours(rnd.Next(0, 24))
                                                             .AddMinutes(rnd.Next(0, 60));
                        break;
                    case Ticket.Status.Assigned:
                        case Ticket.Status.Responded:
                        handlerId = handlerIds[rnd.Next(0, handlerIds.Length)];
                        assignmentDate = submissionDate.AddDays(rnd.Next(0, 7))
                                                       .AddHours(rnd.Next(0, 24))
                                                       .AddMinutes(rnd.Next(0, 60));
                        break;
                }

                _context.Tickets.Add(new Ticket()
                {
                    RequestType = type,
                    CurrentStatus = status,
                    ServiceID = serviceId,
                    LocationID = locationId,
                    SeverityLevel = severity,
                    SubmissionDate = submissionDate,
                    CompletionDate = completionDate,
                    ClientID = submitterId,
                    Summary = string.Empty,
                    Details = string.Empty,
                    Notes = string.Empty,
                    PrimaryUserID = handlerId,
                    CompletedByUserID = completedByUserId,
                    AssignmentDate = assignmentDate,
                    Log = string.Empty
                });
                if (i % 100000 == 0) _context.SaveChanges();
            }
            _context.SaveChanges();
        }
    }
}