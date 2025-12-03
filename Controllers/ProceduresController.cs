using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MMGC.Models;
using MMGC.Services;
using MMGC.Repositories;

namespace MMGC.Controllers;

[Authorize]
public class ProceduresController : Controller
{
    private readonly IProcedureService _procedureService;
    private readonly IPatientService _patientService;
    private readonly IDoctorService _doctorService;
    private readonly IRepository<Nurse> _nurseRepository;
    private readonly ILogger<ProceduresController> _logger;

    public ProceduresController(
        IProcedureService procedureService,
        IPatientService patientService,
        IDoctorService doctorService,
        IRepository<Nurse> nurseRepository,
        ILogger<ProceduresController> logger)
    {
        _procedureService = procedureService;
        _patientService = patientService;
        _doctorService = doctorService;
        _nurseRepository = nurseRepository;
        _logger = logger;
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
        
        // Also provide raw collections for Edit view that needs custom formatting
        ViewBag.PatientsList = patients;
        ViewBag.DoctorsList = doctors.Where(d => d.IsActive).ToList();
        ViewBag.NursesList = nurses;
    }

    // GET: Procedures/Create
    public async Task<IActionResult> Create()
    {
        await PopulateDropDownsAsync();
        var model = new Procedure
        {
            ProcedureDate = DateTime.Now
        };
        return View(model);
    }

    // POST: Procedures/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Procedure procedure)
    {
        _logger.LogDebug("Create POST called. PatientId={PatientId}, DoctorId={DoctorId}", procedure.PatientId, procedure.DoctorId);

        // If model-level validation failed, redisplay with dropdowns
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("ModelState invalid on create. Errors: {@Errors}", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            await PopulateDropDownsAsync(procedure.PatientId, procedure.DoctorId, procedure.NurseId);
            return View(procedure);
        }

        // Server-side existence checks
        var patient = procedure.PatientId.HasValue ? await _patientService.GetPatientByIdAsync(procedure.PatientId.Value) : null;
        var doctor = procedure.DoctorId.HasValue ? await _doctorService.GetDoctorByIdAsync(procedure.DoctorId.Value) : null;

        if (patient == null)
            ModelState.AddModelError(nameof(Procedure.PatientId), "Selected patient does not exist.");

        if (doctor == null)
            ModelState.AddModelError(nameof(Procedure.DoctorId), "Selected doctor does not exist.");

        if (!ModelState.IsValid)
        {
            await PopulateDropDownsAsync(procedure.PatientId, procedure.DoctorId, procedure.NurseId);
            return View(procedure);
        }

        try
        {
            // Avoid sending navigation properties in create (set null so EF only uses FK)
            procedure.Patient = null;
            procedure.Doctor = null;
            procedure.Nurse = null;
            procedure.LabTests = new List<LabTest>();

            procedure.CreatedBy = User.Identity?.Name;
            procedure.CreatedDate = DateTime.Now;

            await _procedureService.CreateProcedureAsync(procedure);

            TempData["SuccessMessage"] = "Procedure recorded successfully!";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException dbEx)
        {
            _logger.LogError(dbEx, "Database error creating procedure");
            ModelState.AddModelError("", "A database error occurred while creating the procedure.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating procedure");
            ModelState.AddModelError("", "An unexpected error occurred while creating the procedure.");
        }

        await PopulateDropDownsAsync(procedure.PatientId, procedure.DoctorId, procedure.NurseId);
        return View(procedure);
    }

    // GET: Procedures/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var procedure = await _procedureService.GetProcedureByIdAsync(id.Value);
        if (procedure == null) return NotFound();

