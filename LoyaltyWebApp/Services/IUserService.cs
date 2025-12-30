using LoyaltyWebApp.Models;

namespace LoyaltyWebApp.Services
{
    public interface IUserService
    {
        Task<LoginResponse?> LoginAsync(LoginRequest request);
        Task<string?> RegisterAsync(RegisterRequest request);
        Task<long?> GetCustomerPointsAsync(long userId);
    }
}

