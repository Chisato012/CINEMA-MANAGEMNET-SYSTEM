namespace Cinema_Management.Controllers;

using Microsoft.AspNetCore.Mvc;
using Cinema_Management.Models;


public class AccountController : Controller
{
    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Login(AuthViewModel model)
    {
        // Nếu dữ liệu không hợp lệ (để trống), trả về lại View kèm theo thông báo lỗi
        if (!ModelState.IsValid)
        {
            return View(model);
        }
        return RedirectToAction("Index", "Home");
    }


    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }
    
    //Logic xử lý lưu database tạo tài khoản
    
}