﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OgainShop.Data;
using OgainShop.Models;
using OgainShop.Models.Authentication;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Net.NetworkInformation;


namespace OgainShop.Controllers
{



    public class AdminController : Controller
    {
        // GET: /<controller>/

        // Order Management

        private readonly OgainShopContext _context;

        public AdminController(OgainShopContext context)
        {
            _context = context;
        }
        [Authentication]
        public async Task<IActionResult> order(string TotalAmount, string ShippingMethod, string PaymentMethod, string IsPaid, string Status, string search)
        {
            // Lấy danh sách đơn hàng từ cơ sở dữ liệu
            var orders = _context.Order.AsQueryable();

            // Áp dụng các tiêu chí lọc nếu chúng được cung cấp
            if (!string.IsNullOrEmpty(TotalAmount))
            {
                decimal totalAmountValue;
                if (decimal.TryParse(TotalAmount, out totalAmountValue))
                {
                    // Lọc theo TotalAmount
                    orders = orders.Where(o => o.TotalAmount == totalAmountValue);
                }
            }

            if (!string.IsNullOrEmpty(ShippingMethod))
            {
                // Lọc theo ShippingMethod
                orders = orders.Where(o => o.ShippingMethod == ShippingMethod);
            }

            if (!string.IsNullOrEmpty(PaymentMethod))
            {
                // Lọc theo PaymentMethod
                orders = orders.Where(o => o.PaymentMethod == PaymentMethod);
            }

            if (!string.IsNullOrEmpty(IsPaid))
            {
                // Lọc theo trạng thái đã thanh toán (Paid/Unpaid)
                bool isPaid = IsPaid == "1";
                orders = orders.Where(o => o.IsPaid == (isPaid ? "paid" : "unpaid"));
            }

            if (!string.IsNullOrEmpty(Status))
            {
                // Lọc theo Status
                orders = orders.Where(o => o.Status == Status);
            }

            if (!string.IsNullOrEmpty(search))
            {
                // Lọc theo từ khóa tìm kiếm trong tên người đặt hàng, email hoặc các trường khác
                orders = orders.Where(o => o.FullName.Contains(search)
                                        || o.Email.Contains(search)
                                        || o.TotalAmount.ToString().Contains(search)
                                        || o.ShippingMethod.Contains(search)
                                        || o.PaymentMethod.Contains(search)
                                        || o.IsPaid.Contains(search)
                                        || o.Status.Contains(search));
            }

            // Trả về view với danh sách đơn hàng đã lọc
            return View("OrderManagement/order", await orders.ToListAsync());
        }


        [Authentication]
        public async Task<IActionResult> detailOrder(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var order = _context.Order
                .Include(o => o.User)
                .Include(o => o.OrderProducts)
                    .ThenInclude(op => op.Product)
                .FirstOrDefault(o => o.OrderId == id);

            if (order == null)
            {
                return NotFound();
            }

            return View("OrderManagement/detailOrder", order);
        }
        [Authentication]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string status, string returnUrl)
        {
            var order = await _context.Order.FindAsync(id);

            if (order == null)
            {
                return NotFound();
            }

            // Cập nhật trạng thái cho đơn hàng
            order.Status = status;
            _context.Update(order);
            await _context.SaveChangesAsync();

            // Chuyển hướng đến returnUrl
            return Redirect(returnUrl);
        }


        [Authentication]
        // Customer Management
        public async Task<IActionResult> Customer()
        {
            if (_context.User != null)
            {
                var userList = await _context.User.ToListAsync();
                return View("CustomerManagement/customer", userList);
            }
            else
            {
                return Problem("Entity set 'OgainShopContext.User' is null.");
            }
        }
        [Authentication]
        public IActionResult OrderUser(int? id)
        {
            if (id == null)
            {
                return NotFound(); // Trả về lỗi 404 nếu không có ID người dùng
            }

            // Lấy thông tin người dùng từ cơ sở dữ liệu dựa trên id
            var user = _context.User.Include(u => u.Orders).FirstOrDefault(u => u.UserId == id);

            if (user == null)
            {
                return NotFound(); // Trả về lỗi 404 nếu không tìm thấy người dùng
            }

            return View("CustomerManagement/OrderUser", user);
        }
        [Authentication]
        public IActionResult OrderDetailsUser(int? id)
        {
            if (id == null)
            {
                return NotFound(); // Trả về lỗi 404 nếu không có ID đơn hàng
            }

            // Lấy thông tin đơn hàng từ cơ sở dữ liệu dựa trên id và nạp thông tin User và OrderProducts
            var order = _context.Order
                .Include(o => o.User)
                .Include(o => o.OrderProducts)
                    .ThenInclude(op => op.Product) // Nạp thông tin sản phẩm cho từng OrderProduct
                .FirstOrDefault(o => o.OrderId == id);

            if (order == null)
            {
                return NotFound(); // Trả về lỗi 404 nếu không tìm thấy đơn hàng
            }

            return View("CustomerManagement/OrderDetailsUser", order);
        }







