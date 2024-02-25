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

namespace OgainShop.Controllers
{
    public class PageController : BaseController
    {
        private readonly OgainShopContext db;
        private readonly IConfiguration _configuration;

        public PageController(OgainShopContext context, IConfiguration configuration) : base(context)
        {
            db = context;
            _configuration = configuration;
        }
        //Home
        [Authentication]
        public async Task<IActionResult> Home()
        {
            // Retrieve distinct categories from the database
            var distinctCategories = await db.Category.ToListAsync();
            // kế thừa các logic chung từ BaseController
            await SetCommonViewData();

            // Pass the distinct categories to the view
            ViewBag.Categories = distinctCategories;

            // Retrieve only the top 8 products with their categories
            var productsWithCategories = await db.Product.Include(p => p.Category)
                                                        .Take(8) // Take only 8 products
                                                        .ToListAsync();

            // Pass the products to the view
            ViewBag.Products = productsWithCategories;

            return View();
        }

        //Search
        public async Task<IActionResult> Search(string searchString)
        {
            // Query all products
            var productsQuery = db.Product.Include(p => p.Category).AsQueryable();
            var distinctCategories = await db.Category.ToListAsync();
            // kế thừa các logic chung từ BaseController
            await SetCommonViewData();


            // Filter by category, price, and product name
            if (!string.IsNullOrEmpty(searchString))
            {
                productsQuery = productsQuery.Where(p =>
                    p.Category.CategoryName.Contains(searchString) ||
                    p.ProductName.Contains(searchString) ||
                    p.Price.ToString().Contains(searchString)
                );
            }

            // Retrieve the filtered products
            var filteredProducts = await productsQuery.ToListAsync();

            // Pass the filtered products to the view
            ViewBag.SearchResults = filteredProducts;

            return View(filteredProducts);
        }

        // Details product
        [Authentication]
        public async Task<IActionResult> Details(int id)
        {
            // kế thừa các logic chung từ BaseController
            await SetCommonViewData();

            // Retrieve the product details from the database based on the provided ID
            var product = await db.Product.Include(p => p.Category)
                                          .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null)
            {
                // Handle the case where the product with the specified ID is not found
                return NotFound();
            }

            var relatedProducts = await db.Product
                .Where(p => p.CategoryId == product.CategoryId && p.ProductId != id)
                .Take(4) // Số lượng sản phẩm liên quan bạn muốn hiển thị
                .ToListAsync();

            ViewBag.RelatedProducts = relatedProducts;

            // Set ViewBag properties for product details
            ViewBag.ProductId = product.ProductId;
            ViewBag.ProductThumbnail = product.Thumbnail;  // Đường dẫn ảnh sản phẩm
            ViewBag.ProductName = product.ProductName; // Tên sản phẩm
            ViewBag.ProductPrice = product.Price;      // Giá sản phẩm
            ViewBag.ProductDescription = product.Description; // Mô tả sản phẩm
            ViewBag.ProductCategory = product.Category?.CategoryName;

            // Set ViewBag properties for product availability
            ViewBag.ProductAvailability = product.Qty;

            return View(product);
        }


