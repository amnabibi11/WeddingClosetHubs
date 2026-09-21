using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Text.Json;
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

        // ============================================================
        // CONSTANTS
        // ============================================================

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

        // ============================================================
        // AUTHENTICATION HELPERS
        // ============================================================

        private bool IsShopkeeper()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            string? roleName = HttpContext.Session.GetString("RoleName");

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
            int? shopkeeperId = GetShopkeeperId();

            if (!shopkeeperId.HasValue)
                return null;

            return await _context.Shops
                .Include(s => s.Shopkeeper)
                .FirstOrDefaultAsync(
                    s => s.ShopkeeperId == shopkeeperId.Value);
        }

        // ============================================================
        // ORDER HELPERS
        // ============================================================

        private static bool IsCancelledOrder(Order order)
        {
            return string.Equals(
                order.OrderStatus,
                "Cancelled",
                StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsActiveOrderStatus(string? status)
        {
            return !string.Equals(
                status,
                "Cancelled",
                StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsCompletedOrder(string? status)
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

        private static bool IsCodPayment(string? paymentMethod)
        {
            if (string.IsNullOrWhiteSpace(paymentMethod))
                return false;

            return paymentMethod.Contains(
                       "cash",
                       StringComparison.OrdinalIgnoreCase)
                   ||
                   string.Equals(
                       paymentMethod.Trim(),
                       "COD",
                       StringComparison.OrdinalIgnoreCase);
        }

        // ============================================================
        // CATEGORY DATA
        // ============================================================

        private Dictionary<string, List<string>> GetCategoryData()
        {
            return new Dictionary<string, List<string>>(
                StringComparer.OrdinalIgnoreCase)
            {
                ["Dress"] = new List<string>
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
                },

                ["Wedding Guest Wear"] = new List<string>
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

                ["Footwear"] = new List<string>
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

                ["Jewellery"] = new List<string>
                {
                    "Necklace",
                    "Earrings",
                    "Bangles",
                    "Bracelet",
                    "Maang Tikka",
                    "Jhumka",
                    "Rings",
                    "Bridal Jewellery Set"
                },

                ["Accessories"] = new List<string>
                {
                    "Clutch",
                    "Handbag",
                    "Bridal Dupatta",
                    "Veil",
                    "Hair Accessories",
                    "Brooch",
                    "Cufflinks",
                    "Tie",
                    "Bow Tie",
                    "Belt"
                }
            };
        }

        // ============================================================
        // GET ALLOWED CATEGORY DATA FOR CURRENT SHOP
        // ============================================================

        private Dictionary<string, List<string>> GetAllowedCategoryDataForShop(
            string? shopCategory)
        {
            var allCategories = GetCategoryData();

            string normalized =
                (shopCategory ?? string.Empty)
                .Trim()
                .ToLowerInvariant()
                .Replace("-", " ");

            var result =
                new Dictionary<string, List<string>>(
                    StringComparer.OrdinalIgnoreCase);

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

            if (normalized.Contains("dress") ||
                normalized.Contains("bridal") ||
                normalized.Contains("bride") ||
                normalized.Contains("groom") ||
                normalized.Contains("gents") ||
                normalized.Contains("men"))
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
                    result[category.Key] = category.Value;
                    break;
                }
            }

            return result;
        }

        // ============================================================
        // PREPARE PRODUCT FORM
        // ============================================================

        private void PrepareProductForm(Shop shop)
        {
            var allowedData =
                GetAllowedCategoryDataForShop(shop.ShopCategory);

            ViewBag.ShopCategory =
                shop.ShopCategory;

            ViewBag.ProductCategories =
                allowedData.Keys.ToList();

            ViewBag.SubCategories =
                allowedData.Values
                    .SelectMany(x => x)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
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
                JsonSerializer.Serialize(allowedData);
        }

        // ============================================================
        // SET PRODUCT TYPE AUTOMATICALLY
        // ============================================================

        private void SetProductType(
            Product product,
            string? shopCategory)
        {
            string normalized =
                (shopCategory ?? string.Empty)
                .Trim()
                .ToLowerInvariant()
                .Replace("-", " ");

            if (normalized.Contains("footwear") ||
                normalized.Contains("shoe"))
            {
                product.ProductType = "Footwear";
                return;
            }

            if (normalized.Contains("jewellery") ||
                normalized.Contains("jewelry"))
            {
                product.ProductType = "Jewellery";
                return;
            }

            if (normalized.Contains("accessor"))
            {
                product.ProductType = "Accessories";
                return;
            }

            if (normalized.Contains("dress") ||
                normalized.Contains("bridal") ||
                normalized.Contains("bride") ||
                normalized.Contains("groom") ||
                normalized.Contains("gents") ||
                normalized.Contains("men") ||
                normalized.Contains("guest"))
            {
                product.ProductType = "Dress / Custom Size";
                return;
            }

            product.ProductType = "Standard";
        }


// ============================================================
// DASHBOARD
// ============================================================

public async Task<IActionResult> Dashboard()
{
    if (!IsShopkeeper())
        return RedirectToAction("Login", "Account");

    int shopkeeperId = GetShopkeeperId()!.Value;

    // ============================================================
    // GET SHOP + SHOPKEEPER
    // ============================================================

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

    // ============================================================
    // CHECK SHOPKEEPER
    // ============================================================

    var shopkeeper = shop.Shopkeeper;

    if (shopkeeper == null)
    {
        TempData["Error"] =
            "Your shopkeeper profile could not be found.";

        return RedirectToAction("Profile");
    }

    // ============================================================
    // PRODUCTS
    // ============================================================

    var shopProducts = _context.Products
        .Where(p => p.ShopId == shop.ShopId);

    // ============================================================
    // ORDERS
    // ============================================================

    var shopOrders = _context.Orders
        .Where(o => o.ShopId == shop.ShopId);

    // Cancelled orders are excluded from active calculations.
    // IMPORTANT:
    // Do not use StringComparison.OrdinalIgnoreCase here.
    // EF Core translates this comparison to SQL correctly.

    var activeOrders = shopOrders
        .Where(o => o.OrderStatus != "Cancelled");

    // ============================================================
    // PRODUCT COUNTS
    // ============================================================

    int totalProducts =
        await shopProducts.CountAsync();

    int activeProducts =
        await shopProducts.CountAsync(p => p.Status);

    int disabledProducts =
        await shopProducts.CountAsync(p => !p.Status);

    // ============================================================
    // ORDER COUNTS
    // ============================================================

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

    // Cancelled orders are counted separately.

    int cancelledOrders =
        await shopOrders.CountAsync(
            o => o.OrderStatus == "Cancelled");

    // ============================================================
    // SALES
    // ============================================================

    decimal totalSales =
        await activeOrders
            .Where(o =>
                o.OrderStatus == "Delivered" ||
                o.OrderStatus == "Completed")
            .SumAsync(o => (decimal?)o.ProductTotal) ?? 0m;

    // ============================================================
    // SHOPKEEPER PAYMENTS
    // ============================================================

    // Shopkeeper payment is based on ProductTotal.
    // ServiceFee is separate and is retained by the admin.

    decimal paidPayments =
        await activeOrders
            .Where(o =>
                o.ShopkeeperPaymentStatus == "Paid")
            .SumAsync(o => (decimal?)o.ProductTotal) ?? 0m;

    decimal pendingPayments =
        await activeOrders
            .Where(o =>
                o.ShopkeeperPaymentStatus != "Paid")
            .SumAsync(o => (decimal?)o.ProductTotal) ?? 0m;

    // ============================================================
    // REVIEWS
    // ============================================================

    var reviews = await _context.Reviews
        .Where(r => r.ShopId == shop.ShopId)
        .ToListAsync();

    // Total feedback/reviews

    int reviewCount =
        reviews.Count;

    // Average rating

    double averageRating =
        reviews.Any()
            ? reviews.Average(r => r.Rating)
            : 0;

    // ============================================================
    // UNREAD FEEDBACK
    // ============================================================

    int unreadFeedback =
        await _context.Reviews
            .CountAsync(r =>
                r.ShopId == shop.ShopId &&
                !r.IsRead);

    // ============================================================
    // NOTIFICATIONS
    // ============================================================

    int unreadNotifications =
        await _context.Notifications
            .CountAsync(n =>
                n.UserId == shopkeeperId &&
                !n.IsRead);

    // ============================================================
    // CHAT MESSAGES
    // ============================================================

    int unreadMessages =
        await _context.ChatMessages
            .CountAsync(m =>
                m.ReceiverId == shopkeeperId &&
                !m.IsRead);

    // ============================================================
    // SHOPKEEPER PROFILE DATA
    // ============================================================

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

    // ============================================================
    // SHOP DATA
    // ============================================================

    ViewBag.ShopName =
        shop.ShopName ?? "My Shop";

    ViewBag.ShopCategory =
        shop.ShopCategory ?? "Not Selected";

    // ============================================================
    // SHOP APPROVAL / STATUS
    // ============================================================

    // The Dashboard view checks ViewBag.ShopApproved.
    // Therefore it MUST be assigned here.

    ViewBag.ShopApproved =
        shop.IsApproved;

    // If Shop.Status is a BOOL in your Shop model:

    ViewBag.ShopStatus =
        shop.Status;

    // ============================================================
    // DASHBOARD STATISTICS
    // ============================================================

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

    // Your previous Dashboard view used AcceptedOrders.
    // Keep this also so it does not display 0 accidentally.

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

    ViewBag.TotalSales =
        totalSales;

    ViewBag.PaidPayments =
        paidPayments;

    ViewBag.PendingPayments =
        pendingPayments;

    // ============================================================
    // REVIEW STATISTICS
    // ============================================================

    // Total number of reviews

    ViewBag.ReviewCount =
        reviewCount;

    ViewBag.TotalReviews =
        reviewCount;

    // Number of unread reviews

    ViewBag.UnreadFeedback =
        unreadFeedback;

    // Average rating

    ViewBag.AverageRating =
        averageRating;

    // ============================================================
    // NOTIFICATION / CHAT COUNTS
    // ============================================================

    ViewBag.UnreadNotifications =
        unreadNotifications;

    ViewBag.UnreadMessages =
        unreadMessages;

    // ============================================================
    // RETURN DASHBOARD
    // ============================================================

    return View();
}



        // ============================================================
        // MY SHOP
        // ============================================================

        public async Task<IActionResult> MyShop()
        {
            if (!IsShopkeeper())
                return RedirectToAction("Login", "Account");

            var shop = await GetMyShop();

            if (shop == null)
                return RedirectToAction("Profile");

            return View(shop);
        }

        // ============================================================
        // EDIT SHOP - GET
        // ============================================================

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

        // ============================================================
        // EDIT SHOP - POST
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditShop(
            Shop model,
            IFormFile? logoFile,
            IFormFile? shopFrontPhoto,
            IFormFile? shopInsidePhoto,
            IFormFile? shopSignboardPhoto)
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

            shop.ShopName =
                model.ShopName?.Trim();

            shop.ShopDescription =
                model.ShopDescription?.Trim();

            shop.ShopAddress =
                model.ShopAddress?.Trim();

            shop.ShopPhone =
                model.ShopPhone?.Trim();

            shop.Email =
                model.Email?.Trim();

            if (logoFile != null &&
                logoFile.Length > 0)
            {
                shop.Logo =
                    await SaveFile(logoFile, "shops");
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

                return View(shop);
            }
        }

        // ============================================================
        // PRODUCTS
        // ============================================================

        public async Task<IActionResult> Products()
        {
            if (!IsShopkeeper())
                return RedirectToAction("Login", "Account");

            int shopkeeperId = GetShopkeeperId()!.Value;

            var shop = await GetMyShop();

            if (shop == null)
                return NotFound();

            var products = await _context.Products
                .Include(p => p.Shop)
                .Where(p => p.ShopId == shop.ShopId)
                .OrderByDescending(p => p.CreatedDate)
                .ToListAsync();

            ViewBag.ShopkeeperName =
                shop.Shopkeeper?.Name ?? "Shopkeeper";

            ViewBag.ShopName =
                shop.ShopName;

            ViewBag.ProfileImage =
                shop.Shopkeeper?.ProfileImage;

            ViewBag.ShopCategory =
                shop.ShopCategory;

            return View(products);
        }
        // ============================================================
