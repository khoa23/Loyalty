using LoyaltyWebApp.Models;

namespace LoyaltyWebApp.Services
{
    public interface IUserService
    {
        Task<LoginResponse?> LoginAsync(LoginRequest request);
        Task<string?> RegisterAsync(RegisterRequest request);
        Task<CustomerInfo?> GetCustomerInfoAsync(long userId);
        Task<long?> GetCustomerPointsAsync(long userId);
    }
}

