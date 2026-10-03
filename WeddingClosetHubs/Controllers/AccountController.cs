using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
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

        private const long MaxImageSize = 5 * 1024 * 1024;

        private static readonly string[] AllowedImageExtensions =
        {
            ".jpg",
            ".jpeg",
            ".png"
        };

        private static readonly string[] AllowedProofExtensions =
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".pdf"
        };

        private static readonly string[] AllowedDeliveryZones =
        {
            "Saddar",
            "6th Road",
            "Liaqat Bagh"
        };

        private static readonly string[] AllowedEmailDomains =
        {
            "gmail.com",
            "yahoo.com",
            "hotmail.com",
            "outlook.com"
        };

        private static readonly string[] AllowedShopProofTypes =
        {
            "Utility Bill",
            "Rental Agreement",
            "Business Registration"
        };

        private string NormalizeCNIC(string cnic)
        {
            return cnic
                .Trim()
                .Replace(" ", "")
                .Replace("-", "");
        }

    

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
            string[]? ShopCollections,
            string? ShopPhone,
            string? ShopAddress,
            string? ShopDescription,
            IFormFile? ShopFrontPhoto,
            IFormFile? ShopInsidePhoto,
            IFormFile? ShopSignboardPhoto,
            string? ShopProofType,
            IFormFile? ShopProofDocument,

         
            string? ShopkeeperCNIC,
            IFormFile? ShopkeeperCNICFrontImage,
            IFormFile? ShopkeeperCNICBackImage,

            string? DeliveryCNIC,
            IFormFile? DeliveryCNICFrontImage,
            IFormFile? DeliveryCNICBackImage,
            string? VehicleType,
            string? MotorbikeNumber,
            string? PreferredZone)
        {
            ModelState.Clear();


            if (string.IsNullOrWhiteSpace(user.Name))
            {
                ModelState.AddModelError(
                    "Name",
                    "Name is required.");
            }
            else
            {
                user.Name = user.Name.Trim();

                if (user.Name.Length < 3)
                {
                    ModelState.AddModelError(
                        "Name",
                        "Name must contain at least 3 characters.");
                }

                if (user.Name.Length > 100)
                {
                    ModelState.AddModelError(
                        "Name",
                        "Name cannot exceed 100 characters.");
                }

                if (!Regex.IsMatch(
                    user.Name,
                    @"^[A-Za-z ]+$"))
                {
                    ModelState.AddModelError(
                        "Name",
                        "Name can contain letters and spaces only.");
                }
            }

            

            if (string.IsNullOrWhiteSpace(user.Email))
            {
                ModelState.AddModelError(
                    "Email",
                    "Email is required.");
            }
            else
            {
                user.Email =
                    user.Email.Trim()
                              .ToLowerInvariant();

                if (!new EmailAddressAttribute()
                    .IsValid(user.Email))
                {
                    ModelState.AddModelError(
                        "Email",
                        "Please enter a valid email address.");
                }
                else
                {
                    string[] emailParts =
                        user.Email.Split('@');

                    if (emailParts.Length != 2)
                    {
                        ModelState.AddModelError(
                            "Email",
                            "Please enter a valid email address.");
                    }
                    else
                    {
                        string emailName =
                            emailParts[0];

                        string emailDomain =
                            emailParts[1];

                        if (string.IsNullOrWhiteSpace(emailName))
                        {
                            ModelState.AddModelError(
                                "Email",
                                "Please enter a valid email address.");
                        }

                        if (!AllowedEmailDomains.Contains(
                            emailDomain,
                            StringComparer.OrdinalIgnoreCase))
                        {
                            ModelState.AddModelError(
                                "Email",
                                "Please use a valid email domain such as gmail.com, yahoo.com, hotmail.com or outlook.com.");
                        }

                        if (!Regex.IsMatch(
                            emailName,
                            @"^[a-zA-Z0-9._%+-]+$"))
                        {
                            ModelState.AddModelError(
                                "Email",
                                "Email address contains invalid characters.");
                        }
                    }
                }
            }

        

            if (!string.IsNullOrWhiteSpace(user.Email) &&
                ModelState["Email"]?.Errors.Count == 0)
            {
                string normalizedEmail =
                    user.Email.Trim()
                              .ToLowerInvariant();

                bool emailExists =
                    await _context.Users.AnyAsync(u =>
                        u.Email != null &&
                        u.Email.ToLower() ==
                        normalizedEmail);

                if (emailExists)
                {
                    ModelState.AddModelError(
                        "Email",
                        "This email is already registered.");
                }
            }

           

            if (string.IsNullOrWhiteSpace(user.Phone))
            {
                ModelState.AddModelError(
                    "Phone",
                    "Phone number is required.");
            }
            else
            {
                user.Phone =
                    user.Phone.Trim();

                if (!IsPakistaniPhone(user.Phone))
                {
                    ModelState.AddModelError(
                        "Phone",
                        "Please enter a valid Pakistani mobile number.");
                }
            }

           

            if (!string.IsNullOrWhiteSpace(user.Phone) &&
                IsPakistaniPhone(user.Phone))
            {
                string normalizedPhone =
                    NormalizePhone(user.Phone);

                bool phoneExists =
                    await _context.Users.AnyAsync(u =>
                        u.Phone != null &&
                        u.Phone == normalizedPhone);

                if (phoneExists)
                {
                    ModelState.AddModelError(
                        "Phone",
                        "This phone number is already registered.");
                }
            }


            if (string.IsNullOrWhiteSpace(user.Address))
            {
                ModelState.AddModelError(
                    "Address",
                    "Address is required.");
            }
            else
            {
                user.Address =
                    user.Address.Trim();

                if (!IsRawalpindiAddress(user.Address))
                {
                    ModelState.AddModelError(
                        "Address",
                        "Please enter an address in Rawalpindi.");
                }
            }

        

            if (!user.RoleId.HasValue)
            {
                ModelState.AddModelError(
                    "RoleId",
                    "Please select an account type.");

                await LoadRoles();

                return View(user);
            }

            var role =
                await _context.Roles
                    .FirstOrDefaultAsync(r =>
                        r.RoleId ==
                        user.RoleId.Value);

            if (role == null)
            {
                ModelState.AddModelError(
                    "RoleId",
                    "Please select a valid account type.");

                await LoadRoles();

                return View(user);
            }

            if (role.RoleName == "Admin")
            {
                ModelState.AddModelError(
                    "RoleId",
                    "Admin account cannot be created through public registration.");

                await LoadRoles();

                return View(user);
            }

          

            if (string.IsNullOrWhiteSpace(user.Password))
            {
                ModelState.AddModelError(
                    "Password",
                    "Password is required.");
            }
            else
            {
                if (user.Password.Length < 8)
                {
                    ModelState.AddModelError(
                        "Password",
                        "Password must contain at least 8 characters.");
                }

                if (!Regex.IsMatch(
                    user.Password,
                    @"[A-Z]"))
                {
                    ModelState.AddModelError(
                        "Password",
                        "Password must contain at least one uppercase letter.");
                }

                if (!Regex.IsMatch(
                    user.Password,
                    @"[a-z]"))
                {
                    ModelState.AddModelError(
                        "Password",
                        "Password must contain at least one lowercase letter.");
                }

                if (!Regex.IsMatch(
                    user.Password,
                    @"\d"))
                {
                    ModelState.AddModelError(
                        "Password",
                        "Password must contain at least one number.");
                }
            }

         

            if (role.RoleName == "Shopkeeper")
            {

                if (string.IsNullOrWhiteSpace(
                    ShopkeeperCNIC))
                {
                    ModelState.AddModelError(
                        "ShopkeeperCNIC",
                        "CNIC is required.");
                }
                else
                {
                    ShopkeeperCNIC =
                        ShopkeeperCNIC.Trim();

                    if (!IsValidCNIC(
                        ShopkeeperCNIC))
                    {
                        ModelState.AddModelError(
                            "ShopkeeperCNIC",
                            "CNIC must be in the format 35202-1234567-1.");
                    }
                }

               

                if (!string.IsNullOrWhiteSpace(
                    ShopkeeperCNIC) &&
                    IsValidCNIC(
                        ShopkeeperCNIC))
                {
                    string normalizedCNIC =
                        NormalizeCNIC(
                            ShopkeeperCNIC);

                    bool cnicExists =
                        await _context.Users.AnyAsync(u =>
                            u.CNIC != null &&
                            u.CNIC
                                .Replace("-", "")
                                .Replace(" ", "") ==
                            normalizedCNIC);

                    if (cnicExists)
                    {
                        ModelState.AddModelError(
                            "ShopkeeperCNIC",
                            "This CNIC is already registered.");
                    }
                }

               

                if (ShopkeeperCNICFrontImage == null ||
                    ShopkeeperCNICFrontImage.Length == 0)
                {
                    ModelState.AddModelError(
                        "ShopkeeperCNICFrontImage",
                        "CNIC front image is required.");
                }
                else if (!IsAllowedImage(
                    ShopkeeperCNICFrontImage))
                {
                    ModelState.AddModelError(
                        "ShopkeeperCNICFrontImage",
                        "CNIC front image must be JPG, JPEG or PNG and maximum 5 MB.");
                }

             

                if (ShopkeeperCNICBackImage == null ||
                    ShopkeeperCNICBackImage.Length == 0)
                {
                    ModelState.AddModelError(
                        "ShopkeeperCNICBackImage",
                        "CNIC back image is required.");
                }
                else if (!IsAllowedImage(
                    ShopkeeperCNICBackImage))
                {
                    ModelState.AddModelError(
                        "ShopkeeperCNICBackImage",
                        "CNIC back image must be JPG, JPEG or PNG and maximum 5 MB.");
                }

             

                if (string.IsNullOrWhiteSpace(
                    ShopName))
                {
                    ModelState.AddModelError(
                        "ShopName",
                        "Shop name is required.");
                }
                else
                {
                    ShopName =
                        ShopName.Trim();

                    if (ShopName.Length < 3)
                    {
                        ModelState.AddModelError(
                            "ShopName",
                            "Shop name must contain at least 3 characters.");
                    }

                    if (ShopName.Length > 100)
                    {
                        ModelState.AddModelError(
                            "ShopName",
                            "Shop name cannot exceed 100 characters.");
                    }
                }


                if (string.IsNullOrWhiteSpace(
                    ShopCategory))
                {
                    ModelState.AddModelError(
                        "ShopCategory",
                        "Shop category is required.");
                }
                else
                {
                    ShopCategory =
                        ShopCategory.Trim();

                    if (ShopCategory.Length > 100)
                    {
                        ModelState.AddModelError(
                            "ShopCategory",
                            "Shop category cannot exceed 100 characters.");
                    }
                }

               

                string[] allowedShopCollections =
                {
                    "Bridal Wear",
                    "Groom Wear",
                    "Formal Wear",
                    "Wedding Guest Wear",
                    "Footwear",
                    "Jewellery",
                    "Bridal Jewellery",
                    "Accessories",
                    "Bridal Accessories"
                };

                ShopCollections =
                    ShopCollections?
                        .Where(x =>
                            !string.IsNullOrWhiteSpace(x))
                        .Select(x =>
                            x.Trim())
                        .Distinct(
                            StringComparer.OrdinalIgnoreCase)
                        .ToArray();

                if (ShopCollections == null ||
                    ShopCollections.Length == 0)
                {
                    ModelState.AddModelError(
                        "ShopCollections",
                        "Please select at least one shop collection.");
                }
                else
                {
                    foreach (string collection
                             in ShopCollections)
                    {
                        if (!allowedShopCollections.Any(
                            x => string.Equals(
                                x,
                                collection,
                                StringComparison.OrdinalIgnoreCase)))
                        {
                            ModelState.AddModelError(
                                "ShopCollections",
                                "One or more selected shop collections are invalid.");

                            break;
                        }
                    }
                }

            

                if (string.IsNullOrWhiteSpace(
                    ShopPhone))
                {
                    ModelState.AddModelError(
                        "ShopPhone",
                        "Shop phone is required.");
                }
                else
                {
                    ShopPhone =
                        ShopPhone.Trim();

                    if (!IsPakistaniPhone(
                        ShopPhone))
                    {
                        ModelState.AddModelError(
                            "ShopPhone",
                            "Please enter a valid Pakistani mobile number.");
                    }
                }

                

                if (string.IsNullOrWhiteSpace(
                    ShopAddress))
                {
                    ModelState.AddModelError(
                        "ShopAddress",
                        "Shop address is required.");
                }
                else
                {
                    ShopAddress =
                        ShopAddress.Trim();

                    if (!IsRawalpindiAddress(
                        ShopAddress))
                    {
                        ModelState.AddModelError(
                            "ShopAddress",
                            "Please enter a shop address in Rawalpindi.");
                    }
                }

                

                if (string.IsNullOrWhiteSpace(
                    ShopDescription))
                {
                    ModelState.AddModelError(
                        "ShopDescription",
                        "Shop description is required.");
                }
                else
                {
                    ShopDescription =
                        ShopDescription.Trim();

                    if (ShopDescription.Length < 10)
                    {
                        ModelState.AddModelError(
                            "ShopDescription",
                            "Shop description must contain at least 10 characters.");
                    }

                    if (ShopDescription.Length > 1000)
                    {
                        ModelState.AddModelError(
                            "ShopDescription",
                            "Shop description cannot exceed 1000 characters.");
                    }
                }


                if (ShopFrontPhoto == null ||
                    ShopFrontPhoto.Length == 0)
                {
                    ModelState.AddModelError(
                        "ShopFrontPhoto",
                        "Shop front photo is required.");
                }
                else if (!IsAllowedImage(
                    ShopFrontPhoto))
                {
                    ModelState.AddModelError(
                        "ShopFrontPhoto",
                        "Shop front photo must be JPG, JPEG or PNG and maximum 5 MB.");
                }

               

                if (ShopInsidePhoto == null ||
                    ShopInsidePhoto.Length == 0)
                {
                    ModelState.AddModelError(
                        "ShopInsidePhoto",
                        "Shop inside photo is required.");
                }
                else if (!IsAllowedImage(
                    ShopInsidePhoto))
                {
                    ModelState.AddModelError(
                        "ShopInsidePhoto",
                        "Shop inside photo must be JPG, JPEG or PNG and maximum 5 MB.");
                }


                if (ShopSignboardPhoto == null ||
                    ShopSignboardPhoto.Length == 0)
                {
                    ModelState.AddModelError(
                        "ShopSignboardPhoto",
                        "Shop signboard photo is required.");
                }
                else if (!IsAllowedImage(
                    ShopSignboardPhoto))
                {
                    ModelState.AddModelError(
                        "ShopSignboardPhoto",
                        "Shop signboard photo must be JPG, JPEG or PNG and maximum 5 MB.");
                }

            

                if (string.IsNullOrWhiteSpace(
                    ShopProofType))
                {
                    ModelState.AddModelError(
                        "ShopProofType",
                        "Please select a shop proof type.");
                }
                else
                {
                    ShopProofType =
                        ShopProofType.Trim();

                    bool validProofType =
                        AllowedShopProofTypes.Any(
                            p => string.Equals(
                                p,
                                ShopProofType,
                                StringComparison.OrdinalIgnoreCase));

                    if (!validProofType)
                    {
                        ModelState.AddModelError(
                            "ShopProofType",
                            "Please select a valid shop proof type.");
                    }
                }

                

                if (!IsAllowedProofDocument(
                    ShopProofDocument))
                {
                    ModelState.AddModelError(
                        "ShopProofDocument",
                        "Shop proof must be JPG, JPEG, PNG or PDF and maximum 5 MB.");
                }
            }

          

            if (role.RoleName == "Delivery")
            {
                
                if (string.IsNullOrWhiteSpace(
                    DeliveryCNIC))
                {
                    ModelState.AddModelError(
                        "DeliveryCNIC",
                        "CNIC is required.");
                }
                else
                {
                    DeliveryCNIC =
                        DeliveryCNIC.Trim();

                    if (!IsValidCNIC(
                        DeliveryCNIC))
                    {
                        ModelState.AddModelError(
                            "DeliveryCNIC",
                            "CNIC must be in the format 35202-1234567-1.");
                    }
                }


                if (!string.IsNullOrWhiteSpace(
                    DeliveryCNIC) &&
                    IsValidCNIC(
                        DeliveryCNIC))
                {
                    string normalizedCNIC =
                        NormalizeCNIC(
                            DeliveryCNIC);

                    bool cnicExists =
                        await _context.DeliveryBoys
                            .AnyAsync(d =>
                                d.CNIC != null &&
                                d.CNIC
                                    .Replace("-", "")
                                    .Replace(" ", "") ==
                                normalizedCNIC);

                    if (cnicExists)
                    {
                        ModelState.AddModelError(
                            "DeliveryCNIC",
                            "This CNIC is already registered.");
                    }
                }


                if (DeliveryCNICFrontImage == null ||
                    DeliveryCNICFrontImage.Length == 0)
                {
                    ModelState.AddModelError(
                        "DeliveryCNICFrontImage",
                        "CNIC front image is required.");
                }
                else if (!IsAllowedImage(
                    DeliveryCNICFrontImage))
                {
                    ModelState.AddModelError(
                        "DeliveryCNICFrontImage",
                        "CNIC front image must be JPG, JPEG or PNG and maximum 5 MB.");
                }

             

                if (DeliveryCNICBackImage == null ||
                    DeliveryCNICBackImage.Length == 0)
                {
                    ModelState.AddModelError(
                        "DeliveryCNICBackImage",
                        "CNIC back image is required.");
                }
                else if (!IsAllowedImage(
                    DeliveryCNICBackImage))
                {
                    ModelState.AddModelError(
                        "DeliveryCNICBackImage",
                        "CNIC back image must be JPG, JPEG or PNG and maximum 5 MB.");
                }

            

                if (string.IsNullOrWhiteSpace(
                    VehicleType))
                {
                    ModelState.AddModelError(
                        "VehicleType",
                        "Vehicle type is required.");
                }
                else if (!string.Equals(
                    VehicleType.Trim(),
                    "Motorbike",
                    StringComparison.OrdinalIgnoreCase))
                {
                    ModelState.AddModelError(
                        "VehicleType",
                        "Only Motorbike is allowed for delivery.");
                }

                

                if (string.IsNullOrWhiteSpace(
                    MotorbikeNumber))
                {
                    ModelState.AddModelError(
                        "MotorbikeNumber",
                        "Motorbike registration number is required.");
                }
                else
                {
                    MotorbikeNumber =
                        MotorbikeNumber
                            .Trim()
                            .ToUpperInvariant();

                    if (MotorbikeNumber.Length < 3)
                    {
                        ModelState.AddModelError(
                            "MotorbikeNumber",
                            "Please enter a valid motorbike registration number.");
                    }

                    if (MotorbikeNumber.Length > 30)
                    {
                        ModelState.AddModelError(
                            "MotorbikeNumber",
                            "Motorbike registration number is too long.");
                    }
                }

              

                if (!string.IsNullOrWhiteSpace(
                    MotorbikeNumber))
                {
                    string normalizedMotorbike =
                        MotorbikeNumber
                            .Trim()
                            .ToUpperInvariant();

                    bool motorbikeExists =
                        await _context.DeliveryBoys
                            .AnyAsync(d =>
                                d.MotorbikeNumber != null &&
                                d.MotorbikeNumber.ToUpper() ==
                                normalizedMotorbike);

                    if (motorbikeExists)
                    {
                        ModelState.AddModelError(
                            "MotorbikeNumber",
                            "This motorbike registration number is already registered.");
                    }
                }

              

                if (string.IsNullOrWhiteSpace(
                    PreferredZone))
                {
                    ModelState.AddModelError(
                        "PreferredZone",
                        "Preferred delivery zone is required.");
                }
                else
                {
                    PreferredZone =
                        PreferredZone.Trim();

                    bool validZone =
                        AllowedDeliveryZones.Any(
                            z => string.Equals(
                                z,
                                PreferredZone,
                                StringComparison.OrdinalIgnoreCase));

                    if (!validZone)
                    {
                        ModelState.AddModelError(
                            "PreferredZone",
                            "Please select a valid Rawalpindi delivery zone.");
                    }
                }
            }

         

            if (!ModelState.IsValid)
            {
                await LoadRoles();

                return View(user);
            }

           

            user.Email =
                user.Email!
                    .Trim()
                    .ToLowerInvariant();

            user.Name =
                user.Name!
                    .Trim();

            user.Phone =
                NormalizePhone(
                    user.Phone!);

            user.Address =
                user.Address!
                    .Trim();

            user.Status = true;

            user.CreatedDate =
                DateTime.Now;

          
            if (role.RoleName == "Shopkeeper")
            {
                user.CNIC =
                    NormalizeCNIC(
                        ShopkeeperCNIC!);
            }

            

            if (role.RoleName == "Customer")
            {
                user.IsApproved = true;

                user.ApprovedDate =
                    DateTime.Now;
            }
            else
            {
                user.IsApproved = false;

                user.ApprovedDate = null;
            }

            

            using var transaction =
                await _context.Database
                    .BeginTransactionAsync();

            try
            {
                

                _context.Users.Add(user);

                await _context.SaveChangesAsync();

               

                if (role.RoleName == "Shopkeeper")
                {
                   

                    string shopFolder =
                        Path.Combine(
                            Directory.GetCurrentDirectory(),
                            "wwwroot",
                            "uploads",
                            "shops");

                    string cnicFolder =
                        Path.Combine(
                            Directory.GetCurrentDirectory(),
                            "wwwroot",
                            "uploads",
                            "cnic");

                    string shopProofFolder =
                        Path.Combine(
                            Directory.GetCurrentDirectory(),
                            "wwwroot",
                            "uploads",
                            "shopproof");

                    Directory.CreateDirectory(
                        shopFolder);

                    Directory.CreateDirectory(
                        cnicFolder);

                    Directory.CreateDirectory(
                        shopProofFolder);

                   

                    string frontExtension =
                        Path.GetExtension(
                            ShopFrontPhoto!.FileName)
                            .ToLowerInvariant();

                    string frontFileName =
                        Guid.NewGuid().ToString() +
                        frontExtension;

                    string frontFilePath =
                        Path.Combine(
                            shopFolder,
                            frontFileName);

                    using (var stream =
                        new FileStream(
                            frontFilePath,
                            FileMode.Create))
                    {
                        await ShopFrontPhoto
                            .CopyToAsync(stream);
                    }


                    string insideExtension =
                        Path.GetExtension(
                            ShopInsidePhoto!.FileName)
                            .ToLowerInvariant();

                    string insideFileName =
                        Guid.NewGuid().ToString() +
                        insideExtension;

                    string insideFilePath =
                        Path.Combine(
                            shopFolder,
                            insideFileName);

                    using (var stream =
                        new FileStream(
                            insideFilePath,
                            FileMode.Create))
                    {
                        await ShopInsidePhoto
                            .CopyToAsync(stream);
                    }


                    string signboardExtension =
                        Path.GetExtension(
                            ShopSignboardPhoto!.FileName)
                            .ToLowerInvariant();

                    string signboardFileName =
                        Guid.NewGuid().ToString() +
                        signboardExtension;

                    string signboardFilePath =
                        Path.Combine(
                            shopFolder,
                            signboardFileName);

                    using (var stream =
                        new FileStream(
                            signboardFilePath,
                            FileMode.Create))
                    {
                        await ShopSignboardPhoto
                            .CopyToAsync(stream);
                    }

             

                    string cnicFrontExtension =
                        Path.GetExtension(
                            ShopkeeperCNICFrontImage!.FileName)
                            .ToLowerInvariant();

                    string cnicFrontFileName =
                        Guid.NewGuid().ToString() +
                        cnicFrontExtension;

                    string cnicFrontFilePath =
                        Path.Combine(
                            cnicFolder,
                            cnicFrontFileName);

                    using (var stream =
                        new FileStream(
                            cnicFrontFilePath,
                            FileMode.Create))
                    {
                        await ShopkeeperCNICFrontImage
                            .CopyToAsync(stream);
                    }

                  

                    string cnicBackExtension =
                        Path.GetExtension(
                            ShopkeeperCNICBackImage!.FileName)
                            .ToLowerInvariant();

                    string cnicBackFileName =
                        Guid.NewGuid().ToString() +
                        cnicBackExtension;

                    string cnicBackFilePath =
                        Path.Combine(
                            cnicFolder,
                            cnicBackFileName);

                    using (var stream =
                        new FileStream(
                            cnicBackFilePath,
                            FileMode.Create))
                    {
                        await ShopkeeperCNICBackImage
                            .CopyToAsync(stream);
                    }

                 
                    string proofExtension =
                        Path.GetExtension(
                            ShopProofDocument!.FileName)
                            .ToLowerInvariant();

                    string proofFileName =
                        Guid.NewGuid().ToString() +
                        proofExtension;

                    string proofFilePath =
                        Path.Combine(
                            shopProofFolder,
                            proofFileName);

                    using (var stream =
                        new FileStream(
                            proofFilePath,
                            FileMode.Create))
                    {
                        await ShopProofDocument
                            .CopyToAsync(stream);
                    }

                  

                    var shop = new Shop
                    {
                        ShopkeeperId =
                            user.UserId,

                        ShopName =
                            ShopName!.Trim(),

                        ShopCategory =
                            ShopCategory!.Trim(),

                        ShopCollections =
                            string.Join(
                                ",",
                                ShopCollections!),

                        ShopPhone =
                            NormalizePhone(
                                ShopPhone!),

                        ShopAddress =
                            ShopAddress!.Trim(),

                        ShopDescription =
                            string.IsNullOrWhiteSpace(
                                ShopDescription)
                                ? null
                                : ShopDescription.Trim(),

                        ShopFrontPhoto =
                            "/uploads/shops/" +
                            frontFileName,

                        ShopInsidePhoto =
                            "/uploads/shops/" +
                            insideFileName,

                        ShopSignboardPhoto =
                            "/uploads/shops/" +
                            signboardFileName,

                        ShopProofType =
                            ShopProofType!.Trim(),

                        ShopProofDocument =
                            "/uploads/shopproof/" +
                            proofFileName,

                        

                        CNICFrontImage =
                            "/uploads/cnic/" +
                            cnicFrontFileName,

                        CNICBackImage =
                            "/uploads/cnic/" +
                            cnicBackFileName,

                       

                        VerificationStatus =
                            "Pending",

                        AdminNotes = null,

                        RejectionReason = null,

                        VerificationSubmittedDate =
                            DateTime.Now,

                        VerifiedDate = null,

                        IsApproved = false,

                        Status = false,

                        ApprovedDate = null,

                        CreatedDate =
                            DateTime.Now
                    };

                    _context.Shops.Add(shop);

                    await _context.SaveChangesAsync();
                }

           

                if (role.RoleName == "Delivery")
                {
                    string uploadFolder =
                        Path.Combine(
                            Directory.GetCurrentDirectory(),
                            "wwwroot",
                            "uploads",
                            "cnic");

                    Directory.CreateDirectory(
                        uploadFolder);

                   

                    string frontExtension =
                        Path.GetExtension(
                            DeliveryCNICFrontImage!.FileName)
                            .ToLowerInvariant();

                    string frontFileName =
                        Guid.NewGuid().ToString() +
                        frontExtension;

                    string frontFilePath =
                        Path.Combine(
                            uploadFolder,
                            frontFileName);

                    using (var stream =
                        new FileStream(
                            frontFilePath,
                            FileMode.Create))
                    {
                        await DeliveryCNICFrontImage
                            .CopyToAsync(stream);
                    }

                

                    string backExtension =
                        Path.GetExtension(
                            DeliveryCNICBackImage!.FileName)
                            .ToLowerInvariant();

                    string backFileName =
                        Guid.NewGuid().ToString() +
                        backExtension;

                    string backFilePath =
                        Path.Combine(
                            uploadFolder,
                            backFileName);

                    using (var stream =
                        new FileStream(
                            backFilePath,
                            FileMode.Create))
                    {
                        await DeliveryCNICBackImage
                            .CopyToAsync(stream);
                    }


                    var deliveryBoy =
                        new DeliveryBoy
                        {
                            UserId =
                                user.UserId,

                            CNIC =
                                DeliveryCNIC!
                                    .Trim(),

                            MotorbikeNumber =
                                MotorbikeNumber!
                                    .Trim()
                                    .ToUpperInvariant(),

                            PreferredZone =
                                PreferredZone!
                                    .Trim(),

                            CNICFrontImage =
                                "/uploads/cnic/" +
                                frontFileName,

                            CNICBackImage =
                                "/uploads/cnic/" +
                                backFileName,

                            AssignedZone = null,

                            VerificationStatus =
                                "Pending",

                            AdminNotes = null,

                            RejectionReason = null,

                            ApprovedDate = null,

                            CreatedDate =
                                DateTime.Now
                        };

                    _context.DeliveryBoys.Add(
                        deliveryBoy);

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
                    (ex.InnerException?.Message ??
                     ex.Message));

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
                    "Shopkeeper registration submitted successfully. Please wait for Admin verification and approval.";
            }
            else if (role.RoleName == "Delivery")
            {
                TempData["Success"] =
                    "Delivery account registration submitted successfully. Please wait for Admin verification and approval.";
            }

            return RedirectToAction(
                "Login");
        }

     

        [HttpGet]
        public IActionResult Login(
            string? returnUrl)
        {
            ViewBag.ReturnUrl =
                returnUrl;

            return View();
        }

     

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(
            string email,
            string password,
            string roleName,
            string? returnUrl)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                ViewBag.Error =
                    "Please enter your email.";

                ViewBag.ReturnUrl =
                    returnUrl;

                return View();
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error =
                    "Please enter your password.";

                ViewBag.ReturnUrl =
                    returnUrl;

                return View();
            }

            if (string.IsNullOrWhiteSpace(roleName))
            {
                ViewBag.Error =
                    "Please select an account type.";

                ViewBag.ReturnUrl =
                    returnUrl;

                return View();
            }

            email =
                email.Trim()
                     .ToLowerInvariant();

            var user =
                await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u =>
                        u.Email != null &&
                        u.Email.ToLower() == email &&
                        u.Password == password &&
                        u.Status == true &&
                        u.Role != null &&
                        u.Role.RoleName == roleName);

            if (user == null)
            {
                ViewBag.Error =
                    "Invalid email, password, or account type.";

                ViewBag.ReturnUrl =
                    returnUrl;

                return View();
            }

            if (!user.IsApproved)
            {
                if (user.Role!.RoleName ==
                    "Shopkeeper")
                {
                    ViewBag.Error =
                        "Your shopkeeper account is waiting for Admin approval.";

                    ViewBag.ReturnUrl =
                        returnUrl;

                    return View();
                }

                if (user.Role!.RoleName ==
                    "Delivery")
                {
                    ViewBag.Error =
                        "Your delivery account is waiting for Admin verification and approval.";

                    ViewBag.ReturnUrl =
                        returnUrl;

                    return View();
                }

                ViewBag.Error =
                    "Your account is waiting for Admin approval.";

                ViewBag.ReturnUrl =
                    returnUrl;

                return View();
            }


            HttpContext.Session.SetInt32(
                "UserId",
                user.UserId);

            HttpContext.Session.SetString(
                "UserName",
                user.Name ?? "");

            if (user.Role!.RoleName ==
                "Customer")
            {
                HttpContext.Session.SetInt32(
                    "CustomerId",
                    user.UserId);
            }

            if (user.RoleId.HasValue)
            {
                HttpContext.Session.SetInt32(
                    "RoleId",
                    user.RoleId.Value);
            }

            HttpContext.Session.SetString(
                "RoleName",
                user.Role.RoleName);

            

            switch (user.Role.RoleName)
            {
                case "Admin":

                    return RedirectToAction(
                        "Dashboard",
                        "Admin");

                case "Shopkeeper":

                    return RedirectToAction(
                        "Dashboard",
                        "Shopkeeper");

                case "Customer":

                    var pendingJson =
                        HttpContext.Session
                            .GetString(
                                "PendingCartItems");

                    if (!string.IsNullOrWhiteSpace(
                        pendingJson))
                    {
                        try
                        {
                            var pendingItems =
                                JsonSerializer
                                    .Deserialize<
                                        List<CustomerCartItem>>(
                                            pendingJson,
                                            new JsonSerializerOptions
                                            {
                                                PropertyNameCaseInsensitive =
                                                    true
                                            })
                                ?? new List<CustomerCartItem>();

                            if (pendingItems.Any())
                            {
                                var cartJson =
                                    HttpContext.Session
                                        .GetString(
                                            "CustomerCart");

                                var cartItems =
                                    string.IsNullOrWhiteSpace(
                                        cartJson)
                                        ? new List<CustomerCartItem>()
                                        : JsonSerializer
                                            .Deserialize<
                                                List<CustomerCartItem>>(
                                                cartJson,
                                                new JsonSerializerOptions
                                                {
                                                    PropertyNameCaseInsensitive =
                                                        true
                                                })
                                          ?? new List<CustomerCartItem>();

                                foreach (
                                    var pendingItem
                                    in pendingItems)
                                {
                                    var existingItem =
                                        cartItems.FirstOrDefault(
                                            x =>
                                                x.ProductId ==
                                                pendingItem.ProductId &&

                                                string.Equals(
                                                    x.Size ?? "",
                                                    pendingItem.Size ?? "",
                                                    StringComparison.OrdinalIgnoreCase) &&

                                                string.Equals(
                                                    x.PurchaseType ??
                                                        "Buy",
                                                    pendingItem.PurchaseType ??
                                                        "Buy",
                                                    StringComparison.OrdinalIgnoreCase));

                                    if (existingItem != null)
                                    {
                                        existingItem.Quantity +=
                                            pendingItem.Quantity;
                                    }
                                    else
                                    {
                                        cartItems.Add(
                                            pendingItem);
                                    }
                                }

                                HttpContext.Session.SetString(
                                    "CustomerCart",
                                    JsonSerializer.Serialize(
                                        cartItems));
                            }

                            HttpContext.Session.Remove(
                                "PendingCartItems");
                        }
                        catch
                        {
                            HttpContext.Session.Remove(
                                "PendingCartItems");
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(
                        returnUrl) &&
                        Url.IsLocalUrl(
                            returnUrl))
                    {
                        return Redirect(
                            returnUrl);
                    }

                    return RedirectToAction(
                        "Cart",
                        "Customer");

                case "Delivery":

                    return RedirectToAction(
                        "Dashboard",
                        "Delivery");

                default:

                    HttpContext.Session.Clear();

                    ViewBag.Error =
                        "Invalid account type.";

                    ViewBag.ReturnUrl =
                        returnUrl;

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
        public async Task<IActionResult> ForgotPassword(
            string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                ViewBag.Error =
                    "Please enter your email address.";

                return View();
            }

            email =
                email.Trim()
                     .ToLowerInvariant();

            var user =
                await _context.Users
                    .FirstOrDefaultAsync(u =>
                        u.Email != null &&
                        u.Email.ToLower() == email);

            if (user == null)
            {
                ViewBag.Error =
                    "No account was found with this email address.";

                return View();
            }

            if (!user.Status)
            {
                ViewBag.Error =
                    "This account is currently disabled.";

                return View();
            }

            TempData["ResetUserId"] =
                user.UserId;

            return RedirectToAction(
                "ResetPassword");
        }


        [HttpGet]
        public IActionResult ResetPassword()
        {
            if (TempData["ResetUserId"] == null)
            {
                return RedirectToAction(
                    "ForgotPassword");
            }

            TempData.Keep(
                "ResetUserId");

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
                return RedirectToAction(
                    "ForgotPassword");
            }

            int userId =
                Convert.ToInt32(
                    TempData["ResetUserId"]);

            if (string.IsNullOrWhiteSpace(
                password))
            {
                ViewBag.Error =
                    "Please enter a new password.";

                TempData.Keep(
                    "ResetUserId");

                return View();
            }

            if (password.Length < 8)
            {
                ViewBag.Error =
                    "Password must contain at least 8 characters.";

                TempData.Keep(
                    "ResetUserId");

                return View();
            }

            if (!Regex.IsMatch(
                password,
                @"[A-Z]"))
            {
                ViewBag.Error =
                    "Password must contain at least one uppercase letter.";

                TempData.Keep(
                    "ResetUserId");

                return View();
            }

            if (!Regex.IsMatch(
                password,
                @"[a-z]"))
            {
                ViewBag.Error =
                    "Password must contain at least one lowercase letter.";

                TempData.Keep(
                    "ResetUserId");

                return View();
            }

            if (!Regex.IsMatch(
                password,
                @"\d"))
            {
                ViewBag.Error =
                    "Password must contain at least one number.";

                TempData.Keep(
                    "ResetUserId");

                return View();
            }

            if (password !=
                confirmPassword)
            {
                ViewBag.Error =
                    "Password and confirm password do not match.";

                TempData.Keep(
                    "ResetUserId");

                return View();
            }

            var user =
                await _context.Users
                    .FirstOrDefaultAsync(
                        u =>
                            u.UserId ==
                            userId);

            if (user == null)
            {
                ViewBag.Error =
                    "User account could not be found.";

                return View();
            }

            user.Password =
                password;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Your password has been reset successfully. You can now login.";

            return RedirectToAction(
                "Login");
        }

   
      

        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();

            return RedirectToAction(
                "Login");
        }

     

        private bool IsPakistaniPhone(
            string? phone)
        {
            if (string.IsNullOrWhiteSpace(
                phone))
            {
                return false;
            }

            phone =
                phone.Trim();

            return Regex.IsMatch(
                phone,
                @"^(03\d{2}-?\d{7}|(\+92|0092)-?3\d{2}-?\d{7})$");
        }

     

        private string NormalizePhone(
            string phone)
        {
            phone =
                phone.Trim()
                     .Replace(" ", "")
                     .Replace("-", "");

            if (phone.StartsWith("+92"))
            {
                phone =
                    "0" +
                    phone.Substring(3);
            }

            if (phone.StartsWith("0092"))
            {
                phone =
                    "0" +
                    phone.Substring(4);
            }

            return phone;
        }

      

        private bool IsValidCNIC(
            string? cnic)
        {
            if (string.IsNullOrWhiteSpace(
                cnic))
            {
                return false;
            }

            cnic =
                cnic.Trim();

            return Regex.IsMatch(
                cnic,
                @"^\d{5}-\d{7}-\d$");
        }

      

        private bool IsRawalpindiAddress(
            string? address)
        {
            if (string.IsNullOrWhiteSpace(
                address))
            {
                return false;
            }

            return address.Contains(
                "Rawalpindi",
                StringComparison.OrdinalIgnoreCase);
        }

      

        private bool IsAllowedImage(
            IFormFile? file)
        {
            if (file == null ||
                file.Length == 0)
            {
                return false;
            }

            if (file.Length > MaxImageSize)
            {
                return false;
            }

            string extension =
                Path.GetExtension(
                    file.FileName)
                    .ToLowerInvariant();

            if (!AllowedImageExtensions.Contains(
                extension))
            {
                return false;
            }

            string contentType =
                file.ContentType?
                    .ToLowerInvariant() ?? "";

            if (contentType != "image/jpeg" &&
                contentType != "image/png")
            {
                return false;
            }

            return true;
        }

       

        private bool IsAllowedProofDocument(
            IFormFile? file)
        {
            if (file == null ||
                file.Length == 0)
            {
                return false;
            }

            if (file.Length > MaxImageSize)
            {
                return false;
            }

            string extension =
                Path.GetExtension(
                    file.FileName)
                    .ToLowerInvariant();

            if (!AllowedProofExtensions.Contains(
                extension))
            {
                return false;
            }

            string contentType =
                file.ContentType?
                    .ToLowerInvariant() ?? "";

            if (contentType != "image/jpeg" &&
                contentType != "image/png" &&
                contentType != "application/pdf")
            {
                return false;
            }

            return true;
        }

        

        private async Task LoadRoles()
        {
            ViewBag.Roles =
                await _context.Roles
                    .Where(r =>
                        r.RoleName != "Admin")
                    .OrderBy(r =>
                        r.RoleId)
                    .ToListAsync();
        }
    }
}