// NEGOTIATIONS
// ============================================================

public async Task<IActionResult> Negotiations()
{
    if (!IsShopkeeper())
        return RedirectToAction("Login", "Account");

    int shopkeeperId = GetShopkeeperId()!.Value;

    var shop = await GetMyShop();

    if (shop == null)
        return NotFound();

    var negotiations = await _context.Negotiations
        .Include(n => n.Product)
        .Include(n => n.Customer)
        .Where(n =>
            n.ShopkeeperId == shopkeeperId &&
            n.Product != null &&
            n.Product.ShopId == shop.ShopId)
        .OrderByDescending(n => n.CreatedDate)
        .ToListAsync();

    ViewBag.ShopkeeperName =
        shop.Shopkeeper?.Name ?? "Shopkeeper";

    ViewBag.ShopName =
        shop.ShopName;

    ViewBag.ProfileImage =
        shop.Shopkeeper?.ProfileImage;

    ViewBag.PendingCount =
        negotiations.Count(n =>
            string.Equals(
                n.Status,
                "Pending",
                StringComparison.OrdinalIgnoreCase));

    ViewBag.CounterOfferCount =
        negotiations.Count(n =>
            string.Equals(
                n.Status,
                "CounterOffer",
                StringComparison.OrdinalIgnoreCase));

    ViewBag.AcceptedCount =
        negotiations.Count(n =>
            string.Equals(
                n.Status,
                "Accepted",
                StringComparison.OrdinalIgnoreCase));

    return View(negotiations);
}


