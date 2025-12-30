var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddHttpClient("LoyaltyAPI", client =>
{
    client.BaseAddress = new Uri(builder.Configuration.GetValue<string>("ApiBaseUrl") ?? "https://localhost:5001/");
    var apiKey = builder.Configuration.GetValue<string>("ApiKey");
    if (!string.IsNullOrEmpty(apiKey))
    {
        client.DefaultRequestHeaders.Add("X-API-KEY", apiKey);
    }
});

// Add session management
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Register services
builder.Services.AddScoped<LoyaltyWebApp.Services.IUserService, LoyaltyWebApp.Services.UserService>();
builder.Services.AddScoped<LoyaltyWebApp.Services.IRewardService, LoyaltyWebApp.Services.RewardService>();
builder.Services.AddScoped<LoyaltyWebApp.Services.ILoyaltyService, LoyaltyWebApp.Services.LoyaltyService>();

var app = builder.Build();

if (!app.Environment.IsProduction())
{
    app.UseDeveloperExceptionPage();
}

app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.MapRazorPages();
app.Run();
