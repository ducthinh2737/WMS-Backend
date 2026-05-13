using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Wms.Api.Extensions;
using Wms.Application.Mapper.Outbound;
using Wms.Infrastructure.Persistence.Context;
using Wms.Infrastructure.Seed;

var builder = WebApplication.CreateBuilder(args);

// --- 1. CONFIGURATION ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var serverVersion = new MySqlServerVersion(new Version(8, 0, 30));
Console.WriteLine($"==> CONNECTION STRING: {connectionString}");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, serverVersion, mysqlOptions =>
    {
        mysqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null);
    }));

builder.Services.AddAuthServices();
builder.Services.AddControllers();
builder.Services.AddApplicationServices();
builder.Services.AddAutoMapper(typeof(OutboundMappingProfile));
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddPermissionPolicies();
builder.Services.AddHttpContextAccessor();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddAutoMapper(typeof(Program));

builder.Services.AddCors(opt =>
{
    opt.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// --- 2. BUILD APP ---
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// --- 3. Migrate + Seed chạy background, không block startup ---
_ = Task.Run(async () =>
{
    await Task.Delay(3000); // Đợi app bind port xong
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    int retry = 0;
    while (retry < 10)
    {
        try
        {
            await db.Database.MigrateAsync();
            await AuthSeeder.SeedAsync(db);
            await TechnicalPlasticWarehouseSeeder.SeedAsync(db);
            logger.LogInformation("✅ DB migration và seed thành công!");
            break;
        }
        catch (Exception ex)
        {
            retry++;
            logger.LogWarning("⚠️ DB chưa sẵn sàng, thử lại lần {0}/10... Lỗi: {1}", retry, ex.Message);
            await Task.Delay(5000);
        }
    }

    if (retry >= 10)
    {
        logger.LogError("❌ Không thể kết nối DB sau 10 lần thử. App vẫn chạy nhưng DB chưa sẵn sàng.");
    }
});

await app.RunAsync();