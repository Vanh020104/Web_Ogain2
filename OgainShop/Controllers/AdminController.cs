using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OgainShop.Data;
using OgainShop.Models;
using OgainShop.Models.Authentication;

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
        public async Task<IActionResult> order()
        {
            var ogainShopContext = _context.Order.Include(o => o.User);

            return View("OrderManagement/order", await ogainShopContext.ToListAsync());
        }
        [Authentication]
        public IActionResult detailOrder()
        {
            return View("OrderManagement/detailOrder");
        }
        public IActionResult successOrder()
        {
            return View("CustomerManagement/successOrder");
        }
        public IActionResult detailsUser(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = _context.User.FirstOrDefault(m => m.UserId == id);
            if (user == null)
            {
                return NotFound();
            }

            return View("CustomerManagement/detailsUser", user);
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
