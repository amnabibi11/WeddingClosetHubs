using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using WeddingClosetHubs.Models;

namespace WeddingClosetHubs.Controllers
{
    public class CustomerController : Controller
    {
        private readonly WeddingClosetHubsContext _context;
        private readonly IWebHostEnvironment _environment;

        private const string CartSessionKey = "CustomerCart";

        private const decimal DeliveryCharge = 300m;
        private const decimal ServiceFeeRate = 0.10m;

        private static readonly string[] AllowedPaymentMethods =
        {
            "Easypaisa",
            "JazzCash",
            "Cash on Delivery"
        };

        private static readonly string[] AllowedPaymentImageExtensions =
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".webp"
        };

        public CustomerController(
            WeddingClosetHubsContext context,
            IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }


        // =========================================================
        // CUSTOMER AUTHORIZATION
        // =========================================================

        private int? GetCustomerId()
        {
            return HttpContext.Session.GetInt32("CustomerId");
        }


        private bool IsCustomerLoggedIn()
        {
            return GetCustomerId().HasValue;
        }


        private IActionResult CustomerLogin()
        {
            HttpContext.Session.SetString(
                "ReturnUrl",
                "/Customer/Dashboard"
            );

            return RedirectToAction(
                "Login",
                "Account"
            );
        }


        // =========================================================
        // CART SESSION HELPERS
        // =========================================================

        private List<CustomerCartItem> GetCartItems()
        {
            var cartJson =
                HttpContext.Session.GetString(CartSessionKey);

            if (string.IsNullOrWhiteSpace(cartJson))
            {
                return new List<CustomerCartItem>();
            }

            try
            {
                return JsonSerializer.Deserialize<
                    List<CustomerCartItem>
                >(
                    cartJson,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }
                ) ?? new List<CustomerCartItem>();
            }
            catch
            {
                HttpContext.Session.Remove(CartSessionKey);

                return new List<CustomerCartItem>();
            }
        }


        private void SaveCartItems(
            List<CustomerCartItem> cartItems)
        {
            HttpContext.Session.SetString(
                CartSessionKey,
                JsonSerializer.Serialize(cartItems)
            );
        }


        private void ClearCart()
        {
            HttpContext.Session.Remove(CartSessionKey);
        }


        // =========================================================
        // CHECK WHETHER PRODUCT IS A DRESS
        // =========================================================

        private bool IsDressProduct(Product product)
        {
            string category =
                (product.Category ?? "")
                    .Trim()
                    .ToLowerInvariant();

            string subCategory =
                (product.Subcategory ?? "")
                    .Trim()
                    .ToLowerInvariant();

            var dressCategories = new[]
            {
                "bridal dresses",
                "bridal wear",
                "groom dresses",
                "groom wear",
                "wedding guest wear",
                "dress"
            };

            if (dressCategories.Contains(category))
            {
                return true;
            }

            var dressSubCategories = new[]
            {
                "mehndi dress",
                "nikkah dress",
                "baraat dress",
                "walima dress",
                "bridal lehenga",
                "bridal gown",
                "bridal maxi",
                "sharara",
                "gharara",
                "pishwas",
                "saree",
                "frock",
                "party wear dress",
                "party dress",
                "maxi",
                "anarkali",
                "lehenga",
                "palazzo suit",
                "shalwar kameez",
                "men's suit",
                "men suit",
                "kurta pajama",
                "men's shalwar kameez",
                "men shalwar kameez",
                "boys sherwani",
                "boys kurta pajama",
                "boys waistcoat",
                "boys suit",
                "sherwani",
                "prince coat",
                "groom suit",
                "tuxedo",
                "bridal dress"
            };

            return dressSubCategories.Contains(subCategory);
        }


        // =========================================================
        // GET EFFECTIVE PRODUCT PRICE
        //
        // Priority:
        // 1. Accepted negotiation
        // 2. Active sale
        // 3. Original product price
        // =========================================================

        private async Task<(decimal Price, string PurchaseType, int? NegotiationId)>
            GetEffectivePrice(
                Product product,
                int customerId,
                int quantity = 1)
        {
            // -----------------------------------------------------
            // CHECK ACCEPTED NEGOTIATION
            // -----------------------------------------------------

            var acceptedNegotiation =
                await _context.Negotiations
                    .Where(n =>
                        n.ProductId == product.ProductId &&
                        n.CustomerId == customerId &&
                        n.AgreedPrice.HasValue &&
                        n.Status == "Accepted")
                    .OrderByDescending(
                        n => n.RespondedDate ?? n.CreatedDate)
                    .FirstOrDefaultAsync();

            if (acceptedNegotiation != null &&
                acceptedNegotiation.AgreedPrice.HasValue &&
                acceptedNegotiation.AgreedPrice.Value > 0)
            {
                return (
                    acceptedNegotiation.AgreedPrice.Value,
                    "Negotiated",
                    acceptedNegotiation.NegotiationId
                );
            }


            // -----------------------------------------------------
            // CHECK SALE
            // -----------------------------------------------------

            if (product.IsOnSale &&
                product.SalePrice.HasValue &&
                product.SalePrice.Value > 0 &&
                product.SalePrice.Value < product.Price)
            {
                return (
                    product.SalePrice.Value,
                    "Sale",
                    null
                );
            }


            // -----------------------------------------------------
            // NORMAL BUY
            // -----------------------------------------------------

            return (
                product.Price,
                "Buy",
                null
            );
        }
        // =========================================================
// CUSTOMER CHAT AUTHORIZATION
// =========================================================

private async Task<bool> CanCustomerChatWithUser(
    int customerId,
    int receiverId)
{
    // ---------------------------------------------
    // ADMIN CAN ALWAYS BE CONTACTED
    // ---------------------------------------------

    var receiver = await _context.Users
        .Include(u => u.Role)
        .FirstOrDefaultAsync(u =>
            u.UserId == receiverId);

    if (receiver == null)
    {
        return false;
    }

    if (receiver.Role?.RoleName == "Admin")
    {
        return true;
    }

    // ---------------------------------------------
    // CHECK CUSTOMER ORDERS
    // ---------------------------------------------

    var customerOrders =
        await _context.Orders
            .Where(o =>
                o.CustomerId == customerId &&
                o.OrderStatus != "Cancelled")
            .Select(o => new
            {
                o.ShopId,
                o.DeliveryId
            })
            .ToListAsync();

    // ---------------------------------------------
    // SHOPKEEPER
    // ---------------------------------------------

    if (receiver.Role?.RoleName == "Shopkeeper")
    {
        var shopkeeperShopIds =
            await _context.Shops
                .Where(s =>
                    s.ShopkeeperId == receiverId)
                .Select(s => s.ShopId)
                .ToListAsync();

        return customerOrders.Any(o =>
            shopkeeperShopIds.Contains(o.ShopId));
    }

    // ---------------------------------------------
    // DELIVERY BOY
    // ---------------------------------------------

    if (receiver.Role?.RoleName == "Delivery")
    {
        return customerOrders.Any(o =>
            o.DeliveryId == receiverId);
    }

    return false;
}

        // =========================================================
        // DASHBOARD
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            if (!IsCustomerLoggedIn())
                return CustomerLogin();

            int customerId =
                GetCustomerId()!.Value;

            var customer = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(
                    u => u.UserId == customerId
                );

            if (customer == null)
            {
                HttpContext.Session.Clear();

                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }

            if (!customer.Status)
            {
                HttpContext.Session.Clear();

                TempData["Error"] =
                    "Your customer account has been disabled by the administrator.";

                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }

            ViewBag.CustomerName =
                customer.Name;

            ViewBag.CustomerEmail =
                customer.Email;

            return View();
        }


        // =========================================================
        // SHOPS
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Shops()
        {
            if (!IsCustomerLoggedIn())
                return CustomerLogin();

            var shops = await _context.Shops
                .Include(s => s.Shopkeeper)
                .Where(s =>
                    s.Status &&
                    s.IsApproved)
                .OrderByDescending(
                    s => s.CreatedDate)
                .ToListAsync();

            return View(shops);
        }


        // =========================================================
        // SHOP DETAILS
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> ShopDetails(int id)
        {
            if (!IsCustomerLoggedIn())
                return CustomerLogin();

            var shop = await _context.Shops
                .Include(s => s.Shopkeeper)
                .FirstOrDefaultAsync(
                    s =>
                        s.ShopId == id &&
                        s.Status &&
                        s.IsApproved
                );

            if (shop == null)
            {
                TempData["Error"] =
                    "Shop not found or is currently unavailable.";

                return RedirectToAction("Shops");
            }

            var products = await _context.Products
                .Where(p =>
                    p.ShopId == id &&
                    p.Status &&
                    p.StockQuantity > 0)
                .OrderByDescending(
                    p => p.CreatedDate)
                .ToListAsync();

            ViewBag.Shop = shop;
            ViewBag.ShopId = id;

            return View(
                "ShopProducts",
                products
            );
        }


        // =========================================================
        // SHOP PRODUCTS
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> ShopProducts(int id)
        {
            if (!IsCustomerLoggedIn())
                return CustomerLogin();

            var shop = await _context.Shops
                .Include(s => s.Shopkeeper)
                .FirstOrDefaultAsync(
                    s =>
                        s.ShopId == id &&
                        s.Status &&
                        s.IsApproved
                );

            if (shop == null)
            {
                TempData["Error"] =
                    "Shop not found or is currently unavailable.";

                return RedirectToAction("Shops");
            }

            var products = await _context.Products
                .Where(p =>
                    p.ShopId == id &&
                    p.Status &&
                    p.StockQuantity > 0)
                .OrderByDescending(
                    p => p.CreatedDate)
                .ToListAsync();

            ViewBag.Shop = shop;
            ViewBag.ShopId = id;

            return View(products);
        }


        // =========================================================
        // PRODUCT DETAILS
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> ProductDetails(int id)
        {
            if (!IsCustomerLoggedIn())
                return CustomerLogin();

            var product = await _context.Products
                .Include(p => p.Shop)
                .ThenInclude(s => s!.Shopkeeper)
                .FirstOrDefaultAsync(
                    p =>
                        p.ProductId == id &&
                        p.Status
                );

            if (product == null)
            {
                TempData["Error"] =
                    "Product not found.";

                return RedirectToAction("Shops");
            }

            if (product.Shop == null ||
                !product.Shop.Status ||
                !product.Shop.IsApproved)
            {
                TempData["Error"] =
                    "This shop is currently unavailable.";

                return RedirectToAction("Shops");
            }
            var cartItems = GetCartItems();

ViewBag.NegotiatedAlreadyInCart =
    cartItems.Any(x =>
        x.ProductId == product.ProductId &&
        string.Equals(
            x.PurchaseType,
            "Negotiated",
            StringComparison.OrdinalIgnoreCase));

            return View(product);
        }
        // =========================================================
// NEGOTIATE PRODUCT
// =========================================================

