﻿using Microsoft.AspNetCore.Mvc;
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
        public async Task<IActionResult> MyOrder(int page = 1, int pageSize = 5)
        {
            // Kế thừa các logic chung từ BaseController
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

            // Logic phân trang ở đây
            var totalOrders = user.Orders.Count;
            var totalPages = (int)Math.Ceiling((double)totalOrders / pageSize);

            user.Orders = user.Orders
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.TotalPages = totalPages;
            ViewBag.CurrentPage = page;


            // Trả dữ liệu danh sách đơn hàng cho view
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
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string status, string returnUrl)
        {
            var order = await _context.Order
                .Include(o => o.OrderProducts)
                    .ThenInclude(op => op.Product)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null)
            {
                return NotFound();
            }

            if (status.ToLower() == "cancel" && order.Status.ToLower() == "pending")
            {
                // Cập nhật trạng thái cho đơn hàng
                order.Status = status;

                // Trả lại số lượng sản phẩm đã mua
                foreach (var orderProduct in order.OrderProducts)
                {
                    // Kiểm tra xem số lượng trả lại có vượt quá số lượng đã mua không
                    int quantityToReturn = Math.Min(orderProduct.Qty, orderProduct.Product.Qty);

                    // Cập nhật số lượng trong kho
                    orderProduct.Product.Qty += quantityToReturn;

                }

                _context.Update(order);
                await _context.SaveChangesAsync();
            }
            // Cập nhật trạng thái cho đơn hàng
            order.Status = status;
            _context.Update(order);
            await _context.SaveChangesAsync();


            // Chuyển hướng đến returnUrl
            return Redirect(returnUrl);
        }
        [Authentication]
        public async Task<IActionResult> OrderPending(int page = 1, int pageSize = 5)
        {
            // Kế thừa các logic chung từ BaseController
            await SetCommonViewData();

            // Lấy userId từ session
            int userId = Convert.ToInt32(HttpContext.Session.GetString("UserId"));

            // Truy vấn database để lấy thông tin người dùng có đơn hàng có trạng thái là "Pending"
            var user = db.User
                .Include(u => u.Orders)
                    .ThenInclude(o => o.OrderProducts)
                .FirstOrDefault(u => u.UserId == userId);

            if (user == null)
            {
                return NotFound();
            }
            // Logic phân trang ở đây
            var totalOrders = user.Orders.Count(o => o.Status == "pending");
            var totalPages = (int)Math.Ceiling((double)totalOrders / pageSize);
            user.Orders = user.Orders
                  .Where(o => o.Status == "pending")
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
              .ToList();

            ViewBag.TotalPages = totalPages;
            ViewBag.CurrentPage = page;



            // Trả dữ liệu người dùng có đơn hàng Pending cho view
            return View(user);
        }

        [Authentication]
        public async Task<IActionResult> OrderConfirmed(int page = 1, int pageSize = 5)
        {
            // Kế thừa các logic chung từ BaseController
            await SetCommonViewData();

            // Lấy userId từ session
            int userId = Convert.ToInt32(HttpContext.Session.GetString("UserId"));

            // Truy vấn database để lấy thông tin người dùng có đơn hàng có trạng thái là "Pending"
            var user = db.User
                .Include(u => u.Orders)
                    .ThenInclude(o => o.OrderProducts)
                .FirstOrDefault(u => u.UserId == userId);

            if (user == null)
            {
                return NotFound();
            }
            // Logic phân trang ở đây
            var totalOrders = user.Orders.Count(o => o.Status == "confirmed");
            var totalPages = (int)Math.Ceiling((double)totalOrders / pageSize);
            user.Orders = user.Orders
                  .Where(o => o.Status == "confirmed")
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
              .ToList();

            ViewBag.TotalPages = totalPages;
            ViewBag.CurrentPage = page;
            // Lọc chỉ những đơn hàng có trạng thái là "Pending"


            // Trả dữ liệu người dùng có đơn hàng Pending cho view
            return View(user);
        }

        [Authentication]
        public async Task<IActionResult> OrderShipping(int page = 1, int pageSize = 5)
        {
            // Kế thừa các logic chung từ BaseController
            await SetCommonViewData();

            // Lấy userId từ session
            int userId = Convert.ToInt32(HttpContext.Session.GetString("UserId"));

            // Truy vấn database để lấy thông tin người dùng có đơn hàng có trạng thái là "Pending"
            var user = db.User
                .Include(u => u.Orders)
                    .ThenInclude(o => o.OrderProducts)
                .FirstOrDefault(u => u.UserId == userId);

            if (user == null)
            {
                return NotFound();
            }
            // Logic phân trang ở đây
            var totalOrders = user.Orders.Count(o => o.Status == "shipping");
            var totalPages = (int)Math.Ceiling((double)totalOrders / pageSize);
            user.Orders = user.Orders
                  .Where(o => o.Status == "shipping")
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
              .ToList();

            ViewBag.TotalPages = totalPages;
            ViewBag.CurrentPage = page;



            // Trả dữ liệu người dùng có đơn hàng Pending cho view
            return View(user);
        }

        [Authentication]
        public async Task<IActionResult> OrderShipped(int page = 1, int pageSize = 5)
        {
            // Kế thừa các logic chung từ BaseController
            await SetCommonViewData();

            // Lấy userId từ session
            int userId = Convert.ToInt32(HttpContext.Session.GetString("UserId"));

            // Truy vấn database để lấy thông tin người dùng có đơn hàng có trạng thái là "Pending"
            var user = db.User
                .Include(u => u.Orders)
                    .ThenInclude(o => o.OrderProducts)
                .FirstOrDefault(u => u.UserId == userId);

            if (user == null)
            {
                return NotFound();
            }
            // Logic phân trang ở đây
            var totalOrders = user.Orders.Count(o => o.Status == "shipped");
            var totalPages = (int)Math.Ceiling((double)totalOrders / pageSize);
            user.Orders = user.Orders
                  .Where(o => o.Status == "shipped")
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
              .ToList();

            ViewBag.TotalPages = totalPages;
            ViewBag.CurrentPage = page;


            // Trả dữ liệu người dùng có đơn hàng Pending cho view
            return View(user);
        }

        [Authentication]
        public async Task<IActionResult> OrderComplete(int page = 1, int pageSize = 5)
        {
            // Kế thừa các logic chung từ BaseController
            await SetCommonViewData();

            // Lấy userId từ session
            int userId = Convert.ToInt32(HttpContext.Session.GetString("UserId"));

            // Truy vấn database để lấy thông tin người dùng có đơn hàng có trạng thái là "Pending"
            var user = db.User
                .Include(u => u.Orders)
                    .ThenInclude(o => o.OrderProducts)
                .FirstOrDefault(u => u.UserId == userId);

            if (user == null)
            {
                return NotFound();
            }
            // Logic phân trang ở đây
            var totalOrders = user.Orders.Count(o => o.Status == "complete");
            var totalPages = (int)Math.Ceiling((double)totalOrders / pageSize);
            user.Orders = user.Orders
                  .Where(o => o.Status == "complete")
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
              .ToList();

            ViewBag.TotalPages = totalPages;
            ViewBag.CurrentPage = page;


            // Trả dữ liệu người dùng có đơn hàng Pending cho view
            return View(user);
        }

        [Authentication]
        public async Task<IActionResult> OrderCancel(int page = 1, int pageSize = 5)
        {
            // Kế thừa các logic chung từ BaseController
            await SetCommonViewData();

            // Lấy userId từ session
            int userId = Convert.ToInt32(HttpContext.Session.GetString("UserId"));

            // Truy vấn database để lấy thông tin người dùng có đơn hàng có trạng thái là "Pending"
            var user = db.User
                .Include(u => u.Orders)
                    .ThenInclude(o => o.OrderProducts)
                .FirstOrDefault(u => u.UserId == userId);

            if (user == null)
            {
                return NotFound();
            }
            // Logic phân trang ở đây
            var totalOrders = user.Orders.Count(o => o.Status == "cancel");
            var totalPages = (int)Math.Ceiling((double)totalOrders / pageSize);
            user.Orders = user.Orders
                  .Where(o => o.Status == "cancel")
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
              .ToList();

            ViewBag.TotalPages = totalPages;
            ViewBag.CurrentPage = page;


            // Trả dữ liệu người dùng có đơn hàng Pending cho view
            return View(user);
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

