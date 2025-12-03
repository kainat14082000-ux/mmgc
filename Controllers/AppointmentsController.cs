using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MMGC.Models;
using MMGC.Services;
using MMGC.Repositories;

namespace MMGC.Controllers;

[Authorize]
public class AppointmentsController : Controller
{
    private readonly IAppointmentService _appointmentService;
    private readonly IPatientService _patientService;
    private readonly IDoctorService _doctorService;
    private readonly IRepository<Nurse> _nurseRepository;
    private readonly ILogger<AppointmentsController> _logger;

    public AppointmentsController(
        IAppointmentService appointmentService,
        IPatientService patientService,
        IDoctorService doctorService,
        IRepository<Nurse> nurseRepository,
        ILogger<AppointmentsController> logger)
    {
        _appointmentService = appointmentService;
        _patientService = patientService;
        _doctorService = doctorService;
        _nurseRepository = nurseRepository;
        _logger = logger;
    }

    // GET: Appointments
    public async Task<IActionResult> Index()
    {
        var appointments = await _appointmentService.GetAllAppointmentsAsync();
        return View(appointments);
    }

    // GET: Appointments/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var appointment = await _appointmentService.GetAppointmentByIdAsync(id.Value);
        if (appointment == null)
        {
            return NotFound();
        }

