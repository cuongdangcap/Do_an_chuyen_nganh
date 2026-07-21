using Admissions.Api.Common;
using Admissions.Application.Dashboard;
using Admissions.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Admissions.Api.Controllers;

[ApiController]
[Route("api/admin/dashboard")]
[Authorize(Roles = RoleCodes.Admin + "," + RoleCodes.Staff)]
public sealed class AdminDashboardController(IDashboardService dashboardService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var dashboard = await dashboardService.GetAdminDashboardAsync(cancellationToken);
        return Ok(ApiResponse<AdminDashboardDto>.Ok(dashboard, "OK", HttpContext.TraceIdentifier));
    }
}
