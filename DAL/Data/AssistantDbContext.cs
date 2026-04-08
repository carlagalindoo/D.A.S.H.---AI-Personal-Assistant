using Domain.Models;
using Microsoft.EntityFrameworkCore;


namespace DAL.Data
{
    public class AssistantDbContext : DbContext
    {
        public AssistantDbContext(DbContextOptions<AssistantDbContext> options)
            : base(options)
        {
        }

       
        public DbSet<Domain.Models.Task> Tasks { get; set; }
    }
}
