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

namespace OgainShop.Controllers
{
    public class PageController : Controller
    {
        private readonly OgainShopContext db;

        public PageController(OgainShopContext context)
        {
            db = context;
        }
        //Home
        [Authentication]
        public async Task<IActionResult> Home()
        {
            // Retrieve distinct categories from the database
            var distinctCategories = await db.Category.ToListAsync();
            var cartItems = HttpContext.Session.Get<List<CartItem>>("cart");
            ViewData["CartItemCount"] = cartItems != null ? cartItems.Count : 0;

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
            var cartItems = HttpContext.Session.Get<List<CartItem>>("cart");
            ViewData["CartItemCount"] = cartItems != null ? cartItems.Count : 0;

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
            var cartItems = HttpContext.Session.Get<List<CartItem>>("cart");
            ViewData["CartItemCount"] = cartItems != null ? cartItems.Count : 0;

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
        public ActionResult Shop(int? id, string searchString, int page = 1, int pageSize = 9)
        {
            var cartItems = HttpContext.Session.Get<List<CartItem>>("cart");
            ViewData["CartItemCount"] = cartItems != null ? cartItems.Count : 0;
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
        public async Task<IActionResult> Category(int page = 1, int pageSize = 9)
        {
            var cartItems = HttpContext.Session.Get<List<CartItem>>("cart");
            ViewData["CartItemCount"] = cartItems != null ? cartItems.Count : 0;

            // Lấy danh sách sản phẩm với phân trang
            var productList = await db.Product
                .OrderBy(p => p.ProductId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(p => p.Category) // Include the Category information

                .ToListAsync();

            // Tính toán và chuyển thông tin phân trang vào ViewBag hoặc ViewModel
            ViewBag.TotalProductCount = await db.Product.CountAsync(); // Tổng số sản phẩm
            ViewBag.TotalPages = (int)Math.Ceiling((double)ViewBag.TotalProductCount / pageSize);
            ViewBag.CurrentPage = page;
            ViewBag.Categories = await db.Category.ToListAsync();

            return View(productList);
        }

        // checkout
        [Authentication]
        public IActionResult Checkout()

        {
            var cartItems = HttpContext.Session.Get<List<CartItem>>("cart");
            ViewData["CartItemCount"] = cartItems != null ? cartItems.Count : 0;
            return View();
        }

        // contact
        [Authentication]
        public IActionResult Contact()
        {
            var cartItems = HttpContext.Session.Get<List<CartItem>>("cart");
            ViewData["CartItemCount"] = cartItems != null ? cartItems.Count : 0;
            return View();
        }


        [Authentication]
        public IActionResult Blog()
        {
            var cartItems = HttpContext.Session.Get<List<CartItem>>("cart");
            ViewData["CartItemCount"] = cartItems != null ? cartItems.Count : 0;
            return View();
        }

        // favourite
        [Authentication]
        public IActionResult Favourite()
        {
            var cartItems = HttpContext.Session.Get<List<CartItem>>("cart");
            ViewData["CartItemCount"] = cartItems != null ? cartItems.Count : 0;
            return View();
        }

        // thank you
        [Authentication]
        public IActionResult Thankyou()
        {
            var cartItems = HttpContext.Session.Get<List<CartItem>>("cart");
            ViewData["CartItemCount"] = cartItems != null ? cartItems.Count : 0;
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