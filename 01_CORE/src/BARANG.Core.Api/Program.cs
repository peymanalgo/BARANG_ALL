using BARANG.Core.Application;
using BARANG.Core.Infrastructure;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddCoreInfrastructure(builder.Configuration);

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/", () => Results.Ok(CoreIdentity.Current))
    .WithName("GetCoreIdentity");
app.MapHealthChecks("/health");

app.Run();

public partial class Program;
