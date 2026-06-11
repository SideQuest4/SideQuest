using Microsoft.EntityFrameworkCore;
using SideQuest.Api.Data;

var builder = WebApplication.CreateBuilder(args);

// ---- Database ----------------------------------------------------------------
// Use PostgreSQL when a connection string is configured (production / real dev),
// otherwise fall back to an in-memory database so the API runs with zero setup.
var postgres = builder.Configuration.GetConnectionString("Postgres");
var useInMemory = string.IsNullOrWhiteSpace(postgres);

builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (useInMemory)
        options.UseInMemoryDatabase("sidequest-dev");
    else
        options.UseNpgsql(postgres);
});

// ---- Web / API ---------------------------------------------------------------
builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        // Serialize enums as strings everywhere for a friendlier API surface.
        o.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

const string CorsPolicy = "frontend";
builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicy, policy => policy
        .WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
        .AllowAnyHeader()
        .AllowAnyMethod());
});

var app = builder.Build();

// ---- Database init -----------------------------------------------------------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (useInMemory)
        await db.Database.EnsureCreatedAsync();
    else
        await db.Database.MigrateAsync();

    await SeedData.EnsureSeededAsync(db);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(CorsPolicy);
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();
