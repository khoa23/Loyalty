using LoyaltyAPI.Security;
using Microsoft.Extensions.FileProviders;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Thêm Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Loyalty API",
        Version = "v1",
        Description = "API quản lý chương trình khách hàng thân thiết"
    });

    // Cấu hình API Key Security Definition
    c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Description = "API Key được sử dụng để xác thực. Nhập API Key của bạn vào đây.",
        Name = "X-API-KEY",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "ApiKeyScheme"
    });

    // Áp dụng Security Requirement cho tất cả endpoints
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "ApiKey"
                },
                In = ParameterLocation.Header
            },
            new List<string>()
        }
    });
});

var app = builder.Build();

// Cấu hình Swagger UI
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Loyalty API v1");
        c.RoutePrefix = "swagger"; // Swagger UI sẽ hiển thị tại /swagger
    });
}

// Kích hoạt Middleware bảo mật cho toàn bộ API (trừ Swagger endpoints)
app.UseMiddleware<ApiKeyMiddleware>();

// Cấu hình static files để serve ảnh từ wwwroot
app.UseStaticFiles();

// Cấu hình static files để serve ảnh từ thư mục Uploads
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(builder.Environment.ContentRootPath, "Uploads")),
    RequestPath = "/Uploads"
});

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.MapGet("/", () => "API is running. Test via .http file or Postman.");

app.Run();