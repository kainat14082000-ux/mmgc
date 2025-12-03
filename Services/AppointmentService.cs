using System.Linq;
using MMGC.Models;
using MMGC.Repositories;
using MMGC.Data;
using Microsoft.EntityFrameworkCore;

namespace MMGC.Services;

public class AppointmentService : IAppointmentService
{
    private readonly IRepository<Appointment> _repository;
    private readonly ApplicationDbContext _context;
    private readonly ISmsService _smsService;
    private readonly ILogger<AppointmentService> _logger;

    public AppointmentService(
        IRepository<Appointment> repository, 
        ApplicationDbContext context,
        ISmsService smsService,
        ILogger<AppointmentService> logger)
    {
        _repository = repository;
        _context = context;
        _smsService = smsService;
        _logger = logger;
    }

    public async Task<IEnumerable<Appointment>> GetAllAppointmentsAsync()
    {
        return await _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .Include(a => a.Nurse)
            .OrderByDescending(a => a.AppointmentDate)
            .ToListAsync();
    }

    public async Task<Appointment?> GetAppointmentByIdAsync(int id)
    {
        return await _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .Include(a => a.Nurse)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<IEnumerable<Appointment>> GetAppointmentsByDateAsync(DateTime date)
    {
        return await _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .Include(a => a.Nurse)
            .Where(a => a.AppointmentDate.Date == date.Date)
            .OrderBy(a => a.AppointmentDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Appointment>> GetTodayAppointmentsAsync()
    {
        return await GetAppointmentsByDateAsync(DateTime.Today);
    }

    public async Task<Appointment> CreateAppointmentAsync(Appointment appointment)
    {
        appointment.CreatedDate = DateTime.Now;
        
        // Use context directly to avoid navigation property issues
        await _context.Appointments.AddAsync(appointment);
        await _context.SaveChangesAsync();
        return appointment;
    }

    public async Task UpdateAppointmentAsync(Appointment appointment)
    {
        appointment.UpdatedDate = DateTime.Now;
        // Use context directly to avoid navigation property issues
        _context.Appointments.Update(appointment);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAppointmentAsync(int id)
    {
        var appointment = await _repository.GetByIdAsync(id);
        if (appointment != null)
        {
            await _repository.DeleteAsync(appointment);
        }
    }

    public async Task<bool> SendSMSNotificationAsync(int appointmentId)
    {
        try
        {
            var appointment = await GetAppointmentByIdAsync(appointmentId);
            if (appointment == null || appointment.Patient == null)
            {
                _logger.LogWarning("Appointment {AppointmentId} not found or patient is null", appointmentId);
                return false;
            }

            // Check if patient has a contact number
            if (string.IsNullOrWhiteSpace(appointment.Patient.ContactNumber))
            {
                _logger.LogWarning("Patient {PatientId} does not have a contact number", appointment.Patient.Id);
                return false;
            }

            // Build SMS message
            var message = BuildAppointmentSmsMessage(appointment);

            // Send SMS via Twilio
            var smsSent = await _smsService.SendSmsAsync(appointment.Patient.ContactNumber, message);

            if (smsSent)
            {
                // Mark as sent in database
                appointment.SMSSent = true;
                _context.Appointments.Update(appointment);
                await _context.SaveChangesAsync();
                _logger.LogInformation("SMS notification sent successfully for appointment {AppointmentId}", appointmentId);
            }
            else
            {
                _logger.LogWarning("Failed to send SMS for appointment {AppointmentId}", appointmentId);
            }

            return smsSent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending SMS notification for appointment {AppointmentId}", appointmentId);
            return false;
        }
    }

    private string BuildAppointmentSmsMessage(Appointment appointment)
    {
        var patientName = appointment.Patient?.FullName ?? "Patient";
        var appointmentDate = appointment.AppointmentDate.ToString("dd MMM yyyy, hh:mm tt");
        var doctorName = appointment.Doctor?.FullName ?? "TBA";

        // Keep message short for Twilio trial account (max 160 chars recommended)
        var message = $"Hi {patientName.Split(' ')[0]}, your appointment is on {appointmentDate}";
        
        if (appointment.Doctor != null)
        {
            message += $" with Dr. {doctorName.Split(' ').Last()}";
        }
        
        message += ". Status: " + appointment.Status;
        
        if (appointment.ConsultationFee > 0)
        {
            message += $". Fee: PKR {appointment.ConsultationFee:N0}";
        }
        
        message += ". -MMGC";

        // Ensure message doesn't exceed 160 characters (SMS limit)
        if (message.Length > 160)
        {
            // Truncate and add ellipsis
            message = message.Substring(0, 157) + "...";
        }

        return message;
    }

    public async Task<bool> SendWhatsAppNotificationAsync(int appointmentId)
    {
        var appointment = await GetAppointmentByIdAsync(appointmentId);
        if (appointment == null || appointment.Patient == null)
            return false;

        // TODO: Implement actual WhatsApp sending logic
        // For now, just mark as sent
        appointment.WhatsAppSent = true;
        await _repository.UpdateAsync(appointment);
        return true;
    }
}
