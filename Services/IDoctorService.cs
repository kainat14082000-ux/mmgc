using MMGC.Models;

namespace MMGC.Services;

public interface IDoctorService
{
    Task<IEnumerable<Doctor>> GetAllDoctorsAsync();
    Task<Doctor?> GetDoctorByIdAsync(int id);
    Task<Doctor?> GetDoctorWithDetailsAsync(int id);
    Task<Doctor> CreateDoctorAsync(Doctor doctor);
    Task UpdateDoctorAsync(Doctor doctor);
    Task<(bool CanDelete, List<string> BlockingRecords, List<string> CascadeRecords)> CheckDeleteDependenciesAsync(int id);
    Task DeleteDoctorAsync(int id, bool forceDelete = false);
    Task<decimal> GetDoctorTotalRevenueAsync(int doctorId);
    Task<int> GetDoctorAppointmentCountAsync(int doctorId);
}