[HttpGet]
public async Task<IActionResult> Negotiate(int productId)
{
    if (!IsCustomerLoggedIn())
        return CustomerLogin();

    int customerId = GetCustomerId()!.Value;

    var product = await _context.Products
        .Include(p => p.Shop)
        .FirstOrDefaultAsync(p =>
            p.ProductId == productId &&
            p.Status);

    if (product == null)
    {
        TempData["Error"] =
            "Product not found.";

        return RedirectToAction("Shops");
    }

    if (product.Shop == null ||
        !product.Shop.Status ||
        !product.Shop.IsApproved)
    {
        TempData["Error"] =
            "This shop is currently unavailable.";

        return RedirectToAction("Shops");
    }

    if (!product.AllowNegotiation)
    {
        TempData["Error"] =
            "Negotiation is not available for this product.";

        return RedirectToAction(
            "ProductDetails",
            new { id = productId }
        );
    }

    // ---------------------------------------------------------
    // CHECK EXISTING ACTIVE NEGOTIATION
    // ---------------------------------------------------------

    var existingNegotiation =
        await _context.Negotiations
            .Where(n =>
                n.ProductId == productId &&
                n.CustomerId == customerId &&
                (n.Status == "Pending" ||
                 n.Status == "CounterOffer"))
            .OrderByDescending(
                n => n.CreatedDate)
            .FirstOrDefaultAsync();

    if (existingNegotiation != null)
    {
        TempData["Error"] =
            "You already have an active negotiation for this product.";

        return RedirectToAction("MyNegotiations");
    }

    ViewBag.ShopName =
        product.Shop.ShopName;

    return View(product);
}
        // ============================================================
// MAKE NEGOTIATION OFFER
// ============================================================

