using LoyaltyWebApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace LoyaltyWebApp.Controllers
{
    [Route("Reward")]
    public class RewardApiController : Microsoft.AspNetCore.Mvc.Controller
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly ILogger<RewardApiController> _logger;

        public RewardApiController(IConfiguration configuration, IHttpClientFactory httpClientFactory, ILogger<RewardApiController> logger)
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
                var apiBaseUrl = _configuration.GetValue<string>("ApiBaseUrl");
                var apiKey = _configuration.GetValue<string>("ApiKey");

                if (string.IsNullOrEmpty(apiBaseUrl))
                {
                    _logger.LogError("ApiBaseUrl is not configured");
                    return BadRequest("Configuration Error: ApiBaseUrl missing");
                }

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
                    return PartialView("~/Views/Customer/_RewardList.cshtml", result);
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
                var apiBaseUrl = _configuration.GetValue<string>("ApiBaseUrl");
                var apiKey = _configuration.GetValue<string>("ApiKey");

                if (string.IsNullOrEmpty(apiBaseUrl))
                {
                    _logger.LogError("ApiBaseUrl is not configured");
                    return BadRequest("Configuration Error: ApiBaseUrl missing");
                }

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
                    return PartialView("~/Views/Admin/_RewardTable.cshtml", result);
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