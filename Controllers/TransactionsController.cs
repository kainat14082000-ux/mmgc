using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MMGC.Models;
using MMGC.Services;

namespace MMGC.Controllers;

[Authorize]
public class TransactionsController : Controller
{
    private readonly ITransactionService _transactionService;
    private readonly IPatientService _patientService;
    private readonly IAppointmentService _appointmentService;
    private readonly IProcedureService _procedureService;
    private readonly ILabTestService _labTestService;
    private readonly ILogger<TransactionsController> _logger;

    public TransactionsController(
        ITransactionService transactionService,
        IPatientService patientService,
        IAppointmentService appointmentService,
        IProcedureService procedureService,
        ILabTestService labTestService,
        ILogger<TransactionsController> logger)
    {
        _transactionService = transactionService;
        _patientService = patientService;
        _appointmentService = appointmentService;
        _procedureService = procedureService;
        _labTestService = labTestService;
        _logger = logger;
    }

    // Helper to populate dropdowns
    private async Task PopulateDropDownsAsync(
        object? selectedPatient = null, 
        object? selectedAppointment = null, 
        object? selectedProcedure = null, 
        object? selectedLabTest = null,
        int? patientId = null)
    {
        var patients = await _patientService.GetAllPatientsAsync();
        
        // Filter related records by patient if patient is selected
        var allAppointments = await _appointmentService.GetAllAppointmentsAsync();
        var allProcedures = await _procedureService.GetAllProceduresAsync();
        var allLabTests = await _labTestService.GetAllLabTestsAsync();
        
        var appointments = patientId.HasValue 
            ? allAppointments.Where(a => a.PatientId == patientId.Value).ToList()
            : allAppointments.ToList();
            
        var procedures = patientId.HasValue 
            ? allProcedures.Where(p => p.PatientId == patientId.Value).ToList()
            : allProcedures.ToList();
            
        var labTests = patientId.HasValue 
            ? allLabTests.Where(lt => lt.PatientId == patientId.Value).ToList()
            : allLabTests.ToList();

        ViewBag.Patients = new SelectList(patients, "Id", "FullName", selectedPatient);
        
        // Create meaningful display text for appointments
        var appointmentItems = appointments.Select(a => new {
            Id = a.Id,
            DisplayText = $"Appointment #{a.Id} - {a.AppointmentDate:dd MMM yyyy HH:mm} - Dr. {a.Doctor?.FullName ?? "N/A"} - PKR {a.ConsultationFee:N2}"
        }).ToList();
        ViewBag.Appointments = new SelectList(appointmentItems, "Id", "DisplayText", selectedAppointment);
        
        ViewBag.Procedures = new SelectList(procedures, "Id", "ProcedureName", selectedProcedure);
        ViewBag.LabTests = new SelectList(labTests, "Id", "TestName", selectedLabTest);
        
        // Also provide raw collections for Edit view
        ViewBag.PatientsList = patients;
        ViewBag.AppointmentsList = appointments;
        ViewBag.ProceduresList = procedures;
        ViewBag.LabTestsList = labTests;
    }
    
