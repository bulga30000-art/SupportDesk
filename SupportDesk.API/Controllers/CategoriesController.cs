using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/categories")]
public class CategoriesController : ControllerBase
{
    private readonly CreateCategoryUseCase _create;
    public CategoriesController(CreateCategoryUseCase create) => _create = create;

    [HttpPost]
    public async Task<IActionResult> Create(CreateCategoryRequest request, CancellationToken ct)
    {
        var dto = await _create.ExecuteAsync(request.Name, request.DepartmentId, ct);
        return Created($"/api/categories/{dto.Id}", dto);
    }
}

public record CreateCategoryRequest(string Name, Guid DepartmentId);