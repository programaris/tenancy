using Programaris.Tenancy.Abstractions;
using Programaris.Tenancy.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://localhost:5001");

builder.Services.AddProgramarisTenancy(options =>
{
    options.HeaderName = TenantHeaderOptions.DefaultHeaderName;
    options.RequireTenant = true;
});

var app = builder.Build();

app.UseRouting();
app.UseProgramarisTenancy();

app
    .MapGet("/health", () => Results.Ok(new { status = "healthy" }))
    .AllowMissingTenant();

app.MapGet("/v1/tenant", (ITenantContextAccessor accessor) =>
    Results.Ok(new { requestedTenant = accessor.GetRequired().Slug }));

app.Run();