using Microsoft.Exchange.WebServices.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Task = Microsoft.Exchange.WebServices.Data.Task;

namespace Domain.Interfaces
{
    public interface IAppointmentService
    {
        Task<Appointment> CreateAppointmentAsync(Appointment appointment);
        Task<List<Appointment>> GetAppointmentListAsync(int userId);
        Task<Appointment> GetAppointmentByIdAsync(int id);
        Task<Appointment> UpdateAppointmentAsync(Appointment appointment);
        Task DeleteAppointmentAsync(int id);
    }
}