    // API endpoint to get patient-related records and calculate fees
    [HttpGet]
    public async Task<IActionResult> GetPatientRecords(int patientId)
    {
        try
        {
            // Get all existing transactions to check which items are already paid
            var allTransactions = await _transactionService.GetAllTransactionsAsync();
            
            // Get paid appointment IDs (where status is Completed and not Cancelled/Refunded)
            var paidAppointmentIds = allTransactions
                .Where(t => t.AppointmentId.HasValue && 
                           t.Status == "Completed" && 
                           t.Status != "Cancelled" && 
                           t.Status != "Refunded")
                .Select(t => t.AppointmentId!.Value)
                .ToHashSet();
                
            // Get paid procedure IDs
            var paidProcedureIds = allTransactions
                .Where(t => t.ProcedureId.HasValue && 
                           t.Status == "Completed" && 
                           t.Status != "Cancelled" && 
                           t.Status != "Refunded")
                .Select(t => t.ProcedureId!.Value)
                .ToHashSet();
                
            // Get paid lab test IDs
            var paidLabTestIds = allTransactions
                .Where(t => t.LabTestId.HasValue && 
                           t.Status == "Completed" && 
                           t.Status != "Cancelled" && 
                           t.Status != "Refunded")
                .Select(t => t.LabTestId!.Value)
                .ToHashSet();
            
            var appointments = await _appointmentService.GetAllAppointmentsAsync();
            var procedures = await _procedureService.GetAllProceduresAsync();
            var labTests = await _labTestService.GetAllLabTestsAsync();
            
            // Filter out already paid items
            var patientAppointments = appointments
                .Where(a => a.PatientId == patientId && !paidAppointmentIds.Contains(a.Id))
                .Select(a => new {
                    Id = a.Id,
                    Type = "Appointment",
                    Description = $"Appointment #{a.Id} - {a.AppointmentDate:dd MMM yyyy} - Dr. {a.Doctor?.FullName ?? "N/A"}",
                    Amount = a.ConsultationFee,
                    Date = a.AppointmentDate
                }).ToList();
                
            var patientProcedures = procedures
                .Where(p => p.PatientId == patientId && !paidProcedureIds.Contains(p.Id))
                .Select(p => new {
                    Id = p.Id,
                    Type = "Procedure",
                    Description = p.ProcedureName,
                    Amount = p.ProcedureFee,
                    Date = p.ProcedureDate
                }).ToList();
                
            var patientLabTests = labTests
                .Where(lt => lt.PatientId == patientId && !paidLabTestIds.Contains(lt.Id))
                .Select(lt => new {
                    Id = lt.Id,
                    Type = "LabTest",
                    Description = lt.TestName,
                    Amount = lt.TestFee,
                    Date = lt.TestDate
                }).ToList();
            
            var totalAmount = patientAppointments.Sum(a => a.Amount) +
                            patientProcedures.Sum(p => p.Amount) +
                            patientLabTests.Sum(lt => lt.Amount);
            
            return Json(new {
                success = true,
                appointments = patientAppointments,
                procedures = patientProcedures,
                labTests = patientLabTests,
                totalAmount = totalAmount
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting patient records for patient {PatientId}", patientId);
            return Json(new { success = false, message = ex.Message });
        }
    }

    // GET: Transactions
    public async Task<IActionResult> Index()
    {
        var transactions = await _transactionService.GetAllTransactionsAsync();
        return View(transactions);
    }

    // GET: Transactions/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var transaction = await _transactionService.GetTransactionByIdAsync(id.Value);
        if (transaction == null)
        {
            return NotFound();
        }

        return View(transaction);
    }

    // GET: Transactions/Create
    public async Task<IActionResult> Create()
    {
        await PopulateDropDownsAsync();
        var model = new Transaction
        {
            TransactionDate = DateTime.Now
        };
        return View(model);
    }

    // POST: Transactions/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Transaction transaction)
    {
        // Clear navigation property errors
        ModelState.Remove(nameof(Transaction.Patient));
        ModelState.Remove(nameof(Transaction.Appointment));
        ModelState.Remove(nameof(Transaction.Procedure));
        ModelState.Remove(nameof(Transaction.LabTest));

        if (!ModelState.IsValid)
        {
            await PopulateDropDownsAsync(transaction.PatientId, transaction.AppointmentId, transaction.ProcedureId, transaction.LabTestId);
            return View(transaction);
        }

        // Server-side existence checks
        var patient = await _patientService.GetPatientByIdAsync(transaction.PatientId);
        if (patient == null)
        {
            ModelState.AddModelError(nameof(Transaction.PatientId), "Selected patient does not exist.");
            await PopulateDropDownsAsync(transaction.PatientId, transaction.AppointmentId, transaction.ProcedureId, transaction.LabTestId);
            return View(transaction);
        }

        // Check if items are already paid
        var allTransactions = await _transactionService.GetAllTransactionsAsync();
        var errors = new List<string>();

        if (transaction.AppointmentId.HasValue)
        {
            var isPaid = allTransactions.Any(t => t.AppointmentId == transaction.AppointmentId.Value && 
                                                 t.Status == "Completed" && 
                                                 t.Status != "Cancelled" && 
                                                 t.Status != "Refunded");
            if (isPaid)
            {
                errors.Add($"Appointment #{transaction.AppointmentId.Value} has already been paid.");
            }
        }

        if (transaction.ProcedureId.HasValue)
        {
            var isPaid = allTransactions.Any(t => t.ProcedureId == transaction.ProcedureId.Value && 
                                                 t.Status == "Completed" && 
                                                 t.Status != "Cancelled" && 
                                                 t.Status != "Refunded");
            if (isPaid)
            {
                errors.Add($"Procedure #{transaction.ProcedureId.Value} has already been paid.");
            }
        }

        if (transaction.LabTestId.HasValue)
        {
            var isPaid = allTransactions.Any(t => t.LabTestId == transaction.LabTestId.Value && 
                                                 t.Status == "Completed" && 
                                                 t.Status != "Cancelled" && 
                                                 t.Status != "Refunded");
            if (isPaid)
            {
                errors.Add($"Lab Test #{transaction.LabTestId.Value} has already been paid.");
            }
        }

        if (errors.Any())
        {
            foreach (var error in errors)
            {
                ModelState.AddModelError("", error);
            }
            await PopulateDropDownsAsync(transaction.PatientId, transaction.AppointmentId, transaction.ProcedureId, transaction.LabTestId, transaction.PatientId);
            return View(transaction);
        }

        try
        {
            // If no specific items selected, get all unpaid items for the patient
            if (!transaction.AppointmentId.HasValue && !transaction.ProcedureId.HasValue && !transaction.LabTestId.HasValue)
            {
                // Get all unpaid items for this patient (reuse existing allTransactions variable)
                var paidAppointmentIds = allTransactions
                    .Where(t => t.AppointmentId.HasValue && t.Status == "Completed" && t.Status != "Cancelled" && t.Status != "Refunded")
                    .Select(t => t.AppointmentId!.Value)
                    .ToHashSet();
                var paidProcedureIds = allTransactions
                    .Where(t => t.ProcedureId.HasValue && t.Status == "Completed" && t.Status != "Cancelled" && t.Status != "Refunded")
                    .Select(t => t.ProcedureId!.Value)
                    .ToHashSet();
                var paidLabTestIds = allTransactions
                    .Where(t => t.LabTestId.HasValue && t.Status == "Completed" && t.Status != "Cancelled" && t.Status != "Refunded")
                    .Select(t => t.LabTestId!.Value)
                    .ToHashSet();

                var appointments = await _appointmentService.GetAllAppointmentsAsync();
                var procedures = await _procedureService.GetAllProceduresAsync();
                var labTests = await _labTestService.GetAllLabTestsAsync();

                var unpaidAppointments = appointments.Where(a => a.PatientId == transaction.PatientId && !paidAppointmentIds.Contains(a.Id)).ToList();
                var unpaidProcedures = procedures.Where(p => p.PatientId == transaction.PatientId && !paidProcedureIds.Contains(p.Id)).ToList();
                var unpaidLabTests = labTests.Where(lt => lt.PatientId == transaction.PatientId && !paidLabTestIds.Contains(lt.Id)).ToList();

                // Calculate total and build description
                decimal calculatedAmount = 0;
                var descriptionParts = new List<string>();
                var transactionTypes = new List<string>();

                if (unpaidAppointments.Any())
                {
                    calculatedAmount += unpaidAppointments.Sum(a => a.ConsultationFee);
                    descriptionParts.Add($"{unpaidAppointments.Count} Appointment(s)");
                    transactionTypes.Add("Appointment");
                    // Set first appointment ID
                    transaction.AppointmentId = unpaidAppointments.First().Id;
                }

                if (unpaidProcedures.Any())
                {
                    calculatedAmount += unpaidProcedures.Sum(p => p.ProcedureFee);
                    descriptionParts.Add($"{unpaidProcedures.Count} Procedure(s)");
                    transactionTypes.Add("Procedure");
                    // Set first procedure ID
                    transaction.ProcedureId = unpaidProcedures.First().Id;
                }

                if (unpaidLabTests.Any())
                {
                    calculatedAmount += unpaidLabTests.Sum(lt => lt.TestFee);
                    descriptionParts.Add($"{unpaidLabTests.Count} Lab Test(s)");
                    transactionTypes.Add("LabTest");
                    // Set first lab test ID
                    transaction.LabTestId = unpaidLabTests.First().Id;
                }

                if (calculatedAmount > 0)
                {
                    transaction.Amount = calculatedAmount;
                    if (string.IsNullOrEmpty(transaction.Description))
                    {
                        transaction.Description = "Payment for: " + string.Join(", ", descriptionParts);
                    }
                    transaction.TransactionType = transactionTypes.Count == 1 ? transactionTypes[0] : "Other";
                }
            }
            else
            {
                // Auto-calculate amount and transaction type from selected related records
                decimal calculatedAmount = 0;
                var descriptionParts = new List<string>();
                var transactionTypes = new List<string>();

                if (transaction.AppointmentId.HasValue)
                {
                    var appointment = await _appointmentService.GetAppointmentByIdAsync(transaction.AppointmentId.Value);
                    if (appointment != null)
                    {
                        calculatedAmount += appointment.ConsultationFee;
                        descriptionParts.Add($"Appointment #{appointment.Id} - {appointment.AppointmentDate:dd MMM yyyy}");
                        transactionTypes.Add("Appointment");
                    }
                }

                if (transaction.ProcedureId.HasValue)
                {
                    var procedure = await _procedureService.GetProcedureByIdAsync(transaction.ProcedureId.Value);
                    if (procedure != null)
                    {
                        calculatedAmount += procedure.ProcedureFee;
                        descriptionParts.Add($"Procedure: {procedure.ProcedureName}");
                        transactionTypes.Add("Procedure");
                    }
                }

                if (transaction.LabTestId.HasValue)
                {
                    var labTest = await _labTestService.GetLabTestByIdAsync(transaction.LabTestId.Value);
                    if (labTest != null)
                    {
                        calculatedAmount += labTest.TestFee;
                        descriptionParts.Add($"Lab Test: {labTest.TestName}");
                        transactionTypes.Add("LabTest");
                    }
                }

                // Update amount, description, and transaction type if calculated
                if (calculatedAmount > 0)
                {
                    transaction.Amount = calculatedAmount;
                    if (descriptionParts.Count > 0 && string.IsNullOrEmpty(transaction.Description))
                    {
                        transaction.Description = "Payment for: " + string.Join(", ", descriptionParts);
                    }
                    // Set transaction type based on selected records (prioritize first type, or use "Other" if multiple types)
                    if (transactionTypes.Count == 1)
                    {
                        transaction.TransactionType = transactionTypes[0];
                    }
                    else if (transactionTypes.Count > 1)
                    {
                        transaction.TransactionType = "Other"; // Multiple types
                    }
                }
            }

            if (string.IsNullOrEmpty(transaction.TransactionType))
            {
                transaction.TransactionType = "Other";
            }

            transaction.Patient = null!;
            transaction.Appointment = null;
            transaction.Procedure = null;
            transaction.LabTest = null;

            transaction.CreatedBy = User.Identity?.Name;
            transaction.CreatedDate = DateTime.Now;

            await _transactionService.CreateTransactionAsync(transaction);

            TempData["SuccessMessage"] = "Transaction created successfully!";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error creating transaction");
            ModelState.AddModelError("", "A database error occurred while creating the transaction.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating transaction");
            ModelState.AddModelError("", $"An error occurred: {ex.Message}");
        }

        await PopulateDropDownsAsync(transaction.PatientId, transaction.AppointmentId, transaction.ProcedureId, transaction.LabTestId);
        return View(transaction);
    }

    // GET: Transactions/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var transaction = await _transactionService.GetTransactionByIdAsync(id.Value);
        if (transaction == null)
        {
            return NotFound();
        }

        await PopulateDropDownsAsync(transaction.PatientId, transaction.AppointmentId, transaction.ProcedureId, transaction.LabTestId);
        return View(transaction);
    }

