using MMGC.Models;
using MMGC.Repositories;
using MMGC.Data;
using Microsoft.EntityFrameworkCore;

namespace MMGC.Services;

public class LabTestService : ILabTestService
{
    private readonly IRepository<LabTest> _repository;
    private readonly IRepository<LabTestCategory> _categoryRepository;
    private readonly ApplicationDbContext _context;

    public LabTestService(
        IRepository<LabTest> repository,
        IRepository<LabTestCategory> categoryRepository,
        ApplicationDbContext context)
    {
        _repository = repository;
        _categoryRepository = categoryRepository;
        _context = context;
    }

    public async Task<IEnumerable<LabTest>> GetAllLabTestsAsync()
    {
        return await _context.LabTests
            .Include(lt => lt.Patient)
            .Include(lt => lt.LabTestCategory)
            .Include(lt => lt.Procedure)
            .OrderByDescending(lt => lt.TestDate)
            .ToListAsync();
    }

    public async Task<LabTest?> GetLabTestByIdAsync(int id)
    {
        return await _context.LabTests
            .Include(lt => lt.Patient)
            .Include(lt => lt.LabTestCategory)
            .Include(lt => lt.Procedure)
            .FirstOrDefaultAsync(lt => lt.Id == id);
    }

    public async Task<IEnumerable<LabTestCategory>> GetAllCategoriesAsync()
    {
        return await _context.LabTestCategories
            .Where(c => c.IsActive)
            .OrderBy(c => c.CategoryName)
            .ToListAsync();
    }

    public async Task<LabTest> CreateLabTestAsync(LabTest labTest)
    {
        labTest.CreatedDate = DateTime.Now;
        // Use context directly to avoid navigation property issues
        await _context.LabTests.AddAsync(labTest);
        await _context.SaveChangesAsync();
        return labTest;
    }

    public async Task UpdateLabTestAsync(LabTest labTest)
    {
        labTest.UpdatedDate = DateTime.Now;
        // Use context directly to avoid navigation property issues
        _context.LabTests.Update(labTest);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteLabTestAsync(int id)
    {
        var labTest = await _repository.GetByIdAsync(id);
        if (labTest != null)
        {
            await _repository.DeleteAsync(labTest);
        }
    }

    public async Task UploadReportAsync(int labTestId, string filePath, string? notes)
    {
        var labTest = await GetLabTestByIdAsync(labTestId);
        if (labTest != null)
        {
            labTest.ReportFilePath = filePath;
            labTest.ReportNotes = notes;
            labTest.Status = "Completed";
            labTest.UpdatedDate = DateTime.Now;
            // Use context directly
            _context.LabTests.Update(labTest);
            await _context.SaveChangesAsync();
        }
    }
}
