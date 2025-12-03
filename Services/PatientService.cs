using MMGC.Models;
using MMGC.Repositories;
using MMGC.Data;
using Microsoft.EntityFrameworkCore;

namespace MMGC.Services;

public class PatientService : IPatientService
{
    private readonly IRepository<Patient> _repository;
    private readonly ApplicationDbContext _context;

    public PatientService(IRepository<Patient> repository, ApplicationDbContext context)
    {
        _repository = repository;
        _context = context;
    }

    public async Task<IEnumerable<Patient>> GetAllPatientsAsync()
    {
        return await _context.Patients
            .OrderByDescending(p => p.CreatedDate)
            .ToListAsync();
    }

    public async Task<Patient?> GetPatientByIdAsync(int id)
    {
        return await _repository.GetByIdAsync(id);
    }

    public async Task<Patient?> GetPatientByMRNumberAsync(string mrNumber)
    {
        return await _repository.FirstOrDefaultAsync(p => p.MRNumber == mrNumber);
    }

    public async Task<Patient> CreatePatientAsync(Patient patient)
    {
        // Generate MR Number if not provided
        if (string.IsNullOrEmpty(patient.MRNumber))
        {
            patient.MRNumber = await GenerateMRNumberAsync();
        }
        
        patient.CreatedDate = DateTime.Now;
        return await _repository.AddAsync(patient);
    }

    public async Task UpdatePatientAsync(Patient patient)
    {
        patient.UpdatedDate = DateTime.Now;
        await _repository.UpdateAsync(patient);
    }

    public async Task DeletePatientAsync(int id)
    {
        var patient = await _repository.GetByIdAsync(id);
        if (patient != null)
        {
            await _repository.DeleteAsync(patient);
        }
    }

    public async Task<Patient?> GetPatientWithHistoryAsync(int id)
    {
        return await _context.Patients
            .Include(p => p.Appointments)
                .ThenInclude(a => a.Doctor)
            .Include(p => p.Procedures)
                .ThenInclude(pr => pr.Doctor)
            .Include(p => p.LabTests)
                .ThenInclude(lt => lt.LabTestCategory)
            .Include(p => p.Transactions)
            .Include(p => p.Prescriptions)
                .ThenInclude(pr => pr.Doctor)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    private async Task<string> GenerateMRNumberAsync()
    {
        var year = DateTime.Now.Year;
        var count = await _context.Patients.CountAsync(p => p.CreatedDate.Year == year);
        return $"MR{year}{count + 1:D5}";
    }
}
