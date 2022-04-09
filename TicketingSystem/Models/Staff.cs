using System.ComponentModel.DataAnnotations;

namespace TicketingSystem.Models
{
    public class Staff
    {
        public enum Type
        {
            None = 0,
            General = 1,
            Handler = 2,
            Manager = 3
        }


        [Key]
        public int ID { get; set; }

        public string Name { get; set; }

        public string Password { get; set; }

        [EnumDataType(typeof(Type))]
        public Type CurrentType { get; set; }

        public int BranchID { get; set; }

        public int DepartmentID { get; set; }

        [Phone]
        public string ContactNumber { get; set; }

        [EmailAddress]
        public string EmailAddress { get; set; }
    }
}