[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> MakeNegotiation(
    int productId,
    decimal requestedPrice,
    string? customerMessage)
{
    if (!IsCustomerLoggedIn())
        return CustomerLogin();

    int customerId = GetCustomerId()!.Value;

    // --------------------------------------------------------
    // VALIDATE PRODUCT
    // --------------------------------------------------------

    var product = await _context.Products
        .Include(p => p.Shop)
        .FirstOrDefaultAsync(p =>
            p.ProductId == productId &&
            p.Status);

    if (product == null)
    {
        TempData["Error"] = "Product not found.";
        return RedirectToAction("MyNegotiations");
    }

    // --------------------------------------------------------
    // VALIDATE SHOP
    // --------------------------------------------------------

    if (product.Shop == null ||
        !product.Shop.Status ||
        !product.Shop.IsApproved)
    {
        TempData["Error"] =
            "This shop is currently unavailable.";

        return RedirectToAction("MyNegotiations");
    }

    // --------------------------------------------------------
    // CHECK NEGOTIATION ENABLED
    // --------------------------------------------------------

    if (!product.AllowNegotiation)
    {
        TempData["Error"] =
            "Negotiation is not available for this product.";

        return RedirectToAction(
            "ProductDetails",
            new { id = productId });
    }

    // --------------------------------------------------------
    // VALIDATE OFFER PRICE
    // --------------------------------------------------------

    if (requestedPrice <= 0)
    {
        TempData["Error"] =
            "Please enter a valid offer price.";

        return RedirectToAction(
            "ProductDetails",
            new { id = productId });
    }

    if (requestedPrice >= product.Price)
    {
        TempData["Error"] =
            "Your negotiation offer must be lower than the original price.";

        return RedirectToAction(
            "ProductDetails",
            new { id = productId });
    }

    // --------------------------------------------------------
    // CHECK ACTIVE NEGOTIATION
    // --------------------------------------------------------

    var existingNegotiation =
        await _context.Negotiations
            .FirstOrDefaultAsync(n =>
                n.ProductId == productId &&
                n.CustomerId == customerId &&
                (n.Status == "Pending" ||
                 n.Status == "CounterOffer"));

    if (existingNegotiation != null)
    {
        TempData["Error"] =
            "You already have an active negotiation for this product.";

        return RedirectToAction("MyNegotiations");
    }

    // --------------------------------------------------------
    // CREATE NEGOTIATION
    // --------------------------------------------------------

    var negotiation = new Negotiation
    {
        ProductId = product.ProductId,
        CustomerId = customerId,
        ShopkeeperId = product.Shop!.ShopkeeperId,

        OriginalPrice = product.Price,
        RequestedPrice = requestedPrice,

        CounterPrice = null,
        AgreedPrice = null,

        CustomerMessage =
            string.IsNullOrWhiteSpace(customerMessage)
                ? null
                : customerMessage.Trim(),

        ShopkeeperMessage = null,

        Status = "Pending",

        CreatedDate = DateTime.Now,
        RespondedDate = null
    };

    _context.Negotiations.Add(negotiation);

    await _context.SaveChangesAsync();

    TempData["Success"] =
        $"Your offer of Rs. {requestedPrice:N0} has been sent to the shopkeeper.";

    return RedirectToAction("MyNegotiations");
}
        // =====================================================
// MY NEGOTIATIONS
// =====================================================

[HttpGet]
public async Task<IActionResult> MyNegotiations()
{
    int? customerId = HttpContext.Session.GetInt32("CustomerId");

    if (customerId == null)
    {
        return RedirectToAction("Login", "Account");
    }

    var negotiations = await _context.Negotiations
        .Include(n => n.Product)
        .Include(n => n.Shopkeeper)
        .Where(n => n.CustomerId == customerId.Value)
        .OrderByDescending(n => n.CreatedDate)
        .ToListAsync();

    var customer = await _context.Users
        .FirstOrDefaultAsync(u => u.UserId == customerId.Value);

    ViewBag.CustomerName = customer?.Name ?? "Customer";
    ViewBag.ProfileImage = customer?.ProfileImage;

    ViewBag.PendingCount = negotiations
        .Count(n => n.Status == "Pending");

    ViewBag.CounterOfferCount = negotiations
        .Count(n => n.Status == "CounterOffer");

    ViewBag.AcceptedCount = negotiations
        .Count(n => n.Status == "Accepted");

    ViewBag.RejectedCount = negotiations
        .Count(n => n.Status == "Rejected");

    return View(negotiations);
}
       // =========================================================
// ADD TO CART
// =========================================================

[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> AddToCart(
    int productId,
    int quantity = 1,
    string? purchaseType = null)
{
    if (!IsCustomerLoggedIn())
        return CustomerLogin();

    int customerId = GetCustomerId()!.Value;

    if (quantity < 1)
        quantity = 1;

    // ---------------------------------------------------------
    // GET PRODUCT
    // ---------------------------------------------------------

    var product = await _context.Products
        .Include(p => p.Shop)
        .FirstOrDefaultAsync(p =>
            p.ProductId == productId &&
            p.Status);

    if (product == null)
    {
        TempData["Error"] = "Product not found.";
        return RedirectToAction("Shops");
    }

    // ---------------------------------------------------------
    // SHOP VALIDATION
    // ---------------------------------------------------------

    if (product.Shop == null ||
        !product.Shop.Status ||
        !product.Shop.IsApproved)
    {
        TempData["Error"] =
            "This shop is currently unavailable.";

        return RedirectToAction("Shops");
    }

    // ---------------------------------------------------------
    // STOCK VALIDATION
    // ---------------------------------------------------------

    if (product.StockQuantity <= 0)
    {
        TempData["Error"] =
            "This product is currently out of stock.";

        return RedirectToAction(
            "ProductDetails",
            new { id = productId });
    }

    if (quantity > product.StockQuantity)
    {
        TempData["Error"] =
            $"Only {product.StockQuantity} item(s) are available.";

        return RedirectToAction(
            "ProductDetails",
            new { id = productId });
    }


    // =========================================================
    // DETERMINE PURCHASE TYPE AND PRICE
    // =========================================================

    string requestedType =
        string.IsNullOrWhiteSpace(purchaseType)
            ? "Buy"
            : purchaseType.Trim();

    string selectedPurchaseType = "Buy";
    decimal selectedPrice = product.Price;
    int? negotiationId = null;


    // =========================================================
    // 1. RENT
    // =========================================================

    if (requestedType.Equals(
            "Rent",
            StringComparison.OrdinalIgnoreCase))
    {
        if (!product.IsAvailableForRent ||
            !product.RentPrice.HasValue ||
            product.RentPrice.Value <= 0)
        {
            TempData["Error"] =
                "This product is not available for rent.";

            return RedirectToAction(
                "ProductDetails",
                new { id = productId });
        }

        selectedPurchaseType = "Rent";

        selectedPrice =
            product.RentPrice.Value;

        negotiationId = null;
    }


    // =========================================================
    // 2. NEGOTIATED PRICE
    // =========================================================

    else if (requestedType.Equals(
                 "Negotiated",
                 StringComparison.OrdinalIgnoreCase))
    {
        var negotiation =
            await _context.Negotiations
                .Where(n =>
                    n.ProductId == productId &&
                    n.CustomerId == customerId &&
                    n.Status == "Accepted" &&
                    n.AgreedPrice.HasValue &&
                    n.AgreedPrice.Value > 0)
                .OrderByDescending(n =>
                    n.RespondedDate ??
                    n.CreatedDate)
                .FirstOrDefaultAsync();

        if (negotiation == null)
        {
            TempData["Error"] =
                "No accepted negotiated price is available for this product.";

            return RedirectToAction(
                "MyNegotiations");
        }

        // IMPORTANT:
        // Use the accepted negotiated price.
        // DO NOT change product.Price.

        selectedPurchaseType = "Negotiated";

        selectedPrice =
            negotiation.AgreedPrice!.Value;

        negotiationId =
            negotiation.NegotiationId;
    }


    // =========================================================
    // 3. SALE / DISCOUNT
    // =========================================================

    else if (requestedType.Equals(
                 "Sale",
                 StringComparison.OrdinalIgnoreCase))
    {
        if (!product.IsOnSale ||
            !product.SalePrice.HasValue ||
            product.SalePrice.Value <= 0 ||
            product.SalePrice.Value >= product.Price)
        {
            TempData["Error"] =
                "This product is not currently available at a sale price.";

            return RedirectToAction(
                "ProductDetails",
                new { id = productId });
        }

        selectedPurchaseType = "Sale";

        selectedPrice =
            product.SalePrice.Value;

        negotiationId = null;
    }


    // =========================================================
    // 4. NORMAL BUY
    // =========================================================

    else
    {
        selectedPurchaseType = "Buy";

        selectedPrice =
            product.Price;

        negotiationId = null;
    }


    // =========================================================
    // GET SESSION CART
    // =========================================================

    var cartItems = GetCartItems();


    // =========================================================
    // CHECK EXISTING ITEM
    // =========================================================

    var existingItem =
        cartItems.FirstOrDefault(x =>
            x.ProductId == productId &&
            string.Equals(
                x.PurchaseType ?? "Buy",
                selectedPurchaseType,
                StringComparison.OrdinalIgnoreCase));

if (existingItem != null)
{
    // =====================================================
    // NEGOTIATED PRODUCT CAN ONLY BE ADDED ONCE
    // =====================================================

    if (selectedPurchaseType.Equals(
            "Negotiated",
            StringComparison.OrdinalIgnoreCase))
    {
        TempData["Error"] =
            "This negotiated product has already been added to your cart. It can only be added once.";

        return RedirectToAction(
            "ProductDetails",
            new { id = productId });
    }


    // =====================================================
    // OTHER PURCHASE TYPES
    // =====================================================

    int newQuantity =
        existingItem.Quantity + quantity;

    if (newQuantity > product.StockQuantity)
    {
        TempData["Error"] =
            $"You can only add up to {product.StockQuantity} item(s) of {product.ProductName}.";

        return RedirectToAction(
            "ProductDetails",
            new { id = productId });
    }

    existingItem.Quantity =
        newQuantity;

    existingItem.ProductName =
        product.ProductName;

    existingItem.Price =
        selectedPrice;

    existingItem.PurchaseType =
        selectedPurchaseType;

    existingItem.NegotiationId =
        negotiationId;
}


    // =========================================================
    // NEW ITEM
    // =========================================================

    else
    {
        cartItems.Add(
            new CustomerCartItem
            {
                ProductId =
                    product.ProductId,

                ProductName =
                    product.ProductName,

                Price =
                    selectedPrice,

                Quantity =
                    quantity,

                CustomMeasurements =
                    null,

                PurchaseType =
                    selectedPurchaseType,

                NegotiationId =
                    negotiationId
            });
    }


    // =========================================================
    // SAVE CART
    // =========================================================

    SaveCartItems(cartItems);

    TempData["Success"] =
        $"{product.ProductName} added to cart successfully.";

    return RedirectToAction("Cart");
}


      // =========================================================
// CART
// =========================================================

[HttpGet]
public async Task<IActionResult> Cart()
{
    if (!IsCustomerLoggedIn())
        return CustomerLogin();

    int customerId =
        GetCustomerId()!.Value;

    var cartItems =
        GetCartItems();

    // ---------------------------------------------------------
    // EMPTY CART
    // ---------------------------------------------------------

    if (!cartItems.Any())
    {
        ViewBag.CartItems =
            cartItems;

        return View(
            new List<Product>());
    }


    // ---------------------------------------------------------
    // GET PRODUCT IDS
    // ---------------------------------------------------------

    var productIds =
        cartItems
            .Select(x => x.ProductId)
            .Distinct()
            .ToList();


    // ---------------------------------------------------------
    // GET PRODUCTS
    // ---------------------------------------------------------

    var products =
        await _context.Products
            .Include(p => p.Shop)
            .Where(p =>
                productIds.Contains(p.ProductId) &&
                p.Status)
            .ToListAsync();


    // ---------------------------------------------------------
    // REMOVE PRODUCTS THAT NO LONGER EXIST
    // ---------------------------------------------------------

    var validIds =
        products
            .Select(p => p.ProductId)
            .ToHashSet();

    cartItems =
        cartItems
            .Where(x =>
                validIds.Contains(x.ProductId))
            .ToList();


    // ---------------------------------------------------------
    // ITEMS TO REMOVE
    // ---------------------------------------------------------

    var itemsToRemove =
        new List<CustomerCartItem>();


    // =========================================================
    // REFRESH CART PRICES
    // =========================================================

    foreach (var item in cartItems)
    {
        var product =
            products.FirstOrDefault(p =>
                p.ProductId == item.ProductId);

        if (product == null)
        {
            itemsToRemove.Add(item);
            continue;
        }


        // -----------------------------------------------------
        // UPDATE PRODUCT NAME
        // -----------------------------------------------------

        item.ProductName =
            product.ProductName;


        string purchaseType =
            string.IsNullOrWhiteSpace(
                item.PurchaseType)
                    ? "Buy"
                    : item.PurchaseType.Trim();


        // =====================================================
        // RENT
        // =====================================================

        if (purchaseType.Equals(
                "Rent",
                StringComparison.OrdinalIgnoreCase))
        {
            if (product.IsAvailableForRent &&
                product.RentPrice.HasValue &&
                product.RentPrice.Value > 0)
            {
                item.Price =
                    product.RentPrice.Value;

                item.PurchaseType =
                    "Rent";

                item.NegotiationId =
                    null;
            }
            else
            {
                itemsToRemove.Add(item);
            }

            continue;
        }


        // =====================================================
        // NEGOTIATED
        // =====================================================

        if (purchaseType.Equals(
                "Negotiated",
                StringComparison.OrdinalIgnoreCase))
        {
            Negotiation? negotiation = null;


            // -------------------------------------------------
            // First try the negotiation stored in cart
            // -------------------------------------------------

            if (item.NegotiationId.HasValue)
            {
                negotiation =
                    await _context.Negotiations
                        .FirstOrDefaultAsync(n =>
                            n.NegotiationId ==
                                item.NegotiationId.Value &&

                            n.ProductId ==
                                product.ProductId &&

                            n.CustomerId ==
                                customerId &&

                            n.Status == "Accepted" &&

                            n.AgreedPrice.HasValue &&

                            n.AgreedPrice.Value > 0);
            }


            // -------------------------------------------------
            // If not found, get latest accepted negotiation
            // -------------------------------------------------

            if (negotiation == null)
            {
                negotiation =
                    await _context.Negotiations
                        .Where(n =>
                            n.ProductId ==
                                product.ProductId &&

                            n.CustomerId ==
                                customerId &&

                            n.Status == "Accepted" &&

                            n.AgreedPrice.HasValue &&

                            n.AgreedPrice.Value > 0)
                        .OrderByDescending(n =>
                            n.RespondedDate ??
                            n.CreatedDate)
                        .FirstOrDefaultAsync();
            }


            // -------------------------------------------------
            // ACCEPTED NEGOTIATION FOUND
            // -------------------------------------------------

            if (negotiation != null &&
                negotiation.AgreedPrice.HasValue)
            {
                // IMPORTANT:
                // Show the agreed negotiated price.
                //
                // Example:
                // Original = Rs. 80,000
                // Agreed   = Rs. 65,000
                //
                // Cart must show Rs. 65,000.

                item.Price =
                    negotiation.AgreedPrice.Value;

                item.PurchaseType =
                    "Negotiated";

                item.NegotiationId =
                    negotiation.NegotiationId;
            }
            else
            {
                // -------------------------------------------------
                // Negotiation is no longer valid.
                // Remove it rather than silently changing it
                // into a normal Buy price.
                // -------------------------------------------------

                itemsToRemove.Add(item);
            }

            continue;
        }


        // =====================================================
        // SALE / DISCOUNT
        // =====================================================

        if (purchaseType.Equals(
                "Sale",
                StringComparison.OrdinalIgnoreCase))
        {
            if (product.IsOnSale &&
                product.SalePrice.HasValue &&
                product.SalePrice.Value > 0 &&
                product.SalePrice.Value < product.Price)
            {
                item.Price =
                    product.SalePrice.Value;

                item.PurchaseType =
                    "Sale";

                item.NegotiationId =
                    null;
            }
            else
            {
                // Sale has ended.
                // Remove the sale item instead of charging
                // the customer the old discounted price.

                itemsToRemove.Add(item);
            }

            continue;
        }


        // =====================================================
        // NORMAL BUY
        // =====================================================

        item.Price =
            product.Price;

        item.PurchaseType =
            "Buy";

        item.NegotiationId =
            null;
    }


    // =========================================================
    // REMOVE INVALID ITEMS
    // =========================================================

    foreach (var item in itemsToRemove)
    {
        cartItems.Remove(item);
    }


    // =========================================================
    // SAVE UPDATED CART
    // =========================================================

    SaveCartItems(cartItems);


    // =========================================================
    // SEND CART DATA TO VIEW
    // =========================================================

    ViewBag.CartItems =
        cartItems;


    return View(products);
}


        // =========================================================
        // UPDATE CART QUANTITY
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateCartQuantity(
            int productId,
            int quantity)
        {
            if (!IsCustomerLoggedIn())
                return CustomerLogin();

            if (quantity < 1)
                quantity = 1;

            var product =
                await _context.Products
                    .FirstOrDefaultAsync(
                        p =>
                            p.ProductId ==
                                productId &&
                            p.Status
                    );

            if (product == null)
            {
                TempData["Error"] =
                    "Product not found.";

                return RedirectToAction("Cart");
            }

            if (quantity >
                product.StockQuantity)
            {
                TempData["Error"] =
                    $"Only {product.StockQuantity} item(s) are available.";

                return RedirectToAction("Cart");
            }

            var cartItems =
                GetCartItems();

            var item =
                cartItems.FirstOrDefault(
                    x =>
                        x.ProductId ==
                        productId
                );

            if (item == null)
            {
                TempData["Error"] =
                    "Product is not in your cart.";

                return RedirectToAction("Cart");
            }

            item.Quantity =
                quantity;

            item.ProductName =
                product.ProductName;


            // -----------------------------------------------------
            // REFRESH PRICE
            // -----------------------------------------------------

            string purchaseType =
                item.PurchaseType ?? "Buy";

            if (purchaseType.Equals(
                    "Rent",
                    StringComparison.OrdinalIgnoreCase))
            {
                if (product.IsAvailableForRent &&
                    product.RentPrice.HasValue)
                {
                    item.Price =
                        product.RentPrice.Value;
                }
            }
            else if (purchaseType.Equals(
                    "Negotiated",
                    StringComparison.OrdinalIgnoreCase))
            {
                int customerId =
                    GetCustomerId()!.Value;

                var negotiation =
                    await _context.Negotiations
                        .Where(n =>
                            n.ProductId ==
                                productId &&
                            n.CustomerId ==
                                customerId &&
                            n.Status ==
                                "Accepted" &&
                            n.AgreedPrice.HasValue)
                        .OrderByDescending(
                            n =>
                                n.RespondedDate ??
                                n.CreatedDate)
                        .FirstOrDefaultAsync();

                if (negotiation != null &&
                    negotiation.AgreedPrice.HasValue)
                {
                    item.Price =
                        negotiation.AgreedPrice.Value;

                    item.NegotiationId =
                        negotiation.NegotiationId;
                }
                else
                {
                    item.PurchaseType =
                        "Buy";

                    item.NegotiationId =
                        null;

                    item.Price =
                        product.IsOnSale &&
                        product.SalePrice.HasValue &&
                        product.SalePrice.Value >
                            0 &&
                        product.SalePrice.Value <
                            product.Price
                            ? product.SalePrice.Value
                            : product.Price;
                }
            }
            else if (
                product.IsOnSale &&
                product.SalePrice.HasValue &&
                product.SalePrice.Value > 0 &&
                product.SalePrice.Value <
                    product.Price)
            {
                item.Price =
                    product.SalePrice.Value;

                item.PurchaseType =
                    "Sale";
            }
            else
            {
                item.Price =
                    product.Price;

                item.PurchaseType =
                    "Buy";
            }

            SaveCartItems(cartItems);

            TempData["Success"] =
                "Cart quantity updated.";

            return RedirectToAction("Cart");
        }


        // =========================================================
        // REMOVE FROM CART
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RemoveFromCart(int id)
        {
            if (!IsCustomerLoggedIn())
                return CustomerLogin();

            var cartItems =
                GetCartItems();

            cartItems.RemoveAll(
                x =>
                    x.ProductId ==
                    id
            );

            SaveCartItems(cartItems);

            TempData["Success"] =
                "Product removed from cart.";

            return RedirectToAction("Cart");
        }


        // =========================================================
        // CUSTOMIZE PRODUCT
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> CustomizeProduct(
            int id)
        {
            if (!IsCustomerLoggedIn())
                return CustomerLogin();

            var cartItems =
                GetCartItems();

            var cartItem =
                cartItems.FirstOrDefault(
                    x =>
                        x.ProductId ==
                        id
                );

            if (cartItem == null)
            {
                TempData["Error"] =
                    "Please add this product to your cart first.";

                return RedirectToAction("Cart");
            }

            var product =
                await _context.Products
                    .Include(p => p.Shop)
                    .FirstOrDefaultAsync(
                        p =>
                            p.ProductId ==
                                id &&
                            p.Status
                    );

            if (product == null)
            {
                TempData["Error"] =
                    "Product not found.";

                return RedirectToAction("Cart");
            }

            if (product.Shop == null ||
                !product.Shop.Status ||
                !product.Shop.IsApproved)
            {
                TempData["Error"] =
                    "This shop is currently unavailable.";

                return RedirectToAction("Cart");
            }

            if (!IsDressProduct(product))
            {
                TempData["Error"] =
                    "Custom measurements are only available for dress products.";

                return RedirectToAction("Cart");
            }

            return View(product);
        }


        // =========================================================
        // SAVE CUSTOM MEASUREMENTS
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult>
            SaveCustomMeasurements(
                int productId,
                string customMeasurements)
        {
            if (!IsCustomerLoggedIn())
                return CustomerLogin();

            if (string.IsNullOrWhiteSpace(
                customMeasurements))
            {
                TempData["Error"] =
                    "Please enter your measurements.";

                return RedirectToAction(
                    "CustomizeProduct",
                    new { id = productId }
                );
            }

            var product =
                await _context.Products
                    .Include(p => p.Shop)
                    .FirstOrDefaultAsync(
                        p =>
                            p.ProductId ==
                                productId &&
                            p.Status
                    );

            if (product == null)
            {
                TempData["Error"] =
                    "Product not found.";

                return RedirectToAction("Cart");
            }

            if (!IsDressProduct(product))
            {
                TempData["Error"] =
                    "Custom measurements are only available for dresses.";

                return RedirectToAction("Cart");
            }

            if (product.Shop == null ||
                !product.Shop.Status ||
                !product.Shop.IsApproved)
            {
                TempData["Error"] =
                    "This shop is currently unavailable.";

                return RedirectToAction("Cart");
            }

            var cartItems =
                GetCartItems();

            var cartItem =
                cartItems.FirstOrDefault(
                    x =>
                        x.ProductId ==
                        productId
                );

            if (cartItem == null)
            {
                TempData["Error"] =
                    "This product is not in your cart.";

                return RedirectToAction("Cart");
            }

            cartItem.CustomMeasurements =
                customMeasurements.Trim();

            SaveCartItems(cartItems);

            TempData["Success"] =
                "Custom measurements saved successfully.";

            return RedirectToAction("Cart");
        }


        // =========================================================
        // REMOVE CUSTOMIZATION
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RemoveCustomMeasurements(
            int productId)
        {
            if (!IsCustomerLoggedIn())
                return CustomerLogin();

            var cartItems =
                GetCartItems();

            var item =
                cartItems.FirstOrDefault(
                    x =>
                        x.ProductId ==
                        productId
                );

            if (item != null)
            {
                item.CustomMeasurements =
                    null;

                SaveCartItems(cartItems);

                TempData["Success"] =
                    "Custom measurements removed.";
            }

            return RedirectToAction("Cart");
        }


        // =========================================================
        // CHECKOUT GET
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            if (!IsCustomerLoggedIn())
                return CustomerLogin();

            int customerId =
                GetCustomerId()!.Value;


            // -----------------------------------------------------
            // CUSTOMER
            // -----------------------------------------------------

            var customer =
                await _context.Users
                    .FirstOrDefaultAsync(
                        u =>
                            u.UserId ==
                            customerId
                    );

            if (customer == null)
            {
                HttpContext.Session.Clear();

                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }

            if (!customer.Status)
            {
                HttpContext.Session.Clear();

                TempData["Error"] =
                    "Your account has been disabled.";

                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }


            // -----------------------------------------------------
            // CART
            // -----------------------------------------------------

            var cartItems =
                GetCartItems();

            if (!cartItems.Any())
            {
                TempData["Error"] =
                    "Your cart is empty.";

                return RedirectToAction("Cart");
            }


            var productIds =
                cartItems
                    .Select(x => x.ProductId)
                    .Distinct()
                    .ToList();


            // -----------------------------------------------------
            // PRODUCTS
            // -----------------------------------------------------

            var products =
                await _context.Products
                    .Include(p => p.Shop)
                    .Where(
                        p =>
                            productIds.Contains(
                                p.ProductId
                            ) &&
                            p.Status
                    )
                    .ToListAsync();


            if (products.Count !=
                productIds.Count)
            {
                TempData["Error"] =
                    "One or more products in your cart are no longer available.";

                return RedirectToAction("Cart");
            }


            // -----------------------------------------------------
            // STOCK
            // -----------------------------------------------------

            foreach (var item in cartItems)
            {
                var product =
                    products.First(
                        p =>
                            p.ProductId ==
                            item.ProductId
                    );

                if (item.Quantity < 1)
                {
                    TempData["Error"] =
                        "Invalid cart quantity.";

                    return RedirectToAction("Cart");
                }

                if (item.Quantity >
                    product.StockQuantity)
                {
                    TempData["Error"] =
                        $"Only {product.StockQuantity} unit(s) of {product.ProductName} are available.";

                    return RedirectToAction("Cart");
                }
            }


            // -----------------------------------------------------
            // SAME SHOP CHECK
            // -----------------------------------------------------

            int shopId =
                products.First().ShopId;

            if (!products.All(
                p =>
                    p.ShopId ==
                    shopId))
            {
                TempData["Error"] =
                    "You can only place an order from one shop at a time.";

                return RedirectToAction("Cart");
            }


            // -----------------------------------------------------
            // SHOP
            // -----------------------------------------------------

            var shop =
                products.First().Shop;

            if (shop == null ||
                !shop.Status ||
                !shop.IsApproved)
            {
                TempData["Error"] =
                    "The selected shop is currently unavailable.";

                return RedirectToAction("Shops");
            }


            // -----------------------------------------------------
            // REFRESH CART PRICES
            //
            // IMPORTANT:
            // Checkout does NOT trust cart Price.
            // Price is recalculated from database.
            // -----------------------------------------------------

            decimal productTotal = 0m;

            foreach (var item in cartItems)
            {
                var product =
                    products.First(
                        p =>
                            p.ProductId ==
                            item.ProductId
                    );

                string purchaseType =
                    item.PurchaseType ??
                    "Buy";

                decimal finalUnitPrice;


                // RENT
                if (purchaseType.Equals(
                        "Rent",
                        StringComparison.OrdinalIgnoreCase))
                {
                    if (!product.IsAvailableForRent ||
                        !product.RentPrice.HasValue ||
                        product.RentPrice.Value <= 0)
                    {
                        TempData["Error"] =
                            $"Rental option for {product.ProductName} is no longer available.";

                        return RedirectToAction("Cart");
                    }

                    finalUnitPrice =
                        product.RentPrice.Value;
                }


                // NEGOTIATED
                else if (purchaseType.Equals(
                    "Negotiated",
                    StringComparison.OrdinalIgnoreCase))
                {
                    var negotiation =
                        await _context.Negotiations
                            .Where(n =>
                                n.ProductId ==
                                    product.ProductId &&
                                n.CustomerId ==
                                    customerId &&
                                n.Status ==
                                    "Accepted" &&
                                n.AgreedPrice.HasValue)
                            .OrderByDescending(
                                n =>
                                    n.RespondedDate ??
                                    n.CreatedDate)
                            .FirstOrDefaultAsync();

                    if (negotiation == null ||
                        !negotiation.AgreedPrice.HasValue ||
                        negotiation.AgreedPrice.Value <= 0)
                    {
                        TempData["Error"] =
                            $"The negotiated price for {product.ProductName} is no longer valid.";

                        return RedirectToAction("Cart");
                    }

                    finalUnitPrice =
                        negotiation.AgreedPrice.Value;

                    item.NegotiationId =
                        negotiation.NegotiationId;
                }


                // SALE
                else if (
                    product.IsOnSale &&
                    product.SalePrice.HasValue &&
                    product.SalePrice.Value > 0 &&
                    product.SalePrice.Value <
                        product.Price)
                {
                    finalUnitPrice =
                        product.SalePrice.Value;

                    item.PurchaseType =
                        "Sale";
                }


                // NORMAL BUY
                else
                {
                    finalUnitPrice =
                        product.Price;

                    item.PurchaseType =
                        "Buy";
                }

                item.Price =
                    finalUnitPrice;

                productTotal +=
                    finalUnitPrice *
                    item.Quantity;
            }

            SaveCartItems(cartItems);


            if (productTotal <= 0)
            {
                TempData["Error"] =
                    "Invalid cart amount.";

                return RedirectToAction("Cart");
            }


            // -----------------------------------------------------
            // CHARGES
            // -----------------------------------------------------

            decimal deliveryCharges =
                DeliveryCharge;

            decimal serviceFee =
                Math.Round(
                    productTotal *
                    ServiceFeeRate,
                    2
                );

            decimal totalAmount =
                productTotal +
                deliveryCharges +
                serviceFee;


            // IMPORTANT:
            // Shopkeeper receives FULL PRODUCT TOTAL.
            // Service fee is NOT deducted from shopkeeper.
            decimal shopkeeperAmount =
                productTotal;


            // -----------------------------------------------------
            // VIEW DATA
            // -----------------------------------------------------

            ViewBag.CustomerName =
                customer.Name;

            ViewBag.CustomerPhone =
                customer.Phone;

            ViewBag.CustomerAddress =
                customer.Address;

            ViewBag.DeliveryCharges =
                deliveryCharges;

            ViewBag.ProductTotal =
                productTotal;

            ViewBag.ServiceFee =
                serviceFee;

            ViewBag.TotalAmount =
                totalAmount;

            ViewBag.ShopkeeperAmount =
                shopkeeperAmount;

            ViewBag.CartItems =
                cartItems;

            ViewBag.Products =
                products;

            ViewBag.Shop =
                shop;


            return View(products);
        }


        // =========================================================
        // PLACE ORDER
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(
            string? cartData,
            string? deliveryCity,
            string deliveryAddress,
            string? notes,
            string paymentMethod,
            string? transactionId,
            IFormFile? transactionImage)
        {
            if (!IsCustomerLoggedIn())
                return CustomerLogin();

            int customerId =
                GetCustomerId()!.Value;


            // -----------------------------------------------------
            // CUSTOMER
            // -----------------------------------------------------

            var customer =
                await _context.Users
                    .FirstOrDefaultAsync(
                        u =>
                            u.UserId ==
                            customerId
                    );

            if (customer == null)
            {
                HttpContext.Session.Clear();
                TempData["Error"] =
                   "Customer account could not be found.";
                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }

            if (!customer.Status)
            {
                TempData["Error"] =
                    "Your customer account has been disabled.";

                return RedirectToAction(
                    "Dashboard"
                );
            }
            // -----------------------------------------------------
// CUSTOMER NAME VALIDATION
// -----------------------------------------------------

if (string.IsNullOrWhiteSpace(customer.Name))
{
    TempData["Error"] =
        "Your account name is missing. Please update your profile before placing an order.";

    return RedirectToAction(
        "Profile",
        "Account"
    );
}

// -----------------------------------------------------
// CUSTOMER PHONE VALIDATION
// -----------------------------------------------------

if (string.IsNullOrWhiteSpace(customer.Phone))
{
    TempData["Error"] =
        "Your phone number is required before placing an order. Please update your profile.";

    return RedirectToAction(
        "Profile",
        "Account"
    );
}


// -----------------------------------------------------
// CUSTOMER ADDRESS VALIDATION
// -----------------------------------------------------

if (string.IsNullOrWhiteSpace(customer.Address))
{
    TempData["Error"] =
        "Your account address is required before placing an order. Please update your profile.";

    return RedirectToAction(
        "Profile",
        "Account"
    );
}
// -----------------------------------------------------
// CUSTOMER RAWALPINDI VALIDATION
// -----------------------------------------------------
//
// Customer's registered address must be based in Rawalpindi.
// This prevents customers registered with another city
// from placing an order.
//

string customerAddress =
    customer.Address.Trim();

if (!customerAddress.Contains(
        "Rawalpindi",
        StringComparison.OrdinalIgnoreCase))
{
    TempData["Error"] =
        "Orders can currently be placed only by customers based in Rawalpindi.";

    return RedirectToAction("Checkout");
}

// -----------------------------------------------------
// DELIVERY CITY VALIDATION
// -----------------------------------------------------
//
// Only Rawalpindi is currently supported.
//

if (string.IsNullOrWhiteSpace(deliveryCity))
{
    TempData["Error"] =
        "Please select your delivery city.";

    return RedirectToAction("Checkout");
}

deliveryCity = deliveryCity.Trim();

if (!deliveryCity.Equals(
        "Rawalpindi",
        StringComparison.OrdinalIgnoreCase))
{
    TempData["Error"] =
        "Orders can currently be placed only for delivery within Rawalpindi.";

    return RedirectToAction("Checkout");
}

// Always save the official city name.
deliveryCity = "Rawalpindi";

           // -----------------------------------------------------
// DELIVERY ADDRESS
// -----------------------------------------------------

if (string.IsNullOrWhiteSpace(deliveryAddress))
{
    TempData["Error"] =
        "Please enter your complete delivery address.";

    return RedirectToAction("Checkout");
}

deliveryAddress = deliveryAddress.Trim();

if (deliveryAddress.Length < 10)
{
    TempData["Error"] =
        "Please enter a complete delivery address.";

    return RedirectToAction("Checkout");
}

if (deliveryAddress.Length > 500)
{
    TempData["Error"] =
        "Delivery address cannot exceed 500 characters.";

    return RedirectToAction("Checkout");
}




if (!deliveryAddress.Contains(
        "Rawalpindi",
        StringComparison.OrdinalIgnoreCase))
{
    TempData["Error"] =
        "Please enter a delivery address located in Rawalpindi.";

    return RedirectToAction("Checkout");
}

            // -----------------------------------------------------
            // PAYMENT METHOD
            // -----------------------------------------------------

            if (string.IsNullOrWhiteSpace(
                paymentMethod))
            {
                TempData["Error"] =
                    "Please select a payment method.";

                return RedirectToAction("Checkout");
            }

            paymentMethod =
                paymentMethod.Trim();

            bool validPaymentMethod =
                AllowedPaymentMethods.Any(
                    x =>
                        x.Equals(
                            paymentMethod,
                            StringComparison.OrdinalIgnoreCase)
                );

            if (!validPaymentMethod)
            {
                TempData["Error"] =
                    "Invalid payment method.";

                return RedirectToAction("Checkout");
            }


            // -----------------------------------------------------
            // SESSION CART
            //
            // cartData is deliberately ignored.
            // Browser values are not trusted.
            // -----------------------------------------------------

            var cartItems =
                GetCartItems();

            if (!cartItems.Any())
            {
                TempData["Error"] =
                    "Your cart is empty.";

                return RedirectToAction("Cart");
            }


            // -----------------------------------------------------
            // PRODUCTS
            // -----------------------------------------------------

            var productIds =
                cartItems
                    .Select(x => x.ProductId)
                    .Distinct()
                    .ToList();

            var products =
                await _context.Products
                    .Include(p => p.Shop)
                    .Where(
                        p =>
                            productIds.Contains(
                                p.ProductId
                            ) &&
                            p.Status
                    )
                    .ToListAsync();


            if (products.Count !=
                productIds.Count)
            {
                TempData["Error"] =
                    "One or more products in your cart are no longer available.";

                return RedirectToAction("Cart");
            }


            // -----------------------------------------------------
            // SAME SHOP
            // -----------------------------------------------------

            int shopId =
                products.First().ShopId;

            if (!products.All(
                p =>
                    p.ShopId ==
                    shopId))
            {
                TempData["Error"] =
                    "You can only place an order from one shop at a time.";

                return RedirectToAction("Cart");
            }


            // -----------------------------------------------------
            // SHOP
            // -----------------------------------------------------

            var shop =
                products.First().Shop;

            if (shop == null ||
                !shop.Status ||
                !shop.IsApproved)
            {
                TempData["Error"] =
                    "The selected shop is currently unavailable.";

                return RedirectToAction("Shops");
            }


            // -----------------------------------------------------
            // STOCK + FINAL PRICE
            // -----------------------------------------------------

            decimal productTotal = 0m;

            var finalPrices =
                new Dictionary<int, decimal>();

            var finalPurchaseTypes =
                new Dictionary<int, string>();

            var finalNegotiationIds =
                new Dictionary<int, int?>();


            foreach (var item in cartItems)
            {
                if (item.Quantity < 1)
                {
                    TempData["Error"] =
                        "Invalid product quantity.";

                    return RedirectToAction("Cart");
                }

                var product =
                    products.First(
                        p =>
                            p.ProductId ==
                            item.ProductId
                    );

                if (item.Quantity >
                    product.StockQuantity)
                {
                    TempData["Error"] =
                        $"Only {product.StockQuantity} unit(s) of {product.ProductName} are available.";

                    return RedirectToAction("Cart");
                }


                string purchaseType =
                    item.PurchaseType ??
                    "Buy";

                decimal finalUnitPrice;
                int? negotiationId = null;


                // -------------------------------------------------
                // RENT
                // -------------------------------------------------

                if (purchaseType.Equals(
                        "Rent",
                        StringComparison.OrdinalIgnoreCase))
                {
                    if (!product.IsAvailableForRent ||
                        !product.RentPrice.HasValue ||
                        product.RentPrice.Value <= 0)
                    {
                        TempData["Error"] =
                            $"Rental option for {product.ProductName} is no longer available.";

                        return RedirectToAction("Cart");
                    }

                    finalUnitPrice =
                        product.RentPrice.Value;
                }


                // -------------------------------------------------
                // NEGOTIATED
                // -------------------------------------------------

                else if (purchaseType.Equals(
                    "Negotiated",
                    StringComparison.OrdinalIgnoreCase))
                {
                    var negotiation =
                        await _context.Negotiations
                            .Where(n =>
                                n.ProductId ==
                                    product.ProductId &&
                                n.CustomerId ==
                                    customerId &&
                                n.Status ==
                                    "Accepted" &&
                                n.AgreedPrice.HasValue)
                            .OrderByDescending(
                                n =>
                                    n.RespondedDate ??
                                    n.CreatedDate)
                            .FirstOrDefaultAsync();

                    if (negotiation == null ||
                        !negotiation.AgreedPrice.HasValue ||
                        negotiation.AgreedPrice.Value <= 0)
                    {
                        TempData["Error"] =
                            $"The negotiated price for {product.ProductName} is no longer valid.";

                        return RedirectToAction("Cart");
                    }

                    finalUnitPrice =
                        negotiation.AgreedPrice.Value;

                    negotiationId =
                        negotiation.NegotiationId;
                }


                // -------------------------------------------------
                // SALE
                // -------------------------------------------------

                else if (
                    product.IsOnSale &&
                    product.SalePrice.HasValue &&
                    product.SalePrice.Value > 0 &&
                    product.SalePrice.Value <
                        product.Price)
                {
                    finalUnitPrice =
                        product.SalePrice.Value;

                    purchaseType =
                        "Sale";
                }


                // -------------------------------------------------
                // NORMAL BUY
                // -------------------------------------------------

                else
                {
                    finalUnitPrice =
                        product.Price;

                    purchaseType =
                        "Buy";
                }


                finalPrices[
                    product.ProductId] =
                    finalUnitPrice;

                finalPurchaseTypes[
                    product.ProductId] =
                    purchaseType;

                finalNegotiationIds[
                    product.ProductId] =
                    negotiationId;

                productTotal +=
                    finalUnitPrice *
                    item.Quantity;
            }


            if (productTotal <= 0)
            {
                TempData["Error"] =
                    "Invalid cart amount.";

                return RedirectToAction("Cart");
            }


            // -----------------------------------------------------
            // CHARGES
            // -----------------------------------------------------

            decimal deliveryCharges =
                DeliveryCharge;

            decimal serviceFee =
                Math.Round(
                    productTotal *
                    ServiceFeeRate,
                    2
                );

            decimal totalAmount =
                productTotal +
                deliveryCharges +
                serviceFee;


            // IMPORTANT:
            // Admin keeps service fee.
            // Shopkeeper receives full product total.
            decimal shopkeeperAmount =
                productTotal;


            // -----------------------------------------------------
            // PAYMENT
            // -----------------------------------------------------

            bool paymentReceived =
                false;

            string paymentStatus =
                "Pending";

            string? savedTransactionImage =
                null;


            // =====================================================
            // COD
            // =====================================================

            if (paymentMethod.Equals(
                "Cash on Delivery",
                StringComparison.OrdinalIgnoreCase))
            {
                paymentReceived =
                    false;

                paymentStatus =
                    "Pending";

                transactionId =
                    null;
            }


            // =====================================================
            // ONLINE PAYMENT
            // =====================================================

            else
            {
                if (string.IsNullOrWhiteSpace(
                    transactionId))
                {
                    TempData["Error"] =
                        "Please enter the transaction ID.";

                    return RedirectToAction("Checkout");
                }

                transactionId =
                    transactionId.Trim();


                if (transactionImage == null ||
                    transactionImage.Length == 0)
                {
                    TempData["Error"] =
                        "Please upload the payment screenshot.";

                    return RedirectToAction("Checkout");
                }


                // -------------------------------------------------
                // PAYMENT IMAGE VALIDATION
                // -------------------------------------------------

                string extension =
                    Path.GetExtension(
                        transactionImage.FileName)
                    .ToLowerInvariant();

                if (!AllowedPaymentImageExtensions.Contains(
                    extension))
                {
                    TempData["Error"] =
                        "Only JPG, JPEG, PNG and WEBP payment screenshots are allowed.";

                    return RedirectToAction("Checkout");
                }

                if (transactionImage.Length >
                    5 * 1024 * 1024)
                {
                    TempData["Error"] =
                        "Payment screenshot must be 5 MB or smaller.";

                    return RedirectToAction("Checkout");
                }


                // -------------------------------------------------
                // SAVE PAYMENT SCREENSHOT
                // -------------------------------------------------

                string uploadsFolder =
                    Path.Combine(
                        _environment.WebRootPath,
                        "uploads"
                    );

                if (!Directory.Exists(
                    uploadsFolder))
                {
                    Directory.CreateDirectory(
                        uploadsFolder
                    );
                }

                string fileName =
                    "payment_" +
                    Guid.NewGuid().ToString("N") +
                    extension;

                string filePath =
                    Path.Combine(
                        uploadsFolder,
                        fileName
                    );

                using (
                    var stream =
                        new FileStream(
                            filePath,
                            FileMode.Create
                        ))
                {
                    await transactionImage
                        .CopyToAsync(stream);
                }

                savedTransactionImage =
                    "/uploads/" +
                    fileName;

                paymentReceived =
                    false;

                paymentStatus =
                    "Pending";
            }


            // =====================================================
            // CREATE ORDER
            // =====================================================

            var order =
                new Order
                {
                    CustomerId =
                        customerId,

                    ShopId =
                        shopId,

                    DeliveryId =
                        null,

                    OrderStatus =
                        "Pending",


                    DeliveryCity =
                        deliveryCity,
                    DeliveryAddress =
                        deliveryAddress.Trim(),

                    Notes =
                        string.IsNullOrWhiteSpace(
                            notes)
                            ? null
                            : notes.Trim(),

                    ProductTotal =
                        productTotal,

                    DeliveryCharges =
                        deliveryCharges,

                    ServiceFee =
                        serviceFee,

                    TotalAmount =
                        totalAmount,

                    // FULL PRODUCT PRICE
                    ShopkeeperAmount =
                        shopkeeperAmount,

                    PaymentStatus =
                        paymentStatus,

                    PaymentMethod =
                        paymentMethod,

                    PaymentReceived =
                        paymentReceived,

                    TransactionId =
                        transactionId,

                    TransactionImage =
                        savedTransactionImage,

                    PaymentDate =
                        null,

                    ShopkeeperPaymentStatus =
                        "Pending",

                    ShopkeeperPaidAmount =
                        0m,

                    ShopkeeperPaymentMethod =
                        null,

                    ShopkeeperPaymentAccount =
                        null,

                    ShopkeeperPaymentDate =
                        null,

                    DeliveryPaymentStatus =
                        "Pending",

                    DeliveryCollectedAmount =
                        0m,

                    DeliveryCollectionDate =
                        null,

                    DeliveryPaidAmount =
                        0m,

                    DeliveryPaymentMethod =
                        null,

                    DeliveryPaymentAccount =
                        null,

                    DeliveryPaymentDate =
                        null,

                    CreatedDate =
                        DateTime.Now
                };


            _context.Orders.Add(order);

            await _context.SaveChangesAsync();


            // =====================================================
            // CREATE ORDER DETAILS
            // =====================================================

            foreach (var cartItem in cartItems)
            {
                var product =
                    products.First(
                        p =>
                            p.ProductId ==
                            cartItem.ProductId
                    );

                decimal unitPrice =
                    finalPrices[
                        product.ProductId];

                decimal subtotal =
                    unitPrice *
                    cartItem.Quantity;

                string purchaseType =
                    finalPurchaseTypes[
                        product.ProductId];

                int? negotiationId =
                    finalNegotiationIds[
                        product.ProductId];


                var orderDetail =
                    new OrderDetail
                    {
                        OrderId =
                            order.OrderId,

                        ProductId =
                            product.ProductId,

                        Quantity =
                            cartItem.Quantity,

                        // HISTORICAL FINAL PRICE
                        UnitPrice =
                            unitPrice,

                        SubTotal =
                            subtotal,

                        CustomMeasurements =
                            cartItem.CustomMeasurements,

                        PurchaseType =
                            purchaseType,

                        RentalSecurity =
                            purchaseType.Equals(
                                "Rent",
                                StringComparison.OrdinalIgnoreCase)
                                ? product.RentalSecurity ?? 0m
                                : 0m,

                        RentalDuration =
                            purchaseType.Equals(
                                "Rent",
                                StringComparison.OrdinalIgnoreCase)
                                ? product.RentalDuration
                                : null,

                        RentalConditions =
                            purchaseType.Equals(
                                "Rent",
                                StringComparison.OrdinalIgnoreCase)
                                ? product.RentalConditions
                                : null
                    };


                _context.OrderDetails.Add(
                    orderDetail
                );


                // -------------------------------------------------
                // REDUCE STOCK
                // -------------------------------------------------

                product.StockQuantity -=
                    cartItem.Quantity;
            }


            await _context.SaveChangesAsync();


            // =====================================================
            // CUSTOMER NOTIFICATION
            // =====================================================

            await CreateNotification(
                customerId,
                "Order Placed",
                $"Your order #{order.OrderId} has been placed successfully. Total amount is Rs. {totalAmount:N2}.",
                "Order",
                order.OrderId
            );


            // =====================================================
            // SHOPKEEPER NOTIFICATION
            // =====================================================

            if (shop.ShopkeeperId > 0)
            {
                await CreateNotification(
                    shop.ShopkeeperId,
                    "New Order Received",
                    $"A new order #{order.OrderId} has been placed for your shop.",
                    "Order",
                    order.OrderId
                );
            }


            // =====================================================
            // ADMIN NOTIFICATION
            // =====================================================

            var admin =
                await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(
                        u =>
                            u.Role != null &&
                            u.Role.RoleName ==
                                "Admin");

            if (admin != null)
            {
                await CreateNotification(
                    admin.UserId,
                    "New Customer Order",
                    $"Customer {customer.Name} placed order #{order.OrderId}. Payment method: {paymentMethod}.",
                    "Order",
                    order.OrderId
                );
            }


            await _context.SaveChangesAsync();


            // =====================================================
            // CLEAR CART
            // =====================================================

            ClearCart();


            TempData["Success"] =
                $"Order #{order.OrderId} placed successfully.";

            return RedirectToAction(
                "OrderDetails",
                new { id = order.OrderId }
            );
        }


        // =========================================================
        // MY ORDERS
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> MyOrders()
        {
            if (!IsCustomerLoggedIn())
                return CustomerLogin();

            int customerId =
                GetCustomerId()!.Value;

            var orders =
                await _context.Orders
                    .Include(o => o.Shop)
                    .Include(o => o.OrderDetails)
                    .ThenInclude(
                        od => od.Product)
                    .Where(
                        o =>
                            o.CustomerId ==
                                customerId &&
                            o.OrderStatus !=
                                "Cancelled")
                    .OrderByDescending(
                        o =>
                            o.CreatedDate
                    )
                    .ToListAsync();

            return View(orders);
        }


        // =========================================================
        // ORDER DETAILS
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> OrderDetails(
            int id)
        {
            if (!IsCustomerLoggedIn())
                return CustomerLogin();

            int customerId =
                GetCustomerId()!.Value;

            var order =
                await _context.Orders
                    .Include(o => o.Customer)
                    .Include(o => o.Shop)
                    .ThenInclude(
                        s => s!.Shopkeeper)
                    .Include(o => o.Delivery)
                    .Include(o => o.OrderDetails)
                    .ThenInclude(
                        od => od.Product)
                    .FirstOrDefaultAsync(
                        o =>
                            o.OrderId == id &&
                            o.CustomerId ==
                                customerId
                    );

            if (order == null)
            {
                TempData["Error"] =
                    "Order not found.";

                return RedirectToAction(
                    "MyOrders"
                );
            }

            return View(order);
        }


        // =========================================================
        // NOTIFICATIONS
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Notifications()
        {
            if (!IsCustomerLoggedIn())
                return CustomerLogin();

            int customerId =
                GetCustomerId()!.Value;

            var notifications =
                await _context.Notifications
                    .Where(
                        n =>
                            n.UserId ==
                            customerId
                    )
                    .OrderByDescending(
                        n =>
                            n.CreatedDate
                    )
                    .ToListAsync();

            return View(notifications);
        }


        // =========================================================
        // MARK ALL NOTIFICATIONS READ
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult>
            MarkAllNotificationsRead()
        {
            if (!IsCustomerLoggedIn())
                return CustomerLogin();

            int customerId =
                GetCustomerId()!.Value;

            var notifications =
                await _context.Notifications
                    .Where(
                        n =>
                            n.UserId ==
                                customerId &&
                            !n.IsRead
                    )
                    .ToListAsync();

            foreach (var notification
                in notifications)
            {
                notification.IsRead =
                    true;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "All notifications marked as read.";

            return RedirectToAction(
                "Notifications"
            );
        }


        // =========================================================
        // MARK SINGLE NOTIFICATION READ
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult>
            MarkNotificationRead(int id)
        {
            if (!IsCustomerLoggedIn())
                return CustomerLogin();

            int customerId =
                GetCustomerId()!.Value;

            var notification =
                await _context.Notifications
                    .FirstOrDefaultAsync(
                        n =>
                            n.NotificationId ==
                                id &&
                            n.UserId ==
                                customerId
                    );

            if (notification != null)
            {
                notification.IsRead =
                    true;

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(
                "Notifications"
            );
        }


        // =========================================================
// FEEDBACK
// =========================================================

[HttpGet]
public async Task<IActionResult> Feedback()
{
    if (!IsCustomerLoggedIn())
        return CustomerLogin();

    int customerId =
        GetCustomerId()!.Value;

    // -----------------------------------------------------
    // PREVIOUS REVIEWS
    // -----------------------------------------------------

    var reviews = await _context.Reviews
        .Include(r => r.Product)
        .Include(r => r.Shop)
        .Where(r => r.UserId == customerId)
        .OrderByDescending(r => r.CreatedAt)
        .ToListAsync();

    // -----------------------------------------------------
    // COMPLETED / DELIVERED ORDERS
    // -----------------------------------------------------

    var completedOrders = await _context.Orders
        .Where(o =>
            o.CustomerId == customerId &&
            (o.OrderStatus == "Delivered" ||
             o.OrderStatus == "Completed"))
        .Include(o => o.Shop)
        .Include(o => o.OrderDetails)
            .ThenInclude(od => od.Product)
        .OrderByDescending(o => o.CreatedDate)
        .ToListAsync();

    // -----------------------------------------------------
    // SEND COMPLETED ORDERS TO VIEW
    // -----------------------------------------------------

    // Your Razor view reads ViewBag.CompletedOrders.
    ViewBag.CompletedOrders = completedOrders;

    // Optional lists, if other parts of your view use them.
    ViewBag.Products = completedOrders
        .SelectMany(o => o.OrderDetails)
        .Where(od => od.Product != null)
        .Select(od => new
        {
            ProductId = od.ProductId,
            ProductName = od.Product!.ProductName,
            ShopId = od.OrderId
        })
        .ToList();

    ViewBag.Shops = completedOrders
        .Where(o => o.Shop != null)
        .Select(o => new
        {
            ShopId = o.ShopId,
            ShopName = o.Shop!.ShopName
        })
        .GroupBy(x => x.ShopId)
        .Select(g => g.First())
        .ToList();

    return View(reviews);
}


  
// =========================================================
// SUBMIT FEEDBACK
// =========================================================

[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> SubmitFeedback(
    int? orderId,
    int? productId,
    int? shopId,
    int rating,
    string? comment,
    string? reviewType)
{
    // -----------------------------------------------------
    // CUSTOMER LOGIN
    // -----------------------------------------------------

    if (!IsCustomerLoggedIn())
        return CustomerLogin();

    int customerId =
        GetCustomerId()!.Value;


    // -----------------------------------------------------
    // ORDER
    // -----------------------------------------------------

    if (!orderId.HasValue ||
        orderId.Value <= 0)
    {
        TempData["Error"] =
            "Please select a completed order.";

        return RedirectToAction("Feedback");
    }


    // -----------------------------------------------------
    // FIND SELECTED ORDER
    // -----------------------------------------------------

    var selectedOrder =
        await _context.Orders
            .Include(o => o.Shop)
            .FirstOrDefaultAsync(
                o =>
                    o.OrderId == orderId.Value &&
                    o.CustomerId == customerId &&
                    (
                        o.OrderStatus == "Delivered" ||
                        o.OrderStatus == "Completed"
                    ));

    if (selectedOrder == null)
    {
        TempData["Error"] =
            "The selected order was not found or is not a completed order.";

        return RedirectToAction("Feedback");
    }


    // -----------------------------------------------------
    // RATING
    // -----------------------------------------------------

    if (rating < 1 ||
        rating > 5)
    {
        TempData["Error"] =
            "Please select a rating between 1 and 5.";

        return RedirectToAction("Feedback");
    }


    // -----------------------------------------------------
    // COMMENT
    // -----------------------------------------------------

    if (string.IsNullOrWhiteSpace(comment))
    {
        TempData["Error"] =
            "Please enter your feedback.";

        return RedirectToAction("Feedback");
    }

    comment = comment.Trim();


    // -----------------------------------------------------
    // REVIEW TYPE
    // -----------------------------------------------------

    if (string.IsNullOrWhiteSpace(reviewType))
    {
        TempData["Error"] =
            "Please select whether your feedback is for a shop or product.";

        return RedirectToAction("Feedback");
    }

    reviewType =
        reviewType.Trim();


    // =====================================================
    // SHOP REVIEW
    // =====================================================

    if (reviewType.Equals(
        "Shop",
        StringComparison.OrdinalIgnoreCase))
    {
        // -------------------------------------------------
        // VERIFY SHOP
        // -------------------------------------------------

        if (!shopId.HasValue ||
            shopId.Value <= 0)
        {
            TempData["Error"] =
                "Please select a shop.";

            return RedirectToAction("Feedback");
        }


        // -------------------------------------------------
        // SHOP MUST MATCH SELECTED ORDER
        // -------------------------------------------------

        if (selectedOrder.ShopId != shopId.Value)
        {
            TempData["Error"] =
                "The selected shop does not belong to this order.";

            return RedirectToAction("Feedback");
        }


        var shop =
            selectedOrder.Shop;

        if (shop == null)
        {
            shop =
                await _context.Shops
                    .FirstOrDefaultAsync(
                        s =>
                            s.ShopId ==
                            shopId.Value);
        }

        if (shop == null)
        {
            TempData["Error"] =
                "Shop not found.";

            return RedirectToAction("Feedback");
        }


        // -------------------------------------------------
        // CHECK EXISTING SHOP REVIEW
        // -------------------------------------------------

        bool alreadyReviewedShop =
            await _context.Reviews
                .AnyAsync(
                    r =>
                        r.UserId == customerId &&
                        r.ShopId == shopId.Value &&
                        r.ReviewType == "Shop");

        if (alreadyReviewedShop)
        {
            TempData["Error"] =
                "You have already submitted a review for this shop.";

            return RedirectToAction("Feedback");
        }


        // -------------------------------------------------
        // CREATE SHOP REVIEW
        // -------------------------------------------------

        var shopReview =
            new Review
            {
                UserId =
                    customerId,

                ShopId =
                    shopId.Value,

                Rating =
                    rating,

                Comment =
                    comment,

                ReviewType =
                    "Shop",

                IsApproved =
                    false,

                CreatedAt =
                    DateTime.Now
            };

        _context.Reviews.Add(
            shopReview);


        // -------------------------------------------------
        // NOTIFY SHOPKEEPER
        // -------------------------------------------------

        await CreateNotification(
            shop.ShopkeeperId,
            "New Customer Review",
            $"A customer submitted a {rating}-star review for your shop: {shop.ShopName}.",
            "Review"
        );


        // -------------------------------------------------
        // NOTIFY ADMIN
        // -------------------------------------------------

        var admin =
            await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(
                    u =>
                        u.Role != null &&
                        u.Role.RoleName == "Admin" &&
                        u.Status);

        if (admin != null)
        {
            await CreateNotification(
                admin.UserId,
                "New Review Pending Approval",
                $"A customer submitted a {rating}-star review for shop '{shop.ShopName}'. The review is waiting for approval.",
                "Review"
            );
        }


        // -------------------------------------------------
        // SAVE
        // -------------------------------------------------

        await _context.SaveChangesAsync();


        TempData["Success"] =
            "Thank you! Your shop feedback has been submitted and is waiting for admin approval.";

        return RedirectToAction("Feedback");
    }


    // =====================================================
    // PRODUCT REVIEW
    // =====================================================

    if (reviewType.Equals(
        "Product",
        StringComparison.OrdinalIgnoreCase))
    {
        // -------------------------------------------------
        // PRODUCT REQUIRED
        // -------------------------------------------------

        if (!productId.HasValue ||
            productId.Value <= 0)
        {
            TempData["Error"] =
                "Please select a product.";

            return RedirectToAction("Feedback");
        }


        // -------------------------------------------------
        // PRODUCT MUST EXIST
        // -------------------------------------------------

        var product =
            await _context.Products
                .FirstOrDefaultAsync(
                    p =>
                        p.ProductId ==
                        productId.Value);

        if (product == null)
        {
            TempData["Error"] =
                "Product not found.";

            return RedirectToAction("Feedback");
        }


        // -------------------------------------------------
        // PRODUCT MUST BELONG TO SELECTED ORDER
        // -------------------------------------------------

        bool productBelongsToOrder =
            await _context.OrderDetails
                .AnyAsync(
                    od =>
                        od.OrderId ==
                            orderId.Value &&
                        od.ProductId ==
                            productId.Value &&
                        od.Order != null &&
                        od.Order.CustomerId ==
                            customerId &&
                        (
                            od.Order.OrderStatus ==
                                "Delivered" ||
                            od.Order.OrderStatus ==
                                "Completed"
                        ));

        if (!productBelongsToOrder)
        {
            TempData["Error"] =
                "The selected product does not belong to the selected completed order.";

            return RedirectToAction("Feedback");
        }


        // -------------------------------------------------
        // GET SHOP FROM SELECTED ORDER
        // -------------------------------------------------

        int productShopId =
            selectedOrder.ShopId;


        var shop =
            await _context.Shops
                .FirstOrDefaultAsync(
                    s =>
                        s.ShopId ==
                        productShopId);

        if (shop == null)
        {
            TempData["Error"] =
                "The shop associated with this product could not be found.";

            return RedirectToAction("Feedback");
        }


        // -------------------------------------------------
        // CHECK EXISTING PRODUCT REVIEW
        // -------------------------------------------------

        bool alreadyReviewedProduct =
            await _context.Reviews
                .AnyAsync(
                    r =>
                        r.UserId ==
                            customerId &&
                        r.ProductId ==
                            productId.Value &&
                        r.ReviewType ==
                            "Product");

        if (alreadyReviewedProduct)
        {
            TempData["Error"] =
                "You have already submitted a review for this product.";

            return RedirectToAction("Feedback");
        }


        // -------------------------------------------------
        // CREATE PRODUCT REVIEW
        // -------------------------------------------------

        var productReview =
            new Review
            {
                UserId =
                    customerId,

                ProductId =
                    productId.Value,

                ShopId =
                    productShopId,

                Rating =
                    rating,

                Comment =
                    comment,

                ReviewType =
                    "Product",

                IsApproved =
                    false,

                CreatedAt =
                    DateTime.Now
            };

        _context.Reviews.Add(
            productReview);


        // -------------------------------------------------
        // NOTIFY SHOPKEEPER
        // -------------------------------------------------

        await CreateNotification(
            shop.ShopkeeperId,
            "New Customer Review",
            $"A customer submitted a {rating}-star review for your product: {product.ProductName}.",
            "Review"
        );


        // -------------------------------------------------
        // NOTIFY ADMIN
        // -------------------------------------------------

        var admin =
            await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(
                    u =>
                        u.Role != null &&
                        u.Role.RoleName == "Admin" &&
                        u.Status);

        if (admin != null)
        {
            await CreateNotification(
                admin.UserId,
                "New Review Pending Approval",
                $"A customer submitted a {rating}-star review for product '{product.ProductName}'. The review is waiting for approval.",
                "Review"
            );
        }


        // -------------------------------------------------
        // SAVE
        // -------------------------------------------------

        await _context.SaveChangesAsync();


        TempData["Success"] =
            "Thank you! Your product feedback has been submitted and is waiting for admin approval.";

        return RedirectToAction("Feedback");
    }


    // =====================================================
    // INVALID REVIEW TYPE
    // =====================================================

    TempData["Error"] =
        "Invalid feedback type.";

    return RedirectToAction("Feedback");
}




        // =========================================================
        // CREATE NOTIFICATION
        // =========================================================

        private async Task CreateNotification(
            int userId,
            string title,
            string message,
            string type = "General",
            int? orderId = null)
        {
            if (userId <= 0)
                return;

            var userExists =
                await _context.Users
                    .AnyAsync(
                        u =>
                            u.UserId ==
                            userId);

            if (!userExists)
                return;

            var notification =
                new Notification
                {
                    UserId =
                        userId,

                    Title =
                        title,

                    Message =
                        message,

                    Type =
                        type,

                    OrderId =
                        orderId,

                    IsRead =
                        false,

                    CreatedDate =
                        DateTime.Now
                };

            _context.Notifications.Add(
                notification);
        }


        // =========================================================
        // CHAT
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Chat()
        {
            if (!IsCustomerLoggedIn())
                return CustomerLogin();

            int customerId =
                GetCustomerId()!.Value;


            // -----------------------------------------------------
            // ADMIN
            // -----------------------------------------------------

            var admin =
                await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(
                        u =>
                            u.Status &&
                            u.Role != null &&
                            u.Role.RoleName ==
                                "Admin");


            // -----------------------------------------------------
            // SHOPKEEPERS FROM CUSTOMER ORDERS
            // -----------------------------------------------------

            var shopkeeperIds =
                await _context.Orders
                    .Where(
                        o =>
                            o.CustomerId ==
                                customerId &&
                            o.ShopId > 0)
                    .Join(
                        _context.Shops,
                        order =>
                            order.ShopId,
                        shop =>
                            shop.ShopId,
                        (order, shop) =>
                            shop.ShopkeeperId)
                    .Distinct()
                    .ToListAsync();


            var shopkeepers =
                await _context.Users
                    .Include(u => u.Role)
                    .Where(
                        u =>
                            u.Status &&
                            shopkeeperIds.Contains(
                                u.UserId) &&
                            u.Role != null &&
                            u.Role.RoleName ==
                                "Shopkeeper")
                    .OrderBy(
                        u =>
                            u.Name)
                    .ToListAsync();


            // -----------------------------------------------------
            // DELIVERY BOYS
            // -----------------------------------------------------

            var deliveryIds =
                await _context.Orders
                    .Where(
                        o =>
                            o.CustomerId ==
                                customerId &&
                            o.DeliveryId.HasValue)
                    .Select(
                        o =>
                            o.DeliveryId!.Value)
                    .Distinct()
                    .ToListAsync();


            var deliveryBoys =
                await _context.Users
                    .Include(u => u.Role)
                    .Where(
                        u =>
                            u.Status &&
                            deliveryIds.Contains(
                                u.UserId) &&
                            u.Role != null &&
                            u.Role.RoleName ==
                                "Delivery")
                    .OrderBy(
                        u =>
                            u.Name)
                    .ToListAsync();


            // -----------------------------------------------------
            // CONTACT IDS
            // -----------------------------------------------------

            var contactIds =
                new List<int>();

            if (admin != null)
                contactIds.Add(
                    admin.UserId);

            contactIds.AddRange(
                shopkeepers.Select(
                    x =>
                        x.UserId));

            contactIds.AddRange(
                deliveryBoys.Select(
                    x =>
                        x.UserId));


            // -----------------------------------------------------
            // UNREAD COUNTS
            // -----------------------------------------------------

            var unreadCounts =
                await _context.ChatMessages
                    .Where(
                        c =>
                            c.ReceiverId ==
                                customerId &&
                            !c.IsRead &&
                            contactIds.Contains(
                                c.SenderId))
                    .GroupBy(
                        c =>
                            c.SenderId)
                    .Select(
                        g =>
                            new
                            {
                                UserId =
                                    g.Key,

                                Count =
                                    g.Count()
                            })
                    .ToDictionaryAsync(
                        x =>
                            x.UserId,
                        x =>
                            x.Count);


            ViewBag.Admin =
                admin;

            ViewBag.Shopkeepers =
                shopkeepers;

            ViewBag.DeliveryBoys =
                deliveryBoys;

            ViewBag.SelectedUser =
                null;

            ViewBag.CurrentUserId =
                customerId;

            ViewBag.UnreadCounts =
                unreadCounts;

            return View(
                new List<ChatMessage>());
        }


        // =========================================================
        // CUSTOMER CHAT CONVERSATION
        // =========================================================

        [HttpGet]
        public async Task<IActionResult>
            Conversation(int userId)
        {
            if (!IsCustomerLoggedIn())
                return CustomerLogin();

            int customerId =
                GetCustomerId()!.Value;


            // -----------------------------------------------------
            // SELECTED USER
            // -----------------------------------------------------

            var selectedUser =
                await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(
                        u =>
                            u.UserId ==
                                userId &&
                            u.Status);

            if (selectedUser == null)
            {
                TempData["Error"] =
                    "User not found or inactive.";

                return RedirectToAction(
                    "Chat");
            }

            string role =
                selectedUser.Role?.RoleName ??
                "";


            // -----------------------------------------------------
            // CHECK CHAT PERMISSION
            // -----------------------------------------------------

            bool allowed =
                false;


            // ADMIN
            if (role == "Admin")
            {
                allowed =
                    true;
            }


            // SHOPKEEPER
            else if (role == "Shopkeeper")
            {
                allowed =
                    await _context.Orders
                        .Join(
                            _context.Shops,
                            order =>
                                order.ShopId,
                            shop =>
                                shop.ShopId,
                            (order, shop) =>
                                new
                                {
                                    order,
                                    shop
                                })
                        .AnyAsync(
                            x =>
                                x.order.CustomerId ==
                                    customerId &&
                                x.shop.ShopkeeperId ==
                                    userId);
            }


            // DELIVERY
            else if (role == "Delivery")
            {
                allowed =
                    await _context.Orders
                        .AnyAsync(
                            o =>
                                o.CustomerId ==
                                    customerId &&
                                o.DeliveryId ==
                                    userId);
            }


            if (!allowed)
            {
                TempData["Error"] =
                    "You are not allowed to chat with this user.";

                return RedirectToAction(
                    "Chat");
            }


            // -----------------------------------------------------
            // GET MESSAGES
            // -----------------------------------------------------

            var messages =
                await _context.ChatMessages
                    .Where(
                        c =>
                            (
                                c.SenderId ==
                                    customerId &&
                                c.ReceiverId ==
                                    userId
                            )
                            ||
                            (
                                c.SenderId ==
                                    userId &&
                                c.ReceiverId ==
                                    customerId
                            ))
                    .Include(c => c.Sender)
                    .Include(c => c.Receiver)
                    .OrderBy(
                        c =>
                            c.SentDate)
                    .ToListAsync();


            // -----------------------------------------------------
            // MARK RECEIVED MESSAGES AS READ
            // -----------------------------------------------------

            var unreadMessages =
                messages
                    .Where(
                        m =>
                            m.ReceiverId ==
                                customerId &&
                            !m.IsRead)
                    .ToList();

            foreach (var message
                in unreadMessages)
            {
                message.IsRead =
                    true;
            }

            if (unreadMessages.Any())
            {
                await _context.SaveChangesAsync();
            }


            // -----------------------------------------------------
            // ADMIN
            // -----------------------------------------------------

            var admin =
                await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(
                        u =>
                            u.Status &&
                            u.Role != null &&
                            u.Role.RoleName ==
                                "Admin");


            // -----------------------------------------------------
            // SHOPKEEPERS
            // -----------------------------------------------------

            var shopkeeperIds =
                await _context.Orders
                    .Where(
                        o =>
                            o.CustomerId ==
                                customerId)
                    .Join(
                        _context.Shops,
                        order =>
                            order.ShopId,
                        shop =>
                            shop.ShopId,
                        (order, shop) =>
                            shop.ShopkeeperId)
                    .Distinct()
                    .ToListAsync();


            var shopkeepers =
                await _context.Users
                    .Include(u => u.Role)
                    .Where(
                        u =>
                            shopkeeperIds.Contains(
                                u.UserId) &&
                            u.Status &&
                            u.Role != null &&
                            u.Role.RoleName ==
                                "Shopkeeper")
                    .OrderBy(
                        u =>
                            u.Name)
                    .ToListAsync();


            // -----------------------------------------------------
            // DELIVERY BOYS
            // -----------------------------------------------------

            var deliveryIds =
                await _context.Orders
                    .Where(
                        o =>
                            o.CustomerId ==
                                customerId &&
                            o.DeliveryId.HasValue)
                    .Select(
                        o =>
                            o.DeliveryId!.Value)
                    .Distinct()
                    .ToListAsync();


            var deliveryBoys =
                await _context.Users
                    .Include(u => u.Role)
                    .Where(
                        u =>
                            deliveryIds.Contains(
                                u.UserId) &&
                            u.Status &&
                            u.Role != null &&
                            u.Role.RoleName ==
                                "Delivery")
                    .OrderBy(
                        u =>
                            u.Name)
                    .ToListAsync();


            // -----------------------------------------------------
            // UNREAD COUNTS
            // -----------------------------------------------------

            var unreadCounts =
                await _context.ChatMessages
                    .Where(
                        c =>
                            c.ReceiverId ==
                                customerId &&
                            !c.IsRead)
                    .GroupBy(
                        c =>
                            c.SenderId)
                    .Select(
                        g =>
                            new
                            {
                                UserId =
                                    g.Key,

                                Count =
                                    g.Count()
                            })
                    .ToDictionaryAsync(
                        x =>
                            x.UserId,
                        x =>
                            x.Count);


            ViewBag.Admin =
                admin;

            ViewBag.Shopkeepers =
                shopkeepers;

            ViewBag.DeliveryBoys =
                deliveryBoys;

            ViewBag.CurrentUserId =
                customerId;

            ViewBag.SelectedUserId =
                userId;

            ViewBag.SelectedUser =
                selectedUser;

            ViewBag.SelectedUserName =
                selectedUser.Name;

            ViewBag.UnreadCounts =
                unreadCounts;

            return View(
                "Chat",
                messages);
        }


      // =========================================================
// SEND MESSAGE (TEXT ONLY)
// =========================================================

[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> SendMessage(
    int receiverId,
    string? message)
{
    int? customerId =
        HttpContext.Session.GetInt32("CustomerId");

    if (!customerId.HasValue)
    {
        return RedirectToAction("Login", "Account");
    }

    // Customer cannot message themselves
    if (receiverId == customerId.Value)
    {
        TempData["Error"] =
            "You cannot send a message to yourself.";

        return RedirectToAction(
            nameof(Conversation),
            new { userId = receiverId });
    }

    // Check receiver
    var receiver = await _context.Users
        .Include(u => u.Role)
        .FirstOrDefaultAsync(u => u.UserId == receiverId);

    if (receiver == null)
    {
        TempData["Error"] =
            "The selected user was not found.";

        return RedirectToAction(nameof(Chat));
    }

    // Check chat permission
    bool canChat = await CanCustomerChatWithUser(
        customerId.Value,
        receiverId);

    if (!canChat)
    {
        TempData["Error"] =
            "You are not allowed to chat with this user.";

        return RedirectToAction(
            nameof(Conversation),
            new { userId = receiverId });
    }

    // Clean message
    string cleanMessage = message?.Trim() ?? "";

    if (string.IsNullOrWhiteSpace(cleanMessage))
    {
        TempData["Error"] =
            "Please enter a message.";

        return RedirectToAction(
            nameof(Conversation),
            new { userId = receiverId });
    }

    if (cleanMessage.Length > 2000)
    {
        TempData["Error"] =
            "Message cannot exceed 2000 characters.";

        return RedirectToAction(
            nameof(Conversation),
            new { userId = receiverId });
    }

    // Get customer name from database
    string? customerName = await _context.Users
        .Where(u => u.UserId == customerId.Value)
        .Select(u => u.Name)
        .FirstOrDefaultAsync();

    customerName ??= "Customer";

    // Create message
    var chatMessage = new ChatMessage
    {
        SenderId = customerId.Value,
        ReceiverId = receiverId,
        Message = cleanMessage,
        SentDate = DateTime.Now,
        IsRead = false,
        IsDeleted = false
    };

    _context.ChatMessages.Add(chatMessage);

    await _context.SaveChangesAsync();

    // Send notification
    try
    {
        await CreateNotification(
            receiverId,
            "New Chat Message",
            $"You received a new message from {customerName}",
            "Chat",
            customerId.Value);
    }
    catch
    {
        // Message remains saved even if notification fails.
    }

    TempData["Success"] =
        "Message sent successfully.";

    return RedirectToAction(
        nameof(Conversation),
        new { userId = receiverId });
}
        
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> DeleteMessage(
    int messageId,
    int receiverId)
{
    int? customerId =
        HttpContext.Session.GetInt32("CustomerId");

    if (!customerId.HasValue)
    {
        return RedirectToAction("Login", "Account");
    }

    var chatMessage = await _context.ChatMessages
        .FirstOrDefaultAsync(m =>
            m.ChatMessageId == messageId);

    if (chatMessage == null)
    {
        TempData["Error"] = "Message not found.";

        return RedirectToAction(
            nameof(Conversation),
            new { userId = receiverId });
    }

    // Only the sender can delete their message.
    if (chatMessage.SenderId != customerId.Value ||
        chatMessage.ReceiverId != receiverId)
    {
        TempData["Error"] =
            "You are not allowed to delete this message.";

        return RedirectToAction(
            nameof(Conversation),
            new { userId = receiverId });
    }

    if (!chatMessage.IsDeleted)
    {
        chatMessage.IsDeleted = true;
        chatMessage.Message = "This message was deleted.";

        await _context.SaveChangesAsync();
    }

    TempData["Success"] = "Message deleted successfully.";

    return RedirectToAction(
        nameof(Conversation),
        new { userId = receiverId });
}
        // =========================================================
        // LOGOUT
        // =========================================================

        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();

            return RedirectToAction(
                "Login",
                "Account"
            );
        }
    }


    // =============================================================
    // CUSTOMER CART ITEM
    // =============================================================

    public class CustomerCartItem
    {
        public int ProductId { get; set; }

        public string? ProductName { get; set; }

        public decimal Price { get; set; }

        public int Quantity { get; set; }

        public string? CustomMeasurements { get; set; }

        // =========================================================
        // NEW
        // Buy / Sale / Negotiated / Rent
        // =========================================================

        public string PurchaseType { get; set; } = "Buy";

        // =========================================================
        // NEW
        // Accepted negotiation associated with this cart item
        // =========================================================

        public int? NegotiationId { get; set; }
    }
}