        // Shop 
        [Authentication]
        public async Task<IActionResult> Shop(int? id, string searchString, int? minPrice, int? maxPrice, int page = 1, int pageSize = 9)
        {

            // kế thừa các logic chung từ BaseController
            await SetCommonViewData();

            if (id == null)
            {
                return NotFound();
            }

            var category = db.Category.Find(id);

            if (category == null)
            {
                return NotFound();
            }

            ViewBag.SearchString = searchString;

            var productsInCategory = db.Product
                .Include(p => p.Category)
                .Where(p => p.CategoryId == id);

            if (!string.IsNullOrEmpty(searchString))
            {
                productsInCategory = productsInCategory.Where(p => p.ProductName.Contains(searchString));
            }

            // Lọc theo giá
            if (minPrice != null)
            {
                productsInCategory = productsInCategory.Where(p => p.Price >= minPrice);
            }

            if (maxPrice != null)
            {
                productsInCategory = productsInCategory.Where(p => p.Price <= maxPrice);
            }

            // Tính toán và chuyển thông tin phân trang vào ViewBag hoặc ViewModel
            ViewBag.CategoryId = id;
            ViewBag.TotalProductCount = productsInCategory.Count(); // Đếm số lượng sản phẩm sau khi áp dụng tìm kiếm
            ViewBag.TotalPages = (int)Math.Ceiling((double)ViewBag.TotalProductCount / pageSize);
            ViewBag.CurrentPage = page;

            // Lấy danh sách sản phẩm với phân trang
            var paginatedProducts = productsInCategory
                .OrderBy(p => p.ProductId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.ProductsInCategory = paginatedProducts;
            ViewBag.Categories = db.Category.ToList();

            return View(paginatedProducts);
        }
        [Authentication]
        public async Task<IActionResult> Category(int page = 1, int pageSize = 9, decimal? minPrice = null, decimal? maxPrice = null)
        {
            // kế thừa các logic chung từ BaseController
            await SetCommonViewData();

            // Lấy danh sách sản phẩm với phân trang
            var query = db.Product
    .Include(p => p.Category) // Include the Category information
    .OrderBy(p => p.ProductId)
    .Skip((page - 1) * pageSize)
    .Take(pageSize);

            // Lọc theo giá
            if (minPrice != null)
            {
                query = query.Where(p => p.Price >= minPrice);
            }

            if (maxPrice != null)
            {
                query = query.Where(p => p.Price <= maxPrice);
            }

            var productList = await query.ToListAsync();
            // Tính toán và chuyển thông tin phân trang vào ViewBag hoặc ViewModel
            ViewBag.TotalProductCount = await db.Product.CountAsync(); // Tổng số sản phẩm
            ViewBag.TotalPages = (int)Math.Ceiling((double)ViewBag.TotalProductCount / pageSize);
            ViewBag.CurrentPage = page;
            ViewBag.Categories = await db.Category.ToListAsync();

            return View(productList);
        }

        // checkout
        [Authentication]
        public async Task<IActionResult> Checkout()
        {
            // Kế thừa các logic chung từ BaseController
            await SetCommonViewData();

            // Lấy danh sách sản phẩm từ giỏ hàng
            List<CartItem> cartItems = HttpContext.Session.Get<List<CartItem>>("cart");
            // Truyền danh sách sản phẩm giỏ hàng cho view
            ViewBag.CartItems = cartItems;

            // Tính toán Subtotal
            decimal subtotal = cartItems.Sum(item => item.Total);
            ViewBag.Subtotal = subtotal;



            // Cộng phí thuế vào tổng số tiền
            decimal total = subtotal;
            ViewBag.Total = total;






            return View();
        }

        [HttpPost]
        [Authentication]
        public IActionResult Checkout(Order model)
        {
            if (true)
            {
                // Lấy UserId từ Session
                if (HttpContext.Session.TryGetValue("UserId", out byte[] userIdBytes) &&
                    int.TryParse(Encoding.UTF8.GetString(userIdBytes), out int userId))
                {
                    // Tạo một đối tượng Order từ dữ liệu trong form
                    var order = new Order
                    {
                        UserId = userId,
                        OrderDate = DateTime.Now,
                        Status = "Pending",
                        IsPaid = "Chua thanh toan",
                        Province = model.Province,
                        District = model.District,
                        Ward = model.Ward,
                        AddressDetail = model.AddressDetail,
                        FullName = model.FullName,
                        Email = model.Email,
                        Telephone = model.Telephone,
                        PaymentMethod = model.PaymentMethod,
                        ShippingMethod = model.ShippingMethod
                    };

                    // Lấy giỏ hàng từ Session
                    List<CartItem> cartItems = HttpContext.Session.Get<List<CartItem>>("cart");

                    // Kiểm tra xem giỏ hàng có dữ liệu không
                    if (cartItems != null && cartItems.Count > 0)
                    {
                        // Tính tổng TotalAmount từ giỏ hàng và phí vận chuyển
                        decimal subtotal = cartItems.Sum(cartItem => cartItem.Total);
                        decimal shippingFee = GetShippingFee(model.ShippingMethod); // Lấy phí vận chuyển từ hàm GetShippingFee

                        // Gán giá trị TotalAmount cho đối tượng Order
                        order.TotalAmount = subtotal + shippingFee;

                        // Lưu đơn đặt hàng vào cơ sở dữ liệu
                        _context.Order.Add(order);
                        _context.SaveChanges();

                        // Lưu thông tin sản phẩm trong đơn hàng vào bảng OrderProducts
                        foreach (var cartItem in cartItems)
                        {
                            var orderProduct = new OrderProduct
                            {
                                OrderId = order.OrderId,
                                ProductId = cartItem.ProductId,
                                Qty = cartItem.Qty,
                                Price = cartItem.Price
                            };

                            _context.OrderProduct.Add(orderProduct);
                        }

                        _context.SaveChanges();

                        // Xóa giỏ hàng sau khi đã đặt hàng thành công
                        HttpContext.Session.Remove("cart");

                        // Thực hiện các bước xử lý thanh toán khác (nếu cần)

                        return RedirectToAction("Thankyou", "Page");
                    }
                }
            }

            // Nếu dữ liệu không hợp lệ hoặc giỏ hàng trống, quay lại trang checkout với các lỗi
            return View("Checkout", model);
        }

        // Hàm lấy phí vận chuyển dựa trên phương thức vận chuyển
        private decimal GetShippingFee(string shippingMethod)
        {
            switch (shippingMethod)
            {
                case "FreeShipping":
                    return 0.00M; // Phí vận chuyển cho Free Shipping
                case "Express":
                    return 10.00M; // Phí vận chuyển cho Express
                default:
                    return 0.00M;
            }
        }

        // thank you
        [Authentication]
        public async Task<IActionResult> Thankyou()
        {
            // kế thừa các logic chung từ BaseController
            await SetCommonViewData();
            return View();
        }



        // contact

        public async Task<IActionResult> Contact()
        {
            // kế thừa các logic chung từ BaseController
            await SetCommonViewData();
            return View();
        }
        [Authentication]
        [HttpPost]
        public async Task<IActionResult> Contact(string name, string email, string message)
        {

            string smtpServer = _configuration["EmailSettings:SmtpServer"];
            int port = _configuration.GetValue<int>("EmailSettings:Port");
            string username = _configuration["EmailSettings:Username"];
            string password = _configuration["EmailSettings:Password"];

            var smtpClient = new SmtpClient(smtpServer)
            {
                Port = port,
                Credentials = new NetworkCredential(username, password),
                EnableSsl = true
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(email),
                Subject = "New Message from Your Website",
                Body = $"Name: {name}\nEmail: {email}\nMessage: {message}"
            };

            mailMessage.To.Add("dungprohn1409@gmail.com"); // Thay đổi địa chỉ email của người nhận

            try
            {
                await smtpClient.SendMailAsync(mailMessage);

                // Thông báo gửi email thành công
                TempData["Message"] = "Information has been sent successfully!!";
            }
            catch (Exception ex)
            {
                // Xử lý lỗi nếu gửi email thất bại
                ViewBag.ErrorMessage = $"Failed to send message: {ex.Message}";
            }

            // Chuyển hướng về trang Contact
            return View();
        }

        [Authentication]
        public async Task<IActionResult> Blog()
        {
            // kế thừa các logic chung từ BaseController
            await SetCommonViewData();
            return View();
        }



        // login , logout , Register
        [HttpGet]
        public IActionResult Login()
        {
            if (HttpContext.Session.GetString("Username") == null)
            {
                return View();
            }
            else
            {
                return RedirectToAction("Home", "Page");
            }
        }

        [HttpPost]
        public IActionResult Login(User user)
        {
            if (HttpContext.Session.GetString("Username") == null)
            {
                User u = db.User.FirstOrDefault(
                    x => x.Email.Equals(user.Email));

                if (u != null && BCrypt.Net.BCrypt.Verify(user.Password, u.Password))
                {
                    HttpContext.Session.SetString("Username", u.Username.ToString());
                    HttpContext.Session.SetString("Role", u.Role);
                    HttpContext.Session.SetString("UserId", u.UserId.ToString());

                    if (u.Role == "Admin")
                    {
                        return RedirectToAction("Home", "Page");
                    }
                    else if (u.Role == "User")
                    {
                        return RedirectToAction("Home", "Page");
                    }
                }
            }

            return View();
        }


        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Register(User user)
        {
            if (true)
            {
                // Kiểm tra xem tên đăng nhập đã tồn tại chưa
                if (db.User.Any(x => x.Username.Equals(user.Username)))
                {
                    ModelState.AddModelError("Username", "Tên đăng nhập đã tồn tại");
                    return View(user);
                }
                user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);
                // Thiết lập vai trò mặc định là "User"
                user.Role = "User";

                // Thêm người dùng mới vào cơ sở dữ liệu
                db.User.Add(user);
                db.SaveChanges();

                // Lưu thông tin đăng nhập vào phiên làm việc
                HttpContext.Session.SetString("Username", user.Username);
                HttpContext.Session.SetString("Role", user.Role);

                return RedirectToAction("Home", "Page");
            }

            return View(user);
        }



        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            HttpContext.Session.Remove("Username");
            return RedirectToAction("login", "Page");
        }


    }
}