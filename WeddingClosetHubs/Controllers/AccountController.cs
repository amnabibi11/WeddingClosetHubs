
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using WeddingClosetHubs.Models;

namespace WeddingClosetHubs.Controllers
{
    public class AccountController : Controller
    {
        private readonly WeddingClosetHubsContext _context;

        public AccountController(WeddingClosetHubsContext context)
        {
            _context = context;
        }

        private string NormalizeCNIC(string cnic)
        {
            return cnic
                .Trim()
                .Replace(" ", "");
        }

        private const long MaxImageSize = 5 * 1024 * 1024;

        private static readonly string[] AllowedImageExtensions =
        {
            ".jpg",
            ".jpeg",
            ".png"
        };

        private static readonly string[] AllowedDeliveryZones =
        {
            "Saddar",
            "6th Road",
            "Liaqat Bagh"
        };

        [HttpGet]
        public async Task<IActionResult> Register()
        {
            await LoadRoles();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(
            User user,
            string? ShopName,
            string? ShopCategory,
            string? ShopPhone,
            string? ShopAddress,
            string? ShopDescription,
            IFormFile? ShopFrontPhoto,
            IFormFile? ShopInsidePhoto,
            IFormFile? ShopSignboardPhoto,
            string? CNIC,
            IFormFile? CNICFrontImage,
            IFormFile? CNICBackImage,
            string? VehicleType,
            string? MotorbikeNumber,
            string? PreferredZone)
        {
            if (!ModelState.IsValid)
            {
                await LoadRoles();
                return View(user);
            }

            if (string.IsNullOrWhiteSpace(user.Name))
            {
                ModelState.AddModelError("Name", "Name is required.");
            }
            else
            {
                user.Name = user.Name.Trim();

                if (user.Name.Length < 3)
                {
                    ModelState.AddModelError(
                        "Name",
                        "Name must contain at least 3 characters."
                    );
                }

                if (user.Name.Length > 100)
                {
                    ModelState.AddModelError(
                        "Name",
                        "Name cannot exceed 100 characters."
                    );
                }

                if (!Regex.IsMatch(user.Name, @"^[A-Za-z ]+$"))
                {
                    ModelState.AddModelError(
                        "Name",
                        "Name can contain letters and spaces only."
                    );
                }
            }

            if (string.IsNullOrWhiteSpace(user.Email))
            {
                ModelState.AddModelError("Email", "Email is required.");
            }
            else
            {
                user.Email = user.Email.Trim().ToLowerInvariant();

                if (!new EmailAddressAttribute().IsValid(user.Email))
                {
                    ModelState.AddModelError(
                        "Email",
                        "Please enter a valid email address."
                    );
                }
            }

            if (!string.IsNullOrWhiteSpace(user.Email))
            {
                string normalizedEmail =
                    user.Email.Trim().ToLowerInvariant();

                bool emailExists =
                    await _context.Users.AnyAsync(u =>
                        u.Email != null &&
                        u.Email.ToLower() == normalizedEmail
                    );

                if (emailExists)
                {
                    ModelState.AddModelError(
                        "Email",
                        "This email is already registered."
                    );
                }
            }

            if (string.IsNullOrWhiteSpace(user.Phone))
            {
                ModelState.AddModelError(
                    "Phone",
                    "Phone number is required."
                );
            }
            else
            {
                user.Phone = user.Phone.Trim();

                if (!IsPakistaniPhone(user.Phone))
                {
                    ModelState.AddModelError(
                        "Phone",
                        "Please enter a valid Pakistani mobile number."
                    );
                }
            }

            if (!string.IsNullOrWhiteSpace(user.Phone))
            {
                string normalizedPhone =
                    NormalizePhone(user.Phone);

                bool phoneExists =
                    await _context.Users.AnyAsync(u =>
                        u.Phone != null &&
                        u.Phone == normalizedPhone
                    );

                if (phoneExists)
                {
                    ModelState.AddModelError(
                        "Phone",
                        "This phone number is already registered."
                    );
                }
            }

            if (string.IsNullOrWhiteSpace(user.Address))
            {
                ModelState.AddModelError(
                    "Address",
                    "Address is required."
                );
            }
            else
            {
                user.Address = user.Address.Trim();

                if (!IsRawalpindiAddress(user.Address))
                {
                    ModelState.AddModelError(
                        "Address",
                        "Please enter an address in Rawalpindi."
                    );
                }
            }

            var role = await _context.Roles
                .FirstOrDefaultAsync(r => r.RoleId == user.RoleId);

            if (role == null)
            {
                ModelState.AddModelError(
                    "RoleId",
                    "Please select a valid account type."
                );

                await LoadRoles();
                return View(user);
            }

            if (role.RoleName == "Admin")
            {
                ModelState.AddModelError(
                    "RoleId",
                    "Admin account cannot be created through public registration."
                );

                await LoadRoles();
                return View(user);
            }

            if (string.IsNullOrWhiteSpace(user.Password))
            {
                ModelState.AddModelError(
                    "Password",
                    "Password is required."
                );
            }
            else
            {
                if (user.Password.Length < 8)
                {
                    ModelState.AddModelError(
                        "Password",
                        "Password must contain at least 8 characters."
                    );
                }

                if (!Regex.IsMatch(user.Password, @"[A-Z]"))
                {
                    ModelState.AddModelError(
                        "Password",
                        "Password must contain at least one uppercase letter."
                    );
                }

                if (!Regex.IsMatch(user.Password, @"[a-z]"))
                {
                    ModelState.AddModelError(
                        "Password",
                        "Password must contain at least one lowercase letter."
                    );
                }

                if (!Regex.IsMatch(user.Password, @"\d"))
                {
                    ModelState.AddModelError(
                        "Password",
                        "Password must contain at least one number."
                    );
                }
            }

            if (role.RoleName == "Shopkeeper")
            {
                if (string.IsNullOrWhiteSpace(ShopName))
                {
                    ModelState.AddModelError(
                        "ShopName",
                        "Shop name is required."
                    );
                }
                else
                {
                    ShopName = ShopName.Trim();

                    if (ShopName.Length < 3)
                    {
                        ModelState.AddModelError(
                            "ShopName",
                            "Shop name must contain at least 3 characters."
                        );
                    }

                    if (ShopName.Length > 100)
                    {
                        ModelState.AddModelError(
                            "ShopName",
                            "Shop name cannot exceed 100 characters."
                        );
                    }
                }

                if (string.IsNullOrWhiteSpace(ShopCategory))
                {
                    ModelState.AddModelError(
                        "ShopCategory",
                        "Shop category is required."
                    );
                }
                else
                {
                    ShopCategory = ShopCategory.Trim();

                    if (ShopCategory.Length > 100)
                    {
                        ModelState.AddModelError(
                            "ShopCategory",
                            "Shop category cannot exceed 100 characters."
                        );
                    }
                }

                if (string.IsNullOrWhiteSpace(ShopPhone))
                {
                    ModelState.AddModelError(
                        "ShopPhone",
                        "Shop phone is required."
                    );
                }
                else
                {
                    ShopPhone = ShopPhone.Trim();

                    if (!IsPakistaniPhone(ShopPhone))
                    {
                        ModelState.AddModelError(
                            "ShopPhone",
                            "Please enter a valid Pakistani mobile number."
                        );
                    }
                }

                if (string.IsNullOrWhiteSpace(ShopAddress))
                {
                    ModelState.AddModelError(
                        "ShopAddress",
                        "Shop address is required."
                    );
                }
                else
                {
                    ShopAddress = ShopAddress.Trim();

                    if (!IsRawalpindiAddress(ShopAddress))
                    {
                        ModelState.AddModelError(
                            "ShopAddress",
                            "Please enter a shop address in Rawalpindi."
                        );
                    }
                }

                if (!string.IsNullOrWhiteSpace(ShopDescription))
                {
                    ShopDescription = ShopDescription.Trim();

                    if (ShopDescription.Length > 1000)
                    {
                        ModelState.AddModelError(
                            "ShopDescription",
                            "Shop description cannot exceed 1000 characters."
                        );
                    }
                }

                if (!IsAllowedImage(ShopFrontPhoto))
                {
                    ModelState.AddModelError(
                        "ShopFrontPhoto",
                        "Shop front photo must be JPG, JPEG or PNG and maximum 5 MB."
                    );
                }

                if (!IsAllowedImage(ShopInsidePhoto))
                {
                    ModelState.AddModelError(
                        "ShopInsidePhoto",
                        "Shop inside photo must be JPG, JPEG or PNG and maximum 5 MB."
                    );
                }

                if (!IsAllowedImage(ShopSignboardPhoto))
                {
                    ModelState.AddModelError(
                        "ShopSignboardPhoto",
                        "Shop signboard photo must be JPG, JPEG or PNG and maximum 5 MB."
                    );
                }
            }

            if (role.RoleName == "Delivery")
            {
                if (string.IsNullOrWhiteSpace(CNIC))
                {
                    ModelState.AddModelError(
                        "CNIC",
                        "CNIC is required."
                    );
                }
                else
                {
                    CNIC = CNIC.Trim();

                    if (!IsValidCNIC(CNIC))
                    {
                        ModelState.AddModelError(
                            "CNIC",
                            "CNIC must be in the format 35202-1234567-1."
                        );
                    }
                }

                if (!string.IsNullOrWhiteSpace(CNIC) && IsValidCNIC(CNIC))
                {
                    string normalizedCNIC = NormalizeCNIC(CNIC);

                    bool cnicExists =
                        await _context.DeliveryBoys.AnyAsync(d =>
                            d.CNIC == normalizedCNIC
                        );

                    if (cnicExists)
                    {
                        ModelState.AddModelError(
                            "CNIC",
                            "This CNIC is already registered."
                        );
                    }
                }

                if (!IsAllowedImage(CNICFrontImage))
                {
                    ModelState.AddModelError(
                        "CNICFrontImage",
                        "CNIC front image must be JPG, JPEG or PNG and maximum 5 MB."
                    );
                }

                if (!IsAllowedImage(CNICBackImage))
                {
                    ModelState.AddModelError(
                        "CNICBackImage",
                        "CNIC back image must be JPG, JPEG or PNG and maximum 5 MB."
                    );
                }

                if (string.IsNullOrWhiteSpace(VehicleType))
                {
                    ModelState.AddModelError(
                        "VehicleType",
                        "Vehicle type is required."
                    );
                }
                else if (!string.Equals(
                    VehicleType.Trim(),
                    "Motorbike",
                    StringComparison.OrdinalIgnoreCase))
                {
                    ModelState.AddModelError(
                        "VehicleType",
                        "Only Motorbike is allowed for delivery."
                    );
                }

                if (string.IsNullOrWhiteSpace(MotorbikeNumber))
                {
                    ModelState.AddModelError(
                        "MotorbikeNumber",
                        "Motorbike registration number is required."
                    );
                }
                else
                {
                    MotorbikeNumber =
                        MotorbikeNumber.Trim().ToUpperInvariant();

                    if (MotorbikeNumber.Length < 3)
                    {
                        ModelState.AddModelError(
                            "MotorbikeNumber",
                            "Please enter a valid motorbike registration number."
                        );
                    }

                    if (MotorbikeNumber.Length > 30)
                    {
                        ModelState.AddModelError(
                            "MotorbikeNumber",
                            "Motorbike registration number is too long."
                        );
                    }
                }

                if (!string.IsNullOrWhiteSpace(MotorbikeNumber))
                {
                    string normalizedMotorbike =
                        MotorbikeNumber.Trim().ToUpperInvariant();

                    bool motorbikeExists =
                        await _context.DeliveryBoys.AnyAsync(d =>
                            d.MotorbikeNumber != null &&
                            d.MotorbikeNumber.ToUpper() == normalizedMotorbike
                        );

                    if (motorbikeExists)
                    {
                        ModelState.AddModelError(
                            "MotorbikeNumber",
                            "This motorbike registration number is already registered."
                        );
                    }
                }

                if (string.IsNullOrWhiteSpace(PreferredZone))
                {
                    ModelState.AddModelError(
                        "PreferredZone",
                        "Preferred delivery zone is required."
                    );
                }
                else
                {
                    PreferredZone = PreferredZone.Trim();

                    bool validZone =
                        AllowedDeliveryZones.Any(z =>
                            string.Equals(
                                z,
                                PreferredZone,
                                StringComparison.OrdinalIgnoreCase
                            )
                        );

                    if (!validZone)
                    {
                        ModelState.AddModelError(
                            "PreferredZone",
                            "Please select a valid Rawalpindi delivery zone."
                        );
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                foreach (var item in ModelState)
                {
                    foreach (var error in item.Value.Errors)
                    {
                        Console.WriteLine(
                            $"FIELD: {item.Key} | ERROR: {error.ErrorMessage}"
                        );
                    }
                }

                await LoadRoles();
                return View(user);
            }

            user.Email = user.Email!.Trim().ToLowerInvariant();
            user.Name = user.Name!.Trim();
            user.Phone = NormalizePhone(user.Phone!);
            user.Address = user.Address!.Trim();
            user.Status = true;
            user.CreatedDate = DateTime.Now;

            if (role.RoleName == "Customer")
            {
                user.IsApproved = true;
                user.ApprovedDate = DateTime.Now;
            }
            else
            {
                user.IsApproved = false;
                user.ApprovedDate = null;
            }

            using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                if (role.RoleName == "Shopkeeper")
                {
                    string shopFolder = Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot",
                        "uploads",
                        "shops"
                    );

                    if (!Directory.Exists(shopFolder))
                    {
                        Directory.CreateDirectory(shopFolder);
                    }

                    string frontFileName =
                        Guid.NewGuid().ToString() +
                        Path.GetExtension(ShopFrontPhoto!.FileName)
                            .ToLowerInvariant();

                    string frontFilePath =
                        Path.Combine(shopFolder, frontFileName);

                    using (var stream = new FileStream(
                        frontFilePath,
                        FileMode.Create))
                    {
                        await ShopFrontPhoto.CopyToAsync(stream);
                    }

                    string insideFileName =
                        Guid.NewGuid().ToString() +
                        Path.GetExtension(ShopInsidePhoto!.FileName)
                            .ToLowerInvariant();

                    string insideFilePath =
                        Path.Combine(shopFolder, insideFileName);

                    using (var stream = new FileStream(
                        insideFilePath,
                        FileMode.Create))
                    {
                        await ShopInsidePhoto.CopyToAsync(stream);
                    }

                    string signboardFileName =
                        Guid.NewGuid().ToString() +
                        Path.GetExtension(ShopSignboardPhoto!.FileName)
                            .ToLowerInvariant();

                    string signboardFilePath =
                        Path.Combine(shopFolder, signboardFileName);

                    using (var stream = new FileStream(
                        signboardFilePath,
                        FileMode.Create))
                    {
                        await ShopSignboardPhoto.CopyToAsync(stream);
                    }

                    var shop = new Shop
                    {
                        ShopkeeperId = user.UserId,
                        ShopName = ShopName!.Trim(),
                        ShopCategory = ShopCategory!.Trim(),
                        ShopPhone = NormalizePhone(ShopPhone!),
                        ShopAddress = ShopAddress!.Trim(),
                        ShopDescription =
                            string.IsNullOrWhiteSpace(ShopDescription)
                                ? null
                                : ShopDescription.Trim(),
                        ShopFrontPhoto = "/uploads/shops/" + frontFileName,
                        ShopInsidePhoto = "/uploads/shops/" + insideFileName,
                        ShopSignboardPhoto = "/uploads/shops/" + signboardFileName,
                        IsApproved = false,
                        Status = false,
                        ApprovedDate = null
                    };

                    _context.Shops.Add(shop);
                    await _context.SaveChangesAsync();
                }

                if (role.RoleName == "Delivery")
                {
                    string uploadFolder = Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot",
                        "uploads",
                        "cnic"
                    );

                    if (!Directory.Exists(uploadFolder))
                    {
                        Directory.CreateDirectory(uploadFolder);
                    }

                    string frontFileName =
                        Guid.NewGuid().ToString() +
                        Path.GetExtension(CNICFrontImage!.FileName)
                            .ToLowerInvariant();

                    string frontFilePath =
                        Path.Combine(uploadFolder, frontFileName);

                    using (var stream = new FileStream(
                        frontFilePath,
                        FileMode.Create))
                    {
                        await CNICFrontImage.CopyToAsync(stream);
                    }

                    string backFileName =
                        Guid.NewGuid().ToString() +
                        Path.GetExtension(CNICBackImage!.FileName)
                            .ToLowerInvariant();

                    string backFilePath =
                        Path.Combine(uploadFolder, backFileName);

                    using (var stream = new FileStream(
                        backFilePath,
                        FileMode.Create))
                    {
                        await CNICBackImage.CopyToAsync(stream);
                    }

                    var deliveryBoy = new DeliveryBoy
                    {
                        UserId = user.UserId,
                        CNIC = CNIC!.Trim(),
                        MotorbikeNumber =
                            MotorbikeNumber!.Trim().ToUpperInvariant(),
                        PreferredZone = PreferredZone!.Trim(),
                        CNICFrontImage = "/uploads/cnic/" + frontFileName,
                        CNICBackImage = "/uploads/cnic/" + backFileName,
                        AssignedZone = null,
                        VerificationStatus = "Pending",
                        AdminNotes = null,
                        RejectionReason = null,
                        ApprovedDate = null,
                        CreatedDate = DateTime.Now
                    };

                    _context.DeliveryBoys.Add(deliveryBoy);
                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                ModelState.AddModelError(
                    "",
                    "Registration failed: " +
                    (ex.InnerException?.Message ?? ex.Message)
                );

                await LoadRoles();
                return View(user);
            }

            if (role.RoleName == "Customer")
            {
                TempData["Success"] =
                    "Registration successful. You can now login.";
            }
            else if (role.RoleName == "Shopkeeper")
            {
                TempData["Success"] =
                    "Shopkeeper registration submitted successfully. Please wait for Admin approval.";
            }
            else if (role.RoleName == "Delivery")
            {
                TempData["Success"] =
                    "Delivery account registration submitted successfully. Please wait for Admin verification and approval.";
            }

            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(
            string email,
            string password,
            string roleName)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                ViewBag.Error = "Please enter your email.";
                return View();
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Please enter your password.";
                return View();
            }

            if (string.IsNullOrWhiteSpace(roleName))
            {
                ViewBag.Error = "Please select an account type.";
                return View();
            }

            email = email.Trim().ToLowerInvariant();

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u =>
                    u.Email != null &&
                    u.Email.ToLower() == email &&
                    u.Password == password &&
                    u.Status == true &&
                    u.Role != null &&
                    u.Role.RoleName == roleName
                );

