using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MMGC.Models;
using MMGC.Services;
using MMGC.Repositories;

namespace MMGC.Controllers;

[Authorize]
public class LabTestsController : Controller
{
    private readonly ILabTestService _labTestService;
    private readonly IPatientService _patientService;
    private readonly IProcedureService _procedureService;
    private readonly ILogger<LabTestsController> _logger;

    public LabTestsController(
        ILabTestService labTestService,
        IPatientService patientService,
        IProcedureService procedureService,
        ILogger<LabTestsController> logger)
    {
        _labTestService = labTestService;
        _patientService = patientService;
        _procedureService = procedureService;
        _logger = logger;
    }

    // Helper to populate dropdowns
    private async Task PopulateDropDownsAsync(object? selectedPatient = null, object? selectedCategory = null, object? selectedProcedure = null)
    {
        var patients = await _patientService.GetAllPatientsAsync();
        var categories = await _labTestService.GetAllCategoriesAsync();
        var procedures = await _procedureService.GetAllProceduresAsync();

        ViewBag.Patients = new SelectList(patients, "Id", "FullName", selectedPatient);
        ViewBag.Categories = new SelectList(categories, "Id", "CategoryName", selectedCategory);
        ViewBag.Procedures = new SelectList(procedures, "Id", "ProcedureName", selectedProcedure);
        
        // Also provide raw collections for Edit view
        ViewBag.PatientsList = patients;
        ViewBag.CategoriesList = categories;
        ViewBag.ProceduresList = procedures;
    }

    // GET: LabTests
    public async Task<IActionResult> Index()
    {
        var labTests = await _labTestService.GetAllLabTestsAsync();
        return View(labTests);
    }

    // GET: LabTests/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var labTest = await _labTestService.GetLabTestByIdAsync(id.Value);
        if (labTest == null)
        {
            return NotFound();
        }

