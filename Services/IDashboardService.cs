namespace MMGC.Services;

public interface IDashboardService
{
    Task<int> GetTotalAppointmentsAsync();
    Task<int> GetTotalPatientsAsync();
    Task<int> GetTotalProceduresAsync();
    Task<int> GetTotalLabReportsAsync();
    Task<decimal> GetTotalRevenueAsync();
    Task<Dictionary<string, decimal>> GetMonthlyRevenueAsync(int year);
    Task<IEnumerable<object>> GetTodayAppointmentsAsync();
}