        await PopulateDropDownsAsync(procedure.PatientId, procedure.DoctorId, procedure.NurseId);
        return View(procedure);
    }

    // POST: Procedures/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Procedure procedure)
    {
        if (id != procedure.Id) return NotFound();

        if (!ModelState.IsValid)
        {
            await PopulateDropDownsAsync(procedure.PatientId, procedure.DoctorId, procedure.NurseId);
            return View(procedure);
        }

        var patient = procedure.PatientId.HasValue ? await _patientService.GetPatientByIdAsync(procedure.PatientId.Value) : null;
        var doctor = procedure.DoctorId.HasValue ? await _doctorService.GetDoctorByIdAsync(procedure.DoctorId.Value) : null;

        if (patient == null)
            ModelState.AddModelError(nameof(Procedure.PatientId), "Selected patient does not exist.");

        if (doctor == null)
            ModelState.AddModelError(nameof(Procedure.DoctorId), "Selected doctor does not exist.");

        if (!ModelState.IsValid)
        {
            await PopulateDropDownsAsync(procedure.PatientId, procedure.DoctorId, procedure.NurseId);
            return View(procedure);
        }

        try
        {
            procedure.Patient = null;
            procedure.Doctor = null;
            procedure.Nurse = null;

            procedure.UpdatedDate = DateTime.Now;

            await _procedureService.UpdateProcedureAsync(procedure);

            TempData["SuccessMessage"] = "Procedure updated successfully!";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await ProcedureExists(procedure.Id))
                return NotFound();

            ModelState.AddModelError("", "Another user modified this record. Please reload and try again.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating procedure");
            ModelState.AddModelError("", "An error occurred while updating the procedure.");
        }

        await PopulateDropDownsAsync(procedure.PatientId, procedure.DoctorId, procedure.NurseId);
        return View(procedure);
    }

    // GET: Procedures
    public async Task<IActionResult> Index()
    {
        var procedures = await _procedureService.GetAllProceduresAsync();
        return View(procedures);
    }

    // GET: Procedures/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var procedure = await _procedureService.GetProcedureByIdAsync(id.Value);
        if (procedure == null)
        {
            return NotFound();
        }

        return View(procedure);
    }

    // POST: Procedures/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, bool forceDelete = false)
    {
        try
        {
            var procedure = await _procedureService.GetProcedureByIdAsync(id);
            if (procedure == null)
            {
                return Json(new { success = false, message = "Procedure not found." });
            }

            // Check dependencies first
            var (canDelete, blockingRecords) = await _procedureService.CheckDeleteDependenciesAsync(id);
            
            if (!canDelete && !forceDelete)
            {
                // Return blocking records so frontend can show force delete option
                var blockingMessage = $"Cannot delete procedure. It is referenced by: {string.Join(", ", blockingRecords)}.";
                return Json(new { 
                    success = false, 
                    message = blockingMessage,
                    blockingRecords = blockingRecords,
                    canForceDelete = true
                });
            }

            // Proceed with deletion (normal or force)
            await _procedureService.DeleteProcedureAsync(id, forceDelete);
            TempData["SuccessMessage"] = forceDelete 
                ? "Procedure force deleted successfully!" 
                : "Procedure deleted successfully!";
            return Json(new { 
                success = true, 
                message = forceDelete 
                    ? "Procedure force deleted successfully!" 
                    : "Procedure deleted successfully!" 
            });
        }
        catch (DbUpdateConcurrencyException concurrencyEx)
        {
            _logger.LogError(concurrencyEx, "Concurrency error deleting procedure ID {ProcedureId}", id);
            return Json(new { success = false, message = "The procedure was modified by another user. Please refresh and try again." });
        }
        catch (DbUpdateException dbEx)
        {
            _logger.LogError(dbEx, "Database error deleting procedure ID {ProcedureId}. Inner exception: {InnerException}", id, dbEx.InnerException?.ToString());
            var innerMessage = dbEx.InnerException?.Message ?? dbEx.Message;
            
            // Check dependencies to provide better error message
            var (_, blockingRecords) = await _procedureService.CheckDeleteDependenciesAsync(id);
            if (blockingRecords.Any())
            {
                var blockingMessage = $"Cannot delete procedure. It is referenced by: {string.Join(", ", blockingRecords)}.";
                return Json(new { 
                    success = false, 
                    message = blockingMessage,
                    blockingRecords = blockingRecords,
                    canForceDelete = true,
                    error = innerMessage
                });
            }
            
            return Json(new { success = false, message = $"Cannot delete procedure. Details: {innerMessage}" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting procedure ID {ProcedureId}. Full exception: {FullException}", id, ex.ToString());
            return Json(new { success = false, message = $"Error deleting procedure: {ex.Message}" });
        }
    }

    private async Task<bool> ProcedureExists(int id)
    {
        var procedure = await _procedureService.GetProcedureByIdAsync(id);
        return procedure != null;
    }
}
