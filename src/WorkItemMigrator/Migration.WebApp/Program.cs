using Microsoft.EntityFrameworkCore;
using Migration.WebApp.Infrastructure.Data;
using Migration.WebApp.Infrastructure.Services;
using Migration.WebApp.Shared;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddDbContext<MigrationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register services
builder.Services.AddScoped<IConfigurationService, ConfigurationService>();
builder.Services.AddScoped<IAttachmentStorageService, AttachmentStorageService>();
builder.Services.AddScoped<IConfigurationImportService, ConfigurationImportService>();
builder.Services.AddScoped<IExportService, ExportService>();
builder.Services.AddScoped<DatabaseSeeder>();

// Register export-related services
builder.Services.AddScoped<ItemHashCalculator>();
builder.Services.AddScoped<SmartExportDecisionEngine>();

// Add web services
builder.Services.AddRazorPages();
builder.Services.AddSignalR();

var app = builder.Build();

// Configure pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapRazorPages();
app.MapHub<MigrationProgressHub>("/migrationHub");

// Add export API endpoints
app.MapPost("/Export/Cancel/{id:int}", async (int id, IExportService exportService) =>
{
    await exportService.CancelExportAsync(id);
    return Results.Ok();
});

// Seed database
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
    await seeder.SeedAsync();
}

app.Run();