using Microsoft.EntityFrameworkCore;


namespace TicketingSystem.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }


        public virtual DbSet<Ticket> Tickets { get; set; }

        public virtual DbSet<Staff> Staff { get; set; }

        public virtual DbSet<StaffToTicketLink> StaffToTickets { get; set; }

        public virtual DbSet<Branch> Branches { get; set; }

        public virtual DbSet<Department> Departments { get; set; }

        public virtual DbSet<DepartmentToBranchLink> DepartmentsToBranches { get; set; }

        public virtual DbSet<Service> Services { get; set; }

        public virtual DbSet<Location> Locations { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<StaffToTicketLink>()
                .HasKey(c => new { c.TicketID, c.UserID });
            modelBuilder.Entity<DepartmentToBranchLink>()
                .HasKey(c => new { c.DepartmentID, c.BranchID });
        }
    }
}