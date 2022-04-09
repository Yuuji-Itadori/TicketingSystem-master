using System.Collections.Generic;

namespace TicketingSystem.Data
{
    public sealed class NavigationOption
    {
        public static readonly List<NavigationOption> General = new List<NavigationOption>()
        {
            new NavigationOption("TicketTypePicker", "Create Ticket"),
            new NavigationOption("MyTickets", "My Tickets"),
        };

        public static readonly List<NavigationOption> Handler = new List<NavigationOption>()
        {
            new NavigationOption("TicketHandler", "Ticket Inbox"),
            new NavigationOption("TicketTypePicker", "Create Ticket"),
            new NavigationOption("MyTickets", "My Tickets"),
        };

        public static readonly List<NavigationOption> Manager = new List<NavigationOption>()
        {
            new NavigationOption("TicketTypePicker", "Create Ticket"),
            new NavigationOption("MyTickets", "My Tickets"),
            new NavigationOption("TicketApproval", "Approvals"),
            new NavigationOption("Statistics", "Statistics")
        };


        public string DestinationUrl { get; private set; }
        public string HyperlinkText { get; private set; }
        public string TitleText { get; private set; }

        public NavigationOption(string destinationUrl, string hyperlinkText, string titleText = "")
        {
            DestinationUrl = destinationUrl;
            HyperlinkText = hyperlinkText;
            TitleText = (titleText.Equals(string.Empty) ? hyperlinkText : titleText);
        }
    }
}