    // POST: Transactions/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Transaction transaction)
    {
        if (id != transaction.Id)
        {
            return NotFound();
        }

        // Clear navigation property errors
        ModelState.Remove(nameof(Transaction.Patient));
        ModelState.Remove(nameof(Transaction.Appointment));
        ModelState.Remove(nameof(Transaction.Procedure));
        ModelState.Remove(nameof(Transaction.LabTest));

        if (!ModelState.IsValid)
        {
            await PopulateDropDownsAsync(transaction.PatientId, transaction.AppointmentId, transaction.ProcedureId, transaction.LabTestId);
            return View(transaction);
        }

        // Server-side existence checks
        var patient = await _patientService.GetPatientByIdAsync(transaction.PatientId);
        if (patient == null)
        {
            ModelState.AddModelError(nameof(Transaction.PatientId), "Selected patient does not exist.");
            await PopulateDropDownsAsync(transaction.PatientId, transaction.AppointmentId, transaction.ProcedureId, transaction.LabTestId);
            return View(transaction);
        }

        try
        {
            transaction.Patient = null!;
            transaction.Appointment = null;
            transaction.Procedure = null;
            transaction.LabTest = null;

            await _transactionService.UpdateTransactionAsync(transaction);

            TempData["SuccessMessage"] = "Transaction updated successfully!";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await TransactionExists(transaction.Id))
            {
                return NotFound();
            }
            ModelState.AddModelError("", "Another user modified this record. Please reload and try again.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating transaction {TransactionId}", id);
            ModelState.AddModelError("", "An error occurred while updating the transaction.");
        }

