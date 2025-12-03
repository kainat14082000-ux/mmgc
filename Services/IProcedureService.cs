using MMGC.Models;

namespace MMGC.Services;

public interface IProcedureService
{
    Task<IEnumerable<Procedure>> GetAllProceduresAsync();
    Task<Procedure?> GetProcedureByIdAsync(int id);
    Task<Procedure> CreateProcedureAsync(Procedure procedure);
    Task UpdateProcedureAsync(Procedure procedure);
    Task<(bool CanDelete, List<string> BlockingRecords)> CheckDeleteDependenciesAsync(int id);
    Task DeleteProcedureAsync(int id, bool forceDelete = false);
}
