using MMGC.Models;

namespace MMGC.Services;

public interface ILabTestService
{
    Task<IEnumerable<LabTest>> GetAllLabTestsAsync();
    Task<LabTest?> GetLabTestByIdAsync(int id);
    Task<IEnumerable<LabTestCategory>> GetAllCategoriesAsync();
    Task<LabTest> CreateLabTestAsync(LabTest labTest);
    Task UpdateLabTestAsync(LabTest labTest);
    Task DeleteLabTestAsync(int id);
    Task UploadReportAsync(int labTestId, string filePath, string? notes);
}
