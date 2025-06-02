using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MealStack.Infrastructure.Data;
using MealStack.Infrastructure.Data.Entities;
using MealStack.Web.Models;
using System.Threading.Tasks;

namespace MealStack.Web.Controllers
{
    public class AccountController : BaseController
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly MealStackDbContext _context;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            UserManager<ApplicationUser> userManager, 
            SignInManager<ApplicationUser> signInManager,
            MealStackDbContext context,
            ILogger<AccountController> logger)
            : base(userManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _logger = logger;
        }

        #region Login & Register

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string returnUrl = null)
        {
            var model = new LoginViewModel
            {
                ReturnUrl = returnUrl ?? string.Empty 
            };
            ViewData["ReturnUrl"] = returnUrl;
            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            _logger.LogInformation("🔑 LOGIN POST METHOD CALLED");
            _logger.LogInformation("Email: {Email}, Password Length: {PasswordLength}", 
                model?.Email ?? "NULL", model?.Password?.Length ?? 0);
            _logger.LogInformation("ReturnUrl: '{ReturnUrl}'", model?.ReturnUrl ?? "NULL");

            ModelState.Remove("ReturnUrl");
            
            if (model != null && model.ReturnUrl == null)
            {
                model.ReturnUrl = string.Empty;
            }

            ViewData["ReturnUrl"] = model?.ReturnUrl;

            _logger.LogInformation("ModelState.IsValid: {IsValid}", ModelState.IsValid);
            foreach (var state in ModelState)
            {
                if (state.Value.Errors.Any())
                {
                    foreach (var error in state.Value.Errors)
                    {
                        _logger.LogWarning("ModelState Error - {Field}: {Error}", state.Key, error.ErrorMessage);
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("❌ ModelState is invalid - returning to view");
                return View(model);
            }

            try
            {
                _logger.LogInformation("🔍 Searching for user with email: {Email}", model.Email);

                var user = await _userManager.FindByEmailAsync(model.Email);
                
                if (user == null)
                {
                    _logger.LogWarning("❌ No user found with email: {Email}", model.Email);
                    ModelState.AddModelError("", "Invalid email or password.");
                    return View(model);
                }

                _logger.LogInformation("✅ Found user: {UserName} (ID: {UserId}) for email: {Email}", 
                    user.UserName, user.Id, model.Email);

                var passwordValid = await _userManager.CheckPasswordAsync(user, model.Password);
                _logger.LogInformation("Password check result: {PasswordValid}", passwordValid);

                if (!passwordValid)
                {
                    _logger.LogWarning("❌ Invalid password for user: {UserName}", user.UserName);
                    ModelState.AddModelError("", "Invalid email or password.");
                    return View(model);
                }

                var result = await _signInManager.PasswordSignInAsync(
                    user.UserName,
                    model.Password, 
                    model.RememberMe, 
                    lockoutOnFailure: false);

                _logger.LogInformation("SignIn result - Succeeded: {Succeeded}", result.Succeeded);

                if (result.Succeeded)
                {
                    _logger.LogInformation("🎉 User logged in successfully: {UserName} ({Email})", user.UserName, model.Email);
                    
                    if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                    {
                        return Redirect(model.ReturnUrl);
                    }
                    return RedirectToAction("Index", "Home");
                }

                _logger.LogWarning("❌ Sign in failed for user: {UserName}", user.UserName);
                ModelState.AddModelError("", "Invalid email or password.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 EXCEPTION during login for email: {Email}", model?.Email);
                ModelState.AddModelError("", "An error occurred during login. Please try again.");
            }

            return View(model);
        }
                
        
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            _logger.LogInformation("🆕 REGISTER POST METHOD CALLED");
            _logger.LogInformation("Username: {UserName}, Email: {Email}", 
                model?.UserName ?? "NULL", model?.Email ?? "NULL");

            ViewData["ReturnUrl"] = model?.ReturnUrl;

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("❌ Registration ModelState is invalid");
                foreach (var error in ModelState)
                {
                    foreach (var errorMsg in error.Value.Errors)
                    {
                        _logger.LogWarning("Registration ModelState Error - {Field}: {Error}", error.Key, errorMsg.ErrorMessage);
                    }
                }
                return View(model);
            }

            try
            {
                var existingUser = await _userManager.FindByEmailAsync(model.Email);
                if (existingUser != null)
                {
                    _logger.LogWarning("❌ User already exists with email: {Email}", model.Email);
                    ModelState.AddModelError("Email", "A user with this email already exists.");
                    return View(model);
                }

                var existingUsername = await _userManager.FindByNameAsync(model.UserName);
                if (existingUsername != null)
                {
                    _logger.LogWarning("❌ Username already taken: {UserName}", model.UserName);
                    ModelState.AddModelError("UserName", "This username is already taken.");
                    return View(model);
                }

                var user = new ApplicationUser
                {
                    UserName = model.UserName,
                    Email = model.Email,
                    EmailConfirmed = true, 
                    ThemePreference = "light" 
                };

                _logger.LogInformation("🔨 Creating new user: {UserName} ({Email})", model.UserName, model.Email);
                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation("✅ New user created successfully: {UserName} ({Email})", model.UserName, model.Email);

                    await _userManager.AddToRoleAsync(user, "User");
                    _logger.LogInformation("✅ Added user to 'User' role: {UserName}", model.UserName);

                    await _signInManager.SignInAsync(user, isPersistent: false);
                    _logger.LogInformation("🎉 New user signed in automatically: {UserName}", model.UserName);

                    TempData["Message"] = "Welcome to MealStack! Your account has been created successfully.";
                    
                    if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                    {
                        return Redirect(model.ReturnUrl);
                    }
                    return RedirectToAction("Index", "Home");
                }

                _logger.LogWarning("❌ User creation failed for: {UserName} ({Email})", model.UserName, model.Email);
                foreach (var error in result.Errors)
                {
                    _logger.LogWarning("Registration Error: {Code} - {Description}", error.Code, error.Description);
                    ModelState.AddModelError("", error.Description);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 EXCEPTION during registration for: {UserName} ({Email})", model?.UserName, model?.Email);
                ModelState.AddModelError("", "An error occurred during registration. Please try again.");
            }

            _logger.LogInformation("🔙 Returning to registration view with errors");
            return View(model);
        }
        
        [HttpGet]
        public async Task<IActionResult> Logout(string returnUrl = null)
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out");
            TempData["Message"] = "You have been logged out successfully.";
            
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out");
            TempData["Message"] = "You have been logged out successfully.";
            return RedirectToAction("Index", "Home");
        }

        #endregion

        #region Forgot Password

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return Json(new { success = false, message = "Email is required." });
            }

            try
            {
                var user = await _userManager.FindByEmailAsync(email);
                if (user != null)
                {
                    var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                    
                    _logger.LogInformation("Password reset requested for {Email}. Token: {Token}", email, token);
                    Console.WriteLine($"=== PASSWORD RESET ===");
                    Console.WriteLine($"Email: {email}");
                    Console.WriteLine($"Reset Token: {token}");
                    Console.WriteLine($"Reset URL: {Url.Action("ResetPassword", "Account", new { token, email }, Request.Scheme)}");
                    Console.WriteLine($"=====================");
                }
                
                return Json(new { success = true, message = "If the email exists, a reset link has been sent." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing forgot password for email: {Email}", email);
                return Json(new { success = false, message = "An error occurred. Please try again." });
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword(string token, string email)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
            {
                return RedirectToAction("Login");
            }

            var model = new ResetPasswordViewModel
            {
                Token = token,
                Email = email
            };

            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null)
                {
                    TempData["Message"] = "Password reset successful. You can now log in with your new password.";
                    return RedirectToAction("Login");
                }

                var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);
                if (result.Succeeded)
                {
                    _logger.LogInformation("Password reset successful for user: {Email}", model.Email);
                    TempData["Message"] = "Password reset successful. You can now log in with your new password.";
                    return RedirectToAction("Login");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password for email: {Email}", model.Email);
                ModelState.AddModelError("", "An error occurred while resetting your password.");
            }

            return View(model);
        }

        #endregion

        #region Profile (Existing code)

        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var model = new ProfileViewModel
            {
                Email = user.Email,
                UserName = user.UserName,
                ThemePreference = user.ThemePreference ?? "light" 
            };

            return View(model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> UpdateUsername(ProfileViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            if (ModelState.IsValid)
            {
                var existingUser = await _userManager.FindByNameAsync(model.UserName);
                if (existingUser != null && existingUser.Id != user.Id)
                {
                    ModelState.AddModelError("UserName", "This username is already taken.");
                    model.Email = user.Email;
                    model.ThemePreference = user.ThemePreference ?? "light";
                    return View("Profile", model);
                }

                user.UserName = model.UserName;
                user.NormalizedUserName = _userManager.NormalizeName(model.UserName);

                var result = await _userManager.UpdateAsync(user);

                if (result.Succeeded)
                {
                    TempData["Message"] = "Username updated successfully.";
                    return RedirectToAction(nameof(Profile));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }

            model.Email = user.Email;
            model.ThemePreference = user.ThemePreference ?? "light";
            return View("Profile", model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> UpdateThemePreference(ProfileViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            if (model.ThemePreference != "light" && model.ThemePreference != "dark")
            {
                ModelState.AddModelError("ThemePreference", "Invalid theme preference.");
                model.Email = user.Email;
                model.UserName = user.UserName;
                return View("Profile", model);
            }

            user.ThemePreference = model.ThemePreference;

            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                TempData["Message"] = "Theme preference updated successfully.";
                return RedirectToAction(nameof(Profile));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            model.Email = user.Email;
            model.UserName = user.UserName;
            return View("Profile", model);
        }

        #endregion
        
        #region Resend Email Confirmation

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResendEmailConfirmation()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendEmailConfirmation(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return Json(new { success = false, message = "Email is required." });
            }

            try
            {
                var user = await _userManager.FindByEmailAsync(email);
                if (user != null)
                {
                    if (await _userManager.IsEmailConfirmedAsync(user))
                    {
                        return Json(new { 
                            success = true, 
                            alreadyConfirmed = true,
                            message = "Your email is already confirmed! You can log in now." 
                        });
                    }

                    var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    
                    var confirmationLink = Url.Action("ConfirmEmail", "Account", 
                        new { userId = user.Id, token = token }, Request.Scheme);
                        
                    _logger.LogInformation("Email confirmation requested for {Email}", email);
                    Console.WriteLine($"=== EMAIL CONFIRMATION ===");
                    Console.WriteLine($"Email: {email}");
                    Console.WriteLine($"User ID: {user.Id}");
                    Console.WriteLine($"Confirmation Token: {token}");
                    Console.WriteLine($"Confirmation URL: {confirmationLink}");
                    Console.WriteLine($"==========================");
                }
                
                return Json(new { 
                    success = true, 
                    message = "If the email exists and isn't confirmed, a confirmation link has been sent." 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing resend confirmation for email: {Email}", email);
                return Json(new { success = false, message = "An error occurred. Please try again." });
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
            {
                TempData["Error"] = "Invalid confirmation link.";
                return RedirectToAction("Login");
            }

            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    TempData["Error"] = "User not found.";
                    return RedirectToAction("Login");
                }

                var result = await _userManager.ConfirmEmailAsync(user, token);
                if (result.Succeeded)
                {
                    _logger.LogInformation("Email confirmed successfully for user: {Email}", user.Email);
                    TempData["Message"] = "Email confirmed successfully! You can now log in.";
                    return RedirectToAction("Login");
                }
                else
                {
                    _logger.LogWarning("Email confirmation failed for user: {Email}. Errors: {Errors}", 
                        user.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
                    TempData["Error"] = "Email confirmation failed. The link may be expired or invalid.";
                    return RedirectToAction("ResendEmailConfirmation");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming email for userId: {UserId}", userId);
                TempData["Error"] = "An error occurred while confirming your email.";
                return RedirectToAction("Login");
            }
        }

        #endregion
        
        
        
    }
}