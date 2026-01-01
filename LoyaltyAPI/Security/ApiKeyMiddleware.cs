namespace LoyaltyAPI.Security
{
    public class ApiKeyMiddleware
    {
        private readonly RequestDelegate _next;
        private const string APIKEYNAME = "X-API-KEY";

        public ApiKeyMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Bỏ qua kiểm tra API Key cho Swagger endpoints, OPTIONS, và Uploads
            var path = context.Request.Path.Value?.ToLower() ?? "";
            if (path.StartsWith("/swagger") || path == "/" || path == "" || path.StartsWith("/uploads") || context.Request.Method == "OPTIONS")
            {
                await _next(context);
                return;
            }

            // Kiểm tra xem Header có chứa X-API-KEY không
            if (!context.Request.Headers.TryGetValue(APIKEYNAME, out var extractedApiKey))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Api Key was not provided.");
                return;
            }

            // Lấy Key từ appsettings.json
            var configuration = context.RequestServices.GetRequiredService<IConfiguration>();
            var apiKey = configuration.GetValue<string>("ApiConfig:ApiKey");

            if (!apiKey.Equals(extractedApiKey))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Unauthorized client.");
                return;
            }

            await _next(context);
        }
    }
}