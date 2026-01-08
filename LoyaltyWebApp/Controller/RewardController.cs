using LoyaltyWebApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace LoyaltyWebApp.Controller
{
    [Route("Reward")]
    public class RewardController : Microsoft.AspNetCore.Mvc.Controller
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly ILogger<RewardController> _logger;

        public RewardController(IConfiguration configuration, IHttpClientFactory httpClientFactory, ILogger<RewardController> logger)
        {
            _configuration = configuration;
            _httpClient = httpClientFactory.CreateClient("LoyaltyAPI");
            _logger = logger;
        }

        [HttpGet("GetRewards")]
        public async Task<IActionResult> GetRewards(int page = 1, int pageSize = 6)
        {
            try
            {
                var apiBaseUrl = _configuration.GetValue<string>("ApiBaseUrl") ?? "https://localhost:5001/";
                var apiKey = _configuration.GetValue<string>("ApiKey");

                if (!apiBaseUrl.EndsWith("/")) apiBaseUrl += "/";

                var requestUrl = $"{apiBaseUrl}api/User/customer/rewards?page={page}&pageSize={pageSize}";
                var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
                
                if (!string.IsNullOrEmpty(apiKey))
                {
                    request.Headers.Add("X-API-KEY", apiKey);
                }

                var response = await _httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var result = JsonSerializer.Deserialize<RewardListViewModel>(content, options);
                    
                    ViewData["ApiBaseUrl"] = apiBaseUrl;
                    return PartialView("~/Pages/Customer/_RewardList.cshtml", result);
                }
                
                _logger.LogError($"API Error: {response.StatusCode}");
                return BadRequest($"Lỗi API: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetRewards");
                return BadRequest("Lỗi hệ thống: " + ex.Message);
            }
        }

        [HttpGet("GetAdminRewards")]
        public async Task<IActionResult> GetAdminRewards(int page = 1, int pageSize = 10)
        {
            try
            {
                var apiBaseUrl = _configuration.GetValue<string>("ApiBaseUrl") ?? "https://localhost:5001/";
                var apiKey = _configuration.GetValue<string>("ApiKey");

                if (!apiBaseUrl.EndsWith("/")) apiBaseUrl += "/";

                // Gọi API Admin
                var requestUrl = $"{apiBaseUrl}api/Loyalty/admin/rewards?page={page}&pageSize={pageSize}";
                var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);

                if (!string.IsNullOrEmpty(apiKey))
                {
                    request.Headers.Add("X-API-KEY", apiKey);
                }

                var response = await _httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        NumberHandling = JsonNumberHandling.AllowReadingFromString
                    };
                    var result = JsonSerializer.Deserialize<RewardListViewModel>(content, options);

                    if (result == null)
                    {
                        _logger.LogError("API returned null or failed to deserialize.");
                        return BadRequest("Không có dữ liệu từ API.");
                    }

                    ViewData["ApiBaseUrl"] = apiBaseUrl;
                    ViewData["PageSize"] = pageSize;
                    return PartialView("~/Pages/Admin/_RewardTable.cshtml", result);
                }

                _logger.LogError($"API Error: {response.StatusCode}");
                return BadRequest($"Lỗi API: {response.StatusCode}");
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error in GetAdminRewards");
                return BadRequest("Lỗi hệ thống: " + ex.Message);
            }
        }

    }
}