        await PopulateDropDownsAsync(transaction.PatientId, transaction.AppointmentId, transaction.ProcedureId, transaction.LabTestId);
        return View(transaction);
    }

    // GET: Transactions/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var transaction = await _transactionService.GetTransactionByIdAsync(id.Value);
        if (transaction == null)
        {
            return NotFound();
        }

        return View(transaction);
    }

    // POST: Transactions/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        try
        {
            await _transactionService.DeleteTransactionAsync(id);
            TempData["SuccessMessage"] = "Transaction deleted successfully!";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting transaction {TransactionId}", id);
            TempData["ErrorMessage"] = "An error occurred while deleting the transaction.";
            return RedirectToAction(nameof(Index));
        }
    }

    // POST: Transactions/GenerateInvoice/5
    [HttpPost]
    public async Task<IActionResult> GenerateInvoice(int id)
    {
        try
        {
            var invoicePath = await _transactionService.GenerateInvoiceAsync(id);
            _logger.LogInformation("Invoice generated for transaction {TransactionId}: {InvoicePath}", id, invoicePath);
            return Json(new { success = true, invoicePath });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating invoice for transaction {TransactionId}", id);
            return Json(new { success = false, message = ex.Message });
        }
    }

    // GET: Transactions/DownloadInvoice/5
    public async Task<IActionResult> DownloadInvoice(int id)
    {
        try
        {
            var transaction = await _transactionService.GetTransactionByIdAsync(id);
            if (transaction == null)
            {
                return NotFound();
            }

            if (string.IsNullOrEmpty(transaction.InvoicePath) || !transaction.InvoiceGenerated)
            {
                TempData["ErrorMessage"] = "Invoice has not been generated for this transaction.";
                return RedirectToAction(nameof(Details), new { id });
            }

            // Remove leading slash if present
            var filePath = transaction.InvoicePath.TrimStart('/');
            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", filePath);

            if (!System.IO.File.Exists(fullPath))
            {
                TempData["ErrorMessage"] = "Invoice file not found. Please regenerate the invoice.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var fileName = Path.GetFileName(fullPath);
            var fileBytes = await System.IO.File.ReadAllBytesAsync(fullPath);
            
            // Determine content type based on file extension
            var contentType = fileName.EndsWith(".html", StringComparison.OrdinalIgnoreCase) 
                ? "text/html" 
                : "application/pdf";
            
            // For HTML files, return as HTML so browser can open it
            if (fileName.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
            {
                return File(fileBytes, contentType);
            }
            
            // For PDF files, force download
            return File(fileBytes, contentType, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading invoice for transaction {TransactionId}", id);
            TempData["ErrorMessage"] = "An error occurred while downloading the invoice.";
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    private async Task<bool> TransactionExists(int id)
    {
        var transaction = await _transactionService.GetTransactionByIdAsync(id);
        return transaction != null;
    }
}