            if (user == null)
            {
                ViewBag.Error =
                    "Invalid email, password, or account type.";

                return View();
            }

            if (!user.IsApproved)
            {
                if (user.Role!.RoleName == "Shopkeeper")
                {
                    ViewBag.Error =
                        "Your shopkeeper account is waiting for Admin approval.";

                    return View();
                }

                if (user.Role!.RoleName == "Delivery")
                {
                    ViewBag.Error =
                        "Your delivery account is waiting for Admin verification and approval.";

                    return View();
                }

                ViewBag.Error =
                    "Your account is waiting for Admin approval.";

                return View();
            }

            HttpContext.Session.SetInt32("UserId", user.UserId);
            HttpContext.Session.SetString("UserName", user.Name ?? "");

            if (user.Role!.RoleName == "Customer")
            {
                HttpContext.Session.SetInt32("CustomerId", user.UserId);
            }

            if (user.RoleId.HasValue)
            {
                HttpContext.Session.SetInt32(
                    "RoleId",
                    user.RoleId.Value
                );
            }

            HttpContext.Session.SetString(
                "RoleName",
                user.Role!.RoleName
            );

            switch (user.Role.RoleName)
            {
                case "Admin":
                    return RedirectToAction("Dashboard", "Admin");

                case "Shopkeeper":
                    return RedirectToAction("Dashboard", "Shopkeeper");

                case "Customer":
                    return RedirectToAction("Dashboard", "Customer");

                case "Delivery":
                    return RedirectToAction("Dashboard", "Delivery");

                default:
                    HttpContext.Session.Clear();
                    ViewBag.Error = "Invalid account type.";
                    return View();
            }
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                ViewBag.Error = "Please enter your email address.";
                return View();
            }

