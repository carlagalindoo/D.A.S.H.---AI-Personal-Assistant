using Microsoft.EntityFrameworkCore;

namespace DAL.Data
{
    public class AssistantDbContext : DbContext
    {
        public AssistantDbContext(DbContextOptions<AssistantDbContext> options)
            : base(options)
        {
        }

        // Add DbSet<Appointment> here once the entity is created
        // public DbSet<Appointment> Appointments { get; set; }
    }
}
