using DAL.Repositories;
using Domain.Models;
using Domain.Interfaces;
using Microsoft.Exchange.WebServices.Data;

namespace BLL.Services
{
    public class AppointmentService : IAppointmentService
    {
        private readonly IAppointmentRepository _appointmentRepository;

        public AppointmentService(IAppointmentRepository appointmentRepository)
        {
            _appointmentRepository = appointmentRepository;
        }

        // CREATE
        public async Task<Appointment> CreateAppointmentAsync(Appointment appointment)
        {
            ValidateAppointment(appointment);

            await _appointmentRepository.AddAsync(appointment);
            return appointment;
        }

        // READ (all for a user)
        public async Task<List<Appointment>> GetAppointmentsAsync(int userId)
        {
            return await _appointmentRepository.GetByUserIdAsync(userId);
        }

        // READ (single)
        public async Task<Appointment?> GetAppointmentByIdAsync(int id)
        {
            return await _appointmentRepository.GetByIdAsync(id);
        }

        // UPDATE
        public async Task<Appointment> UpdateAppointmentAsync(Appointment appointment)
        {
            var existing = await _appointmentRepository.GetByIdAsync(appointment.TaskId);

            if (existing == null)
                throw new Exception("Appointment not found.");

            ValidateAppointment(appointment);

            // Update fields
            existing.Title = appointment.Title;
            existing.Description = appointment.Description;
            existing.DueDate = appointment.DueDate;
            existing.Location = appointment.Location;
            existing.People = appointment.People;

            await _appointmentRepository.UpdateAsync(existing);

            return existing;
        }

        // DELETE
        public async Task DeleteAppointmentAsync(int id)
        {
            var existing = await _appointmentRepository.GetByIdAsync(id);

            if (existing == null)
                throw new Exception("Appointment not found.");

            await _appointmentRepository.DeleteAsync(id);
        }

        // 🔥 BUSINESS RULES LIVE HERE
        private void ValidateAppointment(Appointment appointment)
        {
            if (appointment == null)
                throw new ArgumentNullException(nameof(appointment));

            if (string.IsNullOrWhiteSpace(appointment.Title))
                throw new Exception("Title is required.");

            if (appointment.DueDate < DateTime.Now)
                throw new Exception("Appointment cannot be in the past.");

            if (string.IsNullOrWhiteSpace(appointment.Location))
                throw new Exception("Location is required.");
        }
    }
}
