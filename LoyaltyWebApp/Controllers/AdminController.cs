using LoyaltyWebApp.Models;
using LoyaltyWebApp.Services;
using Microsoft.AspNetCore.Mvc;

namespace LoyaltyWebApp.Controllers
{
    public class AdminController : Controller
    {
        private readonly IRewardService _rewardService;
        private readonly ICustomerService _customerService;

        public AdminController(IRewardService rewardService, ICustomerService customerService)
        {
            _rewardService = rewardService;
            _customerService = customerService;
        }

        public IActionResult Index() // Reward Management
        {
            return View();
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(RewardItem model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await _rewardService.CreateRewardAsync(model.Reward_Name, model.Description, model.Points_Cost, model.Stock_Quantity, model.Is_Active ?? false, null, null);
            
            if (result == null) // null means success
            {
                return RedirectToAction("Index");
            }
             
             ModelState.AddModelError("", result ?? "Lỗi khi tạo quà");
             return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var reward = await _rewardService.GetRewardByIdAsync(id);
            if (reward == null)
            {
                return NotFound();
            }
            return View(reward);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(RewardItem model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await _rewardService.UpdateRewardAsync(model.Reward_Id, model.Reward_Name, model.Description, model.Points_Cost, model.Stock_Quantity, model.Is_Active ?? false, null, null);
            
            if (result == null) // null means success
            {
                return RedirectToAction("Index");
            }

            ModelState.AddModelError("", result ?? "Lỗi khi cập nhật quà");
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            // Interface: Task<string?> DeleteRewardAsync(string rewardId);
            var result = await _rewardService.DeleteRewardAsync(id);
            // return JSON
            return Json(new { success = true });
        }

        [HttpGet]
        public async Task<IActionResult> Customers()
        {
            var userId = HttpContext.Session.GetString("UserId");
            var userRole = HttpContext.Session.GetString("UserRole");

            if (string.IsNullOrEmpty(userId) || userRole != "Admin")
            {
                return RedirectToAction("Login", "Account");
            }

            var customers = await _customerService.GetCustomersAsync();
            if (customers == null)
            {
                ViewBag.ErrorMessage = "Không thể tải danh sách khách hàng từ máy chủ";
                customers = new List<CustomerModel>();
            }

            return View(customers);
        }

        [HttpGet]
        public async Task<IActionResult> TopRedeemers()
        {
            var userId = HttpContext.Session.GetString("UserId");
            var userRole = HttpContext.Session.GetString("UserRole");

            if (string.IsNullOrEmpty(userId) || userRole != "Admin")
            {
                return RedirectToAction("Login", "Account");
            }

            var topRedeemers = await _customerService.GetTopRedeemersAsync() ?? new List<CustomerStatisticViewModel>();
            return View(topRedeemers);
        }
    }
}