// ============================================================
// ACCEPT CUSTOMER OFFER
// ============================================================

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

    // Customer's requested price becomes final agreed price.
    negotiation.AgreedPrice =
        negotiation.RequestedPrice;

    negotiation.Status =
        "Accepted";

    negotiation.RespondedDate =
        DateTime.Now;

    negotiation.ShopkeeperMessage =
        "Your offer has been accepted.";

    await _context.SaveChangesAsync();

    // --------------------------------------------------------
    // CUSTOMER NOTIFICATION
    // --------------------------------------------------------

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


// ============================================================
// REJECT CUSTOMER OFFER
// ============================================================

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

    // --------------------------------------------------------
    // CUSTOMER NOTIFICATION
    // --------------------------------------------------------

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


// ============================================================
// SEND COUNTER OFFER
// ============================================================

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

    // Counter price must be below original product price.
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

    // --------------------------------------------------------
    // CUSTOMER NOTIFICATION
    // --------------------------------------------------------

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
        // ============================================================
 // ADD PRODUCT - GET
 // ============================================================

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

     return View(new Product());
 }


 // ============================================================
 // ADD PRODUCT - POST
 // ============================================================

 [HttpPost]
 [ValidateAntiForgeryToken]
 public async Task<IActionResult> AddProduct(
     Product product,
     IFormFile? productImage)
 {
     // =========================================================
     // CHECK SHOPKEEPER
     // =========================================================

     if (!IsShopkeeper())
         return RedirectToAction("Login", "Account");


     // =========================================================
     // GET SHOP
     // =========================================================

     var shop = await GetMyShop();

     if (shop == null)
         return NotFound();


     // =========================================================
     // SHOP APPROVAL / STATUS
     // =========================================================

     if (!shop.IsApproved || !shop.Status)
     {
         TempData["Error"] =
             "Your shop must be approved and active before adding products.";

         return RedirectToAction("Products");
     }


     // =========================================================
     // ALLOWED CATEGORY DATA
     // =========================================================

     var allowedData =
         GetAllowedCategoryDataForShop(
             shop.ShopCategory);


     // =========================================================
     // CATEGORY VALIDATION
     // =========================================================

     if (string.IsNullOrWhiteSpace(product.Category) ||
         !allowedData.Keys.Any(c =>
             string.Equals(
                 c,
                 product.Category.Trim(),
                 StringComparison.OrdinalIgnoreCase)))
     {
         ModelState.AddModelError(
             "Category",
             "Please select a valid category for your shop.");
     }


     // =========================================================
     // SUBCATEGORY VALIDATION
     // =========================================================

     string selectedCategory =
         product.Category?.Trim() ?? string.Empty;


     var matchingCategory =
         allowedData.Keys.FirstOrDefault(c =>
             string.Equals(
                 c,
                 selectedCategory,
                 StringComparison.OrdinalIgnoreCase));


     if (matchingCategory != null &&
         allowedData.TryGetValue(
             matchingCategory,
             out var allowedSubcategories))
     {
         if (string.IsNullOrWhiteSpace(product.Subcategory) ||
             !allowedSubcategories.Any(s =>
                 string.Equals(
                     s,
                     product.Subcategory?.Trim(),
                     StringComparison.OrdinalIgnoreCase)))
         {
             ModelState.AddModelError(
                 "Subcategory",
                 "Please select a valid subcategory.");
         }
     }


     // =========================================================
     // BASIC PRICE VALIDATION
     // =========================================================

     if (product.Price < 0)
     {
         ModelState.AddModelError(
             "Price",
             "Price cannot be negative.");
     }


     // =========================================================
     // STOCK VALIDATION
     // =========================================================

     if (product.StockQuantity < 0)
     {
         ModelState.AddModelError(
             "StockQuantity",
             "Stock quantity cannot be negative.");
     }


     // =========================================================
     // SALE / DISCOUNT VALIDATION
     // =========================================================

     if (product.IsOnSale)
     {
         // Sale price is required
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


         // Sale details are optional.
         // We do not require them because the shopkeeper
         // may simply want to offer a discounted price.
     }
     else
     {
         // -----------------------------------------------------
         // SALE IS DISABLED
         // Therefore these values must not remain saved.
         // -----------------------------------------------------

         product.SalePrice = null;
         product.SaleDetails = null;
     }


     // =========================================================
     // RENTAL VALIDATION
     // =========================================================

     if (product.IsAvailableForRent)
     {
         // -----------------------------------------------------
         // RENT PRICE
         // -----------------------------------------------------

         if (!product.RentPrice.HasValue ||
             product.RentPrice.Value <= 0)
         {
             ModelState.AddModelError(
                 "RentPrice",
                 "Please enter a valid rental price.");
         }


         // -----------------------------------------------------
         // REFUNDABLE SECURITY
         // -----------------------------------------------------

         if (!product.RentalSecurity.HasValue ||
             product.RentalSecurity.Value < 0)
         {
             ModelState.AddModelError(
                 "RentalSecurity",
                 "Please enter a valid refundable security amount.");
         }


         // -----------------------------------------------------
         // RENTAL DURATION
         // -----------------------------------------------------

         if (string.IsNullOrWhiteSpace(
                 product.RentalDuration))
         {
             ModelState.AddModelError(
                 "RentalDuration",
                 "Rental duration is required.");
         }


         // -----------------------------------------------------
         // RENTAL CONDITIONS
         // -----------------------------------------------------

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
         // -----------------------------------------------------
         // RENTAL IS DISABLED
         // Therefore clear all rental values.
         // -----------------------------------------------------

         product.RentPrice = null;
         product.RentalSecurity = null;
         product.RentalDuration = null;
         product.RentalConditions = null;
     }


     // =========================================================
     // PRODUCT IMAGE VALIDATION
     // =========================================================

     if (productImage != null &&
         productImage.Length > 0)
     {
         string extension =
             Path.GetExtension(
                 productImage.FileName)
             .ToLowerInvariant();


         // -----------------------------------------------------
         // EXTENSION
         // -----------------------------------------------------

         if (!AllowedProductImageExtensions.Contains(
                 extension))
         {
             ModelState.AddModelError(
                 "productImage",
                 "Only JPG, JPEG, PNG and WEBP images are allowed.");
         }


         // -----------------------------------------------------
         // FILE SIZE
         // -----------------------------------------------------

         if (productImage.Length > 5 * 1024 * 1024)
         {
             ModelState.AddModelError(
                 "productImage",
                 "Product image must be 5 MB or smaller.");
         }
     }
     else
     {
         ModelState.AddModelError(
             "productImage",
             "Please select a product image.");
     }


     // =========================================================
     // MODEL VALIDATION
     // =========================================================

     if (!ModelState.IsValid)
     {
         PrepareProductForm(shop);

         return View(product);
     }


     // =========================================================
     // ASSIGN SHOP
     // =========================================================

     product.ShopId =
         shop.ShopId;


     // =========================================================
     // TRIM TEXT VALUES
     // =========================================================

     product.Category =
         product.Category?.Trim();

     product.Subcategory =
         product.Subcategory?.Trim();

     product.ProductName =
         product.ProductName?.Trim();

     product.Description =
         product.Description?.Trim();

     product.Color =
         product.Color?.Trim();

     product.Size =
         product.Size?.Trim();

     product.Occasion =
         product.Occasion?.Trim();


     // =========================================================
     // TRIM NEW RENTAL VALUES
     // =========================================================

     if (product.IsAvailableForRent)
     {
         product.RentalDuration =
             product.RentalDuration?.Trim();

         product.RentalConditions =
             product.RentalConditions?.Trim();
     }


     // =========================================================
     // TRIM SALE DETAILS
     // =========================================================

     if (product.IsOnSale)
     {
         product.SaleDetails =
             product.SaleDetails?.Trim();
     }


     // =========================================================
     // SET PRODUCT TYPE
     //
     // ProductType is controlled by the server.
     // The readonly value from the Razor page is NOT trusted.
     // =========================================================

     SetProductType(
         product,
         shop.ShopCategory);


     // =========================================================
     // SAVE PRODUCT IMAGE
     // =========================================================

     if (productImage != null &&
         productImage.Length > 0)
     {
         product.Image =
             await SaveFile(
                 productImage,
                 "products");
     }


     // =========================================================
     // DEFAULT VALUES
     // =========================================================

     product.CreatedDate =
         DateTime.Now;

     product.Status =
         true;


     // =========================================================
     // ADD PRODUCT
     // =========================================================

     _context.Products.Add(product);

     await _context.SaveChangesAsync();


     // =========================================================
     // SUCCESS
     // =========================================================

     TempData["Success"] =
         "Product added successfully.";

     return RedirectToAction("Products");
 }

        // ============================================================
