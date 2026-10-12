using Crr.Risk.Api.Data.Seeding;
using Crr.Risk.Api.Querying;
using Crr.Risk.Api.Repositories;
using Crr.Risk.Api.Services;
using Crr.Risk.Api.Validation;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendDev", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:5173",
                "http://127.0.0.1:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
builder.Services.Configure<SeedOptions>(builder.Configuration.GetSection("Seed"));
builder.Services.AddSingleton<CsvSeeder>();
builder.Services.AddHostedService<SeedHostedService>();
builder.Services.AddSingleton<AgGridFilterParser>();
builder.Services.AddSingleton<PositionQueryBuilder>();
builder.Services.AddScoped<RowsRequestValidator>();
builder.Services.AddScoped<IPositionRepository, PositionRepository>();
builder.Services.AddScoped<IPositionService, PositionService>();
builder.Services.AddScoped<IAggregationService, AggregationService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("FrontendDev");

app.MapControllers();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

await app.RunAsync();
