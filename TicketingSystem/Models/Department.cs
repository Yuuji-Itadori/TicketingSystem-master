using System.ComponentModel.DataAnnotations;

namespace TicketingSystem.Models
{
    public class Department
    {
        [Key]
        public int ID { get; set; }

        public string Name { get; set; }
    }
}
