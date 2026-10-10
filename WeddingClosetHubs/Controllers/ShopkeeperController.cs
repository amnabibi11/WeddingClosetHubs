using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using WeddingClosetHubs.Models;

namespace WeddingClosetHubs.Controllers
{
    public class ShopkeeperController : Controller
    {
        private readonly WeddingClosetHubsContext _context;
        private readonly IWebHostEnvironment _environment;

        public ShopkeeperController(
            WeddingClosetHubsContext context,
            IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }



        private const string ShopkeeperRole = "Shopkeeper";
        private const string AdminRole = "Admin";
        private const string CustomerRole = "Customer";
        private const string DeliveryRole = "Delivery";



        private const decimal FixedDeliveryCharges = 300m;
        private const decimal ServiceFeePercentage = 10m;



        private static readonly string[] AllowedProductImageExtensions =
        {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp"
    };

        private bool IsShopkeeper()
        {
            int? userId =
                HttpContext.Session.GetInt32("UserId");

            string? roleName =
                HttpContext.Session.GetString("RoleName");

            return userId.HasValue &&
                   string.Equals(
                       roleName,
                       ShopkeeperRole,
                       StringComparison.OrdinalIgnoreCase);
        }



        private int? GetShopkeeperId()
        {
            if (!IsShopkeeper())
                return null;

            return HttpContext.Session.GetInt32("UserId");
        }


        private async Task<Shop?> GetMyShop()
        {
            int? shopkeeperId =
                GetShopkeeperId();

            if (!shopkeeperId.HasValue)
                return null;

            return await _context.Shops
                .Include(s => s.Shopkeeper)
                .FirstOrDefaultAsync(
                    s => s.ShopkeeperId ==
                         shopkeeperId.Value);
        }


        private static bool IsCancelledOrder(
            Order order)
        {
            return string.Equals(
                order.OrderStatus,
                "Cancelled",
                StringComparison.OrdinalIgnoreCase);
        }


        private static bool IsActiveOrderStatus(
            string? status)
        {
            return !string.Equals(
                status,
                "Cancelled",
                StringComparison.OrdinalIgnoreCase);
        }


        private static bool IsCompletedOrder(
            string? status)
        {
            return string.Equals(
                       status,
                       "Delivered",
                       StringComparison.OrdinalIgnoreCase)
                   ||
                   string.Equals(
                       status,
                       "Completed",
                       StringComparison.OrdinalIgnoreCase);
        }


        private static bool IsCodPayment(
            string? paymentMethod)
        {
            if (string.IsNullOrWhiteSpace(
                    paymentMethod))
            {
                return false;
            }

            return paymentMethod.Contains(
                       "cash",
                       StringComparison.OrdinalIgnoreCase)
                   ||
                   string.Equals(
                       paymentMethod.Trim(),
                       "COD",
                       StringComparison.OrdinalIgnoreCase);
        }



        private Dictionary<string, List<string>>
            GetCategoryData()
        {
            return new Dictionary<string, List<string>>(
                StringComparer.OrdinalIgnoreCase)
            {



                ["Bridal Wear"] =
                    new List<string>
                    {
                    "Bridal Lehenga",
                    "Bridal Gown",
                    "Bridal Maxi",
                    "Bridal Sharara",
                    "Bridal Gharara",
                    "Bridal Pishwas",
                    "Bridal Saree",
                    "Nikkah Dress",
                    "Mehndi Dress",
                    "Barat Dress",
                    "Walima Dress",
                    "Reception Dress"
                    },



                ["Groom Wear"] =
                    new List<string>
                    {
                    "Sherwani",
                    "Prince Coat",
                    "Groom Suit",
                    "Tuxedo",
                    "Waistcoat",
                    "Kurta Pajama",
                    "Shalwar Kameez",
                    "Groom Blazer",
                    "Groom Shirt & Trouser",
                    "Groom Waistcoat Suit",
                    "Turban"
                    },


                ["Formal Wear"] =
                    new List<string>
                    {
                    "Formal Dress",
                    "Formal Gown",
                    "Formal Maxi",
                    "Formal Suit",
                    "Formal Saree",
                    "Formal Shalwar Kameez",
                    "Formal Kurta",
                    "Formal Blazer",
                    "Formal Shirt & Trouser",
                    "Formal Tuxedo",
                    "Formal Waistcoat"
                    },



                ["Wedding Guest Wear"] =
                    new List<string>
                    {
                    "Party Dress",
                    "Maxi",
                    "Frock",
                    "Anarkali",
                    "Saree",
                    "Suit",
                    "Shalwar Kameez",
                    "Kurta Pajama"
                    },



                ["Footwear"] =
                    new List<string>
                    {
                    "Bridal Shoes",
                    "Heels",
                    "Khussa",
                    "Sandals",
                    "Pumps",
                    "Men Shoes",
                    "Formal Shoes",
                    "Peshawari Chappal",
                    "Wedding Shoes"
                    },


                ["Jewellery"] =
                    new List<string>
                    {
                    "Necklace",
                    "Earrings",
                    "Bangles",
                    "Bracelet",
                    "Maang Tikka",
                    "Jhumka",
                    "Rings",
                    "Jewellery Set"
                    },



                ["Bridal Jewellery"] =
                    new List<string>
                    {
                    "Bridal Necklace Set",
                    "Bridal Earrings",
                    "Bridal Jhumka",
                    "Bridal Maang Tikka",
                    "Bridal Bangles",
                    "Bridal Bracelet",
                    "Bridal Rings",
                    "Bridal Jewellery Set",
                    "Bridal Nose Ring",
                    "Bridal Matha Patti",
                    "Bridal Passa",
                    "Bridal Choker"
                    },




                ["Accessories"] =
                    new List<string>
                    {
                    "Clutch",
                    "Handbag",
                    "Bridal Dupatta",
                    "Veil",
                    "Hair Accessories",
                    "Hair Pins",
                    "Brooch",
                    "Cufflinks",
                    "Tie",
                    "Bow Tie",
                    "Belt"
                    },



                ["Bridal Accessories"] =
                    new List<string>
                    {
                    "Bridal Clutch",
                    "Bridal Handbag",
                    "Bridal Dupatta",
                    "Bridal Veil",
                    "Bridal Hair Accessories",
                    "Bridal Hair Pins",
                    "Bridal Brooch",
                    "Bridal Belt"
                    },


                ["Dress"] =
                    new List<string>
                    {
                    "Bridal Dress",
                    "Lehenga",
                    "Gown",
                    "Sharara",
                    "Gharara",
                    "Pishwas",
                    "Maxi",
                    "Anarkali",
                    "Saree",
                    "Sherwani",
                    "Prince Coat",
                    "Waistcoat",
                    "Kurta Pajama",
                    "Tuxedo",
                    "Suit",
                    "Blazer",
                    "Shirt & Trouser",
                    "Party Dress",
                    "Frock",
                    "Shalwar Kameez"
                    }
            };
        }



