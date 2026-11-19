using Microsoft.Extensions.DependencyInjection;
using SupportDesk.Application.Departments;

namespace SupportDesk.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<CreateDepartmentUseCase>();
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<CreateCategoryUseCase>();
        services.AddScoped<CreateTicketUseCase>();
        services.AddScoped<ClaimTicketUseCase>();
        services.AddScoped<AssignTicketUseCase>();
        return services;
    }
}