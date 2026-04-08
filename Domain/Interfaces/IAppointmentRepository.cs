using Microsoft.Exchange.WebServices.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Interfaces
{
    public interface IAppointmentRepository
    {
        Models.Task AddAsync(Appointment appointment);
        Task<List<Appointment>> GetByUserIdAsync(int userId);
        Task<Appointment?> GetByIdAsync(int id);
        Models.Task UpdateAsync(Appointment appointment);
        Models.Task DeleteAsync(int id);
    }
}
