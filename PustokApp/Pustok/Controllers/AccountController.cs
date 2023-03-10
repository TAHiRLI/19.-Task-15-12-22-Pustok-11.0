using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using NuGet.Protocol;
using Pustok.DAL;
using Pustok.Models;
using Pustok.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace Pustok.Controllers
{
    public class AccountController : Controller
    {
        private readonly PustokDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;

        public AccountController(PustokDbContext context, UserManager<AppUser> userManager, SignInManager<AppUser> signInManager)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
        }
        public IActionResult Register()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Register(MemberRegisterViewModel memberVm)
        {
            if (!ModelState.IsValid)
            {
                return View();
            }


            if (await _userManager.FindByNameAsync(memberVm.Username) != null)
            {
                ModelState.AddModelError("Username", "User already exists");
                return RedirectToAction("Login");
            }
            if (await _userManager.FindByEmailAsync(memberVm.Email) != null)
            {
                ModelState.AddModelError("Email", "Email already exists");
            }


            AppUser appUser = new AppUser
            {
                Email = memberVm.Email,
                Fullname = memberVm.Fullname,
                UserName = memberVm.Username
            };

            var result = await _userManager.CreateAsync(appUser, memberVm.Password);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
                return View();
            }

            await _userManager.AddToRoleAsync(appUser, "Member");



            return RedirectToAction("Login", "Account");
        }
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(MemberLoginViewModel memberLoginVm, string returnUrl)
        {

            if (!ModelState.IsValid)
            {
                return View();
            }

            AppUser appUser = await _userManager.FindByNameAsync(memberLoginVm.Username);
            if (appUser == null)
            {
                ModelState.AddModelError("", "Username or Password is incorrect !");
                return View();
            }



            var roles = await _userManager.GetRolesAsync(appUser);
            if (!roles.Contains("Member"))
            {
                ModelState.AddModelError("", "Username or Password is incorrect !");
                return View();
            }





            var result = await _signInManager.PasswordSignInAsync(appUser, memberLoginVm.Password, memberLoginVm.IsPersistent, true);

            if (result.IsLockedOut)
            {
                ModelState.AddModelError("", "Too many attempts, please wait 5 minutes");
                return View();
            }

            if (!result.Succeeded)
            {
                ModelState.AddModelError("", "Username or Password is incorrect !");
                return View();
            }
            if (returnUrl != null)
                return Redirect(returnUrl);
            // get basket cookies
            var basketStr = HttpContext.Request.Cookies["basket"];
            if (basketStr != null)
            {
                var basketList = JsonConvert.DeserializeObject<List<BasketCookieViewModel>>(basketStr);

                foreach (var item in basketList)
                {
                    BasketItem basketItem = _context.BasketItems.FirstOrDefault(x=> x.AppUserId == appUser.Id && x.BookId == item.BookId);

                    if (basketItem == null)
                    {
                        basketItem = new BasketItem
                        {

                            BookId = item.BookId,
                            Count = item.Count,
                            CreatedTime = DateTime.UtcNow.AddHours(4)

                        };
                        appUser.BasketItems.Add(basketItem);

                    }
                    else
                    {
                        basketItem.Count += item.Count;
                    }


                }
            }

            _context.SaveChanges();

            return RedirectToAction("Index", "Home");
        }

        //public IActionResult Show()
        //{
        //    if (User.Identity.IsAuthenticated)
        //    {
        //        return Content(User.Identity.Name);
        //    }
        //    return Content("User Is logged Out");
        //}
        [Authorize(Roles = "Member")]
        public async Task<IActionResult> Profile()
        {
            AppUser appUser = await _userManager.FindByNameAsync(User.Identity.Name);
            MemberEditViewModel memberEditVm = new MemberEditViewModel();
            memberEditVm.Fullname = appUser.Fullname;
            memberEditVm.UserName = appUser.UserName;
            memberEditVm.Email = appUser.Email;

            return View(memberEditVm);
        }
        [HttpPost]
        [Authorize(Roles = "Member")]

        public async Task<IActionResult> Profile(MemberEditViewModel memberEditVm)
        {
            AppUser appUser = await _userManager.FindByNameAsync(User.Identity.Name);
            if (User == null)
            {
                await _signInManager.SignOutAsync();
                return RedirectToAction("login");
            }
            var transaction = _context.Database.BeginTransaction();


            if (memberEditVm.Fullname.ToUpper() != appUser.Fullname.ToUpper())
                appUser.Fullname = memberEditVm.Fullname;

            if (memberEditVm.Email.ToUpper() != appUser.NormalizedEmail && _context.AppUsers.Any(x => x.NormalizedEmail == memberEditVm.Email.ToUpper()))
                ModelState.AddModelError("Email", "This email already exists");

            if (memberEditVm.UserName.ToUpper() != appUser.NormalizedUserName && _context.AppUsers.Any(x => x.NormalizedUserName == memberEditVm.UserName.ToUpper()))
                ModelState.AddModelError("Username", "This Username already exists");

            if (!ModelState.IsValid)
                return View(memberEditVm);

            var isUpdated = new IdentityResult();
            if (memberEditVm.CurrentPassword != null || memberEditVm.NewPassword != null)
            {


                if (memberEditVm.CurrentPassword != null)
                {
                    isUpdated = await _userManager.ChangePasswordAsync(appUser, memberEditVm.CurrentPassword, memberEditVm.NewPassword);
                    if (!isUpdated.Succeeded)
                    {
                        foreach (var error in isUpdated.Errors)
                        {
                            ModelState.AddModelError("", error.Description);
                        }
                        return View(memberEditVm);
                    }
                }


            }

            appUser.Email = memberEditVm.Email;
            appUser.UserName = memberEditVm.UserName;

            var result = await _userManager.UpdateAsync(appUser);


            if (result.Succeeded && isUpdated.Succeeded)
                transaction.Commit();

            await _signInManager.SignInAsync(appUser, false);

            return RedirectToAction("index", "home");
        }
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "home");
        }



    }
}