        private Dictionary<string, List<string>>
            GetAllowedCategoryDataForShop(
                string? shopCategory,
                string? shopCollections = null)
        {
            var allCategories =
                GetCategoryData();


            var result =
                new Dictionary<string, List<string>>(
                    StringComparer.OrdinalIgnoreCase);



            var collections =
                (shopCollections ?? string.Empty)
                    .Split(
                        ',',
                        StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(
                        StringComparer.OrdinalIgnoreCase)
                    .ToList();



            if (collections.Count > 0)
            {
                foreach (var collection in collections)
                {
                    var matchingCategory =
                        allCategories.Keys.FirstOrDefault(
                            c =>
                                string.Equals(
                                    c,
                                    collection,
                                    StringComparison.OrdinalIgnoreCase));


                    if (matchingCategory != null)
                    {
                        result[matchingCategory] =
                            allCategories[matchingCategory];
                    }
                }


                return result;
            }



            string normalized =
                (shopCategory ?? string.Empty)
                    .Trim()
                    .ToLowerInvariant()
                    .Replace("-", " ");



            if (normalized.Contains("guest"))
            {
                result["Wedding Guest Wear"] =
                    allCategories["Wedding Guest Wear"];

                return result;
            }


            if (normalized.Contains("footwear") ||
                normalized.Contains("shoe"))
            {
                result["Footwear"] =
                    allCategories["Footwear"];

                return result;
            }



            if (normalized.Contains("bridal jewellery") ||
                normalized.Contains("bridal jewelry"))
            {
                result["Bridal Jewellery"] =
                    allCategories["Bridal Jewellery"];

                return result;
            }



            if (normalized.Contains("jewellery") ||
                normalized.Contains("jewelry"))
            {
                result["Jewellery"] =
                    allCategories["Jewellery"];

                return result;
            }



            if (normalized.Contains("accessor"))
            {
                result["Accessories"] =
                    allCategories["Accessories"];

                return result;
            }



            if (normalized.Contains("bridal") ||
                normalized.Contains("bride"))
            {
                result["Bridal Wear"] =
                    allCategories["Bridal Wear"];

                return result;
            }



            if (normalized.Contains("groom") ||
                normalized.Contains("gents") ||
                normalized.Contains("men"))
            {
                result["Groom Wear"] =
                    allCategories["Groom Wear"];

                return result;
            }



            if (normalized.Contains("formal"))
            {
                result["Formal Wear"] =
                    allCategories["Formal Wear"];

                return result;
            }



            if (normalized.Contains("dress"))
            {
                result["Dress"] =
                    allCategories["Dress"];

                return result;
            }




            foreach (var category in allCategories)
            {
                if (string.Equals(
                        category.Key,
                        shopCategory?.Trim(),
                        StringComparison.OrdinalIgnoreCase))
                {
                    result[category.Key] =
                        category.Value;

                    break;
                }
            }


            return result;
        }




        private void PrepareProductForm(
            Shop shop)
        {
            var allowedData =
                GetAllowedCategoryDataForShop(
                    shop.ShopCategory,
                    shop.ShopCollections);



            ViewBag.ShopCategory =
                shop.ShopCategory;



            ViewBag.ShopCollections =
                shop.ShopCollections;



            ViewBag.ProductCategories =
                allowedData.Keys.ToList();



            ViewBag.SubCategories =
                allowedData.Values
                    .SelectMany(x => x)
                    .Distinct(
                        StringComparer.OrdinalIgnoreCase)
                    .ToList();




            ViewBag.StandardSizes =
                new List<string>
                {
                "XS",
                "S",
                "M",
                "L",
                "XL",
                "XXL",

                "36",
                "37",
                "38",
                "39",
                "40",
                "41",
                "42",
                "43",
                "44",
                "45"
                };


            ViewBag.CategoryDataJson =
                JsonSerializer.Serialize(
                    allowedData);
        }


        private void SetProductType(
            Product product,
            string? shopCategory = null)
        {
            string category =
                product.Category?
                    .Trim()
                    .ToLowerInvariant()
                    ?? string.Empty;


            string subcategory =
                product.Subcategory?
                    .Trim()
                    .ToLowerInvariant()
                    ?? string.Empty;


            string combined =
                category + " " + subcategory;




            if (category.Contains("footwear") ||
                category.Contains("shoe") ||
                subcategory.Contains("shoe") ||
                subcategory.Contains("khussa") ||
                subcategory.Contains("heels") ||
                subcategory.Contains("sandals") ||
                subcategory.Contains("pumps") ||
                subcategory.Contains("chappal"))
            {
                product.ProductType =
                    "Footwear";

                return;
            }


            if (category.Contains("bridal jewellery") ||
                category.Contains("bridal jewelry"))
            {
                product.ProductType =
                    "Jewellery";

                return;
            }



            if (category.Contains("jewellery") ||
                category.Contains("jewelry"))
            {
                product.ProductType =
                    "Jewellery";

                return;
            }



            if (category.Contains("accessor"))
            {
                product.ProductType =
                    "Accessories";

                return;
            }



            if (category.Contains("wedding guest") ||
                category.Contains("guest wear"))
            {
                product.ProductType =
                    "Wedding Guest Wear";

                return;
            }



            if (category.Contains("bridal wear") ||
                category.Contains("groom wear") ||
                category.Contains("formal wear") ||
                category.Contains("dress") ||
                category.Contains("bridal") ||
                category.Contains("bride") ||
                category.Contains("groom") ||
                category.Contains("gents") ||
                category.Contains("men") ||
                combined.Contains("lehenga") ||
                combined.Contains("gown") ||
                combined.Contains("maxi") ||
                combined.Contains("sharara") ||
                combined.Contains("gharara") ||
                combined.Contains("pishwas") ||
                combined.Contains("saree") ||
                combined.Contains("frock") ||
                combined.Contains("anarkali") ||
                combined.Contains("suit") ||
                combined.Contains("tuxedo") ||
                combined.Contains("sherwani") ||
                combined.Contains("prince coat") ||
                combined.Contains("waistcoat") ||
                combined.Contains("blazer") ||
                combined.Contains("shirt & trouser") ||
                combined.Contains("kurta pajama") ||
                combined.Contains("shalwar kameez") ||
                combined.Contains("turban"))
            {
                product.ProductType =
                    "Dress / Custom Size";

                return;
            }


            product.ProductType =
                "Standard";
        }




        private void ValidateProductSizing(
            Product product)
        {
            string category =
                product.Category?.Trim()
                ?? string.Empty;


            string subcategory =
                product.Subcategory?.Trim()
                ?? string.Empty;


            string categoryLower =
                category.ToLowerInvariant();


            string subcategoryLower =
                subcategory.ToLowerInvariant();



            bool isFootwear =
                categoryLower.Contains("footwear") ||
                categoryLower.Contains("shoe") ||
                subcategoryLower.Contains("footwear") ||
                subcategoryLower.Contains("shoe") ||
                subcategoryLower.Contains("khussa") ||
                subcategoryLower.Contains("heels") ||
                subcategoryLower.Contains("sandals") ||
                subcategoryLower.Contains("pumps") ||
                subcategoryLower.Contains("chappal");



            bool isJewellery =
                categoryLower.Contains("jewellery") ||
                categoryLower.Contains("jewelry") ||
                subcategoryLower.Contains("jewellery") ||
                subcategoryLower.Contains("jewelry");



            bool isAccessories =
                categoryLower.Contains("accessories") ||
                categoryLower.Contains("accessory") ||
                subcategoryLower.Contains("accessories") ||
                subcategoryLower.Contains("accessory");



            bool isWeddingGuestWear =
                categoryLower.Contains(
                    "wedding guest wear") ||
                categoryLower.Contains(
                    "guest wear") ||
                subcategoryLower.Contains(
                    "wedding guest wear") ||
                subcategoryLower.Contains(
                    "guest wear");



            bool isBridalWear =
                categoryLower.Equals(
                    "bridal wear") ||
                categoryLower.Contains(
                    "bridal wear");




            bool isGroomWear =
                categoryLower.Equals(
                    "groom wear") ||
                categoryLower.Contains(
                    "groom wear");




            bool isFormalWear =
                categoryLower.Equals(
                    "formal wear") ||
                categoryLower.Contains(
                    "formal wear");



            string[] dressKeywords =
            {
            "dress",
            "lehenga",
            "gown",
            "maxi",
            "sharara",
            "gharara",
            "pishwas",
            "saree",
            "frock",
            "anarkali",
            "palazzo",
            "shalwar kameez",
            "kurta pajama",
            "suit",
            "tuxedo",
            "sherwani",
            "prince coat",
            "waistcoat",
            "blazer",
            "shirt & trouser",
            "turban"
        };


            bool isDress =
                isBridalWear ||
                isGroomWear ||
                isFormalWear ||
                dressKeywords.Any(
                    keyword =>
                        categoryLower.Contains(keyword) ||
                        subcategoryLower.Contains(keyword));



            string[] standardSizes =
            {
            "XS",
            "S",
            "M",
            "L",
            "XL",
            "XXL"
        };



            string[] shoeSizes =
            {
            "36",
            "37",
            "38",
            "39",
            "40",
            "41",
            "42",
            "43",
            "44",
            "45"
        };



            if (isFootwear)
            {
                if (string.IsNullOrWhiteSpace(
                        product.Size))
                {
                    ModelState.AddModelError(
                        "Size",
                        "Please select a shoe size.");
                }
                else if (!shoeSizes.Contains(
                             product.Size.Trim()))
                {
                    ModelState.AddModelError(
                        "Size",
                        "Please select a valid shoe size from 36 to 45.");
                }


                product.HasCustomMeasurement =
                    false;

                product.CustomMeasurements =
                    null;

                product.SizeType =
                    null;


                return;
            }


            if (isJewellery ||
                isAccessories)
            {
                product.Size =
                    null;

                product.SizeType =
                    null;

                product.HasCustomMeasurement =
                    false;

                product.CustomMeasurements =
                    null;


                return;
            }


            if (isWeddingGuestWear)
            {
                if (string.IsNullOrWhiteSpace(
                        product.Size) ||
                    !standardSizes.Contains(
                        product.Size.Trim(),
                        StringComparer.OrdinalIgnoreCase))
                {
                    ModelState.AddModelError(
                        "Size",
                        "Please select a valid size from XS to XXL.");
                }


                product.HasCustomMeasurement =
                    false;

                product.CustomMeasurements =
                    null;


                return;
            }



            if (isDress)
            {
                product.Size =
                    null;


                if (!product.HasCustomMeasurement)
                {
                    product.CustomMeasurements =
                        null;
                }


                return;
            }



            if (!string.IsNullOrWhiteSpace(
                    product.Size) &&
                !standardSizes.Contains(
                    product.Size.Trim(),
                    StringComparer.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(
                    "Size",
                    "Please select a valid size from XS to XXL.");
            }


            product.HasCustomMeasurement =
                false;

            product.CustomMeasurements =
                null;
        }

        public async Task<IActionResult> Dashboard()
        {
            if (!IsShopkeeper())
                return RedirectToAction("Login", "Account");


            int shopkeeperId = GetShopkeeperId()!.Value;

            var shop = await _context.Shops
                .Include(s => s.Shopkeeper)
                .FirstOrDefaultAsync(s =>
                    s.ShopkeeperId == shopkeeperId);

            if (shop == null)
            {
                TempData["Error"] =
                    "Your shop profile could not be found.";

                return RedirectToAction("Profile");
            }

            var shopkeeper = shop.Shopkeeper;

            if (shopkeeper == null)
            {
                TempData["Error"] =
                    "Your shopkeeper profile could not be found.";

                return RedirectToAction("Profile");
            }

            var shopProducts = _context.Products
                .Where(p => p.ShopId == shop.ShopId);

            var shopOrders = _context.Orders
                .Where(o => o.ShopId == shop.ShopId);

            var activeOrders = shopOrders
                .Where(o => o.OrderStatus != "Cancelled");

            int totalProducts =
                await shopProducts.CountAsync();

            int activeProducts =
                await shopProducts.CountAsync(p => p.Status);

            int disabledProducts =
                await shopProducts.CountAsync(p => !p.Status);

            int totalOrders =
                await activeOrders.CountAsync();

            int pendingOrders =
                await activeOrders.CountAsync(
                    o => o.OrderStatus == "Pending");

            int confirmedOrders =
                await activeOrders.CountAsync(
                    o => o.OrderStatus == "Confirmed");

            int processingOrders =
                await activeOrders.CountAsync(
                    o => o.OrderStatus == "Processing");

            int readyOrders =
                await activeOrders.CountAsync(
                    o => o.OrderStatus == "Ready");

            int completedOrders =
                await activeOrders.CountAsync(
                    o =>
                        o.OrderStatus == "Delivered" ||
                        o.OrderStatus == "Completed");

            int cancelledOrders =
                await shopOrders.CountAsync(
                    o => o.OrderStatus == "Cancelled");

            decimal totalSales =
                await activeOrders
                    .Where(o =>
                        o.OrderStatus == "Delivered" ||
                        o.OrderStatus == "Completed")
                    .SumAsync(o =>
                        (decimal?)o.ProductTotal) ?? 0m;

            decimal paidPayments =
                await activeOrders
                    .Where(o =>
                        o.ShopkeeperPaymentStatus == "Paid")
                    .SumAsync(o =>
                        (decimal?)o.ProductTotal) ?? 0m;

            decimal pendingPayments =
                await activeOrders
                    .Where(o =>
                        o.ShopkeeperPaymentStatus != "Paid")
                    .SumAsync(o =>
                        (decimal?)o.ProductTotal) ?? 0m;


            int totalRentalOrders =
                await _context.OrderDetails
                    .Where(od =>
                        od.Order.ShopId == shop.ShopId &&
                        od.PurchaseType == "Rent" &&
                        od.Order.OrderStatus != "Cancelled")
                    .Select(od => od.OrderId)
                    .Distinct()
                    .CountAsync();


            int pendingRentalOrders =
                await _context.OrderDetails
                    .Where(od =>
                        od.Order.ShopId == shop.ShopId &&
                        od.PurchaseType == "Rent" &&
                        od.Order.OrderStatus != "Cancelled" &&
                        (
                            od.Order.OrderStatus == "Pending" ||
                            od.Order.OrderStatus == "Confirmed" ||
                            od.Order.OrderStatus == "Processing" ||
                            od.Order.OrderStatus == "Ready"
                        ))
                    .Select(od => od.OrderId)
                    .Distinct()
                    .CountAsync();


            int totalCustomizationRequests =
                await _context.CustomizationRequests
                    .CountAsync(c =>
                        c.ShopId == shop.ShopId);


            int pendingCustomizationRequests =
                await _context.CustomizationRequests
                    .CountAsync(c =>
                        c.ShopId == shop.ShopId &&
                        (
                            c.Status == "Pending" ||
                            c.Status == "PriceSent" ||
                            c.Status == "CustomerChangesRequested"
                        ));


            int totalNegotiations =
             await _context.Negotiations
                 .CountAsync(n =>
                      n.Product != null &&
                      n.Product.ShopId == shop.ShopId);


            int pendingNegotiations =
                await _context.Negotiations
                    .CountAsync(n =>
                        n.Product != null &&
                        n.Product.ShopId == shop.ShopId &&
                        (
                            n.Status == "Pending" ||
                            n.Status == "CounterOffer"


                        ));


            var reviews = await _context.Reviews
                .Where(r =>
                    r.ShopId == shop.ShopId)
                .ToListAsync();

            int reviewCount =
                reviews.Count;

            double averageRating =
                reviews.Any()
                    ? reviews.Average(r => r.Rating)
                    : 0;

            int unreadFeedback =
                await _context.Reviews
                    .CountAsync(r =>
                        r.ShopId == shop.ShopId &&
                        !r.IsRead);

            int unreadNotifications =
                await _context.Notifications
                    .CountAsync(n =>
                        n.UserId == shopkeeperId &&
                        !n.IsRead);

            int unreadMessages =
                await _context.ChatMessages
                    .CountAsync(m =>
                        m.ReceiverId == shopkeeperId &&
                        !m.IsRead);


            ViewBag.ShopkeeperName =
                shopkeeper.Name ?? "Shopkeeper";

            ViewBag.ShopkeeperEmail =
                shopkeeper.Email ?? "Email not available";

            ViewBag.ShopkeeperPhone =
                shopkeeper.Phone ?? "Phone not available";

            ViewBag.ShopkeeperAddress =
                shopkeeper.Address ?? "Address not available";

            ViewBag.ShopkeeperProfileImage =
                shopkeeper.ProfileImage;


            ViewBag.ShopName =
                shop.ShopName ?? "My Shop";

            ViewBag.ShopCategory =
                shop.ShopCategory ?? "Not Selected";

            ViewBag.ShopApproved =
                shop.IsApproved;

            ViewBag.ShopStatus =
                shop.Status;


            ViewBag.ShopPhone =
                shop.ShopPhone;

            ViewBag.ShopEmail =
                shop.Email;

            ViewBag.ShopWhatsApp =
                shop.WhatsAppNumber;

            ViewBag.ShopFacebook =
                shop.FacebookUrl;

            ViewBag.ShopInstagram =
                shop.InstagramUrl;

            ViewBag.ShopTikTok =
                shop.TikTokUrl;

            ViewBag.ShopWebsite =
                shop.WebsiteUrl;


            ViewBag.TotalProducts =
                totalProducts;

            ViewBag.ActiveProducts =
                activeProducts;

            ViewBag.DisabledProducts =
                disabledProducts;


            ViewBag.TotalOrders =
                totalOrders;

            ViewBag.PendingOrders =
                pendingOrders;

            ViewBag.ConfirmedOrders =
                confirmedOrders;

            ViewBag.AcceptedOrders =
                confirmedOrders;

            ViewBag.ProcessingOrders =
                processingOrders;

            ViewBag.ReadyOrders =
                readyOrders;

            ViewBag.CompletedOrders =
                completedOrders;

            ViewBag.CancelledOrders =
                cancelledOrders;


            ViewBag.TotalRentalOrders =
                totalRentalOrders;

            ViewBag.PendingRentalOrders =
                pendingRentalOrders;


            ViewBag.TotalCustomizationRequests =
                totalCustomizationRequests;

            ViewBag.PendingCustomizationRequests =
                pendingCustomizationRequests;


            ViewBag.TotalNegotiations =
                totalNegotiations;

            ViewBag.PendingNegotiations =
                pendingNegotiations;


            ViewBag.TotalSales =
                totalSales;

            ViewBag.PaidPayments =
                paidPayments;

            ViewBag.PendingPayments =
                pendingPayments;


            ViewBag.ReviewCount =
                reviewCount;

            ViewBag.TotalReviews =
                reviewCount;

            ViewBag.UnreadFeedback =
                unreadFeedback;

            ViewBag.AverageRating =
                averageRating;

            ViewBag.UnreadNotifications =
                unreadNotifications;

            ViewBag.UnreadMessages =
                unreadMessages;


            return View();


        }



        public async Task<IActionResult> MyShop()
        {
            if (!IsShopkeeper())
                return RedirectToAction("Login", "Account");

            var shop = await GetMyShop();

            if (shop == null)
                return RedirectToAction("Profile");

            return View(shop);
        }

        [HttpGet]
        public async Task<IActionResult> EditShop()
        {
            if (!IsShopkeeper())
                return RedirectToAction("Login", "Account");

            var shop = await GetMyShop();

            if (shop == null)
                return NotFound();

            return View(shop);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditShop(
            Shop model,
            IFormFile? logoFile,
            IFormFile? shopFrontPhoto,
            IFormFile? shopInsidePhoto,
            IFormFile? shopSignboardPhoto,
            string[]? selectedCategories)
        {
            if (!IsShopkeeper())
                return RedirectToAction("Login", "Account");

            int shopkeeperId = GetShopkeeperId()!.Value;

            var shop = await _context.Shops
                .FirstOrDefaultAsync(
                    s => s.ShopId == model.ShopId &&
                         s.ShopkeeperId == shopkeeperId);

            if (shop == null)
                return NotFound();

            if (string.IsNullOrWhiteSpace(model.ShopName))
            {
                ModelState.AddModelError(
                    "ShopName",
                    "Shop name is required.");
            }

            if (string.IsNullOrWhiteSpace(model.ShopAddress))
            {
                ModelState.AddModelError(
                    "ShopAddress",
                    "Shop address is required.");
            }

            if (selectedCategories == null ||
                selectedCategories.Length == 0)
            {
                ModelState.AddModelError(
                    "ShopCollections",
                    "Please select at least one shop category.");
            }

            if (!model.OffersBuy &&
                !model.OffersRent &&
                !model.OffersCustomization)
            {
                ModelState.AddModelError(
                    "OffersBuy",
                    "Please select at least one service.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            shop.ShopName =
                model.ShopName.Trim();

            shop.ShopCategory =
                model.ShopCategory?.Trim() ?? string.Empty;

            shop.ShopCollections =
                selectedCategories != null &&
                selectedCategories.Length > 0
                    ? string.Join(",", selectedCategories)
                    : null;

            shop.OffersBuy =
                model.OffersBuy;

            shop.OffersRent =
                model.OffersRent;

            shop.OffersCustomization =
                model.OffersCustomization;

            shop.ShopDescription =
                string.IsNullOrWhiteSpace(model.ShopDescription)
                    ? null
                    : model.ShopDescription.Trim();

            shop.ShopAddress =
                model.ShopAddress.Trim();

            shop.ShopPhone =
                string.IsNullOrWhiteSpace(model.ShopPhone)
                    ? null
                    : model.ShopPhone.Trim();

            shop.Email =
                string.IsNullOrWhiteSpace(model.Email)
                    ? null
                    : model.Email.Trim();

            shop.WhatsAppNumber =
                string.IsNullOrWhiteSpace(model.WhatsAppNumber)
                    ? null
                    : model.WhatsAppNumber.Trim();

            shop.FacebookUrl =
                string.IsNullOrWhiteSpace(model.FacebookUrl)
                    ? null
                    : model.FacebookUrl.Trim();

            shop.InstagramUrl =
                string.IsNullOrWhiteSpace(model.InstagramUrl)
                    ? null
                    : model.InstagramUrl.Trim();

            shop.TikTokUrl =
                string.IsNullOrWhiteSpace(model.TikTokUrl)
                    ? null
                    : model.TikTokUrl.Trim();

            shop.WebsiteUrl =
                string.IsNullOrWhiteSpace(model.WebsiteUrl)
                    ? null
                    : model.WebsiteUrl.Trim();

            if (logoFile != null &&
                logoFile.Length > 0)
            {
                shop.Logo =
                    await SaveFile(
                        logoFile,
                        "shops");
            }

            if (shopFrontPhoto != null &&
                shopFrontPhoto.Length > 0)
            {
                shop.ShopFrontPhoto =
                    await SaveFile(
                        shopFrontPhoto,
                        "shops");
            }

            if (shopInsidePhoto != null &&
                shopInsidePhoto.Length > 0)
            {
                shop.ShopInsidePhoto =
                    await SaveFile(
                        shopInsidePhoto,
                        "shops");
            }

            if (shopSignboardPhoto != null &&
                shopSignboardPhoto.Length > 0)
            {
                shop.ShopSignboardPhoto =
                    await SaveFile(
                        shopSignboardPhoto,
                        "shops");
            }

            try
            {
                await _context.SaveChangesAsync();

                TempData["Success"] =
                    "Shop information updated successfully.";

                return RedirectToAction("MyShop");
            }
            catch
            {
                TempData["Error"] =
                    "Unable to update shop information.";

                return View(model);
            }
        }


public async Task<IActionResult> Products()
{
    if (!IsShopkeeper())
        return RedirectToAction("Login", "Account");

    var shop = await GetMyShop();

    if (shop == null)
        return NotFound();

    var products = await _context.Products
        .Include(p => p.Shop)
        .Include(p => p.ProductVariants)
        .Include(p => p.ProductImages)
        .Where(p => p.ShopId == shop.ShopId)
        .OrderByDescending(p => p.CreatedDate)
        .ToListAsync();

    foreach (var product in products)
    {
        if (product.ProductVariants != null &&
            product.ProductVariants.Any())
        {
            product.StockQuantity = product.ProductVariants
                .Sum(v => v.StockQuantity);
        }
    }

    await _context.SaveChangesAsync();

    ViewBag.ShopkeeperName =
        shop.Shopkeeper?.Name ?? "Shopkeeper";

    ViewBag.ShopName = shop.ShopName;
    ViewBag.ProfileImage = shop.Shopkeeper?.ProfileImage;
    ViewBag.ShopCategory = shop.ShopCategory;

    return View(products);
}


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptNegotiation(int id)
        {
            if (!IsShopkeeper())
                return RedirectToAction("Login", "Account");

            int shopkeeperId = GetShopkeeperId()!.Value;

            var negotiation = await _context.Negotiations
                .Include(n => n.Product)
                .Include(n => n.Customer)
                .FirstOrDefaultAsync(
                    n => n.NegotiationId == id &&
                         n.ShopkeeperId == shopkeeperId);

            if (negotiation == null ||
                negotiation.Product == null)
            {
                TempData["Error"] =
                    "Negotiation not found.";

                return RedirectToAction("Negotiations");
            }

            if (!negotiation.Product.AllowNegotiation)
            {
                TempData["Error"] =
                    "Negotiation is disabled for this product.";

                return RedirectToAction("Negotiations");
            }

            if (!string.Equals(
                    negotiation.Status,
                    "Pending",
                    StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] =
                    "This negotiation is no longer pending.";

                return RedirectToAction("Negotiations");
            }

            if (negotiation.RequestedPrice <= 0)
            {
                TempData["Error"] =
                    "Invalid requested price.";

                return RedirectToAction("Negotiations");
            }


            negotiation.AgreedPrice =
                negotiation.RequestedPrice;

            negotiation.Status =
                "Accepted";

            negotiation.RespondedDate =
                DateTime.Now;

            negotiation.ShopkeeperMessage =
                "Your offer has been accepted.";

            await _context.SaveChangesAsync();


            await CreateNotification(
                negotiation.CustomerId,
                "Negotiation Accepted",
                $"Your offer of Rs. {negotiation.AgreedPrice:N0} for {negotiation.Product.ProductName} has been accepted by the shopkeeper.",
                "Negotiation",
                null);

            TempData["Success"] =
                "Customer offer accepted successfully.";

            return RedirectToAction("Negotiations");
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectNegotiation(
            int id,
            string? shopkeeperMessage)
        {
            if (!IsShopkeeper())
                return RedirectToAction("Login", "Account");

            int shopkeeperId = GetShopkeeperId()!.Value;

            var negotiation = await _context.Negotiations
                .Include(n => n.Product)
                .FirstOrDefaultAsync(
                    n => n.NegotiationId == id &&
                         n.ShopkeeperId == shopkeeperId);

            if (negotiation == null)
            {
                TempData["Error"] =
                    "Negotiation not found.";

                return RedirectToAction("Negotiations");
            }

            if (!string.Equals(
                    negotiation.Status,
                    "Pending",
                    StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] =
                    "This negotiation is no longer pending.";

                return RedirectToAction("Negotiations");
            }

            negotiation.Status =
                "Rejected";

            negotiation.RespondedDate =
                DateTime.Now;

            negotiation.ShopkeeperMessage =
                string.IsNullOrWhiteSpace(shopkeeperMessage)
                    ? "Your offer has been rejected."
                    : shopkeeperMessage.Trim();

            await _context.SaveChangesAsync();


            string productName =
                negotiation.Product?.ProductName
                ?? "product";

            await CreateNotification(
                negotiation.CustomerId,
                "Negotiation Rejected",
                $"Your offer for {productName} has been rejected by the shopkeeper.",
                "Negotiation",
                null);

            TempData["Success"] =
                "Negotiation rejected successfully.";

            return RedirectToAction("Negotiations");
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CounterNegotiation(
            int id,
            decimal counterPrice,
            string? shopkeeperMessage)
        {
            if (!IsShopkeeper())
                return RedirectToAction("Login", "Account");

            int shopkeeperId = GetShopkeeperId()!.Value;

            var negotiation = await _context.Negotiations
                .Include(n => n.Product)
                .FirstOrDefaultAsync(
                    n => n.NegotiationId == id &&
                         n.ShopkeeperId == shopkeeperId);

            if (negotiation == null ||
                negotiation.Product == null)
            {
                TempData["Error"] =
                    "Negotiation not found.";

                return RedirectToAction("Negotiations");
            }

            if (!string.Equals(
                    negotiation.Status,
                    "Pending",
                    StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] =
                    "This negotiation is no longer pending.";

                return RedirectToAction("Negotiations");
            }

            if (counterPrice <= 0)
            {
                TempData["Error"] =
                    "Counter offer must be greater than zero.";

                return RedirectToAction("Negotiations");
            }


            if (counterPrice >= negotiation.OriginalPrice)
            {
                TempData["Error"] =
                    "Counter offer must be lower than the original product price.";

                return RedirectToAction("Negotiations");
            }

            negotiation.CounterPrice =
                counterPrice;

            negotiation.Status =
                "CounterOffer";

            negotiation.RespondedDate =
                DateTime.Now;

            negotiation.ShopkeeperMessage =
                string.IsNullOrWhiteSpace(shopkeeperMessage)
                    ? $"Shopkeeper offered Rs. {counterPrice:N0}."
                    : shopkeeperMessage.Trim();

            await _context.SaveChangesAsync();


            await CreateNotification(
                negotiation.CustomerId,
                "New Counter Offer",
                $"The shopkeeper has made a counter offer of Rs. {counterPrice:N0} for {negotiation.Product.ProductName}.",
                "Negotiation",
                null);

            TempData["Success"] =
                "Counter offer sent successfully.";

            return RedirectToAction("Negotiations");
        }



        [HttpGet]
        public async Task<IActionResult> AddProduct()
        {
            if (!IsShopkeeper())
                return RedirectToAction("Login", "Account");

            var shop = await GetMyShop();

            if (shop == null)
                return NotFound();

            if (!shop.IsApproved || !shop.Status)
            {
                TempData["Error"] =
                    "Your shop must be approved and active before adding products.";

                return RedirectToAction("Products");
            }

            PrepareProductForm(shop);

            ViewBag.OffersBuy = shop.OffersBuy;
            ViewBag.OffersRent = shop.OffersRent;
            ViewBag.OffersCustomization = shop.OffersCustomization;

            var shopPurchaseTypes = new List<string>();

            if (shop.OffersBuy)
                shopPurchaseTypes.Add("Buy");

            if (shop.OffersRent)
                shopPurchaseTypes.Add("Rent");

            if (shop.OffersCustomization)
                shopPurchaseTypes.Add("Customize");

            ViewBag.ShopPurchaseTypes = shopPurchaseTypes;

            return View(new Product());
        }





        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddProduct(
            Product product,
            IFormFile[]? productImages,
            string[]? VariantSizes,
            int[]? VariantQuantities)
        {
            if (!IsShopkeeper())
                return RedirectToAction("Login", "Account");

            TempData.Remove("Success");
            TempData.Remove("Error");

            var shop = await GetMyShop();

            if (shop == null)
                return NotFound();

            if (!shop.IsApproved || !shop.Status)
            {
                ModelState.AddModelError(
                    "",
                    "Your shop must be approved and active before adding products.");

                PrepareProductForm(shop);

                ViewBag.OffersBuy = shop.OffersBuy;
                ViewBag.OffersRent = shop.OffersRent;
                ViewBag.OffersCustomization = shop.OffersCustomization;

                return View(product);
            }

            var allowedData =
                GetAllowedCategoryDataForShop(
                    shop.ShopCategory,
                    shop.ShopCollections);

            string selectedCategory =
                product.Category?.Trim() ?? string.Empty;

            var matchingCategory =
                allowedData.Keys.FirstOrDefault(c =>
                    string.Equals(
                        c,
                        selectedCategory,
                        StringComparison.OrdinalIgnoreCase));

            if (matchingCategory == null)
            {
                ModelState.AddModelError(
                    "Category",
                    "Please select a valid category from your shop collections.");
            }
            else
            {
                product.Category =
                    matchingCategory;

                if (allowedData.TryGetValue(
                        matchingCategory,
                        out var allowedSubcategories))
                {
                    string selectedSubcategory =
                        product.Subcategory?.Trim() ?? string.Empty;

                    var matchingSubcategory =
                        allowedSubcategories.FirstOrDefault(s =>
                            string.Equals(
                                s,
                                selectedSubcategory,
                                StringComparison.OrdinalIgnoreCase));

                    if (matchingSubcategory == null)
                    {
                        ModelState.AddModelError(
                            "Subcategory",
                            "Please select a valid subcategory.");
                    }
                    else
                    {
                        product.Subcategory =
                            matchingSubcategory;
                    }
                }
            }

            if (!shop.OffersBuy)
            {
                product.IsAvailableForBuy = false;
                product.IsOnSale = false;
                product.AllowNegotiation = false;

                product.SalePrice = null;
                product.SaleDetails = null;
            }

            if (!shop.OffersRent)
            {
                product.IsAvailableForRent = false;

                product.RentPrice = null;
                product.RentalSecurity = null;
                product.RentalDuration = null;
                product.RentalConditions = null;
            }

            if (!shop.OffersBuy && !shop.OffersRent)
            {
                ModelState.AddModelError(
                    "",
                    "Your shop must offer at least Buy or Rent before adding products.");
            }

            ValidateProductSizing(product);

            if (!product.IsAvailableForBuy &&
                !product.IsAvailableForRent)
            {
                ModelState.AddModelError(
                    "",
                    "Please select at least one available option: Buy or Rent.");
            }

            if (!product.IsAvailableForBuy)
            {
                product.IsOnSale = false;
                product.AllowNegotiation = false;

                product.SalePrice = null;
                product.SaleDetails = null;
            }

            if (product.Price <= 0)
            {
                ModelState.AddModelError(
                    "Price",
                    "Product price must be greater than zero.");
            }

            if (product.StockQuantity < 0)
            {
                ModelState.AddModelError(
                    "StockQuantity",
                    "Stock quantity cannot be negative.");
            }

            bool usesSizeVariants =
                string.Equals(
                    product.Category,
                    "Bridal Wear",
                    StringComparison.OrdinalIgnoreCase) ||

                string.Equals(
                    product.Category,
                    "Groom Wear",
                    StringComparison.OrdinalIgnoreCase) ||

                string.Equals(
                    product.Category,
                    "Formal Wear",
                    StringComparison.OrdinalIgnoreCase) ||

                string.Equals(
                    product.Category,
                    "Wedding Guest Wear",
                    StringComparison.OrdinalIgnoreCase);

            if (usesSizeVariants)
            {
                var sizes =
                    VariantSizes ?? Array.Empty<string>();

                var quantities =
                    VariantQuantities ?? Array.Empty<int>();

                if (sizes.Length == 0)
                {
                    ModelState.AddModelError(
                        "",
                        "Please add at least one size and quantity.");
                }
                else if (sizes.Length != quantities.Length)
                {
                    ModelState.AddModelError(
                        "",
                        "Size and quantity information is incomplete.");
                }
                else
                {
                    var allowedSizes =
                        new HashSet<string>(
                            new[]
                            {
                        "XS",
                        "S",
                        "M",
                        "L",
                        "XL",
                        "XXL"
                            },
                            StringComparer.OrdinalIgnoreCase);

                    var selectedSizes =
                        new HashSet<string>(
                            StringComparer.OrdinalIgnoreCase);

                    int totalVariantStock = 0;

                    for (int i = 0; i < sizes.Length; i++)
                    {
                        string size =
                            sizes[i]?.Trim() ?? string.Empty;

                        int quantity =
                            quantities[i];

                        if (string.IsNullOrWhiteSpace(size))
                        {
                            ModelState.AddModelError(
                                "",
                                "Size cannot be empty.");

                            continue;
                        }

                        if (!allowedSizes.Contains(size))
                        {
                            ModelState.AddModelError(
                                "",
                                $"Invalid size '{size}'.");
                        }

                        if (!selectedSizes.Add(size))
                        {
                            ModelState.AddModelError(
                                "",
                                $"Size '{size}' has been added more than once.");
                        }

                        if (quantity < 0)
                        {
                            ModelState.AddModelError(
                                "",
                                $"Stock quantity for size '{size}' cannot be negative.");
                        }

                        totalVariantStock += quantity;
                    }

                    product.StockQuantity =
                        totalVariantStock;
                }
            }

            product.ProductName =
                product.ProductName?.Trim() ?? string.Empty;

            product.Category =
                product.Category?.Trim();

            product.Subcategory =
                product.Subcategory?.Trim();

            product.Size =
                product.Size?.Trim();

            product.Color =
                product.Color?.Trim();

            product.Description =
                product.Description?.Trim();

            product.Occasion =
                product.Occasion?.Trim();

            product.SizeType =
                product.SizeType?.Trim();

            product.CustomMeasurements =
                product.CustomMeasurements?.Trim();

            product.RentalDuration =
                product.RentalDuration?.Trim();

            product.RentalConditions =
                product.RentalConditions?.Trim();

            product.SaleDetails =
                product.SaleDetails?.Trim();

            string normalizedName =
                product.ProductName.ToLower();

            string normalizedCategory =
                product.Category?.ToLower() ?? "";

            string normalizedSubcategory =
                product.Subcategory?.ToLower() ?? "";

            string normalizedColor =
                product.Color?.ToLower() ?? "";

            bool duplicateProduct =
                await _context.Products.AnyAsync(p =>
                    p.ShopId == shop.ShopId &&
                    p.ProductName.ToLower() ==
                        normalizedName &&
                    (p.Category ?? "").ToLower() ==
                        normalizedCategory &&
                    (p.Subcategory ?? "").ToLower() ==
                        normalizedSubcategory &&
                    (p.Color ?? "").ToLower() ==
                        normalizedColor &&
                    p.Status);

            if (duplicateProduct)
            {
                ModelState.AddModelError(
                    "",
                    "This product with the same name, category, subcategory and color already exists in your shop. " +
                    "For the same dress, add different sizes and their stock quantities in the size variants section.");
            }

            if (product.IsOnSale)
            {
                if (!product.SalePrice.HasValue ||
                    product.SalePrice.Value <= 0)
                {
                    ModelState.AddModelError(
                        "SalePrice",
                        "Please enter a valid sale price.");
                }
                else if (product.SalePrice.Value >= product.Price)
                {
                    ModelState.AddModelError(
                        "SalePrice",
                        "Sale price must be lower than the original product price.");
                }
            }
            else
            {
                product.SalePrice = null;
                product.SaleDetails = null;
            }

            if (product.IsAvailableForRent)
            {
                if (!product.RentPrice.HasValue ||
                    product.RentPrice.Value <= 0)
                {
                    ModelState.AddModelError(
                        "RentPrice",
                        "Please enter a valid rental price.");
                }
                else
                {
                    decimal maximumRentalPrice =
                        product.Price * (2m / 3m);

                    if (product.RentPrice.Value >
                        maximumRentalPrice)
                    {
                        ModelState.AddModelError(
                            "RentPrice",
                            $"Rental price cannot be more than two-thirds of the original product price. " +
                            $"Maximum allowed rental price is Rs. {maximumRentalPrice:N2}.");
                    }
                }

                if (!product.RentalSecurity.HasValue ||
                    product.RentalSecurity.Value < 0)
                {
                    ModelState.AddModelError(
                        "RentalSecurity",
                        "Please enter a valid refundable security amount.");
                }
                else
                {
                    decimal maximumSecurity =
                        product.Price * (1m / 3m);

                    if (product.RentalSecurity.Value >
                        maximumSecurity)
                    {
                        ModelState.AddModelError(
                            "RentalSecurity",
                            $"Refundable security cannot be more than one-third of the original product price. " +
                            $"Maximum allowed security is Rs. {maximumSecurity:N2}.");
                    }
                }

                if (string.IsNullOrWhiteSpace(
                        product.RentalDuration))
                {
                    ModelState.AddModelError(
                        "RentalDuration",
                        "Rental duration is required.");
                }

                if (string.IsNullOrWhiteSpace(
                        product.RentalConditions))
                {
                    ModelState.AddModelError(
                        "RentalConditions",
                        "Rental conditions are required.");
                }
            }
            else
            {
                product.RentPrice = null;
                product.RentalSecurity = null;
                product.RentalDuration = null;
                product.RentalConditions = null;
            }

            if (productImages == null ||
                productImages.Length == 0)
            {
                ModelState.AddModelError(
                    "productImages",
                    "Please select at least one product image.");
            }
            else
            {
                foreach (var image in productImages)
                {
                    if (image == null ||
                        image.Length == 0)
                    {
                        continue;
                    }

                    string extension =
                        Path.GetExtension(
                            image.FileName)
                        .ToLowerInvariant();

                    if (!AllowedProductImageExtensions.Contains(
                            extension))
                    {
                        ModelState.AddModelError(
                            "productImages",
                            "Only JPG, JPEG, PNG and WEBP images are allowed.");

                        break;
                    }

                    if (image.Length > 5 * 1024 * 1024)
                    {
                        ModelState.AddModelError(
                            "productImages",
                            "Each product image must be 5 MB or smaller.");

                        break;
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                PrepareProductForm(shop);

                ViewBag.OffersBuy = shop.OffersBuy;
                ViewBag.OffersRent = shop.OffersRent;
                ViewBag.OffersCustomization = shop.OffersCustomization;

                ViewBag.AddProductError =
                    "Product could not be added. Please correct the errors shown below.";

                return View(product);
            }

            SetProductType(
                product,
                shop.ShopCategory);

            product.ShopId =
                shop.ShopId;

            product.CreatedDate =
                DateTime.Now;

            product.Status =
                true;

            var savedImagePaths =
                new List<string>();

            foreach (var image in productImages!)
            {
                if (image == null ||
                    image.Length == 0)
                {
                    continue;
                }

                string savedPath =
                    await SaveFile(
                        image,
                        "products");

                savedImagePaths.Add(
                    savedPath);
            }

            if (savedImagePaths.Count > 0)
            {
                product.Image =
                    savedImagePaths[0];

                if (savedImagePaths.Count > 1)
                {
                    product.AdditionalImages =
                        string.Join(
                            "|",
                            savedImagePaths.Skip(1));
                }
            }

            _context.Products.Add(
                product);

            if (usesSizeVariants &&
                VariantSizes != null &&
                VariantQuantities != null)
            {
                for (int i = 0;
                     i < VariantSizes.Length;
                     i++)
                {
                    string size =
                        VariantSizes[i]?.Trim() ?? string.Empty;

                    if (string.IsNullOrWhiteSpace(size))
                        continue;

                    int quantity =
                        VariantQuantities[i];

                    product.ProductVariants.Add(
                        new ProductVariant
                        {
                            Size = size,
                            StockQuantity = quantity,
                            CreatedDate = DateTime.Now
                        });
                }
            }

            foreach (var imagePath
                     in savedImagePaths)
            {
                product.ProductImages.Add(
                    new ProductImage
                    {
                        ImagePath =
                            imagePath,

                        IsPrimary =
                            imagePath ==
                            savedImagePaths[0],

                        CreatedDate =
                            DateTime.Now
                    });
            }

            await _context.SaveChangesAsync();

            ModelState.Clear();

            PrepareProductForm(shop);

            ViewBag.OffersBuy = shop.OffersBuy;
            ViewBag.OffersRent = shop.OffersRent;
            ViewBag.OffersCustomization = shop.OffersCustomization;

            ViewBag.AddProductSuccess =
                "Product added successfully.";

            return View(new Product());
        }


        [HttpGet]
        public async Task<IActionResult> EditProduct(int id)
        {
            if (!IsShopkeeper())
                return RedirectToAction("Login", "Account");

            var shop = await GetMyShop();

            if (shop == null)
                return NotFound();

            if (!shop.IsApproved || !shop.Status)
            {
                TempData["Error"] =
                    "Your shop must be approved and active before editing products.";

                return RedirectToAction("Products");
            }

            var product =
                await _context.Products
                    .Include(p => p.ProductImages)
                    .Include(p => p.ProductVariants)
                    .FirstOrDefaultAsync(
                        p =>
                            p.ProductId == id &&
                            p.ShopId == shop.ShopId);

            if (product == null)
                return NotFound();

            PrepareProductForm(shop);

            ViewBag.OffersBuy = shop.OffersBuy;
            ViewBag.OffersRent = shop.OffersRent;
            ViewBag.OffersCustomization = shop.OffersCustomization;

            return View(product);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProduct(
     Product product,
     List<IFormFile>? productImages,
     string[]? VariantSizes,
     int[]? VariantQuantities)
        {
            if (!IsShopkeeper())
                return RedirectToAction("Login", "Account");

            var shop = await GetMyShop();

            if (shop == null)
                return NotFound();

            if (!shop.IsApproved || !shop.Status)
            {
                TempData["Error"] =
                    "Your shop must be approved and active before editing products.";

                return RedirectToAction("Products");
            }

            productImages ??= new List<IFormFile>();
            VariantSizes ??= Array.Empty<string>();
            VariantQuantities ??= Array.Empty<int>();

            var existingProduct =
                await _context.Products
                    .Include(p => p.ProductImages)
                    .Include(p => p.ProductVariants)
                    .FirstOrDefaultAsync(
                        p => p.ProductId == product.ProductId);

            if (existingProduct == null)
                return NotFound();

            if (existingProduct.ShopId != shop.ShopId)
                return Forbid();

            var allowedData =
                GetAllowedCategoryDataForShop(
                    shop.ShopCategory,
                    shop.ShopCollections);

            string selectedCategory =
                product.Category?.Trim() ?? string.Empty;

            var matchingCategory =
                allowedData.Keys.FirstOrDefault(c =>
                    string.Equals(
                        c,
                        selectedCategory,
                        StringComparison.OrdinalIgnoreCase));

            if (matchingCategory == null)
            {
                ModelState.AddModelError(
                    "Category",
                    "Please select a valid category from your shop collections.");
            }
            else
            {
                product.Category =
                    matchingCategory;

                if (allowedData.TryGetValue(
                        matchingCategory,
                        out var allowedSubcategories))
                {
                    string selectedSubcategory =
                        product.Subcategory?.Trim() ?? string.Empty;

                    var matchingSubcategory =
                        allowedSubcategories.FirstOrDefault(s =>
                            string.Equals(
                                s,
                                selectedSubcategory,
                                StringComparison.OrdinalIgnoreCase));

                    if (matchingSubcategory == null)
                    {
                        ModelState.AddModelError(
                            "Subcategory",
                            "Please select a valid subcategory.");
                    }
                    else
                    {
                        product.Subcategory =
                            matchingSubcategory;
                    }
                }
            }

            SetProductType(
                product,
                shop.ShopCategory);

            string category =
                product.Category?.Trim().ToLowerInvariant() ?? "";

            string subcategory =
                product.Subcategory?.Trim().ToLowerInvariant() ?? "";

            string[] dressKeywords =
            {
        "dress",
        "bridal lehenga",
        "bridal gown",
        "bridal maxi",
        "bridal sharara",
        "bridal gharara",
        "bridal pishwas",
        "bridal saree",
        "nikkah dress",
        "mehndi dress",
        "barat dress",
        "walima dress",
        "reception dress",
        "sherwani",
        "prince coat",
        "groom suit",
        "tuxedo",
        "waistcoat",
        "kurta pajama",
        "shalwar kameez",
        "groom blazer",
        "groom shirt & trouser",
        "groom waistcoat suit",
        "turban",
        "formal dress",
        "formal gown",
        "formal maxi",
        "formal suit",
        "formal saree",
        "formal shalwar kameez",
        "formal kurta",
        "formal blazer",
        "formal shirt & trouser",
        "formal tuxedo",
        "formal waistcoat",
        "lehenga",
        "gown",
        "maxi",
        "sharara",
        "gharara",
        "pishwas",
        "saree",
        "frock",
        "anarkali",
        "palazzo",
        "suit",
        "blazer",
        "shirt & trouser"
    };

            string[] dressCategories =
            {
        "bridal wear",
        "groom wear",
        "formal wear",
        "wedding guest wear"
    };

            bool isDressProduct =
                dressCategories.Contains(category) ||
                dressKeywords.Any(x =>
                    subcategory.Contains(x));

            bool usesVariants =
                isDressProduct;

            ValidateProductSizing(product);

            if (usesVariants)
            {
                product.Size = null;

                if (VariantSizes.Length == 0)
                {
                    ModelState.AddModelError(
                        "",
                        "Please add at least one size variant for this dress product.");
                }
                else if (VariantSizes.Length != VariantQuantities.Length)
                {
                    ModelState.AddModelError(
                        "",
                        "Size and quantity information is incomplete.");
                }
                else
                {
                    var cleanedSizes =
                        new List<string>();

                    var cleanedQuantities =
                        new List<int>();

                    string[] allowedSizes =
                    {
                "XS",
                "S",
                "M",
                "L",
                "XL",
                "XXL"
            };

                    for (int i = 0;
                         i < VariantSizes.Length;
                         i++)
                    {
                        string size =
                            VariantSizes[i]?.Trim() ?? "";

                        int quantity =
                            VariantQuantities[i];

                        if (string.IsNullOrWhiteSpace(size))
                        {
                            ModelState.AddModelError(
                                "",
                                $"Variant #{i + 1}: Please select a size.");

                            continue;
                        }

                        if (quantity < 0)
                        {
                            ModelState.AddModelError(
                                "",
                                $"Variant #{i + 1}: Stock quantity cannot be negative.");

                            continue;
                        }

                        if (!allowedSizes.Any(x =>
                                string.Equals(
                                    x,
                                    size,
                                    StringComparison.OrdinalIgnoreCase)))
                        {
                            ModelState.AddModelError(
                                "",
                                $"Variant #{i + 1}: Invalid clothing size.");

                            continue;
                        }

                        string formattedSize =
                            allowedSizes.First(x =>
                                string.Equals(
                                    x,
                                    size,
                                    StringComparison.OrdinalIgnoreCase));

                        if (cleanedSizes.Any(x =>
                                string.Equals(
                                    x,
                                    formattedSize,
                                    StringComparison.OrdinalIgnoreCase)))
                        {
                            ModelState.AddModelError(
                                "",
                                $"Size {formattedSize} has been added more than once. Each size can only be added once.");

                            continue;
                        }

                        cleanedSizes.Add(
                            formattedSize);

                        cleanedQuantities.Add(
                            quantity);
                    }

                    if (cleanedSizes.Count > 0)
                    {
                        product.StockQuantity =
                            cleanedQuantities.Sum();
                    }
                }
            }
            else
            {
                VariantSizes =
                    Array.Empty<string>();

                VariantQuantities =
                    Array.Empty<int>();
            }

            if (!shop.OffersBuy)
            {
                product.IsAvailableForBuy = false;
                product.IsOnSale = false;
                product.AllowNegotiation = false;

                product.SalePrice = null;
                product.SaleDetails = null;
            }

            if (!shop.OffersRent)
            {
                product.IsAvailableForRent = false;

                product.RentPrice = null;
                product.RentalSecurity = null;
                product.RentalDuration = null;
                product.RentalConditions = null;
            }

            if (!shop.OffersBuy && !shop.OffersRent)
            {
                ModelState.AddModelError(
                    "",
                    "Your shop must offer at least Buy or Rent before products can be sold or rented.");
            }

            if (!product.IsAvailableForBuy &&
                !product.IsAvailableForRent)
            {
                ModelState.AddModelError(
                    "",
                    "Please select at least one available option: Buy or Rent.");
            }

            if (!product.IsAvailableForBuy)
            {
                product.IsOnSale = false;
                product.AllowNegotiation = false;

                product.SalePrice = null;
                product.SaleDetails = null;
            }

            if (product.Price <= 0)
            {
                ModelState.AddModelError(
                    "Price",
                    "Original product price must be greater than zero.");
            }

            if (!usesVariants &&
                product.StockQuantity < 0)
            {
                ModelState.AddModelError(
                    "StockQuantity",
                    "Stock quantity cannot be negative.");
            }

            if (product.IsOnSale)
            {
                if (!product.SalePrice.HasValue ||
                    product.SalePrice.Value <= 0)
                {
                    ModelState.AddModelError(
                        "SalePrice",
                        "Please enter a valid sale price.");
                }
                else if (product.SalePrice.Value >= product.Price)
                {
                    ModelState.AddModelError(
                        "SalePrice",
                        "Sale price must be lower than the original price.");
                }
            }
            else
            {
                product.SalePrice = null;
                product.SaleDetails = null;
            }

            if (product.IsAvailableForRent)
            {
                if (!product.RentPrice.HasValue ||
                    product.RentPrice.Value <= 0)
                {
                    ModelState.AddModelError(
                        "RentPrice",
                        "Please enter a valid rental price.");
                }
                else
                {
                    decimal maximumRentalPrice =
                        product.Price * (2m / 3m);

                    if (product.RentPrice.Value >
                        maximumRentalPrice)
                    {
                        ModelState.AddModelError(
                            "RentPrice",
                            $"Rental price cannot be more than two-thirds of the original product price. Maximum allowed rental price is Rs. {maximumRentalPrice:N2}.");
                    }
                }

                if (!product.RentalSecurity.HasValue ||
                    product.RentalSecurity.Value < 0)
                {
                    ModelState.AddModelError(
                        "RentalSecurity",
                        "Please enter a valid refundable security amount.");
                }
                else
                {
                    decimal maximumSecurity =
                        product.Price * (1m / 3m);

                    if (product.RentalSecurity.Value >
                        maximumSecurity)
                    {
                        ModelState.AddModelError(
                            "RentalSecurity",
                            $"Refundable security cannot be more than one-third of the original product price. Maximum allowed security is Rs. {maximumSecurity:N2}.");
                    }
                }

                if (string.IsNullOrWhiteSpace(
                    product.RentalDuration))
                {
                    ModelState.AddModelError(
                        "RentalDuration",
                        "Rental duration is required.");
                }

                if (string.IsNullOrWhiteSpace(
                    product.RentalConditions))
                {
                    ModelState.AddModelError(
                        "RentalConditions",
                        "Rental conditions are required.");
                }
            }
            else
            {
                product.RentPrice = null;
                product.RentalSecurity = null;
                product.RentalDuration = null;
                product.RentalConditions = null;
            }

            foreach (var file in productImages)
            {
                if (file == null ||
                    file.Length == 0)
                    continue;

                string extension =
                    Path.GetExtension(
                        file.FileName)
                        .ToLowerInvariant();

                if (!AllowedProductImageExtensions.Contains(
                    extension))
                {
                    ModelState.AddModelError(
                        "productImages",
                        $"{file.FileName}: Only JPG, JPEG, PNG and WEBP images are allowed.");
                }

                if (file.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError(
                        "productImages",
                        $"{file.FileName}: Image must be 5 MB or smaller.");
                }
            }

            product.ShopId =
                shop.ShopId;

            product.ProductName =
                product.ProductName?.Trim();

            product.Description =
                product.Description?.Trim();

            product.Category =
                product.Category?.Trim();

            product.Subcategory =
                product.Subcategory?.Trim();

            product.Color =
                product.Color?.Trim();

            product.Size =
                product.Size?.Trim();

            product.Occasion =
                product.Occasion?.Trim();

            product.SizeType =
                product.SizeType?.Trim();

            product.CustomMeasurements =
                product.CustomMeasurements?.Trim();

            product.RentalDuration =
                product.RentalDuration?.Trim();

            product.RentalConditions =
                product.RentalConditions?.Trim();

            product.SaleDetails =
                product.SaleDetails?.Trim();

            string normalizedName =
                product.ProductName?.ToLower() ?? "";

            string normalizedCategory =
                product.Category?.ToLower() ?? "";

            string normalizedSubcategory =
                product.Subcategory?.ToLower() ?? "";

            string normalizedColor =
                product.Color?.ToLower() ?? "";

            bool duplicateProduct =
                await _context.Products.AnyAsync(p =>
                    p.ProductId != product.ProductId &&
                    p.ShopId == shop.ShopId &&
                    p.ProductName.ToLower() ==
                        normalizedName &&
                    (p.Category ?? "").ToLower() ==
                        normalizedCategory &&
                    (p.Subcategory ?? "").ToLower() ==
                        normalizedSubcategory &&
                    (p.Color ?? "").ToLower() ==
                        normalizedColor &&
                    p.Status);

            if (duplicateProduct)
            {
                ModelState.AddModelError(
                    "",
                    "This product with the same name, category, subcategory and color already exists in your shop. Edit the existing product instead of creating a duplicate.");
            }

            if (!ModelState.IsValid)
            {
                PrepareProductForm(shop);

                ViewBag.OffersBuy =
                    shop.OffersBuy;

                ViewBag.OffersRent =
                    shop.OffersRent;

                ViewBag.OffersCustomization =
                    shop.OffersCustomization;

                return View(product);
            }

            existingProduct.ProductName =
                product.ProductName;

            existingProduct.Description =
                product.Description;

            existingProduct.Category =
                product.Category;

            existingProduct.Subcategory =
                product.Subcategory;

            existingProduct.Occasion =
                product.Occasion;

            existingProduct.Size =
                product.Size;

            existingProduct.SizeType =
                product.SizeType;

            existingProduct.CustomMeasurements =
                product.CustomMeasurements;

            existingProduct.Color =
                product.Color;

            existingProduct.Price =
                product.Price;

            existingProduct.StockQuantity =
                product.StockQuantity;

            existingProduct.Status =
                product.Status;

            existingProduct.ProductType =
                product.ProductType;

            existingProduct.HasCustomMeasurement =
                product.HasCustomMeasurement;

            existingProduct.IsAvailableForBuy =
                product.IsAvailableForBuy;

            existingProduct.IsOnSale =
                product.IsOnSale;

            existingProduct.SalePrice =
                product.SalePrice;

            existingProduct.SaleDetails =
                product.SaleDetails;

            existingProduct.IsAvailableForRent =
                product.IsAvailableForRent;

            existingProduct.RentPrice =
                product.RentPrice;

            existingProduct.RentalSecurity =
                product.RentalSecurity;

            existingProduct.RentalDuration =
                product.RentalDuration;

            existingProduct.RentalConditions =
                product.RentalConditions;

            existingProduct.AllowNegotiation =
                product.AllowNegotiation;

            if (usesVariants)
            {
                var oldVariants =
                    existingProduct.ProductVariants?
                        .ToList()
                    ?? new List<ProductVariant>();

                if (oldVariants.Count > 0)
                {
                    _context.ProductVariants.RemoveRange(
                        oldVariants);

                    existingProduct.ProductVariants.Clear();
                }

                for (int i = 0;
                     i < VariantSizes.Length;
                     i++)
                {
                    string size =
                        VariantSizes[i].Trim();

                    int quantity =
                        VariantQuantities[i];

                    var variant =
                        new ProductVariant
                        {
                            ProductId =
                                existingProduct.ProductId,

                            Size =
                                size,

                            StockQuantity =
                                quantity,

                            CreatedDate =
                                DateTime.Now
                        };

                    _context.ProductVariants.Add(
                        variant);
                }

                existingProduct.StockQuantity =
                    VariantQuantities.Sum();

                existingProduct.Size =
                    null;
            }
            else
            {
                var oldVariants =
                    existingProduct.ProductVariants?
                        .ToList()
                    ?? new List<ProductVariant>();

                if (oldVariants.Count > 0)
                {
                    _context.ProductVariants.RemoveRange(
                        oldVariants);

                    existingProduct.ProductVariants.Clear();
                }
            }

            if (productImages.Count > 0)
            {
                var savedImagePaths =
                    new List<string>();

                foreach (var file in productImages)
                {
                    if (file == null ||
                        file.Length == 0)
                        continue;

                    string imagePath =
                        await SaveFile(
                            file,
                            "products");

                    savedImagePaths.Add(
                        imagePath);
                }

                if (savedImagePaths.Count > 0)
                {
                    foreach (var oldImage
                             in existingProduct.ProductImages)
                    {
                        oldImage.IsPrimary = false;
                    }

                    existingProduct.Image =
                        savedImagePaths[0];

                    foreach (var imagePath
                             in savedImagePaths)
                    {
                        existingProduct.ProductImages.Add(
                            new ProductImage
                            {
                                ProductId =
                                    existingProduct.ProductId,

                                ImagePath =
                                    imagePath,

                                IsPrimary =
                                    imagePath ==
                                    savedImagePaths[0],

                                CreatedDate =
                                    DateTime.Now
                            });
                    }

                    var existingAdditionalImages =
                        existingProduct.ProductImages
                            .Where(x => !x.IsPrimary)
                            .Select(x => x.ImagePath)
                            .ToList();

                    existingProduct.AdditionalImages =
                        existingAdditionalImages.Count > 0
                            ? string.Join(
                                "|",
                                existingAdditionalImages)
                            : null;
                }
            }

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Product updated successfully.";

            return RedirectToAction("Products");
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DisableProduct(int id)
        {
            if (!IsShopkeeper())
                return RedirectToAction("Login", "Account");

            var shop = await GetMyShop();

            if (shop == null)
                return NotFound();

            var product = await _context.Products
                .FirstOrDefaultAsync(
                    p => p.ProductId == id &&
                         p.ShopId == shop.ShopId);

            if (product == null)
            {
                TempData["Error"] =
                    "Product not found.";

                return RedirectToAction("Products");
            }

            product.Status = false;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Product disabled successfully.";

            return RedirectToAction("Products");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnableProduct(int id)
        {
            if (!IsShopkeeper())
                return RedirectToAction("Login", "Account");

            var shop = await GetMyShop();

            if (shop == null)
                return NotFound();

            var product = await _context.Products
                .FirstOrDefaultAsync(
                    p => p.ProductId == id &&
                         p.ShopId == shop.ShopId);

            if (product == null)
            {
                TempData["Error"] =
                    "Product not found.";

                return RedirectToAction("Products");
            }

            product.Status = true;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Product enabled successfully.";

            return RedirectToAction("Products");
        }

        private void ValidateProductSizing(
            Product product,
            string? shopCategory)
        {
            string category =
                product.Category?.Trim() ?? string.Empty;

            string subcategory =
                product.Subcategory?.Trim() ?? string.Empty;

            string categoryLower =
                category.ToLowerInvariant();

            string subcategoryLower =
                subcategory.ToLowerInvariant();

            bool isFootwear =
                categoryLower.Contains("footwear") ||
                categoryLower.Contains("shoe") ||
                subcategoryLower.Contains("footwear") ||
                subcategoryLower.Contains("shoe");

            bool isJewellery =
                categoryLower.Contains("jewellery") ||
                categoryLower.Contains("jewelry") ||
                subcategoryLower.Contains("jewellery") ||
                subcategoryLower.Contains("jewelry");

            bool isAccessories =
                categoryLower.Contains("accessories") ||
                categoryLower.Contains("accessory") ||
                subcategoryLower.Contains("accessories") ||
                subcategoryLower.Contains("accessory");

            bool isWeddingGuestWear =
                categoryLower.Contains("wedding guest wear") ||
                categoryLower.Contains("guest wear") ||
                subcategoryLower.Contains("wedding guest wear") ||
                subcategoryLower.Contains("guest wear");

            string[] dressKeywords =
            {
        "dress",
        "lehenga",
        "gown",
        "maxi",
        "sharara",
        "gharara",
        "pishwas",
        "saree",
        "frock",
        "anarkali",
        "palazzo",
        "shalwar kameez",
        "kurta pajama",
        "suit",
        "tuxedo",
        "sherwani",
        "prince coat",
        "waistcoat",
        "blazer",
        "shirt & trouser"
    };

            bool isDress =
                dressKeywords.Any(keyword =>
                    categoryLower.Contains(keyword) ||
                    subcategoryLower.Contains(keyword));

            string[] standardSizes =
            {
        "XS",
        "S",
        "M",
        "L",
        "XL",
        "XXL"
    };

            string[] shoeSizes =
            {
        "36",
        "37",
        "38",
        "39",
        "40",
        "41",
        "42",
        "43",
        "44",
        "45"
    };

            if (isFootwear)
            {
                if (string.IsNullOrWhiteSpace(product.Size))
                {
                    ModelState.AddModelError(
                        "Size",
                        "Please select a shoe size.");
                }
                else if (!shoeSizes.Contains(
                             product.Size.Trim()))
                {
                    ModelState.AddModelError(
                        "Size",
                        "Please select a valid shoe size from 36 to 45.");
                }

                product.HasCustomMeasurement = false;
                product.CustomMeasurements = null;
                product.SizeType = null;

                return;
            }

            if (isJewellery || isAccessories)
            {
                product.Size = null;
                product.SizeType = null;
                product.HasCustomMeasurement = false;
                product.CustomMeasurements = null;

                return;
            }

            if (isWeddingGuestWear)
            {
                if (string.IsNullOrWhiteSpace(product.Size) ||
                    !standardSizes.Contains(
                        product.Size.Trim(),
                        StringComparer.OrdinalIgnoreCase))
                {
                    ModelState.AddModelError(
                        "Size",
                        "Please select a valid size from XS to XXL.");
                }

                product.HasCustomMeasurement = false;
                product.CustomMeasurements = null;

                return;
            }

            if (isDress)
            {
                if (string.IsNullOrWhiteSpace(product.Size) ||
                    !standardSizes.Contains(
                        product.Size.Trim(),
                        StringComparer.OrdinalIgnoreCase))
                {
                    ModelState.AddModelError(
                        "Size",
                        "Please select a valid size from XS to XXL.");
                }

                if (!product.HasCustomMeasurement)
                {
                    product.CustomMeasurements = null;
                }

                return;
            }

            if (!string.IsNullOrWhiteSpace(product.Size) &&
                !standardSizes.Contains(
                    product.Size.Trim(),
                    StringComparer.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(
                    "Size",
                    "Please select a valid size from XS to XXL.");
            }

            product.HasCustomMeasurement = false;
            product.CustomMeasurements = null;
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            if (!IsShopkeeper())
                return RedirectToAction("Login", "Account");

            var shop = await GetMyShop();

            if (shop == null)
                return NotFound();

            var product = await _context.Products
                .FirstOrDefaultAsync(
                    p => p.ProductId == id &&
                         p.ShopId == shop.ShopId);

            if (product == null)
            {
                TempData["Error"] =
                    "Product not found.";

                return RedirectToAction("Products");
            }

            bool usedInOrders =
                await _context.OrderDetails
                    .AnyAsync(
                        od => od.ProductId == id);

            if (usedInOrders)
            {
                TempData["Error"] =
                    "This product cannot be deleted because it is already linked to an order.";

                return RedirectToAction("Products");
            }

            _context.Products.Remove(product);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Product deleted successfully.";

            return RedirectToAction("Products");
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleProductStatus(int id)
        {
            if (!IsShopkeeper())
                return RedirectToAction("Login", "Account");

            var shop = await GetMyShop();

            if (shop == null)
                return NotFound();

            var product = await _context.Products
                .FirstOrDefaultAsync(
                    p => p.ProductId == id &&
                         p.ShopId == shop.ShopId);

            if (product == null)
            {
                TempData["Error"] =
                    "Product not found.";

                return RedirectToAction("Products");
            }

            product.Status =
                !product.Status;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                product.Status
                    ? "Product enabled successfully."
                    : "Product disabled successfully.";

            return RedirectToAction("Products");
        }



        [HttpGet]
        public async Task<IActionResult> Orders()
        {
            if (!IsShopkeeper())
                return RedirectToAction("Login", "Account");

            var shop = await GetMyShop();

            if (shop == null)
                return NotFound();

            var orders = await _context.Orders
                .AsNoTracking()
                .AsSplitQuery()
                .Include(o => o.Customer)
                .Include(o => o.Delivery)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .Where(o => o.ShopId == shop.ShopId)
                .OrderByDescending(o => o.CreatedDate)
                .ToListAsync();

            ViewBag.ShopkeeperName =
                shop.Shopkeeper?.Name ?? "Shopkeeper";

            ViewBag.ShopName =
                shop.ShopName;

            ViewBag.ProfileImage =
                shop.Shopkeeper?.ProfileImage;

            ViewBag.CurrentUserId =
                GetShopkeeperId();

            return View(orders);
        }



        public async Task<IActionResult> OrderDetails(int id)
        {
            if (!IsShopkeeper())
                return RedirectToAction("Login", "Account");

            var shop = await GetMyShop();

            if (shop == null)
                return NotFound();

            var order = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.Delivery)
                .Include(o => o.Shop)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(
                    o => o.OrderId == id &&
                         o.ShopId == shop.ShopId &&
                         o.OrderStatus != "Cancelled");

            if (order == null)
            {
                TempData["Error"] =
                    "Order not found or the order has been cancelled.";

                return RedirectToAction("Orders");
            }

            ViewBag.ShopkeeperName =
                shop.Shopkeeper?.Name ?? "Shopkeeper";

            ViewBag.ShopName =
                shop.ShopName;

            ViewBag.ProfileImage =
                shop.Shopkeeper?.ProfileImage;

            return View(order);
        }

        [HttpGet]
        public async Task<IActionResult> CustomizationRequests()
        {
            if (!IsShopkeeper())
                return RedirectToAction("Login", "Account");

            var shop = await GetMyShop();

            if (shop == null)
                return NotFound();

            var requests = await _context.CustomizationRequests
                .AsNoTracking()

                .Include(r => r.Customer)
                .Where(r =>
                    r.ShopId == shop.ShopId &&
                    r.Status != "Cancelled")
                .OrderByDescending(r => r.CreatedDate)
                .ToListAsync();

            ViewBag.ShopkeeperName =
                shop.Shopkeeper?.Name ?? "Shopkeeper";

            ViewBag.ShopName =
                shop.ShopName;

            ViewBag.ProfileImage =
                shop.Shopkeeper?.ProfileImage;

            ViewBag.CurrentUserId =
                GetShopkeeperId();

            return View(requests);
        }

        [HttpGet]
        public async Task<IActionResult> CustomizationRequestDetails(int id)
        {
            if (!IsShopkeeper())
                return RedirectToAction("Login", "Account");

            var shop = await GetMyShop();

            if (shop == null)
                return NotFound();

            var request = await _context.CustomizationRequests
                .Include(r => r.Customer)
                .FirstOrDefaultAsync(r =>
                    r.CustomizationRequestId == id &&
                    r.ShopId == shop.ShopId);

            if (request == null)
            {
                TempData["Error"] =
                    "Customization request not found.";

                return RedirectToAction(
                    "CustomizationRequests");
            }

            ViewBag.ShopkeeperName =
                shop.Shopkeeper?.Name ?? "Shopkeeper";

            ViewBag.ShopName =
                shop.ShopName;

            ViewBag.ProfileImage =
                shop.Shopkeeper?.ProfileImage;

            return View(request);
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendCustomizationPrice(
     int id,
     decimal price,
     string? notes)
        {
            if (!IsShopkeeper())
            {
                return RedirectToAction("Login", "Account");
            }

            var shop = await GetMyShop();

            if (shop == null)
            {
                return NotFound();
            }

            var request = await _context.CustomizationRequests
                .FirstOrDefaultAsync(r =>
                    r.CustomizationRequestId == id &&
                    r.ShopId == shop.ShopId);

            if (request == null)
            {
                TempData["Error"] =
                    "Customization request not found.";

                return RedirectToAction(
                    "CustomizationRequests");
            }

            if (string.Equals(
                request.Status,
                "Accepted",
                StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] =
                    "This customization request has already been accepted.";

                return RedirectToAction(
                    "CustomizationRequestDetails",
                    new { id });
            }

            if (string.Equals(
                request.Status,
                "Cancelled",
                StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] =
                    "This customization request has been cancelled.";

                return RedirectToAction(
                    "CustomizationRequests");
            }

            if (price <= 0)
            {
                TempData["Error"] =
                    "Please enter a valid customized price.";

                return RedirectToAction(
                    "CustomizationRequestDetails",
                    new { id });
            }

            bool isRevision =
                request.Status == "ChangeRequested" ||
                request.Status == "PriceRejected";

            notes =
                string.IsNullOrWhiteSpace(notes)
                    ? null
                    : notes.Trim();

            request.ShopkeeperPrice =
                price;

            request.ShopkeeperNotes =
                notes;

            request.Status =
                isRevision
                    ? "RevisedPriceSent"
                    : "PriceSent";

            request.UpdatedDate =
                DateTime.Now;

            await _context.SaveChangesAsync();

            if (request.CustomerId > 0)
            {
                string notificationTitle =
                    isRevision
                        ? "Revised Customization Price Received"
                        : "Customized Price Received";

                string notificationMessage =
                    isRevision
                        ? $"The shopkeeper has sent a revised customized price of Rs. {price:N2} for {request.ProductName}."
                        : $"The shopkeeper has sent a customized price of Rs. {price:N2} for {request.ProductName}.";

                await CreateNotification(
                    request.CustomerId,
                    notificationTitle,
                    notificationMessage,
                    "Customization",
                    null);
            }

            TempData["Success"] =
                isRevision
                    ? "Revised customized price has been sent to the customer."
                    : "Customized price has been sent to the customer.";

            return RedirectToAction(
                "CustomizationRequests");
        }


        [HttpGet]
        public async Task<IActionResult> RentalOrders()
        {
            if (!IsShopkeeper())
                return RedirectToAction("Login", "Account");

            var shop = await GetMyShop();

            if (shop == null)
                return NotFound();

            var rentalOrders = await _context.OrderDetails
                .Include(d => d.Order)
                    .ThenInclude(o => o.Customer)
                .Include(d => d.Order)
                    .ThenInclude(o => o.Delivery)
                .Include(d => d.Product)
                .Where(d =>
                    d.Order != null &&
                    d.Order.ShopId == shop.ShopId &&
                    d.Order.OrderStatus != "Cancelled" &&
                    d.PurchaseType != null &&
                    d.PurchaseType.Contains("Rent"))
                .OrderByDescending(d => d.Order!.CreatedDate)
                .ToListAsync();

            ViewBag.ShopkeeperName =
                shop.Shopkeeper?.Name ?? "Shopkeeper";

            ViewBag.ShopName =
                shop.ShopName;

            ViewBag.ProfileImage =
                shop.Shopkeeper?.ProfileImage;

            return View(rentalOrders);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmRentalDressReceived(int id)
        {
            if (!IsShopkeeper())
                return RedirectToAction("Login", "Account");

            var shop = await GetMyShop();

            if (shop == null)
                return NotFound();

            var detail = await _context.OrderDetails
                .Include(d => d.Order)
                    .ThenInclude(o => o.Customer)
                .Include(d => d.Product)
                .FirstOrDefaultAsync(d =>
                    d.OrderDetailId == id &&
                    d.Order != null &&
                    d.Order.ShopId == shop.ShopId);

            if (detail == null)
            {
                TempData["Error"] = "Rental item not found.";
                return RedirectToAction("RentalOrders");
            }

            if (string.IsNullOrWhiteSpace(detail.PurchaseType) ||
                !detail.PurchaseType.Contains("Rent"))
            {
                TempData["Error"] = "This order item is not a rental.";
                return RedirectToAction("RentalOrders");
            }

            if (string.Equals(
                detail.Order!.OrderStatus,
                "Cancelled",
                StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "Cancelled orders cannot be processed.";
                return RedirectToAction("RentalOrders");
            }

            var rentalReturnStatus =
                detail.RentalReturnStatus?.Trim() ?? "";

            if (!string.Equals(
                    rentalReturnStatus,
                    "Return Picked Up",
                    StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(
                    rentalReturnStatus,
                    "Returned to Shop",
                    StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] =
                    "The returned dress has not yet been delivered to the shop.";

                return RedirectToAction("RentalOrders");
            }

            detail.RentalReturnStatus = "Inspected";
            detail.DressReceivedDate = DateTime.Now;
            detail.DressReturnedDate = DateTime.Now;
            detail.InspectionResult = "Passed";
            detail.SecurityRefundNotes =
                "Dress received and inspected by shopkeeper.";

            var adminUsers = await _context.Users
                .Include(u => u.Role)
                .Where(u =>
                    u.Role != null &&
                    u.Role.RoleName == AdminRole &&
                    u.Status)
                .Select(u => u.UserId)
                .ToListAsync();

            foreach (var adminUserId in adminUsers)
            {
                _context.Notifications.Add(new Notification
                {
                    UserId = adminUserId,
                    Title = "Rental Dress Ready for Security Refund",
                    Message =
                        $"Rental dress from Order #{detail.OrderId} was received and inspected by {shop.ShopName}. The security is ready for refund processing.",
                    Type = "Rental Return",
                    OrderId = detail.OrderId,
                    IsRead = false,
                    CreatedDate = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Rental dress has been received and inspected. Admin has been notified for security refund.";

            return RedirectToAction("RentalOrders");
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectRentalReturn(
     int id,
     string? notes)
        {
            if (!IsShopkeeper())
                return RedirectToAction("Login", "Account");

            var shop = await GetMyShop();

            if (shop == null)
                return NotFound();

            var detail = await _context.OrderDetails
                .Include(d => d.Order)
                    .ThenInclude(o => o.Customer)
                .Include(d => d.Product)
                .FirstOrDefaultAsync(d =>
                    d.OrderDetailId == id &&
                    d.Order != null &&
                    d.Order.ShopId == shop.ShopId);

            if (detail == null)
            {
                TempData["Error"] = "Rental item not found.";
                return RedirectToAction("RentalOrders");
            }

            if (string.IsNullOrWhiteSpace(detail.PurchaseType) ||
                !detail.PurchaseType.Contains("Rent"))
            {
                TempData["Error"] = "This order item is not a rental.";
                return RedirectToAction("RentalOrders");
            }

            if (string.Equals(
                detail.Order!.OrderStatus,
                "Cancelled",
                StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] =
                    "Cancelled orders cannot be processed.";

                return RedirectToAction("RentalOrders");
            }

            var rentalReturnStatus =
                detail.RentalReturnStatus?.Trim() ?? "";

            if (!string.Equals(
                    rentalReturnStatus,
                    "Return Picked Up",
                    StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(
                    rentalReturnStatus,
                    "Returned to Shop",
                    StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] =
                    "The rental dress must be delivered to the shop before processing the return.";

                return RedirectToAction("RentalOrders");
            }

            if (string.IsNullOrWhiteSpace(notes))
            {
                TempData["Error"] =
                    "Please enter the damage or return issue details.";

                return RedirectToAction("RentalOrders");
            }

            detail.RentalReturnStatus = "Return Issue";
            detail.InspectionResult = "Failed";
            detail.ReturnNotes = notes.Trim();
            detail.DressReceivedDate = DateTime.Now;
            detail.SecurityRefundNotes =
                $"Rental return issue reported by shopkeeper: {notes.Trim()}";

            var customerId = detail.Order!.CustomerId;

            _context.Notifications.Add(new Notification
            {
                UserId = customerId,
                Title = "Rental Return Issue",
                Message =
                    $"There is an issue with the returned rental dress from Order #{detail.OrderId}. {notes.Trim()}",
                Type = "Rental Return",
                OrderId = detail.OrderId,
                IsRead = false,
                CreatedDate = DateTime.Now
            });

            var adminUsers = await _context.Users
                .Include(u => u.Role)
                .Where(u =>
                    u.Role != null &&
                    u.Role.RoleName == AdminRole &&
                    u.Status)
                .Select(u => u.UserId)
                .ToListAsync();

            foreach (var adminUserId in adminUsers)
            {
                _context.Notifications.Add(new Notification
                {
                    UserId = adminUserId,
                    Title = "Rental Return Damage Reported",
                    Message =
                        $"Shopkeeper at {shop.ShopName} reported an issue with the returned rental dress from Order #{detail.OrderId}. Issue: {notes.Trim()}",
                    Type = "Rental Return",
                    OrderId = detail.OrderId,
                    IsRead = false,
                    CreatedDate = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "The rental return issue has been recorded. The customer and Admin have been notified.";

            return RedirectToAction("RentalOrders");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateOrderStatus(
            int id,
            string status)
        {
            if (!IsShopkeeper())
                return RedirectToAction("Login", "Account");

            var shop = await GetMyShop();

            if (shop == null)
                return NotFound();

            var order = await _context.Orders
                .FirstOrDefaultAsync(
                    o => o.OrderId == id &&
                         o.ShopId == shop.ShopId);

            if (order == null)
            {
                TempData["Error"] =
                    "Order not found.";

                return RedirectToAction("Orders");
            }

            if (IsCancelledOrder(order))
            {
                TempData["Error"] =
                    "This order has already been cancelled and cannot be updated.";

                return RedirectToAction("Orders");
            }

            string newStatus =
                status?.Trim() ?? string.Empty;

            string oldStatus =
                order.OrderStatus?.Trim() ?? "Pending";

            string[] allowedStatuses =
            {
                "Pending",
                "Confirmed",
                "Processing",
                "Ready",
                "Cancelled"
            };

            if (!allowedStatuses.Any(s =>
                    string.Equals(
                        s,
                        newStatus,
                        StringComparison.OrdinalIgnoreCase)))
            {
                TempData["Error"] =
                    "Invalid order status.";

                return RedirectToAction(
                    "OrderDetails",
                    new { id });
            }

            bool validTransition = false;

            if (oldStatus.Equals(
                    "Pending",
                    StringComparison.OrdinalIgnoreCase))
            {
                validTransition =
                    newStatus.Equals(
                        "Confirmed",
                        StringComparison.OrdinalIgnoreCase)
                    ||
                    newStatus.Equals(
                        "Cancelled",
                        StringComparison.OrdinalIgnoreCase);
            }
            else if (oldStatus.Equals(
                         "Confirmed",
                         StringComparison.OrdinalIgnoreCase))
            {
                validTransition =
                    newStatus.Equals(
                        "Processing",
                        StringComparison.OrdinalIgnoreCase)
                    ||
                    newStatus.Equals(
                        "Cancelled",
                        StringComparison.OrdinalIgnoreCase);
            }
            else if (oldStatus.Equals(
                         "Processing",
                         StringComparison.OrdinalIgnoreCase))
            {
                validTransition =
                    newStatus.Equals(
                        "Ready",
                        StringComparison.OrdinalIgnoreCase)
                    ||
                    newStatus.Equals(
                        "Cancelled",
                        StringComparison.OrdinalIgnoreCase);
            }
            else if (oldStatus.Equals(
                         "Ready",
                         StringComparison.OrdinalIgnoreCase))
            {
                validTransition = false;
            }
            else if (oldStatus.Equals(
                         "Delivered",
                         StringComparison.OrdinalIgnoreCase)
                     ||
                     oldStatus.Equals(
                         "Completed",
                         StringComparison.OrdinalIgnoreCase))
            {
                validTransition = false;
            }

            if (!validTransition)
            {
                TempData["Error"] =
                    $"You cannot change the order from {oldStatus} to {newStatus}.";

                return RedirectToAction(
                    "OrderDetails",
                    new { id });
            }

            order.OrderStatus =
                newStatus;

            await _context.SaveChangesAsync();

            if (order.CustomerId > 0)
            {
                string notificationMessage;

                if (newStatus.Equals(
                        "Cancelled",
                        StringComparison.OrdinalIgnoreCase))
                {
                    notificationMessage =
                        $"Your order #{order.OrderId} has been cancelled by the shopkeeper.";
                }
                else
                {
                    notificationMessage =
                        $"Your order #{order.OrderId} status has been updated to {newStatus}.";
                }

                await CreateNotification(
                    order.CustomerId,
                    $"Order #{order.OrderId} Updated",
                    notificationMessage,
                    "Order",
                    order.OrderId);
            }


            if (newStatus.Equals(
                    "Ready",
                    StringComparison.OrdinalIgnoreCase))
            {
                var admins = await _context.Users
                    .Include(u => u.Role)
                    .Where(u =>
                        u.Role != null &&
                        u.Role.RoleName == AdminRole &&
                        u.Status &&
                        u.IsApproved)
                    .ToListAsync();

                foreach (var admin in admins)
                {
                    await CreateNotification(
                        admin.UserId,
                        "Order Ready for Delivery",
                        $"Order #{order.OrderId} from {shop.ShopName} is ready for delivery assignment.",
                        "Order",
                        order.OrderId);
                }
            }

            if (newStatus.Equals(
                    "Cancelled",
                    StringComparison.OrdinalIgnoreCase))
            {
                TempData["Success"] =
                    $"Order #{order.OrderId} cancelled successfully.";

                return RedirectToAction("Orders");
            }

            TempData["Success"] =
                $"Order #{order.OrderId} status updated to {newStatus}.";

            return RedirectToAction(
                "OrderDetails",
                new { id });
        }


        public async Task<IActionResult> Payments()
        {
            if (!IsShopkeeper())
                return RedirectToAction("Login", "Account");

            var shop = await GetMyShop();

            if (shop == null)
                return NotFound();

            var orders = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.Shop)
                .Include(o => o.Delivery)
                .Where(o =>
                    o.ShopId == shop.ShopId &&
                    o.OrderStatus != "Cancelled")
                .OrderByDescending(o => o.CreatedDate)
                .ToListAsync();

            ViewBag.ShopkeeperName =
                shop.Shopkeeper?.Name ?? "Shopkeeper";

            ViewBag.ShopName =
                shop.ShopName;

            ViewBag.ProfileImage =
                shop.Shopkeeper?.ProfileImage;


            decimal totalProductAmount =
                orders.Sum(o => o.ProductTotal);

            decimal totalServiceFee =
                orders.Sum(o => o.ServiceFee);

            decimal totalDeliveryCharges =
                orders.Sum(o => o.DeliveryCharges);

            decimal totalCustomerAmount =
                orders.Sum(o => o.TotalAmount);

            decimal paidShopkeeperAmount =
                orders
                    .Where(o =>
                        string.Equals(
                            o.ShopkeeperPaymentStatus,
                            "Paid",
                            StringComparison.OrdinalIgnoreCase))
                    .Sum(o => o.ProductTotal);

            decimal pendingShopkeeperAmount =
                orders
                    .Where(o =>
                        !string.Equals(
                            o.ShopkeeperPaymentStatus,
                            "Paid",
                            StringComparison.OrdinalIgnoreCase))
                    .Sum(o => o.ProductTotal);

            ViewBag.TotalProductAmount =
                totalProductAmount;

            ViewBag.TotalServiceFee =
                totalServiceFee;

            ViewBag.TotalDeliveryCharges =
                totalDeliveryCharges;

            ViewBag.TotalCustomerAmount =
                totalCustomerAmount;

            ViewBag.PaidShopkeeperAmount =
                paidShopkeeperAmount;

            ViewBag.PendingShopkeeperAmount =
                pendingShopkeeperAmount;

            return View(orders);
        }


        public async Task<IActionResult> PaymentDetails(int id)
        {
            if (!IsShopkeeper())
                return RedirectToAction("Login", "Account");

            var shop = await GetMyShop();

            if (shop == null)
                return NotFound();

            var order = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.Shop)
                .Include(o => o.Delivery)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(
                    o => o.OrderId == id &&
                         o.ShopId == shop.ShopId &&
                         o.OrderStatus != "Cancelled");

            if (order == null)
            {
                TempData["Error"] =
                    "Payment record not found or the order has been cancelled.";

                return RedirectToAction("Payments");
            }

            decimal productAmount =
                order.ProductTotal;

            decimal serviceFee =
                order.ServiceFee;

            decimal deliveryCharges =
                order.DeliveryCharges;

            decimal totalAmount =
                order.TotalAmount;

            decimal shopkeeperAmount =
                order.ProductTotal;

            decimal adminAmount =
                order.ServiceFee;

            decimal deliveryBoyAmount =
                order.DeliveryCharges;

            ViewBag.ProductAmount =
                productAmount;

            ViewBag.ServiceFee =
                serviceFee;

            ViewBag.DeliveryCharges =
                deliveryCharges;

            ViewBag.TotalAmount =
                totalAmount;

            ViewBag.ShopkeeperAmount =
                shopkeeperAmount;

            ViewBag.AdminAmount =
                adminAmount;

            ViewBag.DeliveryBoyAmount =
                deliveryBoyAmount;

            ViewBag.ShopkeeperPaymentStatus =
                order.ShopkeeperPaymentStatus;

            ViewBag.ShopkeeperPaidAmount =
                order.ShopkeeperPaidAmount;

            ViewBag.ShopkeeperPaymentMethod =
                order.ShopkeeperPaymentMethod;

            ViewBag.ShopkeeperPaymentDate =
                order.ShopkeeperPaymentDate;

            ViewBag.DeliveryPaymentStatus =
                order.DeliveryPaymentStatus;

            ViewBag.DeliveryPaidAmount =
                order.DeliveryPaidAmount;

            ViewBag.DeliveryPaymentMethod =
                order.DeliveryPaymentMethod;

            ViewBag.DeliveryPaymentDate =
                order.DeliveryPaymentDate;

            ViewBag.PaymentMethod =
                order.PaymentMethod;

            ViewBag.PaymentStatus =
                order.PaymentStatus;

            ViewBag.PaymentReceived =
                order.PaymentReceived;

            ViewBag.PaymentDate =
                order.PaymentDate;



            ViewBag.IsCOD =
                IsCodPayment(order.PaymentMethod);

            ViewBag.ShopkeeperName =
                shop.Shopkeeper?.Name ?? "Shopkeeper";

            ViewBag.ShopName =
                shop.ShopName;

            ViewBag.ProfileImage =
                shop.Shopkeeper?.ProfileImage;

            return View(order);
        }


        public async Task<IActionResult> Feedback()
        {
            if (!IsShopkeeper())
                return RedirectToAction("Login", "Account");

            var shop = await GetMyShop();

            if (shop == null)
                return NotFound();

            var unreadFeedback = await _context.Reviews
                .Where(r =>
                    r.ShopId == shop.ShopId &&
                    !r.IsRead)
                .ToListAsync();

            if (unreadFeedback.Any())
            {
                foreach (var review in unreadFeedback)
                {
                    review.IsRead = true;
                }

                await _context.SaveChangesAsync();
            }

            var feedback = await _context.Reviews
                .Include(r => r.User)
                .Include(r => r.Product)
                .Include(r => r.Shop)
                .Where(r => r.ShopId == shop.ShopId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            ViewBag.ShopkeeperName =
                shop.Shopkeeper?.Name ?? "Shopkeeper";

            ViewBag.ShopName =
                shop.ShopName;

            ViewBag.ProfileImage =
                shop.Shopkeeper?.ProfileImage;

            return View(feedback);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkFeedbackRead(int reviewId)
        {
            if (!IsShopkeeper())
                return RedirectToAction("Login", "Account");

            var shop = await GetMyShop();

            if (shop == null)
                return NotFound();

            var review = await _context.Reviews
                .FirstOrDefaultAsync(r =>
                    r.ReviewId == reviewId &&
                    r.ShopId == shop.ShopId);

            if (review == null)
                return NotFound();

            if (!review.IsRead)
            {
                review.IsRead = true;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Feedback));
        }


        public async Task<IActionResult> Chat()
        {
            if (!IsShopkeeper())
                return RedirectToAction("Login", "Account");

            int shopkeeperId =
                GetShopkeeperId()!.Value;

            var shop = await GetMyShop();

            if (shop == null)
                return NotFound();

            var customerIds = await _context.Orders
                .Where(o =>
                    o.ShopId == shop.ShopId &&
                    o.OrderStatus != "Cancelled" &&
                    o.CustomerId != shopkeeperId)
                .Select(o => o.CustomerId)
                .Distinct()
                .ToListAsync();

            var customers = await _context.Users
                .Include(u => u.Role)
                .Where(u =>
                    customerIds.Contains(u.UserId) &&
                    u.Status)
                .OrderBy(u => u.Name)
                .ToListAsync();

            var deliveryIds = await _context.Orders
                .Where(o =>
                    o.ShopId == shop.ShopId &&
                    o.OrderStatus != "Cancelled" &&
                    o.DeliveryId.HasValue)
                .Select(o => o.DeliveryId!.Value)
                .Distinct()
                .ToListAsync();

            var deliveryUsers = await _context.Users
                .Include(u => u.Role)
                .Where(u =>
                    deliveryIds.Contains(u.UserId) &&
                    u.Status)
                .OrderBy(u => u.Name)
                .ToListAsync();

            var admins = await _context.Users
                .Include(u => u.Role)
                .Where(u =>
                    u.Role != null &&
                    u.Role.RoleName == AdminRole &&
                    u.Status &&
                    u.IsApproved)
                .OrderBy(u => u.Name)
                .ToListAsync();

            var unreadCounts = await _context.ChatMessages
                .Where(m =>
                    m.ReceiverId == shopkeeperId &&
                    !m.IsRead &&
                    !m.IsDeleted)
                .GroupBy(m => m.SenderId)
                .Select(g => new
                {
                    UserId = g.Key,
                    Count = g.Count()
                })
                .ToDictionaryAsync(
                    x => x.UserId,
                    x => x.Count);


            int totalUnreadMessages =
                await _context.ChatMessages
                    .CountAsync(m =>
                        m.ReceiverId == shopkeeperId &&
                        !m.IsRead &&
                        !m.IsDeleted);


            ViewBag.CurrentUserId =
                shopkeeperId;

            ViewBag.TotalUnreadMessages =
                totalUnreadMessages;

            ViewBag.Customers =
                customers;

            ViewBag.DeliveryUsers =
                deliveryUsers;

            ViewBag.Admins =
                admins;

            ViewBag.UnreadCounts =
                unreadCounts;

            ViewBag.SelectedUserId =
                null;

            ViewBag.SelectedUserName =
                null;

            ViewBag.ShopkeeperName =
                shop.Shopkeeper?.Name ?? "Shopkeeper";

            ViewBag.ShopName =
                shop.ShopName;

            ViewBag.ProfileImage =
                shop.Shopkeeper?.ProfileImage;

            return View(
                new List<ChatMessage>());
        }


        public async Task<IActionResult> Conversation(int userId)
        {
            if (!IsShopkeeper())
                return RedirectToAction("Login", "Account");

            int shopkeeperId =
                GetShopkeeperId()!.Value;

            var shop = await GetMyShop();

            if (shop == null)
                return NotFound();


            if (userId == shopkeeperId)
            {
                TempData["Error"] =
                    "You cannot chat with yourself.";

                return RedirectToAction("Chat");
            }

            var selectedUser = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(
                    u =>
                        u.UserId == userId &&
                        u.Status);

            if (selectedUser == null ||
                selectedUser.Role == null)
            {
                TempData["Error"] =
                    "Selected user was not found.";

                return RedirectToAction("Chat");
            }

            string selectedRole =
                selectedUser.Role.RoleName;

            bool isAllowed = false;


            if (string.Equals(
                    selectedRole,
                    AdminRole,
                    StringComparison.OrdinalIgnoreCase))
            {
                isAllowed = true;
            }


            else if (string.Equals(
                         selectedRole,
                         CustomerRole,
                         StringComparison.OrdinalIgnoreCase))
            {
                isAllowed =
                    await _context.Orders.AnyAsync(
                        o =>
                            o.ShopId == shop.ShopId &&
                            o.CustomerId == userId &&
                            o.OrderStatus != "Cancelled");
            }

            else if (string.Equals(
                         selectedRole,
                         DeliveryRole,
                         StringComparison.OrdinalIgnoreCase))
            {
                isAllowed =
                    await _context.Orders.AnyAsync(
                        o =>
                            o.ShopId == shop.ShopId &&
                            o.DeliveryId == userId &&
                            o.OrderStatus != "Cancelled");
            }

            if (!isAllowed)
            {
                TempData["Error"] =
                    "You are not allowed to chat with this user.";

                return RedirectToAction("Chat");
            }


            var messages = await _context.ChatMessages
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .Where(m =>
                    (m.SenderId == shopkeeperId &&
                     m.ReceiverId == userId)
                    ||
                    (m.SenderId == userId &&
                     m.ReceiverId == shopkeeperId))
                .OrderBy(m => m.SentDate)
                .ToListAsync();


            var unreadMessages =
                messages.Where(m =>
                    m.ReceiverId == shopkeeperId &&
                    !m.IsRead &&
                    !m.IsDeleted);

            foreach (var message in unreadMessages)
            {
                message.IsRead = true;
            }

            await _context.SaveChangesAsync();

            var customerIds = await _context.Orders
                .Where(o =>
                    o.ShopId == shop.ShopId &&
                    o.OrderStatus != "Cancelled")
                .Select(o => o.CustomerId)
                .Distinct()
                .ToListAsync();

            var customers = await _context.Users
                .Include(u => u.Role)
                .Where(u =>
                    customerIds.Contains(u.UserId) &&
                    u.Status)
                .OrderBy(u => u.Name)
                .ToListAsync();


            var deliveryIds = await _context.Orders
                .Where(o =>
                    o.ShopId == shop.ShopId &&
                    o.OrderStatus != "Cancelled" &&
                    o.DeliveryId.HasValue)
                .Select(o => o.DeliveryId!.Value)
                .Distinct()
                .ToListAsync();

            var deliveryUsers = await _context.Users
                .Include(u => u.Role)
                .Where(u =>
                    deliveryIds.Contains(u.UserId) &&
                    u.Status)
                .OrderBy(u => u.Name)
                .ToListAsync();

            var admins = await _context.Users
                .Include(u => u.Role)
                .Where(u =>
                    u.Role != null &&
                    u.Role.RoleName == AdminRole &&
                    u.Status &&
                    u.IsApproved)
                .OrderBy(u => u.Name)
                .ToListAsync();


            var unreadCounts = await _context.ChatMessages
                .Where(m =>
                    m.ReceiverId == shopkeeperId &&
                    !m.IsRead &&
                    !m.IsDeleted)
                .GroupBy(m => m.SenderId)
                .Select(g => new
                {
                    UserId = g.Key,
                    Count = g.Count()
                })
                .ToDictionaryAsync(
                    x => x.UserId,
                    x => x.Count);

            int totalUnreadMessages =
                await _context.ChatMessages
                    .CountAsync(m =>
                        m.ReceiverId == shopkeeperId &&
                        !m.IsRead &&
                        !m.IsDeleted);

            ViewBag.CurrentUserId =
                shopkeeperId;

            ViewBag.TotalUnreadMessages =
                totalUnreadMessages;

            ViewBag.Customers =
                customers;

            ViewBag.DeliveryUsers =
                deliveryUsers;

            ViewBag.Admins =
                admins;

            ViewBag.UnreadCounts =
                unreadCounts;

            ViewBag.SelectedUserId =
                userId;

            ViewBag.SelectedUserName =
                selectedUser.Name;

            ViewBag.ShopkeeperName =
                shop.Shopkeeper?.Name ?? "Shopkeeper";

            ViewBag.ShopName =
                shop.ShopName;

            ViewBag.ProfileImage =
                shop.Shopkeeper?.ProfileImage;

            return View(
                "Chat",
                messages);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessage(
            int receiverId,
            string message)
        {
            if (!IsShopkeeper())
                return RedirectToAction("Login", "Account");

            int shopkeeperId =
                GetShopkeeperId()!.Value;

            if (receiverId == shopkeeperId)
            {
                TempData["Error"] =
                    "You cannot send a message to yourself.";

                return RedirectToAction("Chat");
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                TempData["Error"] =
                    "Message cannot be empty.";

                return RedirectToAction(
                    "Conversation",
                    new { userId = receiverId });
            }

            var shop = await GetMyShop();

            if (shop == null)
                return NotFound();


            var receiver = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(
                    u =>
                        u.UserId == receiverId &&
                        u.Status);

            if (receiver == null ||
                receiver.Role == null)
            {
                TempData["Error"] =
                    "Receiver not found.";

                return RedirectToAction("Chat");
            }

            string role =
                receiver.Role.RoleName;

            bool allowed = false;


            if (string.Equals(
                    role,
                    AdminRole,
                    StringComparison.OrdinalIgnoreCase))
            {
                allowed = true;
            }


            else if (string.Equals(
                         role,
                         CustomerRole,
                         StringComparison.OrdinalIgnoreCase))
            {
                allowed =
                    await _context.Orders.AnyAsync(
                        o =>
                            o.ShopId == shop.ShopId &&
                            o.CustomerId == receiverId &&
                            o.OrderStatus != "Cancelled");
            }

            else if (string.Equals(
                         role,
                         DeliveryRole,
                         StringComparison.OrdinalIgnoreCase))
            {
                allowed =
                    await _context.Orders.AnyAsync(
                        o =>
                            o.ShopId == shop.ShopId &&
                            o.DeliveryId == receiverId &&
                            o.OrderStatus != "Cancelled");
            }

            if (!allowed)
            {
                TempData["Error"] =
                    "You are not allowed to message this user.";

                return RedirectToAction("Chat");
            }

            var chatMessage = new ChatMessage
            {
                SenderId = shopkeeperId,
                ReceiverId = receiverId,
                Message = message.Trim(),
                IsRead = false,
                IsDeleted = false,
                SentDate = DateTime.Now
            };

            _context.ChatMessages.Add(chatMessage);

            await _context.SaveChangesAsync();


            await CreateNotification(
                receiverId,
                "New Message",
                $"You have a new message from {HttpContext.Session.GetString("UserName") ?? "Shopkeeper"}.",
                "Chat",
                null);

            return RedirectToAction(
                "Conversation",
                new { userId = receiverId });
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMessage(
            int messageId,
            int receiverId)
        {
            if (!IsShopkeeper())
                return RedirectToAction("Login", "Account");

            int shopkeeperId =
                GetShopkeeperId()!.Value;

            if (messageId <= 0 ||
                receiverId <= 0)
            {
                TempData["Error"] =
                    "Invalid message.";

                return RedirectToAction("Chat");
            }

            var chatMessage =
                await _context.ChatMessages
                    .FirstOrDefaultAsync(
                        m =>
                            m.ChatMessageId == messageId);

            if (chatMessage == null)
            {
                TempData["Error"] =
                    "Message not found.";

                return RedirectToAction(
                    "Conversation",
                    new { userId = receiverId });
            }


            if (chatMessage.SenderId != shopkeeperId)
            {
                TempData["Error"] =
                    "You can only delete your own messages.";

                return RedirectToAction(
                    "Conversation",
                    new { userId = receiverId });
            }


            if (chatMessage.ReceiverId != receiverId)
            {
                TempData["Error"] =
                    "Invalid conversation.";

                return RedirectToAction(
                    "Conversation",
                    new { userId = receiverId });
            }

            if (chatMessage.IsDeleted)
            {
                return RedirectToAction(
                    "Conversation",
                    new { userId = receiverId });
            }

            chatMessage.IsDeleted = true;

            chatMessage.Message =
                "This message was deleted.";

            await _context.SaveChangesAsync();


            return RedirectToAction(
                "Conversation",
                new { userId = receiverId });
        }


        public async Task<IActionResult> Notifications()
        {
            if (!IsShopkeeper())
                return RedirectToAction("Login", "Account");

            int userId =
                GetShopkeeperId()!.Value;

            var shop = await GetMyShop();

            if (shop == null)
                return NotFound();

            var notifications = await _context.Notifications
                .Include(n => n.Order)
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedDate)
                .ToListAsync();

            int unreadCount =
               notifications.Count(n => !n.IsRead);
            int readCount =
                notifications.Count(n => n.IsRead);
            ViewBag.ShopkeeperName =
                shop.Shopkeeper?.Name ?? "Shopkeeper";

            ViewBag.ShopName =
                shop.ShopName;

            ViewBag.ProfileImage =
                shop.Shopkeeper?.ProfileImage;

            ViewBag.UnreadCount =
                notifications.Count(n => !n.IsRead);

            ViewBag.ReadCount =
                notifications.Count(n => n.IsRead);

            return View(notifications);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkNotificationRead(
            int id)
        {
            if (!IsShopkeeper())
                return RedirectToAction("Login", "Account");

            int userId =
                GetShopkeeperId()!.Value;

            var notification =
                await _context.Notifications
                    .FirstOrDefaultAsync(
                        n => n.NotificationId == id &&
                             n.UserId == userId);

            if (notification != null)
            {
                notification.IsRead = true;

                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Notifications");
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllNotificationsRead()
        {
            if (!IsShopkeeper())
                return RedirectToAction("Login", "Account");

            int userId =
                GetShopkeeperId()!.Value;

            var notifications =
                await _context.Notifications
                    .Where(n =>
                        n.UserId == userId &&
                        !n.IsRead)
                    .ToListAsync();

            foreach (var notification in notifications)
            {
                notification.IsRead = true;
            }

            if (notifications.Count > 0)
            {
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Notifications");
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            if (!IsShopkeeper())
                return RedirectToAction("Login", "Account");

            int userId = GetShopkeeperId()!.Value;

            var user = await _context.Users
                .Include(u => u.Role)
                .Include(u => u.Shop)
                .Include(u => u.DeliveryBoy)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
            {
                HttpContext.Session.Clear();

                return RedirectToAction(
                    "Login",
                    "Account");
            }

            return View(user);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(
            int UserId,
            string? Name,
            string? Email,
            string? Phone,
            string? Address,
            string? PaymentMethod,
            string? PaymentAccount,
            IFormFile? profileImage)
        {
            if (!IsShopkeeper())
                return RedirectToAction("Login", "Account");

            int sessionUserId = GetShopkeeperId()!.Value;

            if (UserId != sessionUserId)
            {
                TempData["Error"] = "Invalid user profile.";
                return RedirectToAction("Profile");
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == sessionUserId);

            if (user == null)
            {
                HttpContext.Session.Clear();
                return RedirectToAction("Login", "Account");
            }

            Name = Name?.Trim();
            Email = Email?.Trim().ToLowerInvariant();
            Phone = Phone?.Trim();
            Address = Address?.Trim();
            PaymentMethod = PaymentMethod?.Trim();
            PaymentAccount = PaymentAccount?
                .Trim()
                .Replace(" ", "")
                .Replace("-", "");

            if (string.IsNullOrWhiteSpace(Name))
            {
                TempData["Error"] = "Name is required.";
                return RedirectToAction("Profile");
            }

            if (string.IsNullOrWhiteSpace(Email))
            {
                TempData["Error"] = "Email is required.";
                return RedirectToAction("Profile");
            }

            if (!new EmailAddressAttribute().IsValid(Email))
            {
                TempData["Error"] = "Please enter a valid email address.";
                return RedirectToAction("Profile");
            }

            string[] allowedEmailDomains =
            {
        "gmail.com",
        "yahoo.com",
        "hotmail.com",
        "outlook.com"
    };

            string emailDomain = Email.Split('@').Last();

            if (!allowedEmailDomains.Contains(
                emailDomain,
                StringComparer.OrdinalIgnoreCase))
            {
                TempData["Error"] =
                    "Please use gmail.com, yahoo.com, hotmail.com or outlook.com.";

                return RedirectToAction("Profile");
            }

            bool emailExists = await _context.Users.AnyAsync(
                u => u.UserId != sessionUserId &&
                     u.Email == Email);

            if (emailExists)
            {
                TempData["Error"] =
                    "This email is already registered.";

                return RedirectToAction("Profile");
            }

            if (!string.IsNullOrWhiteSpace(PaymentMethod))
            {
                if (!PaymentMethod.Equals(
                        "Easypaisa",
                        StringComparison.OrdinalIgnoreCase) &&
                    !PaymentMethod.Equals(
                        "JazzCash",
                        StringComparison.OrdinalIgnoreCase))
                {
                    TempData["Error"] =
                        "Only Easypaisa or JazzCash is allowed.";

                    return RedirectToAction("Profile");
                }

                if (string.IsNullOrWhiteSpace(PaymentAccount))
                {
                    TempData["Error"] =
                        "Payment account is required.";

                    return RedirectToAction("Profile");
                }
            }

            if (!string.IsNullOrWhiteSpace(PaymentAccount) &&
                string.IsNullOrWhiteSpace(PaymentMethod))
            {
                TempData["Error"] =
                    "Please select a payment method.";

                return RedirectToAction("Profile");
            }

            if (!string.IsNullOrWhiteSpace(PaymentAccount))
            {
                if (!Regex.IsMatch(
                    PaymentAccount,
                    @"^(03\d{9}|(\+92|0092)3\d{9})$"))
                {
                    TempData["Error"] =
                        "Please enter a valid Pakistani mobile number.";

                    return RedirectToAction("Profile");
                }
            }

            string? newProfileImage = null;

            if (profileImage != null && profileImage.Length > 0)
            {
                string extension =
                    Path.GetExtension(profileImage.FileName)
                        .ToLowerInvariant();

                string[] allowedExtensions =
                {
            ".jpg",
            ".jpeg",
            ".png"
        };

                if (!allowedExtensions.Contains(extension))
                {
                    TempData["Error"] =
                        "Only JPG, JPEG and PNG images are allowed.";

                    return RedirectToAction("Profile");
                }

                if (profileImage.Length > 2 * 1024 * 1024)
                {
                    TempData["Error"] =
                        "Profile image must be 2 MB or smaller.";

                    return RedirectToAction("Profile");
                }

                string contentType =
                    profileImage.ContentType?.ToLowerInvariant() ?? "";

                if (contentType != "image/jpeg" &&
                    contentType != "image/png")
                {
                    TempData["Error"] =
                        "Only JPG, JPEG and PNG images are allowed.";

                    return RedirectToAction("Profile");
                }

                try
                {
                    newProfileImage =
                        await SaveFile(profileImage, "profiles");
                }
                catch
                {
                    TempData["Error"] =
                        "Unable to save the profile image.";

                    return RedirectToAction("Profile");
                }
            }

            string oldProfileImage =
                user.ProfileImage ?? "";

            user.Name = Name;
            user.Email = Email;
            user.Phone = Phone;
            user.Address = Address;
            user.PaymentMethod =
                string.IsNullOrWhiteSpace(PaymentMethod)
                    ? null
                    : PaymentMethod;
            user.PaymentAccount =
                string.IsNullOrWhiteSpace(PaymentAccount)
                    ? null
                    : PaymentAccount;

            if (!string.IsNullOrWhiteSpace(newProfileImage))
            {
                user.ProfileImage = newProfileImage;
            }

            try
            {
                await _context.SaveChangesAsync();

                HttpContext.Session.SetString(
                    "UserName",
                    user.Name);

                HttpContext.Session.SetString(
                    "Email",
                    user.Email);

                if (!string.IsNullOrWhiteSpace(newProfileImage) &&
                    !string.IsNullOrWhiteSpace(oldProfileImage))
                {
                    try
                    {
                        DeleteOldProfileImage(oldProfileImage);
                    }
                    catch
                    {
                    }
                }

                TempData["Success"] =
                    "Profile updated successfully.";

                return RedirectToAction("Profile");
            }
            catch
            {
                if (!string.IsNullOrWhiteSpace(newProfileImage))
                {
                    try
                    {
                        DeleteOldProfileImage(newProfileImage);
                    }
                    catch
                    {
                    }
                }

                TempData["Error"] =
                    "Unable to update profile.";

                return RedirectToAction("Profile");
            }
        }


        private async Task<string> SaveFile(
            IFormFile file,
            string folderName)
        {
            string webRootPath =
                _environment.WebRootPath;

            if (string.IsNullOrWhiteSpace(webRootPath))
            {
                webRootPath =
                    Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot");
            }

            string uploadsRoot =
                Path.Combine(
                    webRootPath,
                    "uploads",
                    folderName);

            if (!Directory.Exists(uploadsRoot))
            {
                Directory.CreateDirectory(uploadsRoot);
            }

            string extension =
                Path.GetExtension(
                    file.FileName)
                .ToLowerInvariant();

            string fileName =
                $"{Guid.NewGuid():N}{extension}";

            string filePath =
                Path.Combine(
                    uploadsRoot,
                    fileName);

            using (var stream =
                   new FileStream(
                       filePath,
                       FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return
                $"/uploads/{folderName}/{fileName}";
        }

        private void DeleteOldProfileImage(
            string imagePath)
        {
            if (string.IsNullOrWhiteSpace(imagePath))
                return;


            if (!imagePath.StartsWith(
                "/uploads/profiles/",
                StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            string relativePath =
                imagePath.TrimStart('/')
                    .Replace(
                        '/',
                        Path.DirectorySeparatorChar);

            string webRootPath =
                _environment.WebRootPath;

            if (string.IsNullOrWhiteSpace(webRootPath))
            {
                webRootPath =
                    Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot");
            }

            string fullPath =
                Path.Combine(
                    webRootPath,
                    relativePath
                        .Substring(
                            "wwwroot".Length)
                        .TrimStart(
                            Path.DirectorySeparatorChar));

            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }
        }
        private async Task CreateNotification(
            int userId,
            string title,
            string message,
            string type = "General",
            int? orderId = null)
        {
            var notification = new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Type = type,
                OrderId = orderId,
                IsRead = false,
                CreatedDate = DateTime.Now
            };
            _context.Notifications.Add(
                notification);
            await _context.SaveChangesAsync();
        }
    }
}