// EDIT PRODUCT - GET
// ============================================================

[HttpGet]
public async Task<IActionResult> EditProduct(int id)
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
        return NotFound();

    PrepareProductForm(shop);

    return View(product);
}


// ============================================================
// EDIT PRODUCT - POST
// ============================================================

[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> EditProduct(
    Product model,
    IFormFile? productImage)
{
    if (!IsShopkeeper())
        return RedirectToAction("Login", "Account");

    var shop = await GetMyShop();

    if (shop == null)
        return NotFound();


    // ========================================================
    // FIND PRODUCT
    // ========================================================

    var product = await _context.Products
        .FirstOrDefaultAsync(
            p => p.ProductId == model.ProductId &&
                 p.ShopId == shop.ShopId);

    if (product == null)
        return NotFound();


    // ========================================================
    // REMOVE NAVIGATION PROPERTY VALIDATION
    // ========================================================

    ModelState.Remove("Shop");


    // ========================================================
    // CATEGORY VALIDATION
    // ========================================================

    var allowedData =
        GetAllowedCategoryDataForShop(
            shop.ShopCategory);

    string selectedCategory =
        model.Category?.Trim() ?? string.Empty;


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
            "Please select a valid category for your shop.");
    }


    // ========================================================
    // SUBCATEGORY VALIDATION
    // ========================================================

    if (matchingCategory != null &&
        allowedData.TryGetValue(
            matchingCategory,
            out var allowedSubcategories))
    {
        if (string.IsNullOrWhiteSpace(model.Subcategory) ||
            !allowedSubcategories.Any(s =>
                string.Equals(
                    s,
                    model.Subcategory?.Trim(),
                    StringComparison.OrdinalIgnoreCase)))
        {
            ModelState.AddModelError(
                "Subcategory",
                "Please select a valid subcategory.");
        }
    }


    // ========================================================
    // PRICE VALIDATION
    // ========================================================

    if (model.Price < 0)
    {
        ModelState.AddModelError(
            "Price",
            "Price cannot be negative.");
    }


    // ========================================================
    // STOCK VALIDATION
    // ========================================================

    if (model.StockQuantity < 0)
    {
        ModelState.AddModelError(
            "StockQuantity",
            "Stock quantity cannot be negative.");
    }


    // ========================================================
    // SALE / DISCOUNT VALIDATION
    // ========================================================

    if (model.IsOnSale)
    {
        if (!model.SalePrice.HasValue ||
            model.SalePrice.Value <= 0)
        {
            ModelState.AddModelError(
                "SalePrice",
                "Please enter a valid sale price.");
        }
        else if (model.SalePrice.Value >= model.Price)
        {
            ModelState.AddModelError(
                "SalePrice",
                "Sale price must be lower than the original price.");
        }
    }
    else
    {
        // If sale is disabled, remove old sale information.
        model.SalePrice = null;
        model.SaleDetails = null;
    }


    // ========================================================
    // RENTAL VALIDATION
    // ========================================================

    if (model.IsAvailableForRent)
    {
        // Rental Price
        if (!model.RentPrice.HasValue ||
            model.RentPrice.Value <= 0)
        {
            ModelState.AddModelError(
                "RentPrice",
                "Please enter a valid rental price.");
        }


        // Refundable Security
        if (!model.RentalSecurity.HasValue ||
            model.RentalSecurity.Value < 0)
        {
            ModelState.AddModelError(
                "RentalSecurity",
                "Please enter a valid refundable security amount.");
        }


        // Rental Duration
        if (string.IsNullOrWhiteSpace(
                model.RentalDuration))
        {
            ModelState.AddModelError(
                "RentalDuration",
                "Rental duration is required.");
        }


        // Rental Conditions
        if (string.IsNullOrWhiteSpace(
                model.RentalConditions))
        {
            ModelState.AddModelError(
                "RentalConditions",
                "Rental conditions are required.");
        }
    }
    else
    {
        // If rental is disabled, remove old rental information.

        model.RentPrice = null;

        model.RentalSecurity = null;

        model.RentalDuration = null;

        model.RentalConditions = null;
    }


    // ========================================================
    // IMAGE VALIDATION
    // ========================================================

    if (productImage != null &&
        productImage.Length > 0)
    {
        string extension =
            Path.GetExtension(
                productImage.FileName)
            .ToLowerInvariant();


        if (!AllowedProductImageExtensions.Contains(
                extension))
        {
            ModelState.AddModelError(
                "productImage",
                "Only JPG, JPEG, PNG and WEBP images are allowed.");
        }


        if (productImage.Length >
            5 * 1024 * 1024)
        {
            ModelState.AddModelError(
                "productImage",
                "Product image must be 5 MB or smaller.");
        }
    }


    // ========================================================
    // RETURN VIEW IF VALIDATION FAILED
    // ========================================================

    if (!ModelState.IsValid)
    {
        PrepareProductForm(shop);

        return View(model);
    }


    // ========================================================
    // UPDATE BASIC PRODUCT INFORMATION
    // ========================================================

    product.ProductName =
        model.ProductName?.Trim();

    product.Description =
        model.Description?.Trim();

    product.Category =
        model.Category?.Trim();

    product.Subcategory =
        model.Subcategory?.Trim();

    product.Occasion =
        model.Occasion?.Trim();

    product.Size =
        model.Size?.Trim();

    product.SizeType =
        model.SizeType?.Trim();

    product.CustomMeasurements =
        model.CustomMeasurements?.Trim();

    product.Color =
        model.Color?.Trim();


    // ========================================================
    // PRICE & STOCK
    // ========================================================

    // IMPORTANT:
    // This is always the ORIGINAL product price.
    // Sale price and negotiated price do NOT replace it.

    product.Price =
        model.Price;

    product.StockQuantity =
        model.StockQuantity;


    // ========================================================
    // CUSTOM MEASUREMENT
    // ========================================================

    product.HasCustomMeasurement =
        model.HasCustomMeasurement;


    // ========================================================
    // SALE / DISCOUNT
    // ========================================================

    product.IsOnSale =
        model.IsOnSale;

    product.SalePrice =
        model.SalePrice;

    product.SaleDetails =
        model.SaleDetails?.Trim();


    // ========================================================
    // RENTAL
    // ========================================================

    product.IsAvailableForRent =
        model.IsAvailableForRent;

    product.RentPrice =
        model.RentPrice;

    product.RentalSecurity =
        model.RentalSecurity;

    product.RentalDuration =
        model.RentalDuration?.Trim();

    product.RentalConditions =
        model.RentalConditions?.Trim();


    // ========================================================
    // NEGOTIATION
    // ========================================================

    product.AllowNegotiation =
        model.AllowNegotiation;


    // ========================================================
    // PRODUCT TYPE
    // ========================================================

    // ProductType is controlled by the server.
    // Customer/browser cannot decide it.

    SetProductType(
        product,
        shop.ShopCategory);


    // ========================================================
    // PRODUCT IMAGE
    // ========================================================

    if (productImage != null &&
        productImage.Length > 0)
    {
        product.Image =
            await SaveFile(
                productImage,
                "products");
    }


    // ========================================================
    // SAVE
    // ========================================================

    await _context.SaveChangesAsync();


    TempData["Success"] =
        "Product updated successfully.";


    return RedirectToAction("Products");
}
        // ============================================================
        // DELETE PRODUCT
        // ============================================================

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

        // ============================================================
        // TOGGLE PRODUCT STATUS
        // ============================================================

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

        // ============================================================
        // ORDERS
        // CANCELLED ORDERS ARE NOT SHOWN
        // ============================================================

        public async Task<IActionResult> Orders()
        {
            if (!IsShopkeeper())
                return RedirectToAction("Login", "Account");

            var shop = await GetMyShop();

            if (shop == null)
                return NotFound();

            var orders = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.Delivery)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
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

            ViewBag.CurrentUserId =
                GetShopkeeperId();

            return View(orders);
        }

        // ============================================================
        // ORDER DETAILS
        // CANCELLED ORDERS CANNOT BE OPENED
        // ============================================================

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

        // ============================================================
        // UPDATE ORDER STATUS
        // ============================================================

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

            // --------------------------------------------------------
            // CUSTOMER NOTIFICATION
            // --------------------------------------------------------

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

            // --------------------------------------------------------
            // ADMIN NOTIFICATION WHEN ORDER IS READY
            // --------------------------------------------------------

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

        // ============================================================
        // PAYMENTS
        // CANCELLED ORDERS ARE NOT SHOWN
        // ============================================================

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

            // --------------------------------------------------------
            // FINANCIAL TOTALS
            //
            // Shopkeeper gets FULL ProductTotal.
            // ServiceFee is separate and belongs to Admin.
            // DeliveryCharges are separate.
            // --------------------------------------------------------

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

        // ============================================================
        // PAYMENT DETAILS
        // CANCELLED ORDERS CANNOT BE OPENED
        // ============================================================

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

            // --------------------------------------------------------
            // IMPORTANT PAYMENT CALCULATION
            //
            // ProductTotal = FINAL / NEGOTIATED DRESS PRICE
            //
            // Shopkeeper receives FULL ProductTotal.
            //
            // ServiceFee = Admin's 10% service fee.
            //
            // DeliveryCharges = Delivery person's charge.
            //
            // Customer Total =
            // ProductTotal + ServiceFee + DeliveryCharges
            // --------------------------------------------------------

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