            email = email.Trim().ToLowerInvariant();

            var user = await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.Email != null &&
                    u.Email.ToLower() == email
                );

            if (user == null)
            {
                ViewBag.Error =
                    "No account was found with this email address.";

                return View();
            }

            if (!user.Status)
            {
                ViewBag.Error = "This account is currently disabled.";
                return View();
            }

            TempData["ResetUserId"] = user.UserId;

            return RedirectToAction("ResetPassword");
        }

        [HttpGet]
        public IActionResult ResetPassword()
        {
            if (TempData["ResetUserId"] == null)
            {
                return RedirectToAction("ForgotPassword");
            }

            TempData.Keep("ResetUserId");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(
            string password,
            string confirmPassword)
        {
            if (TempData["ResetUserId"] == null)
            {
                return RedirectToAction("ForgotPassword");
            }

            int userId = Convert.ToInt32(TempData["ResetUserId"]);

            if (string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Please enter a new password.";
                TempData.Keep("ResetUserId");
                return View();
            }

            if (password.Length < 8)
            {
                ViewBag.Error =
                    "Password must contain at least 8 characters.";

                TempData.Keep("ResetUserId");
                return View();
            }

            if (!Regex.IsMatch(password, @"[A-Z]"))
            {
                ViewBag.Error =
                    "Password must contain at least one uppercase letter.";

                TempData.Keep("ResetUserId");
                return View();
            }

            if (!Regex.IsMatch(password, @"[a-z]"))
            {
                ViewBag.Error =
                    "Password must contain at least one lowercase letter.";

                TempData.Keep("ResetUserId");
                return View();
            }

            if (!Regex.IsMatch(password, @"\d"))
            {
                ViewBag.Error =
                    "Password must contain at least one number.";

                TempData.Keep("ResetUserId");
                return View();
            }

            if (password != confirmPassword)
            {
                ViewBag.Error =
                    "Password and confirm password do not match.";

                TempData.Keep("ResetUserId");
                return View();
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
            {
                ViewBag.Error = "User account could not be found.";
                return View();
            }

            user.Password = password;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Your password has been reset successfully. You can now login.";

            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        private bool IsPakistaniPhone(string? phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return false;

            phone = phone.Trim();

            return Regex.IsMatch(
                phone,
                @"^(03\d{2}-?\d{7}|(\+92|0092)-?3\d{2}-?\d{7})$"
            );
        }

        private string NormalizePhone(string phone)
        {
            phone = phone.Trim();

            phone = phone
                .Replace(" ", "")
                .Replace("-", "");

            if (phone.StartsWith("+92"))
            {
                phone = "0" + phone.Substring(3);
            }

            if (phone.StartsWith("0092"))
            {
                phone = "0" + phone.Substring(4);
            }

            return phone;
        }

        private bool IsValidCNIC(string? cnic)
        {
            if (string.IsNullOrWhiteSpace(cnic))
                return false;

            cnic = cnic.Trim();

            return Regex.IsMatch(
                cnic,
                @"^\d{5}-\d{7}-\d$"
            );
        }

        private bool IsRawalpindiAddress(string? address)
        {
            if (string.IsNullOrWhiteSpace(address))
                return false;

            return address.Contains(
                "Rawalpindi",
                StringComparison.OrdinalIgnoreCase
            );
        }

        private bool IsAllowedImage(IFormFile? file)
        {
            if (file == null || file.Length == 0)
            {
                return false;
            }

            if (file.Length > MaxImageSize)
            {
                return false;
            }

            string extension =
                Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!AllowedImageExtensions.Contains(extension))
            {
                return false;
            }

            string contentType =
                file.ContentType?.ToLowerInvariant() ?? "";

            if (contentType != "image/jpeg" &&
                contentType != "image/png")
            {
                return false;
            }

            return true;
        }

        private async Task LoadRoles()
        {
            ViewBag.Roles = await _context.Roles
                .Where(r => r.RoleName != "Admin")
                .OrderBy(r => r.RoleId)
                .ToListAsync();
        }
    }
}