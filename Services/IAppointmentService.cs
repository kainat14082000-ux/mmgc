using MMGC.Models;

namespace MMGC.Services;

public interface IAppointmentService
{
    Task<IEnumerable<Appointment>> GetAllAppointmentsAsync();
    Task<Appointment?> GetAppointmentByIdAsync(int id);
    Task<IEnumerable<Appointment>> GetAppointmentsByDateAsync(DateTime date);
    Task<IEnumerable<Appointment>> GetTodayAppointmentsAsync();
    Task<Appointment> CreateAppointmentAsync(Appointment appointment);
    Task UpdateAppointmentAsync(Appointment appointment);
    Task DeleteAppointmentAsync(int id);
    Task<bool> SendSMSNotificationAsync(int appointmentId);
    Task<bool> SendWhatsAppNotificationAsync(int appointmentId);
}
