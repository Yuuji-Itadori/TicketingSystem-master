using System.ComponentModel.DataAnnotations;

namespace TicketingSystem.Models
{
    public class Branch
    {
        [Key]
        public int ID { get; set; }

        public string Name { get; set; }
    }
}