using Microsoft.AspNetCore.Mvc;
using SupportDesk.Application.Departments;

namespace SupportDesk.API.Controllers;

[ApiController]
[Route("api/departments")]
public class DepartmentsController(CreateDepartmentUseCase createDepartment) : ControllerBase
{
    public record CreateDepartmentRequest(string Name);

    [HttpPost]
    public async Task<IActionResult> Create(CreateDepartmentRequest request, CancellationToken ct)
    {
        var dto = await createDepartment.ExecuteAsync(request.Name, ct);
        return Created($"/api/departments/{dto.Id}", dto);
    }
}