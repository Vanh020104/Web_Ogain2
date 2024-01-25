using Microsoft.AspNetCore.Mvc;
using OgainShop.Models;
using OgainShop.Data;
using Microsoft.AspNetCore.Http;
using OgainShop.Models.Authentication;

namespace OgainShop.Controllers
{
    public class PageController : Controller
    {
        private readonly OgainShopContext db;

        public PageController(OgainShopContext context)
        {
            db = context;
        }
        [Authentication]
        public IActionResult Home()
        {
            return View();
        }
        [Authentication]
        public IActionResult Details()

        {
            return View();
        }
        [Authentication]
        public IActionResult Cart()
        {
            return View();
        }
        [Authentication]
        public IActionResult Checkout()
        {
            return View();
        }
        [Authentication]
        public IActionResult Contact()
        {
            return View();
        }
        [Authentication]
        public IActionResult Blog()
        {
            return View();
        }
        [Authentication]
        public IActionResult Favourite()
        {
            return View();
        }
        [Authentication]
        public IActionResult Category()
        {
            return View();
        }
        [Authentication]
        public IActionResult Thankyou()
        {
            return View();
        }

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
                    x => x.Username.Equals(user.Username) &&
                         x.Password.Equals(user.Password));

                if (u != null)
                {
                    HttpContext.Session.SetString("Username", user.Username.ToString());
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