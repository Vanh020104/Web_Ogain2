using Microsoft.AspNetCore.Mvc;
using OgainShop.Models;
using OgainShop.Data;
using Microsoft.AspNetCore.Http;
using OgainShop.Models.Authentication;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;
using System;
using System.Text;
using OgainShop.Heplers;
using System.Threading.Tasks;
using System.Net;
using System.Net.Mail;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace OgainShop.Controllers
{
    public class MyAccountController : BaseController
    {
        private readonly OgainShopContext db;
        private readonly IConfiguration _configuration;

        public MyAccountController(OgainShopContext context, IConfiguration configuration) : base(context)
        {
            db = context;
            _configuration = configuration;
        }
        [Authentication]
        public async Task<IActionResult> MyOrder()
        {
            // kế thừa các logic chung từ BaseController
            await SetCommonViewData();
            // Lấy userId từ session
            int userId = Convert.ToInt32(HttpContext.Session.GetString("UserId"));

            // Truy vấn database để lấy thông tin người dùng có đơn hàng
            var user = db.User
             .Include(u => u.Orders)
             .ThenInclude(o => o.OrderProducts)
             .FirstOrDefault(u => u.UserId == userId);

            if (user == null)
            {
                return NotFound();
            }

            // Trả dữ liệu người dùng có đơn hàng cho view
            return View(user);
        }

        public IActionResult OrderDetail(int? id)
        {

            if (id == null)
            {
                return NotFound(); // Trả về lỗi 404 nếu không có ID đơn hàng
            }

            // Lấy thông tin đơn hàng từ cơ sở dữ liệu dựa trên id và nạp thông tin User và OrderProducts
            var order = db.Order
                .Include(o => o.User)
                .Include(o => o.OrderProducts)
                    .ThenInclude(op => op.Product) // Nạp thông tin sản phẩm cho từng OrderProduct
                .FirstOrDefault(o => o.OrderId == id);

            if (order == null)
            {
                return NotFound(); // Trả về lỗi 404 nếu không tìm thấy đơn hàng
            }

            return View(order);
        }


        [Authentication]
        public async Task<IActionResult> ChangePassword()
        {
            await SetCommonViewData();
            return View();
        }

        [HttpPost]
        [Authentication]
        public async Task<IActionResult> ChangePassword(string current_password, string new_password, string new_password_confirmation)
        {
            if (new_password != new_password_confirmation)
            {
                TempData["Message"] = "Password confirmation does not match.";
                TempData["MessageColor"] = "alert-danger"; // Màu đỏ
                return RedirectToAction("ChangePassword", "MyAccount");
            }

            var userId = HttpContext.Session.GetString("UserId");
            var user = await db.User.FindAsync(int.Parse(userId));

            if (user == null)
            {
                return NotFound();
            }

            if (!BCrypt.Net.BCrypt.Verify(current_password, user.Password))
            {
                TempData["Message"] = "Current password is incorrect.";
                TempData["MessageColor"] = "alert-danger"; // Màu đỏ
                return RedirectToAction("ChangePassword", "MyAccount");
            }

            user.Password = BCrypt.Net.BCrypt.HashPassword(new_password);
            db.Entry(user).State = EntityState.Modified;
            await db.SaveChangesAsync();

            TempData["Message"] = "Password changed successfully.";
            TempData["MessageColor"] = "alert-success"; // Màu xanh lá cây
            return RedirectToAction("ChangePassword", "MyAccount");
        }




        [Authentication]
        public async Task<IActionResult> Profile()
        {
            // kế thừa các logic chung từ BaseController
            await SetCommonViewData();
            return View();
        }

        [Authentication]
        public async Task<IActionResult> EditProfile()
        {
            // kế thừa các logic chung từ BaseController
            await SetCommonViewData();
            return View();
        }
    }
}