        return View(labTest);
    }

    // GET: LabTests/Create
    public async Task<IActionResult> Create()
    {
        await PopulateDropDownsAsync();
        var model = new LabTest
        {
            TestDate = DateTime.Now
        };
        return View(model);
    }

    // POST: LabTests/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(LabTest labTest)
    {
        // Clear navigation property errors
        ModelState.Remove(nameof(LabTest.Patient));
        ModelState.Remove(nameof(LabTest.LabTestCategory));
        ModelState.Remove(nameof(LabTest.Procedure));

        if (!ModelState.IsValid)
        {
            await PopulateDropDownsAsync(labTest.PatientId, labTest.LabTestCategoryId, labTest.ProcedureId);
            return View(labTest);
        }

        // Server-side existence checks
        var patient = await _patientService.GetPatientByIdAsync(labTest.PatientId);
        if (patient == null)
        {
            ModelState.AddModelError(nameof(LabTest.PatientId), "Selected patient does not exist.");
            await PopulateDropDownsAsync(labTest.PatientId, labTest.LabTestCategoryId, labTest.ProcedureId);
            return View(labTest);
        }

        var category = await _labTestService.GetAllCategoriesAsync();
        if (!category.Any(c => c.Id == labTest.LabTestCategoryId))
        {
            ModelState.AddModelError(nameof(LabTest.LabTestCategoryId), "Selected category does not exist.");
            await PopulateDropDownsAsync(labTest.PatientId, labTest.LabTestCategoryId, labTest.ProcedureId);
            return View(labTest);
        }

        try
        {
            labTest.Patient = null!;
            labTest.LabTestCategory = null!;
            labTest.Procedure = null;

            labTest.CreatedBy = User.Identity?.Name;
            labTest.Status = "Pending";
            labTest.CreatedDate = DateTime.Now;

            await _labTestService.CreateLabTestAsync(labTest);

            TempData["SuccessMessage"] = "Lab test created successfully!";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error creating lab test");
            ModelState.AddModelError("", "A database error occurred while creating the lab test.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating lab test");
            ModelState.AddModelError("", $"An error occurred: {ex.Message}");
        }

        await PopulateDropDownsAsync(labTest.PatientId, labTest.LabTestCategoryId, labTest.ProcedureId);
        return View(labTest);
    }

    // GET: LabTests/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var labTest = await _labTestService.GetLabTestByIdAsync(id.Value);
        if (labTest == null)
        {
            return NotFound();
        }

        await PopulateDropDownsAsync(labTest.PatientId, labTest.LabTestCategoryId, labTest.ProcedureId);
        return View(labTest);
    }

    // POST: LabTests/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, LabTest labTest)
    {
        if (id != labTest.Id)
        {
            return NotFound();
        }

        // Clear navigation property errors
        ModelState.Remove(nameof(LabTest.Patient));
        ModelState.Remove(nameof(LabTest.LabTestCategory));
        ModelState.Remove(nameof(LabTest.Procedure));

        if (!ModelState.IsValid)
        {
            await PopulateDropDownsAsync(labTest.PatientId, labTest.LabTestCategoryId, labTest.ProcedureId);
            return View(labTest);
        }

        // Server-side existence checks
        var patient = await _patientService.GetPatientByIdAsync(labTest.PatientId);
        if (patient == null)
        {
            ModelState.AddModelError(nameof(LabTest.PatientId), "Selected patient does not exist.");
            await PopulateDropDownsAsync(labTest.PatientId, labTest.LabTestCategoryId, labTest.ProcedureId);
            return View(labTest);
        }

        try
        {
            labTest.Patient = null!;
            labTest.LabTestCategory = null!;
            labTest.Procedure = null;

            labTest.UpdatedDate = DateTime.Now;

            await _labTestService.UpdateLabTestAsync(labTest);

            TempData["SuccessMessage"] = "Lab test updated successfully!";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await LabTestExists(labTest.Id))
            {
                return NotFound();
            }
            ModelState.AddModelError("", "Another user modified this record. Please reload and try again.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating lab test {LabTestId}", id);
            ModelState.AddModelError("", "An error occurred while updating the lab test.");
        }

        await PopulateDropDownsAsync(labTest.PatientId, labTest.LabTestCategoryId, labTest.ProcedureId);
        return View(labTest);
    }

    // GET: LabTests/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var labTest = await _labTestService.GetLabTestByIdAsync(id.Value);
        if (labTest == null)
        {
            return NotFound();
        }

        return View(labTest);
    }

    // POST: LabTests/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        try
        {
            await _labTestService.DeleteLabTestAsync(id);
            TempData["SuccessMessage"] = "Lab test deleted successfully!";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting lab test {LabTestId}", id);
            TempData["ErrorMessage"] = "An error occurred while deleting the lab test.";
            return RedirectToAction(nameof(Index));
        }
    }

    // GET: LabTests/UploadReport/5
    public async Task<IActionResult> UploadReport(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var labTest = await _labTestService.GetLabTestByIdAsync(id.Value);
        if (labTest == null)
        {
            return NotFound();
        }

        return View(labTest);
    }

    // POST: LabTests/UploadReport/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadReport(int id, IFormFile reportFile, string? notes)
    {
        var labTest = await _labTestService.GetLabTestByIdAsync(id);
        if (labTest == null)
        {
            return NotFound();
        }

        if (reportFile == null || reportFile.Length == 0)
        {
            ModelState.AddModelError("reportFile", "Please select a file to upload.");
            return View(labTest);
        }

        // Validate file size (10MB max)
        const long maxFileSize = 10 * 1024 * 1024; // 10MB
        if (reportFile.Length > maxFileSize)
        {
            ModelState.AddModelError("reportFile", "File size exceeds 10MB. Please select a smaller file.");
            return View(labTest);
        }

        // Validate file extension
        var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png", ".doc", ".docx" };
        var fileExtension = Path.GetExtension(reportFile.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(fileExtension))
        {
            ModelState.AddModelError("reportFile", "Invalid file type. Allowed types: PDF, JPG, PNG, DOC, DOCX");
            return View(labTest);
        }

        try
        {
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "labreports");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var fileName = $"{id}_{DateTime.Now:yyyyMMddHHmmss}_{Path.GetFileName(reportFile.FileName)}";
            var filePath = Path.Combine(uploadsFolder, fileName);
            var relativePath = $"/uploads/labreports/{fileName}";

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await reportFile.CopyToAsync(stream);
            }

            await _labTestService.UploadReportAsync(id, relativePath, notes);
            TempData["SuccessMessage"] = "Lab test report uploaded successfully!";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading report for lab test {LabTestId}", id);
            ModelState.AddModelError("", "An error occurred while uploading the report. Please try again.");
            return View(labTest);
        }
    }

    private async Task<bool> LabTestExists(int id)
    {
        var labTest = await _labTestService.GetLabTestByIdAsync(id);
        return labTest != null;
    }
}
