using MMGC.Models;
using MMGC.Repositories;
using MMGC.Data;
using Microsoft.EntityFrameworkCore;

namespace MMGC.Services;

public class DoctorService : IDoctorService
{
    private readonly IRepository<Doctor> _repository;
    private readonly ApplicationDbContext _context;

    public DoctorService(IRepository<Doctor> repository, ApplicationDbContext context)
    {
        _repository = repository;
        _context = context;
    }

    public async Task<IEnumerable<Doctor>> GetAllDoctorsAsync()
    {
        return await _context.Doctors
            .OrderBy(d => d.FirstName)
            .ThenBy(d => d.LastName)
            .ToListAsync();
    }

    public async Task<Doctor?> GetDoctorByIdAsync(int id)
    {
        return await _repository.GetByIdAsync(id);
    }

    public async Task<Doctor?> GetDoctorWithDetailsAsync(int id)
    {
        return await _context.Doctors
            .Include(d => d.Appointments)
            .Include(d => d.Procedures)
            .Include(d => d.Schedules)
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task<Doctor> CreateDoctorAsync(Doctor doctor)
    {
        doctor.CreatedDate = DateTime.Now;
        return await _repository.AddAsync(doctor);
    }

    public async Task UpdateDoctorAsync(Doctor doctor)
    {
        doctor.UpdatedDate = DateTime.Now;
        await _repository.UpdateAsync(doctor);
    }

    public async Task<(bool CanDelete, List<string> BlockingRecords, List<string> CascadeRecords)> CheckDeleteDependenciesAsync(int id)
    {
        var blockingRecords = new List<string>();
        var cascadeRecords = new List<string>();

        // Procedures have Cascade delete (will be deleted automatically)
        var proceduresCount = await _context.Procedures.CountAsync(p => p.DoctorId == id);
        if (proceduresCount > 0)
        {
            cascadeRecords.Add($"{proceduresCount} Procedure(s)");
        }

        // DoctorSchedules have Cascade (will be deleted automatically)
        var schedulesCount = await _context.DoctorSchedules.CountAsync(ds => ds.DoctorId == id);
        if (schedulesCount > 0)
        {
            cascadeRecords.Add($"{schedulesCount} Schedule(s)");
        }

        // Check Prescriptions (Restrict - blocks deletion)
        var prescriptionsCount = await _context.Prescriptions.CountAsync(pr => pr.DoctorId == id);
        if (prescriptionsCount > 0)
        {
            blockingRecords.Add($"{prescriptionsCount} Prescription(s)");
        }

        // Appointments have SetNull, so they don't block (DoctorId will be set to NULL)

        return (blockingRecords.Count == 0, blockingRecords, cascadeRecords);
    }

    public async Task DeleteDoctorAsync(int id, bool forceDelete = false)
    {
        // Check if doctor exists
        var exists = await _context.Doctors.AnyAsync(d => d.Id == id);
        if (!exists)
        {
            throw new InvalidOperationException($"Doctor with ID {id} not found.");
        }

        // Always use transaction and set FKs to NULL first to avoid constraint issues
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Set foreign keys to NULL in related tables first
            // Prescriptions (Restrict - need to set NULL manually)
            await _context.Database.ExecuteSqlRawAsync(
                "UPDATE Prescriptions SET DoctorId = NULL WHERE DoctorId = {0}", id);
            
            // Appointments have SetNull, but set explicitly to be safe
            await _context.Database.ExecuteSqlRawAsync(
                "UPDATE Appointments SET DoctorId = NULL WHERE DoctorId = {0}", id);
            
            // Procedures will be deleted automatically by Cascade
            // DoctorSchedules will be deleted automatically by Cascade
            
            // Now delete the doctor (Procedures and DoctorSchedules will cascade automatically)
            var rowsAffected = await _context.Database.ExecuteSqlRawAsync(
                "DELETE FROM Doctors WHERE Id = {0}", id);

            if (rowsAffected == 0)
            {
                throw new InvalidOperationException($"Doctor with ID {id} could not be deleted.");
            }

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<decimal> GetDoctorTotalRevenueAsync(int doctorId)
    {
        var appointments = await _context.Appointments
            .Where(a => a.DoctorId == doctorId && a.Status == "Completed")
            .SumAsync(a => a.ConsultationFee);

        var procedures = await _context.Procedures
            .Where(p => p.DoctorId == doctorId && p.Status == "Completed")
            .SumAsync(p => p.ProcedureFee);

        return appointments + procedures;
    }

    public async Task<int> GetDoctorAppointmentCountAsync(int doctorId)
    {
        return await _context.Appointments
            .CountAsync(a => a.DoctorId == doctorId);
    }
}
