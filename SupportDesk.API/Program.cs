using SupportDesk.API.Middleware;
using SupportDesk.Application;
using SupportDesk.Application.Options;
using SupportDesk.Infrastructure;
using System.Text.Json.Serialization;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddOptions<SlaOptions>()
    .Bind(builder.Configuration.GetSection(SlaOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart(); 

var app = builder.Build();

app.UseExceptionHandler(); 
app.UseHttpsRedirection();
app.MapControllers();

app.Run();

public partial class Program;