        [Authentication]
        // Product Management
        public async Task<IActionResult> Product()
        {
            var ogainShopContext = _context.Product.Include(p => p.Category);
            return View("ProductManagement/Product", await ogainShopContext.ToListAsync());
        }
        [Authentication]

        public IActionResult addProduct()
        {
            var categories = _context.Category.OrderBy(c => c.CategoryName).ToList();
            ViewBag.Categories = new SelectList(categories, "CategoryId", "CategoryName");

            return View("ProductManagement/addProduct");
        }
        [Authentication]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> addProduct(Product model, IFormFile thumbnail)
        {
            if (true)
            {
                // Xử lý khi dữ liệu hợp lệ

                // Xử lý tệp tin ảnh và lưu đường dẫn
                if (thumbnail != null && thumbnail.Length > 0)
                {
                    var uploadsFolder = Path.Combine("wwwroot", "img", "product");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    var imagePath = Path.Combine(uploadsFolder, Guid.NewGuid().ToString() + "_" + thumbnail.FileName);
                    using (var stream = new FileStream(imagePath, FileMode.Create))
                    {
                        await thumbnail.CopyToAsync(stream);
                    }

                    // Lưu đường dẫn vào trường Thumbnail của mô hình
                    model.Thumbnail = "/img/product/" + Path.GetFileName(imagePath);
                }
                _context.Add(model);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Product));
            }

            // Trả về View nếu dữ liệu không hợp lệ
            return View(model);
        }

        [Authentication]
        [HttpGet]
        public async Task<IActionResult> editProduct(int id)
        {
            // Lấy thông tin sản phẩm cần chỉnh sửa từ cơ sở dữ liệu
            var product = await _context.Product.FindAsync(id);

            if (product == null)
            {
                return NotFound();
            }

            // Lấy danh sách các danh mục để hiển thị trong dropdownlist
            var categories = _context.Category.OrderBy(c => c.CategoryName).ToList();
            ViewBag.Categories = new SelectList(categories, "CategoryId", "CategoryName");

            return View("ProductManagement/editProduct", product);
        }

        [Authentication]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> editProduct(Product model, IFormFile thumbnail)
        {
            if (true)
            {
                // Xử lý tệp tin ảnh và lưu đường dẫn
                if (thumbnail != null && thumbnail.Length > 0)
                {
                    var uploadsFolder = Path.Combine("wwwroot", "img", "product");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    var imagePath = Path.Combine(uploadsFolder, Guid.NewGuid().ToString() + "_" + thumbnail.FileName);
                    using (var stream = new FileStream(imagePath, FileMode.Create))
                    {
                        await thumbnail.CopyToAsync(stream);
                    }

                    // Lưu đường dẫn vào trường Thumbnail của mô hình
                    model.Thumbnail = "/img/product/" + Path.GetFileName(imagePath);
                }
                else
                {
                    var existingProduct = _context.Product.AsNoTracking().FirstOrDefault(p => p.ProductId == model.ProductId);
                    if (existingProduct != null)
                    {
                        model.Thumbnail = existingProduct.Thumbnail;
                    }
                }

                _context.Update(model);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Product));
            }

            // Trả về View nếu dữ liệu không hợp lệ
            var categories = _context.Category.OrderBy(c => c.CategoryName).ToList();
            ViewBag.Categories = new SelectList(categories, "CategoryId", "CategoryName");

            return View("ProductManagement/editProduct", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> deleteProduct(int id)
        {
            var product = await _context.Product.FindAsync(id);

            if (product == null)
            {
                return NotFound();
            }

            // Xóa ảnh sản phẩm khỏi thư mục
            if (!string.IsNullOrEmpty(product.Thumbnail))
            {
                var imagePath = Path.Combine("wwwroot", product.Thumbnail.TrimStart('/'));
                if (System.IO.File.Exists(imagePath))
                {
                    System.IO.File.Delete(imagePath);
                }
            }

            _context.Product.Remove(product);
            await _context.SaveChangesAsync();

            // Chuyển hướng về trang ProductManagement/Product
            return RedirectToAction("Product", "Admin");
        }






        [Authentication]
        // Dashboard
        public IActionResult dashboard()
        {
            return View("DashboardAdmin/dashboard");
        }





        [Authentication]
        // Revenue
        public IActionResult revenue()
        {
            return View("RevenueManagement/revenue");
        }

    }
}