// ============================================================
// FEEDBACK
// ============================================================

public async Task<IActionResult> Feedback()
{
    if (!IsShopkeeper())
        return RedirectToAction("Login", "Account");

    var shop = await GetMyShop();

    if (shop == null)
        return NotFound();

    // ============================================================
    // MARK ALL UNREAD FEEDBACK FOR THIS SHOP AS READ
    // ============================================================

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

    // ============================================================
    // LOAD ALL FEEDBACK
    // ============================================================

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


    
// ============================================================
// MARK FEEDBACK AS READ
// ============================================================

[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> MarkFeedbackRead(int reviewId)
{
    if (!IsShopkeeper())
        return RedirectToAction("Login", "Account");

    var shop = await GetMyShop();

    if (shop == null)
        return NotFound();

    // Find only a review that belongs to the logged-in shopkeeper's shop
    var review = await _context.Reviews
        .FirstOrDefaultAsync(r =>
            r.ReviewId == reviewId &&
            r.ShopId == shop.ShopId);

    if (review == null)
        return NotFound();

    // Mark this feedback as read
    if (!review.IsRead)
    {
        review.IsRead = true;
        await _context.SaveChangesAsync();
    }

    // Return to feedback page
    return RedirectToAction(nameof(Feedback));
}


// ============================================================
// CHAT
// ============================================================

public async Task<IActionResult> Chat()
{
    if (!IsShopkeeper())
        return RedirectToAction("Login", "Account");

    int shopkeeperId =
        GetShopkeeperId()!.Value;

    var shop = await GetMyShop();

    if (shop == null)
        return NotFound();

    // --------------------------------------------------------
    // CUSTOMERS WHO HAVE ACTIVE ORDERS FROM THIS SHOP
    // --------------------------------------------------------

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

    // --------------------------------------------------------
    // DELIVERY BOYS WHO HAVE ACTIVE ORDERS FROM THIS SHOP
    // --------------------------------------------------------

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

    // --------------------------------------------------------
    // ACTIVE ADMINS
    // --------------------------------------------------------

    var admins = await _context.Users
        .Include(u => u.Role)
        .Where(u =>
            u.Role != null &&
            u.Role.RoleName == AdminRole &&
            u.Status &&
            u.IsApproved)
        .OrderBy(u => u.Name)
        .ToListAsync();

    // --------------------------------------------------------
    // UNREAD MESSAGE COUNTS
    // --------------------------------------------------------

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

    // --------------------------------------------------------
    // TOTAL UNREAD MESSAGES
    // --------------------------------------------------------

    int totalUnreadMessages =
        await _context.ChatMessages
            .CountAsync(m =>
                m.ReceiverId == shopkeeperId &&
                !m.IsRead &&
                !m.IsDeleted);

    // --------------------------------------------------------
    // VIEWBAG
    // --------------------------------------------------------

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


// ============================================================
// CONVERSATION
// ============================================================

public async Task<IActionResult> Conversation(int userId)
{
    if (!IsShopkeeper())
        return RedirectToAction("Login", "Account");

    int shopkeeperId =
        GetShopkeeperId()!.Value;

    var shop = await GetMyShop();

    if (shop == null)
        return NotFound();

    // --------------------------------------------------------
    // PREVENT SELF CHAT
    // --------------------------------------------------------

    if (userId == shopkeeperId)
    {
        TempData["Error"] =
            "You cannot chat with yourself.";

        return RedirectToAction("Chat");
    }

    // --------------------------------------------------------
    // SELECTED USER
    // --------------------------------------------------------

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

    // --------------------------------------------------------
    // ADMIN
    // --------------------------------------------------------

    if (string.Equals(
            selectedRole,
            AdminRole,
            StringComparison.OrdinalIgnoreCase))
    {
        isAllowed = true;
    }

    // --------------------------------------------------------
    // CUSTOMER
    // ACTIVE ORDER ONLY
    // --------------------------------------------------------

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

    // --------------------------------------------------------
    // DELIVERY
    // ACTIVE ORDER ONLY
    // --------------------------------------------------------

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

    // --------------------------------------------------------
    // INVALID USER
    // --------------------------------------------------------

    if (!isAllowed)
    {
        TempData["Error"] =
            "You are not allowed to chat with this user.";

        return RedirectToAction("Chat");
    }

    // --------------------------------------------------------
    // LOAD CONVERSATION
    // --------------------------------------------------------

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

    // --------------------------------------------------------
    // MARK RECEIVED MESSAGES AS READ
    // --------------------------------------------------------

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

    // --------------------------------------------------------
    // RELOAD CHAT USERS
    // --------------------------------------------------------

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

    // --------------------------------------------------------
    // DELIVERY USERS
    // --------------------------------------------------------

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

    // --------------------------------------------------------
    // ADMINS
    // --------------------------------------------------------

    var admins = await _context.Users
        .Include(u => u.Role)
        .Where(u =>
            u.Role != null &&
            u.Role.RoleName == AdminRole &&
            u.Status &&
            u.IsApproved)
        .OrderBy(u => u.Name)
        .ToListAsync();

    // --------------------------------------------------------
    // UNREAD COUNTS
    // --------------------------------------------------------

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

    // --------------------------------------------------------
    // TOTAL UNREAD
    // --------------------------------------------------------

    int totalUnreadMessages =
        await _context.ChatMessages
            .CountAsync(m =>
                m.ReceiverId == shopkeeperId &&
                !m.IsRead &&
                !m.IsDeleted);

    // --------------------------------------------------------
    // VIEWBAG
    // IMPORTANT: THIS FIX MAKES DELETE ICON APPEAR
    // --------------------------------------------------------

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


// ============================================================
// SEND MESSAGE
// ============================================================

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

    // --------------------------------------------------------
    // PREVENT SELF MESSAGE
    // --------------------------------------------------------

    if (receiverId == shopkeeperId)
    {
        TempData["Error"] =
            "You cannot send a message to yourself.";

        return RedirectToAction("Chat");
    }

    // --------------------------------------------------------
    // EMPTY MESSAGE
    // --------------------------------------------------------

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

    // --------------------------------------------------------
    // FIND RECEIVER
    // --------------------------------------------------------

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

    // --------------------------------------------------------
    // ADMIN
    // --------------------------------------------------------

    if (string.Equals(
            role,
            AdminRole,
            StringComparison.OrdinalIgnoreCase))
    {
        allowed = true;
    }

    // --------------------------------------------------------
    // CUSTOMER
    // --------------------------------------------------------

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

    // --------------------------------------------------------
    // DELIVERY
    // --------------------------------------------------------

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

    // --------------------------------------------------------
    // NOT ALLOWED
    // --------------------------------------------------------

    if (!allowed)
    {
        TempData["Error"] =
            "You are not allowed to message this user.";

        return RedirectToAction("Chat");
    }

    // --------------------------------------------------------
    // CREATE MESSAGE
    // --------------------------------------------------------

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

    // --------------------------------------------------------
    // NOTIFICATION
    // --------------------------------------------------------

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


// ============================================================
// DELETE MESSAGE
// ============================================================

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

    // --------------------------------------------------------
    // VALIDATE IDS
    // --------------------------------------------------------

    if (messageId <= 0 ||
        receiverId <= 0)
    {
        TempData["Error"] =
            "Invalid message.";

        return RedirectToAction("Chat");
    }

    // --------------------------------------------------------
    // FIND MESSAGE
    // --------------------------------------------------------

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

    // --------------------------------------------------------
    // ONLY MESSAGE SENDER CAN DELETE
    // --------------------------------------------------------

    if (chatMessage.SenderId != shopkeeperId)
    {
        TempData["Error"] =
            "You can only delete your own messages.";

        return RedirectToAction(
            "Conversation",
            new { userId = receiverId });
    }

    // --------------------------------------------------------
    // VERIFY CONVERSATION
    // --------------------------------------------------------

    if (chatMessage.ReceiverId != receiverId)
    {
        TempData["Error"] =
            "Invalid conversation.";

        return RedirectToAction(
            "Conversation",
            new { userId = receiverId });
    }

    // --------------------------------------------------------
    // ALREADY DELETED
    // --------------------------------------------------------

    if (chatMessage.IsDeleted)
    {
        return RedirectToAction(
            "Conversation",
            new { userId = receiverId });
    }

    // --------------------------------------------------------
    // SOFT DELETE
    // --------------------------------------------------------

    chatMessage.IsDeleted = true;

    chatMessage.Message =
        "This message was deleted.";

    await _context.SaveChangesAsync();

    // --------------------------------------------------------
    // RETURN TO SAME CONVERSATION
    // --------------------------------------------------------

    return RedirectToAction(
        "Conversation",
        new { userId = receiverId });
}



        // ============================================================
        // NOTIFICATIONS
        // ============================================================

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
            // ------------------------------------------------------------
            // UNREAD / READ COUNTS 
            // ------------------------------------------------------------
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

        // ============================================================
        // MARK ONE NOTIFICATION READ
        // ============================================================

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

        // ============================================================
        // MARK ALL NOTIFICATIONS READ
        // ============================================================

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

        // ============================================================
        // PROFILE - GET
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            if (!IsShopkeeper())
                return RedirectToAction("Login", "Account");

            int userId =
                GetShopkeeperId()!.Value;

            var user = await _context.Users
                .Include(u => u.Role)
                .Include(u => u.Shop)
                .Include(u => u.DeliveryBoy)
                .FirstOrDefaultAsync(
                    u => u.UserId == userId);

            if (user == null)
            {
                HttpContext.Session.Clear();

                return RedirectToAction(
                    "Login",
                    "Account");
            }

            ViewBag.PaymentMethod =
                user.PaymentMethod;

            ViewBag.PaymentAccount =
                user.PaymentAccount;

            return View(user);
        }

        // ============================================================
        // PROFILE - POST
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(
            User model,
            IFormFile? profileImage)
        {
            if (!IsShopkeeper())
                return RedirectToAction("Login", "Account");

            int sessionUserId =
                GetShopkeeperId()!.Value;

            if (model.UserId != sessionUserId)
            {
                TempData["Error"] =
                    "Invalid user profile.";

                return RedirectToAction("Profile");
            }

            var user = await _context.Users
                .Include(u => u.Role)
                .Include(u => u.Shop)
                .Include(u => u.DeliveryBoy)
                .FirstOrDefaultAsync(
                    u => u.UserId == sessionUserId);

            if (user == null)
            {
                HttpContext.Session.Clear();

                return RedirectToAction(
                    "Login",
                    "Account");
            }

            // --------------------------------------------------------
            // BASIC VALIDATION
            // --------------------------------------------------------

            if (string.IsNullOrWhiteSpace(model.Name))
            {
                ModelState.AddModelError(
                    "Name",
                    "Name is required.");
            }

            if (string.IsNullOrWhiteSpace(model.Email))
            {
                ModelState.AddModelError(
                    "Email",
                    "Email is required.");
            }

            // --------------------------------------------------------
            // EMAIL DUPLICATE CHECK
            // --------------------------------------------------------

            if (!string.IsNullOrWhiteSpace(model.Email))
            {
                string email =
                    model.Email.Trim().ToLower();

                bool emailExists =
                    await _context.Users.AnyAsync(
                        u =>
                            u.UserId != sessionUserId &&
                            u.Email != null &&
                            u.Email.ToLower() == email);

                if (emailExists)
                {
                    ModelState.AddModelError(
                        "Email",
                        "This email is already registered.");
                }
            }

            // --------------------------------------------------------
            // PAYMENT VALIDATION
            // --------------------------------------------------------

            string? paymentMethod =
                model.PaymentMethod?.Trim();

            if (!string.IsNullOrWhiteSpace(paymentMethod))
            {
                bool validPaymentMethod =
                    paymentMethod.Equals(
                        "Easypaisa",
                        StringComparison.OrdinalIgnoreCase)
                    ||
                    paymentMethod.Equals(
                        "JazzCash",
                        StringComparison.OrdinalIgnoreCase);

                if (!validPaymentMethod)
                {
                    ModelState.AddModelError(
                        "PaymentMethod",
                        "Only Easypaisa or JazzCash is allowed.");
                }

                if (string.IsNullOrWhiteSpace(
                        model.PaymentAccount))
                {
                    ModelState.AddModelError(
                        "PaymentAccount",
                        "Payment account is required.");
                }
            }

            // --------------------------------------------------------
            // PROFILE IMAGE VALIDATION
            // --------------------------------------------------------

            if (profileImage != null &&
                profileImage.Length > 0)
            {
                string extension =
                    Path.GetExtension(
                        profileImage.FileName)
                    .ToLowerInvariant();

                string[] allowedExtensions =
                {
                    ".jpg",
                    ".jpeg",
                    ".png"
                };

                if (!allowedExtensions.Contains(
                        extension))
                {
                    ModelState.AddModelError(
                        "profileImage",
                        "Only JPG, JPEG and PNG images are allowed.");
                }

                if (profileImage.Length >
                    2 * 1024 * 1024)
                {
                    ModelState.AddModelError(
                        "profileImage",
                        "Profile image must be 2 MB or smaller.");
                }
            }

            if (!ModelState.IsValid)
            {
                ViewBag.PaymentMethod =
                    user.PaymentMethod;

                ViewBag.PaymentAccount =
                    user.PaymentAccount;

                return View(model);
            }

            // --------------------------------------------------------
            // UPDATE USER
            // --------------------------------------------------------

            user.Name =
                model.Name.Trim();

            user.Email =
                model.Email.Trim();

            user.Phone =
                model.Phone?.Trim();

            user.Address =
                model.Address?.Trim();

            user.PaymentMethod =
                string.IsNullOrWhiteSpace(
                    model.PaymentMethod)
                    ? null
                    : model.PaymentMethod.Trim();

            user.PaymentAccount =
                string.IsNullOrWhiteSpace(
                    model.PaymentAccount)
                    ? null
                    : model.PaymentAccount.Trim();

            // --------------------------------------------------------
            // UPDATE PROFILE IMAGE
            // --------------------------------------------------------

            if (profileImage != null &&
                profileImage.Length > 0)
            {
                user.ProfileImage =
                    await SaveFile(
                        profileImage,
                        "profiles");
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

                TempData["Success"] =
                    "Profile updated successfully.";

                return RedirectToAction("Profile");
            }
            catch
            {
                TempData["Error"] =
                    "Unable to update profile.";

                ViewBag.PaymentMethod =
                    user.PaymentMethod;

                ViewBag.PaymentAccount =
                    user.PaymentAccount;

                return View(model);
            }
        }

        // ============================================================
        // LOGOUT
        // ============================================================

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();

            return RedirectToAction(
                "Login",
                "Account");
        }

        // ============================================================
        // CREATE NOTIFICATION
        // ============================================================

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

        // ============================================================
        // SAVE FILE
        // ============================================================

        private async Task<string> SaveFile(
            IFormFile file,
            string folderName)
        {
            string extension =
                Path.GetExtension(
                    file.FileName)
                .ToLowerInvariant();

            string uploadsRoot =
                Path.Combine(
                    _environment.WebRootPath,
                    "uploads",
                    folderName);

            if (!Directory.Exists(uploadsRoot))
            {
                Directory.CreateDirectory(
                    uploadsRoot);
            }

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
    }
}