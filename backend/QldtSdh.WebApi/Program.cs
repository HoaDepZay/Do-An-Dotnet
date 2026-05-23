using Microsoft.EntityFrameworkCore;
using QldtSdh.Data;

var builder = WebApplication.CreateBuilder(args);

// 1. Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure Database Connection
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured in appsettings.json.");
}
builder.Services.AddDataServices(connectionString);

var app = builder.Build();

// 2. Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "QLDT Sau đại học API v1");
    });
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

// 3. Auto-Migrate & Seed Database at Startup
try
{
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<QldtSdhDbContext>();
        Console.WriteLine("Đang đồng bộ cơ sở dữ liệu trên Azure SQL...");
        DbInitializer.Initialize(dbContext);
        Console.WriteLine("Cơ sở dữ liệu đã đồng bộ và sẵn sàng!");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"[LỖI] Khởi tạo database khi start Web API thất bại: {ex.Message}");
}

app.Run();
