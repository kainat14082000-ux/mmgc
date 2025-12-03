using MMGC.Data;
using Microsoft.EntityFrameworkCore;

namespace MMGC.Services;

public class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _context;

    public DashboardService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> GetTotalAppointmentsAsync()
    {
        return await _context.Appointments.CountAsync();
    }

    public async Task<int> GetTotalPatientsAsync()
    {
        return await _context.Patients.CountAsync();
    }

    public async Task<int> GetTotalProceduresAsync()
    {
        return await _context.Procedures.CountAsync();
    }

    public async Task<int> GetTotalLabReportsAsync()
    {
        return await _context.LabTests.CountAsync(lt => lt.Status == "Completed" && !string.IsNullOrEmpty(lt.ReportFilePath));
    }

    public async Task<decimal> GetTotalRevenueAsync()
    {
        return await _context.Transactions
            .Where(t => t.Status == "Completed")
            .SumAsync(t => t.Amount);
    }

    public async Task<Dictionary<string, decimal>> GetMonthlyRevenueAsync(int year)
    {
        var monthlyRevenue = await _context.Transactions
            .Where(t => t.TransactionDate.Year == year && t.Status == "Completed")
            .GroupBy(t => t.TransactionDate.Month)
            .Select(g => new { Month = g.Key, Revenue = g.Sum(t => t.Amount) })
            .ToListAsync();

        var result = new Dictionary<string, decimal>();
        var monthNames = new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };

        for (int i = 1; i <= 12; i++)
        {
            var monthData = monthlyRevenue.FirstOrDefault(m => m.Month == i);
            result[monthNames[i - 1]] = monthData?.Revenue ?? 0;
        }

        return result;
    }

    public async Task<IEnumerable<object>> GetTodayAppointmentsAsync()
    {
        var today = DateTime.Today;
        return await _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .Where(a => a.AppointmentDate.Date == today)
            .OrderBy(a => a.AppointmentDate)
            .Select(a => new
            {
                a.Id,
                PatientName = a.Patient.FirstName + " " + a.Patient.LastName,
                DoctorName = a.Doctor != null ? a.Doctor.FirstName + " " + a.Doctor.LastName : "Not Assigned",
                a.AppointmentDate,
                a.Status,
                a.AppointmentType
            })
            .ToListAsync();
    }
}
