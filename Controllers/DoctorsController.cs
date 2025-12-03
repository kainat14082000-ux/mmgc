using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MMGC.Models;
using MMGC.Services;

namespace MMGC.Controllers;

[Authorize]
public class DoctorsController : Controller
{
    private readonly IDoctorService _doctorService;

    public DoctorsController(IDoctorService doctorService)
    {
        _doctorService = doctorService;
    }

    // GET: Doctors
    public async Task<IActionResult> Index()
    {
        var doctors = await _doctorService.GetAllDoctorsAsync();
        var doctorsWithStats = new List<object>();
        
        foreach (var doctor in doctors)
        {
            var revenue = await _doctorService.GetDoctorTotalRevenueAsync(doctor.Id);
            var appointments = await _doctorService.GetDoctorAppointmentCountAsync(doctor.Id);
            
            doctorsWithStats.Add(new
            {
                doctor.Id,
                doctor.FirstName,
                doctor.LastName,
                doctor.FullName,
                doctor.Specialization,
                doctor.ContactNumber,
                doctor.Email,
                doctor.IsActive,
                TotalAppointments = appointments,
                TotalRevenue = revenue
            });
        }
        
        ViewBag.DoctorsWithStats = doctorsWithStats;
        return View(doctors);
    }

    // GET: Doctors/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var doctor = await _doctorService.GetDoctorWithDetailsAsync(id.Value);
        if (doctor == null)
        {
            return NotFound();
        }

        ViewBag.TotalRevenue = await _doctorService.GetDoctorTotalRevenueAsync(id.Value);
        ViewBag.TotalAppointments = await _doctorService.GetDoctorAppointmentCountAsync(id.Value);

        return View(doctor);
    }

    // GET: Doctors/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: Doctors/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("FirstName,LastName,Specialization,ContactNumber,Email,LicenseNumber,Address,ConsultationFee,IsActive")] Doctor doctor)
    {
        if (ModelState.IsValid)
        {
            try
            {
                await _doctorService.CreateDoctorAsync(doctor);
                TempData["SuccessMessage"] = "Doctor added successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error creating doctor: {ex.Message}");
            }
        }
        return View(doctor);
    }

    // GET: Doctors/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var doctor = await _doctorService.GetDoctorByIdAsync(id.Value);
        if (doctor == null)
        {
            return NotFound();
        }
        return View(doctor);
    }

    // POST: Doctors/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,FirstName,LastName,Specialization,ContactNumber,Email,LicenseNumber,Address,ConsultationFee,IsActive")] Doctor doctor)
    {
        if (id != doctor.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                await _doctorService.UpdateDoctorAsync(doctor);
                TempData["SuccessMessage"] = "Doctor updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                if (!await DoctorExists(doctor.Id))
                {
                    return NotFound();
                }
                ModelState.AddModelError("", $"Error updating doctor: {ex.Message}");
            }
        }
        return View(doctor);
    }

    // POST: Doctors/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, bool forceDelete = false)
    {
        try
        {
            var doctor = await _doctorService.GetDoctorByIdAsync(id);
            if (doctor == null)
            {
                return Json(new { success = false, message = "Doctor not found." });
            }

            // Check dependencies first
            var (canDelete, blockingRecords, cascadeRecords) = await _doctorService.CheckDeleteDependenciesAsync(id);
            
            // If there are cascade records, show warning even if no blocking records
            if (cascadeRecords.Any() && !forceDelete)
            {
                var warningMessage = cascadeRecords.Count == 1 
                    ? $"Deleting this doctor will also delete {cascadeRecords[0]}."
                    : $"Deleting this doctor will also delete: {string.Join(", ", cascadeRecords)}.";
                
                return Json(new { 
                    success = false, 
                    message = warningMessage,
                    blockingRecords = blockingRecords,
                    cascadeRecords = cascadeRecords,
                    canForceDelete = true,
                    isWarning = true
                });
            }
            
            if (!canDelete && !forceDelete)
            {
                // Return blocking records so frontend can show force delete option
                var blockingMessage = $"Cannot delete doctor. It is referenced by: {string.Join(", ", blockingRecords)}.";
                return Json(new { 
                    success = false, 
                    message = blockingMessage,
                    blockingRecords = blockingRecords,
                    cascadeRecords = cascadeRecords,
                    canForceDelete = true
                });
            }

            // Proceed with deletion (normal or force)
            await _doctorService.DeleteDoctorAsync(id, forceDelete);
            TempData["SuccessMessage"] = forceDelete 
                ? "Doctor force deleted successfully!" 
                : "Doctor deleted successfully!";
            return Json(new { 
                success = true, 
                message = forceDelete 
                    ? "Doctor force deleted successfully!" 
                    : "Doctor deleted successfully!" 
            });
        }
        catch (DbUpdateConcurrencyException)
        {
            return Json(new { success = false, message = "The doctor was modified by another user. Please refresh and try again." });
        }
        catch (DbUpdateException dbEx)
        {
            // Always check dependencies to provide better error message
            var (_, blockingRecords, cascadeRecords) = await _doctorService.CheckDeleteDependenciesAsync(id);
            if (blockingRecords.Any())
            {
                var blockingMessage = $"Cannot delete doctor. It is referenced by: {string.Join(", ", blockingRecords)}.";
                return Json(new { 
                    success = false, 
                    message = blockingMessage,
                    blockingRecords = blockingRecords,
                    cascadeRecords = cascadeRecords,
                    canForceDelete = true
                });
            }
            
            // If no blocking records found but still error, show generic message
            var innerMessage = dbEx.InnerException?.Message ?? dbEx.Message;
            return Json(new { success = false, message = $"Cannot delete doctor. Please try again or contact support if the issue persists." });
        }
        catch (Exception)
        {
            // Check dependencies even for other exceptions
            try
            {
                var (_, blockingRecords, cascadeRecords) = await _doctorService.CheckDeleteDependenciesAsync(id);
                if (blockingRecords.Any())
                {
                    var blockingMessage = $"Cannot delete doctor. It is referenced by: {string.Join(", ", blockingRecords)}.";
                    return Json(new { 
                        success = false, 
                        message = blockingMessage,
                        blockingRecords = blockingRecords,
                        cascadeRecords = cascadeRecords,
                        canForceDelete = true
                    });
                }
            }
            catch { }
            
            // Final fallback - show user-friendly message
            return Json(new { 
                success = false, 
                message = "Cannot delete doctor. It may be referenced by other records. Please check and try again."
            });
        }
    }

    private async Task<bool> DoctorExists(int id)
    {
        var doctor = await _doctorService.GetDoctorByIdAsync(id);
        return doctor != null;
    }
}
