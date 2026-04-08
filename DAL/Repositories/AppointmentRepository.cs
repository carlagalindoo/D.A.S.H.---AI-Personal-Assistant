using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DAL.Data;

namespace DAL.Repositories
{
    public class AppointmentRepository : IAppointmentRepository
    {
        private readonly AssistantDbContext _context;

        public AppointmentRepository(AssistantDbContext context)
        {
            _context = context;
        }
    }
}
