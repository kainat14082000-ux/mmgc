using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MMGC.Services;

namespace MMGC.Controllers;

[Authorize]
public class AdminController : Controller
{
    private readonly IDashboardService _dashboardService;

    public AdminController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    public async Task<IActionResult> Dashboard()
    {
        ViewBag.TotalAppointments = await _dashboardService.GetTotalAppointmentsAsync();
        ViewBag.TotalPatients = await _dashboardService.GetTotalPatientsAsync();
        ViewBag.TotalProcedures = await _dashboardService.GetTotalProceduresAsync();
        ViewBag.TotalLabReports = await _dashboardService.GetTotalLabReportsAsync();
        ViewBag.TotalRevenue = await _dashboardService.GetTotalRevenueAsync();
        ViewBag.MonthlyRevenue = await _dashboardService.GetMonthlyRevenueAsync(DateTime.Now.Year);
        ViewBag.TodayAppointments = await _dashboardService.GetTodayAppointmentsAsync();

        return View();
    }
}
