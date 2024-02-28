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

namespace OgainShop.Controllers
{
    public class PageController : BaseController
    {
        private readonly OgainShopContext db;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _env;


        public PageController(OgainShopContext context, IConfiguration configuration, IWebHostEnvironment env) : base(context)
        {
            db = context;
            _configuration = configuration;
            _env = env;
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
                        Status = "pending",
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
                    List<OrderProduct> orderProducts = new List<OrderProduct>();



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
                            // Giảm số lượng sản phẩm trong bảng Product
                            var product = _context.Product.Find(cartItem.ProductId);
                            if (product != null)
                            {
                                product.Qty -= cartItem.Qty;
                                // Kiểm tra nếu muốn xử lý các điều kiện khác khi số lượng dưới 0, thì thêm điều kiện ở đây
                            }
                            // Thêm thông tin sản phẩm vào danh sách để gửi qua email
                            orderProducts.Add(orderProduct);
                        }
                       
                        _context.SaveChanges();

                        // Xóa giỏ hàng sau khi đã đặt hàng thành công
                        HttpContext.Session.Remove("cart");

                        SendInvoiceEmail(model.Email, order, orderProducts);




                        // Thực hiện các bước xử lý thanh toán khác (nếu cần)

                        return RedirectToAction("Thankyou", "Page", new { orderId = order.OrderId, totalAmount = order.TotalAmount });
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
                case "FastExpress":
                    return 20.00M; // Phí vận chuyển cho FastExpress 
                case "Express":
                    return 10.00M; // Phí vận chuyển cho Express
                default:
                    return 0.00M;
            }
        }
        private string GenerateEmailContent(Order order, List<OrderProduct> orderProducts)
        {
            // Đọc nội dung mẫu email từ file
            string emailTemplatePath = _env.ContentRootPath + "/Views/Email/SendEmail.cshtml";
            string emailContent = System.IO.File.ReadAllText(emailTemplatePath);

            // Thay thế các placeholder trong mẫu email bằng thông tin đơn hàng
            emailContent = emailContent.Replace("{OrderNumber}", order.OrderId.ToString());
            emailContent = emailContent.Replace("{OrderDate}", order.OrderDate.ToString("dd/MM/yyyy HH:mm"));
            emailContent = emailContent.Replace("{CustomerName}", order.FullName);
            emailContent = emailContent.Replace("{CustomerEmail}", order.Email);
            emailContent = emailContent.Replace("{ShippingMethod}", order.ShippingMethod);
            emailContent = emailContent.Replace("{PaymentMethod}", order.PaymentMethod);
            emailContent = emailContent.Replace("{Status}", order.Status);
            emailContent = emailContent.Replace("{Telephone}", order.Telephone);
            emailContent = emailContent.Replace("{ShippingAddress}", order.GetFullAddress());

            // Thêm thông tin sản phẩm vào email content
            string productList = "";
    foreach (var ordProduct in order.OrderProducts) // Đặt tên biến khác đây
    {
        productList += $"<div class='Order_list_product'><div class='Order_list_product1'><h5>{ordProduct.Product.ProductName}</h5></div><div class='quantity'><p>Qty: {ordProduct.Qty}</p></div><div class='total'><p>${ordProduct.Price}</p></div></div>";
    }
    emailContent = emailContent.Replace("{OrderProducts}", productList);

            // Thêm tổng đơn hàng vào email content
            decimal subtotal = order.OrderProducts.Sum(op => op.Price * op.Qty);
            decimal shippingFee = order.ShippingMethod == "Express" ? 10.00m : 20.00m; // Assume shipping fee is $10 for Express, $20 for other methods
            decimal totalAmount = subtotal + shippingFee;
            emailContent = emailContent.Replace("{Subtotal}", subtotal.ToString("0.00"));
            emailContent = emailContent.Replace("{ShippingFee}", shippingFee.ToString("0.00"));
            emailContent = emailContent.Replace("{TotalAmount}", totalAmount.ToString("0.00"));

            return emailContent;
        }


            private async Task SendInvoiceEmail(string recipientEmail, Order order, List<OrderProduct> orderProducts)
        {
            string emailContent = GenerateEmailContent(order, orderProducts);

            string smtpServer = _configuration["EmailSettings:SmtpServer"];
            int port = _configuration.GetValue<int>
           ("EmailSettings:Port");
            string username = _configuration["EmailSettings:Username"];
            string password = _configuration["EmailSettings:Password"];

            using (var client = new SmtpClient(smtpServer))
            {
                client.Port = port;
                client.Credentials = new System.Net.NetworkCredential(username, password);
                client.EnableSsl = true;

                var message = new MailMessage(username, recipientEmail)
                {
                    Subject = "Your Order Invoice",
                    Body = emailContent, // Nội dung email từ mẫu Razor
                    IsBodyHtml = true
                };


                try
                {
                    await client.SendMailAsync(message);
                    ViewBag.Message = "Email sent successfully";
                }
                catch (Exception ex)
                {
                    ViewBag.Error = $"Failed to send email: {ex.Message}";
                }
            }
        }



        //thankyou
        [Authentication]
        public IActionResult Thankyou(int orderId)
        {
            // Lấy thông tin đơn hàng từ cơ sở dữ liệu
            var order = _context.Order
                .Include(o => o.OrderProducts)
                .ThenInclude(op => op.Product)
                .FirstOrDefault(o => o.OrderId == orderId);

            if (order == null)
            {
                // Xử lý trường hợp không tìm thấy đơn hàng
                return NotFound();
            }

            // Tính tổng số tiền đơn hàng
            decimal subtotal = order.OrderProducts.Sum(op => op.Product.Price * op.Qty);
            decimal totalAmount = order.TotalAmount;

            // Kiểm tra trạng thái Shipping Method từ SQL
            bool isExpressShipping = _context.Order.Any(o => o.OrderId == orderId && (o.ShippingMethod == "Express" || o.ShippingMethod == "FastExpress"));

            // Lấy địa chỉ từ đối tượng Order
            string fullAddress = order.GetFullAddress();

            // Truyền thông tin đơn hàng và các thông tin cần thiết vào ViewBag
            ViewBag.Order = order;
            ViewBag.OrderProducts = order.OrderProducts;
            ViewBag.Subtotal = subtotal;
            ViewBag.TotalAmount = order.TotalAmount;
            ViewBag.IsExpressShipping = isExpressShipping;
            ViewBag.FullAddress = fullAddress;
            ViewBag.OrderId = orderId;

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