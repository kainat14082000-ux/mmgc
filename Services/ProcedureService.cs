using MMGC.Models;
using MMGC.Repositories;
using MMGC.Data;
using Microsoft.EntityFrameworkCore;

namespace MMGC.Services;

public class ProcedureService : IProcedureService
{
    private readonly IRepository<Procedure> _repository;
    private readonly ApplicationDbContext _context;

    public ProcedureService(IRepository<Procedure> repository, ApplicationDbContext context)
    {
        _repository = repository;
        _context = context;
    }

    public async Task<IEnumerable<Procedure>> GetAllProceduresAsync()
    {
        return await _context.Procedures
            .Include(p => p.Patient)
            .Include(p => p.Doctor)
            .Include(p => p.Nurse)
            .OrderByDescending(p => p.ProcedureDate)
            .ToListAsync();
    }

    public async Task<Procedure?> GetProcedureByIdAsync(int id)
    {
        return await _context.Procedures
            .Include(p => p.Patient)
            .Include(p => p.Doctor)
            .Include(p => p.Nurse)
            .Include(p => p.LabTests)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Procedure> CreateProcedureAsync(Procedure procedure)
    {
        procedure.CreatedDate = DateTime.Now;
        
        // Use context directly to avoid navigation property issues
        await _context.Procedures.AddAsync(procedure);
        await _context.SaveChangesAsync();
        return procedure;
    }

    public async Task UpdateProcedureAsync(Procedure procedure)
    {
        procedure.UpdatedDate = DateTime.Now;
        await _repository.UpdateAsync(procedure);
    }

    public async Task<(bool CanDelete, List<string> BlockingRecords)> CheckDeleteDependenciesAsync(int id)
    {
        var blockingRecords = new List<string>();

        // Check LabTests
        var labTestsCount = await _context.LabTests.CountAsync(lt => lt.ProcedureId == id);
        if (labTestsCount > 0)
        {
            blockingRecords.Add($"{labTestsCount} Lab Test(s)");
        }

        // Check Transactions
        var transactionsCount = await _context.Transactions.CountAsync(t => t.ProcedureId == id);
        if (transactionsCount > 0)
        {
            blockingRecords.Add($"{transactionsCount} Transaction(s)");
        }

        // Check Prescriptions
        var prescriptionsCount = await _context.Prescriptions.CountAsync(pr => pr.ProcedureId == id);
        if (prescriptionsCount > 0)
        {
            blockingRecords.Add($"{prescriptionsCount} Prescription(s)");
        }

        return (blockingRecords.Count == 0, blockingRecords);
    }

    public async Task DeleteProcedureAsync(int id, bool forceDelete = false)
    {
        // Check if procedure exists
        var exists = await _context.Procedures.AnyAsync(p => p.Id == id);
        if (!exists)
        {
            throw new InvalidOperationException($"Procedure with ID {id} not found.");
        }

        // Always use transaction and set FKs to NULL first to avoid constraint issues
        // SQL Server's SET NULL behavior doesn't work reliably with raw SQL DELETE
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Set foreign keys to NULL in related tables first
            await _context.Database.ExecuteSqlRawAsync(
                "UPDATE LabTests SET ProcedureId = NULL WHERE ProcedureId = {0}", id);
            await _context.Database.ExecuteSqlRawAsync(
                "UPDATE Transactions SET ProcedureId = NULL WHERE ProcedureId = {0}", id);
            await _context.Database.ExecuteSqlRawAsync(
                "UPDATE Prescriptions SET ProcedureId = NULL WHERE ProcedureId = {0}", id);
            
            // Now delete the procedure
            var rowsAffected = await _context.Database.ExecuteSqlRawAsync(
                "DELETE FROM Procedures WHERE Id = {0}", id);

            if (rowsAffected == 0)
            {
                throw new InvalidOperationException($"Procedure with ID {id} could not be deleted.");
            }

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
