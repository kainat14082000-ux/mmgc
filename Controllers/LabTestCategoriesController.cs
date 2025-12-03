using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MMGC.Models;
using MMGC.Services;
using MMGC.Repositories;
using MMGC.Data;

namespace MMGC.Controllers;

[Authorize]
public class LabTestCategoriesController : Controller
{
    private readonly ILabTestService _labTestService;
    private readonly IRepository<LabTestCategory> _categoryRepository;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<LabTestCategoriesController> _logger;

    public LabTestCategoriesController(
        ILabTestService labTestService,
        IRepository<LabTestCategory> categoryRepository,
        ApplicationDbContext context,
        ILogger<LabTestCategoriesController> logger)
    {
        _labTestService = labTestService;
        _categoryRepository = categoryRepository;
        _context = context;
        _logger = logger;
    }

    // GET: LabTestCategories
    public async Task<IActionResult> Index()
    {
        // Get all categories (not just active ones) for admin view
        var categories = await _context.LabTestCategories
            .OrderBy(c => c.CategoryName)
            .ToListAsync();
        return View(categories);
    }

    // GET: LabTestCategories/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: LabTestCategories/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(LabTestCategory category)
    {
        if (!ModelState.IsValid)
        {
            return View(category);
        }

        try
        {
            category.CreatedDate = DateTime.Now;
            await _categoryRepository.AddAsync(category);
            TempData["SuccessMessage"] = "Lab test category created successfully!";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating lab test category");
            ModelState.AddModelError("", "An error occurred while creating the category.");
            return View(category);
        }
    }

    // GET: LabTestCategories/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var category = await _categoryRepository.GetByIdAsync(id.Value);
        if (category == null)
        {
            return NotFound();
        }
        return View(category);
    }

    // POST: LabTestCategories/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, LabTestCategory category)
    {
        if (id != category.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(category);
        }

        try
        {
            await _categoryRepository.UpdateAsync(category);
            TempData["SuccessMessage"] = "Lab test category updated successfully!";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await CategoryExists(category.Id))
            {
                return NotFound();
            }
            ModelState.AddModelError("", "Another user modified this record. Please reload and try again.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating lab test category {CategoryId}", id);
            ModelState.AddModelError("", "An error occurred while updating the category.");
        }

        return View(category);
    }

    // POST: LabTestCategories/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        try
        {
            var category = await _categoryRepository.GetByIdAsync(id);
            if (category == null)
            {
                return NotFound();
            }

            // Check if category has associated lab tests
            var hasLabTests = await _context.LabTests.AnyAsync(lt => lt.LabTestCategoryId == id);
            if (hasLabTests)
            {
                TempData["ErrorMessage"] = "Cannot delete category. It has associated lab tests. Please deactivate it instead.";
                return RedirectToAction(nameof(Index));
            }

            await _categoryRepository.DeleteAsync(category);
            TempData["SuccessMessage"] = "Lab test category deleted successfully!";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting lab test category {CategoryId}", id);
            TempData["ErrorMessage"] = "An error occurred while deleting the category.";
            return RedirectToAction(nameof(Index));
        }
    }

    private async Task<bool> CategoryExists(int id)
    {
        var category = await _categoryRepository.GetByIdAsync(id);
        return category != null;
    }
}