        return View(appointment);
    }

    // Helper to populate dropdowns
    private async Task PopulateDropDownsAsync(object? selectedPatient = null, object? selectedDoctor = null, object? selectedNurse = null)
    {
        var patients = await _patientService.GetAllPatientsAsync();
        var doctors = await _doctorService.GetAllDoctorsAsync();
        var nurses = await _nurseRepository.GetAllAsync();

        ViewBag.Patients = new SelectList(patients, "Id", "FullName", selectedPatient);
        ViewBag.Doctors = new SelectList(doctors.Where(d => d.IsActive).ToList(), "Id", "FullName", selectedDoctor);
        ViewBag.Nurses = new SelectList(nurses, "Id", "FullName", selectedNurse);
    }

    // GET: Appointments/Create
    public async Task<IActionResult> Create()
    {
        await PopulateDropDownsAsync();
        var model = new Appointment
        {
            AppointmentDate = DateTime.Now.AddHours(1) // Default to 1 hour from now
        };
        return View(model);
    }

    // POST: Appointments/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Appointment appointment)
    {
        // Clear any ModelState errors for navigation properties (they're not bound from form)
        ModelState.Remove(nameof(Appointment.Patient));
        ModelState.Remove(nameof(Appointment.Doctor));
        ModelState.Remove(nameof(Appointment.Nurse));
        
        // Log the raw model values to debug binding issues
        _logger.LogDebug("Create POST called. PatientId={PatientId}, DoctorId={DoctorId}, NurseId={NurseId}, AppointmentDate={AppointmentDate}, AppointmentType={AppointmentType}, ConsultationFee={ConsultationFee}, ModelState.IsValid={IsValid}", 
            appointment.PatientId, appointment.DoctorId, appointment.NurseId, appointment.AppointmentDate, appointment.AppointmentType, appointment.ConsultationFee, ModelState.IsValid);
        

        // Log all ModelState errors
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            _logger.LogWarning("ModelState invalid on create. PatientId={PatientId}, DoctorId={DoctorId}, Errors: {@Errors}", 
                appointment.PatientId, appointment.DoctorId, errors);
            
            // Log each ModelState entry for debugging
            foreach (var key in ModelState.Keys)
            {
                var state = ModelState[key];
                if (state != null && state.Errors.Count > 0)
                {
                    _logger.LogWarning("ModelState error for {Key}: {Errors}", key, 
                        string.Join(", ", state.Errors.Select(e => e.ErrorMessage)));
                }
            }
            
            await PopulateDropDownsAsync(appointment.PatientId, appointment.DoctorId, appointment.NurseId);
            return View(appointment);
        }

        // Server-side existence checks
        var patient = appointment.PatientId.HasValue ? await _patientService.GetPatientByIdAsync(appointment.PatientId.Value) : null;
        if (patient == null)
        {
            ModelState.AddModelError(nameof(Appointment.PatientId), "Selected patient does not exist.");
            await PopulateDropDownsAsync(appointment.PatientId, appointment.DoctorId, appointment.NurseId);
            return View(appointment);
        }

        if (appointment.DoctorId.HasValue)
        {
            var doctor = await _doctorService.GetDoctorByIdAsync(appointment.DoctorId.Value);
            if (doctor == null || !doctor.IsActive)
            {
                ModelState.AddModelError(nameof(Appointment.DoctorId), "Selected doctor does not exist or is not active.");
                await PopulateDropDownsAsync(appointment.PatientId, appointment.DoctorId, appointment.NurseId);
                return View(appointment);
            }
        }

        try
        {
            // Avoid sending navigation properties in create (set null so EF only uses FK)
            appointment.Patient = null;
            appointment.Doctor = null;
            appointment.Nurse = null;

            appointment.CreatedBy = User.Identity?.Name;
            appointment.Status = "Scheduled";
            appointment.CreatedDate = DateTime.Now;

            await _appointmentService.CreateAppointmentAsync(appointment);

            TempData["SuccessMessage"] = "Appointment created successfully!";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError("", "A database error occurred while creating the appointment.");
            await PopulateDropDownsAsync(appointment.PatientId, appointment.DoctorId, appointment.NurseId);
            return View(appointment);
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", $"An error occurred while creating the appointment: {ex.Message}");
            await PopulateDropDownsAsync(appointment.PatientId, appointment.DoctorId, appointment.NurseId);
            return View(appointment);
        }
    }

    // GET: Appointments/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var appointment = await _appointmentService.GetAppointmentByIdAsync(id.Value);
        if (appointment == null)
        {
            return NotFound();
        }

        await PopulateDropDownsAsync(appointment.PatientId, appointment.DoctorId, appointment.NurseId);
        
        // Also provide raw collections for Edit view that needs custom formatting
        var patients = await _patientService.GetAllPatientsAsync();
        var doctors = await _doctorService.GetAllDoctorsAsync();
        var nurses = await _nurseRepository.GetAllAsync();
        ViewBag.PatientsList = patients;
        ViewBag.DoctorsList = doctors.Where(d => d.IsActive).ToList();
        ViewBag.NursesList = nurses;
        
        return View(appointment);
    }

    // POST: Appointments/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Appointment appointment)
    {
        if (id != appointment.Id)
        {
            return NotFound();
        }

        // Clear any ModelState errors for navigation properties (they're not bound from form)
        ModelState.Remove(nameof(Appointment.Patient));
        ModelState.Remove(nameof(Appointment.Doctor));
        ModelState.Remove(nameof(Appointment.Nurse));

        if (!ModelState.IsValid)
        {
            await PopulateDropDownsAsync(appointment.PatientId, appointment.DoctorId, appointment.NurseId);
            var patients = await _patientService.GetAllPatientsAsync();
            var doctors = await _doctorService.GetAllDoctorsAsync();
            var nurses = await _nurseRepository.GetAllAsync();
            ViewBag.PatientsList = patients;
            ViewBag.DoctorsList = doctors.Where(d => d.IsActive).ToList();
            ViewBag.NursesList = nurses;
            return View(appointment);
        }

        // Server-side existence checks
        var patient = appointment.PatientId.HasValue ? await _patientService.GetPatientByIdAsync(appointment.PatientId.Value) : null;
        if (patient == null)
        {
            ModelState.AddModelError(nameof(Appointment.PatientId), "Selected patient does not exist.");
            await PopulateDropDownsAsync(appointment.PatientId, appointment.DoctorId, appointment.NurseId);
            var patients = await _patientService.GetAllPatientsAsync();
            var doctors = await _doctorService.GetAllDoctorsAsync();
            var nurses = await _nurseRepository.GetAllAsync();
            ViewBag.PatientsList = patients;
            ViewBag.DoctorsList = doctors.Where(d => d.IsActive).ToList();
            ViewBag.NursesList = nurses;
            return View(appointment);
        }

        if (appointment.DoctorId.HasValue)
        {
            var doctor = await _doctorService.GetDoctorByIdAsync(appointment.DoctorId.Value);
            if (doctor == null || !doctor.IsActive)
            {
                ModelState.AddModelError(nameof(Appointment.DoctorId), "Selected doctor does not exist or is not active.");
                await PopulateDropDownsAsync(appointment.PatientId, appointment.DoctorId, appointment.NurseId);
                var patients = await _patientService.GetAllPatientsAsync();
                var doctors = await _doctorService.GetAllDoctorsAsync();
                var nurses = await _nurseRepository.GetAllAsync();
                ViewBag.PatientsList = patients;
                ViewBag.DoctorsList = doctors.Where(d => d.IsActive).ToList();
                ViewBag.NursesList = nurses;
                return View(appointment);
            }
        }

        try
        {
            // Avoid sending navigation properties in update (set null so EF only uses FK)
            appointment.Patient = null;
            appointment.Doctor = null;
            appointment.Nurse = null;

            appointment.UpdatedDate = DateTime.Now;

            await _appointmentService.UpdateAppointmentAsync(appointment);

            TempData["SuccessMessage"] = "Appointment updated successfully!";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await AppointmentExists(appointment.Id))
            {
                return NotFound();
            }

            ModelState.AddModelError("", "Another user modified this record. Please reload and try again.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating appointment");
            ModelState.AddModelError("", "An error occurred while updating the appointment.");
        }

        await PopulateDropDownsAsync(appointment.PatientId, appointment.DoctorId, appointment.NurseId);
        var patientsList = await _patientService.GetAllPatientsAsync();
        var doctorsList = await _doctorService.GetAllDoctorsAsync();
        var nursesList = await _nurseRepository.GetAllAsync();
        ViewBag.PatientsList = patientsList;
        ViewBag.DoctorsList = doctorsList.Where(d => d.IsActive).ToList();
        ViewBag.NursesList = nursesList;
        return View(appointment);
    }

    // GET: Appointments/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var appointment = await _appointmentService.GetAppointmentByIdAsync(id.Value);
        if (appointment == null)
        {
            return NotFound();
        }

        return View(appointment);
    }

    // POST: Appointments/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        try
        {
            await _appointmentService.DeleteAppointmentAsync(id);
            TempData["SuccessMessage"] = "Appointment deleted successfully!";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting appointment {AppointmentId}", id);
            TempData["ErrorMessage"] = "An error occurred while deleting the appointment.";
            return RedirectToAction(nameof(Index));
        }
    }

    // POST: Appointments/SendSMS/5
    [HttpPost]
    public async Task<IActionResult> SendSMS(int id)
    {
        try
        {
            var result = await _appointmentService.SendSMSNotificationAsync(id);
            
            if (result)
            {
                return Json(new { 
                    success = true, 
                    message = "SMS sent successfully! Note: If using a Twilio trial account, SMS will only be delivered to verified phone numbers." 
                });
            }
            else
            {
                return Json(new { 
                    success = false, 
                    message = "Failed to send SMS. Please check the logs for details. If using a Twilio trial account, ensure the phone number is verified in your Twilio console." 
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SendSMS action for appointment {AppointmentId}", id);
            return Json(new { 
                success = false, 
                message = $"An error occurred: {ex.Message}" 
            });
        }
    }

    // POST: Appointments/SendWhatsApp/5
    [HttpPost]
    public async Task<IActionResult> SendWhatsApp(int id)
    {
        var result = await _appointmentService.SendWhatsAppNotificationAsync(id);
        return Json(new { success = result });
    }

    private async Task<bool> AppointmentExists(int id)
    {
        var appointment = await _appointmentService.GetAppointmentByIdAsync(id);
        return appointment != null;
    }
}
