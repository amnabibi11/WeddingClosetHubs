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
        private readonly IConfiguration _configuration;

        private const string CartSessionKey = "CustomerCart";
        private const string PendingCartSessionKey = "PendingCartItems";

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
         IWebHostEnvironment environment,
         IConfiguration configuration)
        {
            _context = context;
            _environment = environment;
            _configuration = configuration;
        }


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
        private List<CustomerCartItem> GetPendingCartItems()
        {
            var json =
                HttpContext.Session.GetString(PendingCartSessionKey);

            if (string.IsNullOrWhiteSpace(json))
                return new List<CustomerCartItem>();

            try
            {
                return JsonSerializer.Deserialize<List<CustomerCartItem>>(
                           json,
                           new JsonSerializerOptions
                           {
                               PropertyNameCaseInsensitive = true
                           })
                       ?? new List<CustomerCartItem>();
            }
            catch
            {
                HttpContext.Session.Remove(PendingCartSessionKey);
                return new List<CustomerCartItem>();
            }
        }

        private void SavePendingCartItems(
            List<CustomerCartItem> cartItems)
        {
            HttpContext.Session.SetString(
                PendingCartSessionKey,
                JsonSerializer.Serialize(cartItems));
        }

        private void ClearPendingCartItems()
        {
            HttpContext.Session.Remove(PendingCartSessionKey);
        }


        private void ClearCart()
        {
            HttpContext.Session.Remove(CartSessionKey);
        }


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


        private async Task<(decimal Price, string PurchaseType, int? NegotiationId)>
            GetEffectivePrice(
                Product product,
                int customerId,
                int quantity = 1)
        {

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

            return (
                product.Price,
                "Buy",
                null
            );
        }

        private async Task<bool> CanCustomerChatWithUser(
            int customerId,
            int receiverId)
        {
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

            if (receiver.Role?.RoleName == "Delivery")
            {
                return customerOrders.Any(o =>
                    o.DeliveryId == receiverId);
            }

            return false;
        }
        private string GetMeasurementType(string subcategory)
        {
            subcategory = subcategory
                .Trim()
                .ToLowerInvariant();

            if (subcategory.Contains("lehenga"))
                return "lehenga";

            if (subcategory.Contains("gown"))
                return "gown";

            if (subcategory.Contains("maxi"))
                return "maxi";

            if (subcategory.Contains("sharara") ||
                subcategory.Contains("gharara"))
                return "sharara";

            if (subcategory.Contains("pishwas"))
                return "pishwas";

            if (subcategory.Contains("saree") ||
                subcategory.Contains("sari"))
                return "saree";

            if (subcategory.Contains("frock"))
                return "frock";

            if (subcategory.Contains("anarkali"))
                return "anarkali";

            if (subcategory.Contains("palazzo"))
                return "palazzo";

            if (subcategory.Contains("sherwani"))
                return "sherwani";

            if (subcategory.Contains("prince coat") ||
                subcategory.Contains("princecoat"))
                return "princecoat";

            if (subcategory.Contains("kurta shalwar") ||
                subcategory.Contains("kurtashalwar"))
                return "kurtashalwar";

            if (subcategory.Contains("suit"))
                return "suit";

            return "general";
        }
        private List<(string Name, bool Required, decimal Min, decimal Max)>
    GetAllowedMeasurements(string measurementType)
        {
            switch (measurementType)
            {
                case "lehenga":
                    return new()
            {
                ("Blouse Bust", true, 20, 80),
                ("Blouse Waist", true, 18, 75),
                ("Shoulder", true, 10, 30),
                ("Armhole", true, 10, 30),
                ("Blouse Length", true, 10, 30),
                ("Sleeve Length", false, 10, 40),
                ("Wrist", false, 4, 20),
                ("Lehenga Waist", true, 18, 75),
                ("Lehenga Length", true, 25, 55)
            };

                case "gown":
                    return new()
            {
                ("Bust", true, 20, 80),
                ("Waist", true, 18, 75),
                ("Hips", true, 20, 85),
                ("Shoulder", true, 10, 30),
                ("Armhole", true, 10, 30),
                ("Sleeve Length", false, 10, 40),
                ("Wrist", false, 4, 20),
                ("Gown Length", true, 25, 70)
            };

                case "maxi":
                    return new()
            {
                ("Bust", true, 20, 80),
                ("Waist", true, 18, 75),
                ("Hips", true, 20, 85),
                ("Shoulder", true, 10, 30),
                ("Armhole", true, 10, 30),
                ("Sleeve Length", false, 10, 40),
                ("Wrist", false, 4, 20),
                ("Maxi Length", true, 25, 70)
            };

                case "sharara":
                    return new()
            {
                ("Bust", true, 20, 80),
                ("Waist", true, 18, 75),
                ("Hips", true, 20, 85),
                ("Shoulder", true, 10, 30),
                ("Armhole", true, 10, 30),
                ("Sleeve Length", false, 10, 40),
                ("Kameez Length", true, 20, 45),
                ("Sharara Waist", true, 18, 75),
                ("Sharara Length", true, 25, 55)
            };

                case "pishwas":
                    return new()
            {
                ("Bust", true, 20, 80),
                ("Waist", true, 18, 75),
                ("Shoulder", true, 10, 30),
                ("Armhole", true, 10, 30),
                ("Sleeve Length", false, 10, 40),
                ("Wrist", false, 4, 20),
                ("Pishwas Length", true, 25, 70)
            };

                case "saree":
                    return new()
            {
                ("Blouse Bust", true, 20, 80),
                ("Blouse Waist", true, 18, 75),
                ("Shoulder", true, 10, 30),
                ("Armhole", true, 10, 30),
                ("Blouse Length", true, 10, 30),
                ("Sleeve Length", false, 10, 40),
                ("Wrist", false, 4, 20),
                ("Saree Length", true, 25, 70)
            };

                case "frock":
                    return new()
            {
                ("Bust", true, 20, 80),
                ("Waist", true, 18, 75),
                ("Hips", false, 20, 85),
                ("Shoulder", true, 10, 30),
                ("Armhole", true, 10, 30),
                ("Sleeve Length", false, 10, 40),
                ("Wrist", false, 4, 20),
                ("Frock Length", true, 25, 70)
            };

                case "anarkali":
                    return new()
            {
                ("Bust", true, 20, 80),
                ("Waist", true, 18, 75),
                ("Hips", true, 20, 85),
                ("Shoulder", true, 10, 30),
                ("Armhole", true, 10, 30),
                ("Sleeve Length", false, 10, 40),
                ("Wrist", false, 4, 20),
                ("Anarkali Length", true, 25, 70)
            };

                case "palazzo":
                    return new()
            {
                ("Bust", true, 20, 80),
                ("Waist", true, 18, 75),
                ("Hips", true, 20, 85),
                ("Shoulder", true, 10, 30),
                ("Armhole", true, 10, 30),
                ("Shirt Length", true, 20, 45),
                ("Palazzo Waist", true, 18, 75),
                ("Palazzo Length", true, 25, 50)
            };

                case "sherwani":
                    return new()
            {
                ("Chest", true, 20, 80),
                ("Waist", true, 18, 75),
                ("Shoulder", true, 10, 30),
                ("Armhole", true, 10, 30),
                ("Sleeve Length", true, 10, 40),
                ("Wrist", false, 4, 20),
                ("Sherwani Length", true, 25, 70)
            };

                case "princecoat":
                    return new()
            {
                ("Chest", true, 20, 80),
                ("Waist", true, 18, 75),
                ("Shoulder", true, 10, 30),
                ("Armhole", true, 10, 30),
                ("Sleeve Length", true, 10, 40),
                ("Wrist", false, 4, 20),
                ("Coat Length", true, 25, 50)
            };

                case "kurtashalwar":
                    return new()
            {
                ("Chest", true, 20, 80),
                ("Waist", true, 18, 75),
                ("Shoulder", true, 10, 30),
                ("Armhole", true, 10, 30),
                ("Sleeve Length", true, 10, 40),
                ("Wrist", false, 4, 20),
                ("Kameez Length", true, 20, 45),
                ("Shalwar Waist", true, 18, 75),
                ("Shalwar Length", true, 25, 50)
            };

                case "suit":
                    return new()
            {
                ("Chest", true, 20, 80),
                ("Waist", true, 18, 75),
                ("Hips", false, 20, 85),
                ("Shoulder", true, 10, 30),
                ("Armhole", true, 10, 30),
                ("Sleeve Length", false, 10, 40),
                ("Wrist", false, 4, 20),
                ("Coat Length", true, 25, 50),
                ("Trouser Waist", true, 18, 75),
                ("Trouser Length", true, 25, 50)
            };

                default:
                    return new()
            {
                ("Bust", true, 20, 80),
                ("Waist", true, 18, 75),
                ("Hips", true, 20, 85),
                ("Shoulder", true, 10, 30),
                ("Armhole", true, 10, 30),
                ("Sleeve Length", false, 10, 40),
                ("Wrist", false, 4, 20),
                ("Dress Length", true, 25, 70)
            };
            }
        }

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
        [HttpGet]
        public async Task<IActionResult> Shops(string? category)
        {
            var shopsQuery = _context.Shops
                .Include(s => s.Shopkeeper)
                .Where(s =>
                    s.Status &&
                    s.IsApproved);

            if (!string.IsNullOrWhiteSpace(category))
            {
                shopsQuery = shopsQuery.Where(s =>
                    _context.Products.Any(p =>
                        p.ShopId == s.ShopId &&
                        p.Status &&
                        p.Category == category &&
                        (
                            (p.ProductVariants != null &&
                             p.ProductVariants.Any(v => v.StockQuantity > 0))
                            ||
                            (p.ProductVariants == null &&
                             p.StockQuantity > 0)
                        )));
            }

            var shops = await shopsQuery
                .OrderBy(s => s.ShopName)
                .ToListAsync();

            ViewBag.SelectedCategory = category;

            return View(shops);
        }


        [HttpGet]
        public async Task<IActionResult> ShopDetails(int id)
        {
            var shop = await _context.Shops
                .Include(s => s.Shopkeeper)
                .FirstOrDefaultAsync(s =>
                    s.ShopId == id &&
                    s.Status &&
                    s.IsApproved);

            if (shop == null)
            {
                TempData["Error"] =
                    "Shop not found or is currently unavailable.";

                return RedirectToAction("Shops");
            }

            var products = await _context.Products
                .Include(p => p.ProductVariants)
                .Include(p => p.ProductImages)
                .Where(p =>
                    p.ShopId == id &&
                    p.Status &&
                    (
                        p.StockQuantity > 0 ||
                        (p.ProductVariants != null &&
                         p.ProductVariants.Any(v => v.StockQuantity > 0))
                    ))
                .OrderByDescending(p => p.CreatedDate)
                .ToListAsync();

            ViewBag.Shop = shop;
            ViewBag.ShopId = id;

            return View("ShopProducts", products);
        }


        [HttpGet]
        public async Task<IActionResult> ShopProducts(int id)
        {
            var shop = await _context.Shops
                .Include(s => s.Shopkeeper)
                .FirstOrDefaultAsync(s =>
                    s.ShopId == id &&
                    s.Status &&
                    s.IsApproved);

            if (shop == null)
            {
                TempData["Error"] =
                    "Shop not found or is currently unavailable.";

                return RedirectToAction("Shops");
            }

            var products = await _context.Products
                .Include(p => p.ProductVariants)
                .Include(p => p.ProductImages)
                .Where(p =>
                    p.ShopId == id &&
                    p.Status &&
                    (
                        p.StockQuantity > 0 ||
                        (p.ProductVariants != null &&
                         p.ProductVariants.Any(v => v.StockQuantity > 0))
                    ))
                .OrderByDescending(p => p.CreatedDate)
                .ToListAsync();

            ViewBag.Shop = shop;
            ViewBag.ShopId = id;
            ViewBag.OffersCustomization =
                shop.OffersCustomization;

            return View(products);
        }

        [HttpGet]
        public async Task<IActionResult> ProductDetails(int id)
        {
            var product = await _context.Products
                .Include(p => p.Shop)
                    .ThenInclude(s => s!.Shopkeeper)
                .Include(p => p.ProductVariants)
                .Include(p => p.ProductImages)
                .FirstOrDefaultAsync(p =>
                    p.ProductId == id &&
                    p.Status);

            if (product == null)
            {
                TempData["Error"] = "Product not found.";
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

            ViewBag.OffersBuy = product.Shop.OffersBuy;
            ViewBag.OffersRent = product.Shop.OffersRent;
            ViewBag.OffersCustomization =
                product.Shop.OffersCustomization;

            return View(product);
        }



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
            if (!product.Shop.OffersBuy)
            {
                TempData["Error"] =
                    "Buying and negotiation are not available from this shop.";

                return RedirectToAction(
                    "ProductDetails",
                    new { id = productId });
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


            if (product.Shop == null ||
                !product.Shop.Status ||
                !product.Shop.IsApproved)
            {
                TempData["Error"] =
                    "This shop is currently unavailable.";

                return RedirectToAction("MyNegotiations");
            }

            if (!product.Shop.OffersBuy)
            {
                TempData["Error"] =
                    "Buying and negotiation are not available from this shop.";

                return RedirectToAction(
                    "ProductDetails",
                    new { id = productId });
            }

            if (!product.AllowNegotiation)
            {
                TempData["Error"] =
                    "Negotiation is not available for this product.";

                return RedirectToAction(
                    "ProductDetails",
                    new { id = productId });
            }


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
            await CreateNotification(
                product.Shop.ShopkeeperId,
                 "New Negotiation Offer",
                  $"Customer has submitted an offer of Rs. {requestedPrice:N0} for {product.ProductName}.",
                 "Negotiation"
            );

            TempData["Success"] =
                $"Your offer of Rs. {requestedPrice:N0} has been sent to the shopkeeper.";

            return RedirectToAction("MyNegotiations");
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptCounterOffer(int negotiationId)
        {
            if (!IsCustomerLoggedIn())
                return CustomerLogin();

            int customerId = GetCustomerId()!.Value;

            var negotiation = await _context.Negotiations
                .Include(n => n.Product)
                .Include(n => n.Shopkeeper)
                .FirstOrDefaultAsync(n =>
                    n.NegotiationId == negotiationId &&
                    n.CustomerId == customerId);

            if (negotiation == null)
            {
                TempData["Error"] =
                    "Negotiation not found.";

                return RedirectToAction("MyNegotiations");
            }

            if (!string.Equals(
                negotiation.Status,
                "CounterOffer",
                StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] =
                    "This negotiation does not have an active counter offer.";

                return RedirectToAction("MyNegotiations");
            }

            if (!negotiation.CounterPrice.HasValue ||
                negotiation.CounterPrice.Value <= 0)
            {
                TempData["Error"] =
                    "No valid counter offer was found.";

                return RedirectToAction("MyNegotiations");
            }

            negotiation.AgreedPrice =
                negotiation.CounterPrice.Value;

            negotiation.Status =
                "Accepted";

            negotiation.RespondedDate =
                DateTime.Now;

            await _context.SaveChangesAsync();

            await CreateNotification(
                negotiation.ShopkeeperId,
                "Negotiation Accepted",
                $"Customer accepted your counter offer of Rs. {negotiation.AgreedPrice:N0} for {negotiation.Product?.ProductName}.",
                "Negotiation"
            );

            TempData["Success"] =
                $"Counter offer accepted at Rs. {negotiation.AgreedPrice:N0}.";

            return RedirectToAction("MyNegotiations");
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectNegotiation(int negotiationId)
        {
            if (!IsCustomerLoggedIn())
                return CustomerLogin();

            int customerId = GetCustomerId()!.Value;

            var negotiation = await _context.Negotiations
                .Include(n => n.Product)
                .Include(n => n.Shopkeeper)
                .FirstOrDefaultAsync(n =>
                    n.NegotiationId == negotiationId &&
                    n.CustomerId == customerId);

            if (negotiation == null)
            {
                TempData["Error"] =
                    "Negotiation not found.";

                return RedirectToAction("MyNegotiations");
            }

            if (!string.Equals(
                negotiation.Status,
                "CounterOffer",
                StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] =
                    "Only a counter offer can be rejected.";

                return RedirectToAction("MyNegotiations");
            }

            negotiation.Status =
                "Rejected";

            negotiation.RespondedDate =
                DateTime.Now;

            await _context.SaveChangesAsync();

            await CreateNotification(
                negotiation.ShopkeeperId,
                "Negotiation Rejected",
                $"Customer rejected your counter offer for {negotiation.Product?.ProductName}.",
                "Negotiation"
            );

            TempData["Success"] =
                "The counter offer has been rejected.";

            return RedirectToAction("MyNegotiations");
        }


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
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> AddToCart(
    int productId,
    int quantity,
    string? purchaseType,
    string? size,
    string[]? VariantSizes,
    int[]? VariantQuantities)
{
    if (!IsCustomerLoggedIn())
    {
        var pendingItems = new List<CustomerCartItem>();

        purchaseType =
            string.IsNullOrWhiteSpace(purchaseType)
                ? "Buy"
                : purchaseType.Trim();

        if (VariantSizes != null &&
            VariantQuantities != null &&
            VariantSizes.Length == VariantQuantities.Length &&
            VariantSizes.Length > 0)
        {
            for (int i = 0; i < VariantSizes.Length; i++)
            {
                string selectedSize =
                    VariantSizes[i]?.Trim() ?? "";

                int selectedQuantity =
                    VariantQuantities[i];

                if (string.IsNullOrWhiteSpace(selectedSize) ||
                    selectedQuantity <= 0)
                {
                    continue;
                }

                pendingItems.Add(
                    new CustomerCartItem
                    {
                        ProductId = productId,
                        Quantity = selectedQuantity,
                        Size = selectedSize,
                        PurchaseType = purchaseType
                    });
            }
        }
        else
        {
            pendingItems.Add(
                new CustomerCartItem
                {
                    ProductId = productId,
                    Quantity = quantity < 1 ? 1 : quantity,
                    Size = size,
                    PurchaseType = purchaseType
                });
        }

        if (pendingItems.Any())
        {
            SavePendingCartItems(pendingItems);
        }

        string returnUrl =
            Url.Action(
                "Cart",
                "Customer") ?? "/";

        return RedirectToAction(
            "Login",
            "Account",
            new { returnUrl });
    }

    var product = await _context.Products
        .Include(p => p.ProductVariants)
        .Include(p => p.Shop)
        .FirstOrDefaultAsync(p =>
            p.ProductId == productId &&
            p.Status);

    if (product == null)
    {
        TempData["Error"] = "Product not found.";
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

    purchaseType =
        string.IsNullOrWhiteSpace(purchaseType)
            ? "Buy"
            : purchaseType.Trim();

    if (purchaseType.Equals(
            "Rent",
            StringComparison.OrdinalIgnoreCase))
    {
        if (!product.Shop.OffersRent ||
            !product.IsAvailableForRent)
        {
            TempData["Error"] =
                "This product is no longer available for rent.";

            return RedirectToAction(
                "ProductDetails",
                new { id = productId });
        }
    }
    else if (purchaseType.Equals(
                 "Buy",
                 StringComparison.OrdinalIgnoreCase) ||
             purchaseType.Equals(
                 "Sale",
                 StringComparison.OrdinalIgnoreCase) ||
             purchaseType.Equals(
                 "Negotiated",
                 StringComparison.OrdinalIgnoreCase))
    {
        if (!product.Shop.OffersBuy ||
            !product.IsAvailableForBuy)
        {
            TempData["Error"] =
                "Buying is not available for this product.";

            return RedirectToAction(
                "ProductDetails",
                new { id = productId });
        }
    }

    var cartItems = GetCartItems();

    bool hasVariants =
        product.ProductVariants != null &&
        product.ProductVariants.Any();

    if (hasVariants &&
        !purchaseType.Equals(
            "Negotiated",
            StringComparison.OrdinalIgnoreCase))
    {
        if (VariantSizes == null ||
            VariantQuantities == null ||
            VariantSizes.Length == 0 ||
            VariantQuantities.Length == 0)
        {
            TempData["Error"] =
                "Please select at least one size and quantity.";

            return RedirectToAction(
                "ProductDetails",
                new { id = productId });
        }

        if (VariantSizes.Length != VariantQuantities.Length)
        {
            TempData["Error"] =
                "Invalid size and quantity selection.";

            return RedirectToAction(
                "ProductDetails",
                new { id = productId });
        }

        bool atLeastOneSelected = false;

        for (int i = 0; i < VariantSizes.Length; i++)
        {
            string selectedSize =
                VariantSizes[i]?.Trim() ?? "";

            int selectedQuantity =
                VariantQuantities[i];

            if (string.IsNullOrWhiteSpace(selectedSize))
                continue;

            if (selectedQuantity <= 0)
                continue;

            atLeastOneSelected = true;

            var variant =
                product.ProductVariants
                    .FirstOrDefault(v =>
                        string.Equals(
                            v.Size,
                            selectedSize,
                            StringComparison.OrdinalIgnoreCase));

            if (variant == null)
            {
                TempData["Error"] =
                    $"Size {selectedSize} is not available.";

                return RedirectToAction(
                    "ProductDetails",
                    new { id = productId });
            }

            if (variant.StockQuantity <= 0)
            {
                TempData["Error"] =
                    $"Size {variant.Size} is out of stock.";

                return RedirectToAction(
                    "ProductDetails",
                    new { id = productId });
            }

            if (selectedQuantity > variant.StockQuantity)
            {
                TempData["Error"] =
                    $"Only {variant.StockQuantity} item(s) are available in size {variant.Size}.";

                return RedirectToAction(
                    "ProductDetails",
                    new { id = productId });
            }

            decimal selectedPrice;

            if (purchaseType.Equals(
                    "Sale",
                    StringComparison.OrdinalIgnoreCase))
            {
                if (!product.Shop.OffersBuy ||
                    !product.IsAvailableForBuy ||
                    !product.IsOnSale ||
                    !product.SalePrice.HasValue ||
                    product.SalePrice.Value <= 0 ||
                    product.SalePrice.Value >= product.Price)
                {
                    TempData["Error"] =
                        "This sale price is no longer available.";

                    return RedirectToAction(
                        "ProductDetails",
                        new { id = productId });
                }

                selectedPrice =
                    product.SalePrice.Value;
            }
            else if (purchaseType.Equals(
                         "Rent",
                         StringComparison.OrdinalIgnoreCase))
            {
                if (!product.Shop.OffersRent ||
                    !product.IsAvailableForRent ||
                    !product.RentPrice.HasValue ||
                    product.RentPrice.Value <= 0)
                {
                    TempData["Error"] =
                        "This product is no longer available for rent.";

                    return RedirectToAction(
                        "ProductDetails",
                        new { id = productId });
                }

                selectedPrice =
                    product.RentPrice.Value;
            }
            else
            {
                if (!product.Shop.OffersBuy ||
                    !product.IsAvailableForBuy)
                {
                    TempData["Error"] =
                        "Buying is not available for this product.";

                    return RedirectToAction(
                        "ProductDetails",
                        new { id = productId });
                }

                selectedPrice =
                    product.Price;
            }

            var existingItem =
                cartItems.FirstOrDefault(x =>
                    x.ProductId == productId &&
                    string.Equals(
                        x.Size ?? "",
                        variant.Size ?? "",
                        StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(
                        x.PurchaseType ?? "Buy",
                        purchaseType,
                        StringComparison.OrdinalIgnoreCase));

            if (existingItem != null)
            {
                int newQuantity =
                    existingItem.Quantity +
                    selectedQuantity;

                if (newQuantity > variant.StockQuantity)
                {
                    TempData["Error"] =
                        $"You already have {existingItem.Quantity} item(s) of size {variant.Size} in your cart. " +
                        $"Only {variant.StockQuantity} item(s) are available.";

                    return RedirectToAction(
                        "ProductDetails",
                        new { id = productId });
                }

                existingItem.Quantity =
                    newQuantity;

                existingItem.Price =
                    selectedPrice;

                existingItem.ProductName =
                    product.ProductName;
            }
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
                            selectedQuantity,

                        Size =
                            variant.Size,

                        PurchaseType =
                            purchaseType,

                        NegotiationId =
                            null
                    });
            }
        }

        if (!atLeastOneSelected)
        {
            TempData["Error"] =
                "Please select at least one size and quantity.";

            return RedirectToAction(
                "ProductDetails",
                new { id = productId });
        }

        SaveCartItems(cartItems);

        TempData["Success"] =
            "Selected sizes and quantities were added to your cart.";

        return RedirectToAction("Cart");
    }

    if (!hasVariants)
    {
        if (quantity < 1)
            quantity = 1;

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

        decimal selectedPrice;

        if (purchaseType.Equals(
                "Sale",
                StringComparison.OrdinalIgnoreCase))
        {
            if (!product.Shop.OffersBuy ||
                !product.IsAvailableForBuy ||
                !product.IsOnSale ||
                !product.SalePrice.HasValue ||
                product.SalePrice.Value <= 0 ||
                product.SalePrice.Value >= product.Price)
            {
                TempData["Error"] =
                    "This sale price is no longer available.";

                return RedirectToAction(
                    "ProductDetails",
                    new { id = productId });
            }

            selectedPrice =
                product.SalePrice.Value;
        }
        else if (purchaseType.Equals(
                     "Rent",
                     StringComparison.OrdinalIgnoreCase))
        {
            if (!product.Shop.OffersRent ||
                !product.IsAvailableForRent ||
                !product.RentPrice.HasValue ||
                product.RentPrice.Value <= 0)
            {
                TempData["Error"] =
                    "This product is no longer available for rent.";

                return RedirectToAction(
                    "ProductDetails",
                    new { id = productId });
            }

            selectedPrice =
                product.RentPrice.Value;
        }
        else
        {
            if (!product.Shop.OffersBuy ||
                !product.IsAvailableForBuy)
            {
                TempData["Error"] =
                    "Buying is not available for this product.";

                return RedirectToAction(
                    "ProductDetails",
                    new { id = productId });
            }

            selectedPrice =
                product.Price;
        }

        var existingItem =
            cartItems.FirstOrDefault(x =>
                x.ProductId == productId &&
                string.Equals(
                    x.Size ?? "",
                    "",
                    StringComparison.OrdinalIgnoreCase) &&
                string.Equals(
                    x.PurchaseType ?? "Buy",
                    purchaseType,
                    StringComparison.OrdinalIgnoreCase));

        if (existingItem != null)
        {
            int newQuantity =
                existingItem.Quantity + quantity;

            if (newQuantity > product.StockQuantity)
            {
                TempData["Error"] =
                    $"Only {product.StockQuantity} item(s) are available.";

                return RedirectToAction(
                    "ProductDetails",
                    new { id = productId });
            }

            existingItem.Quantity =
                newQuantity;

            existingItem.Price =
                selectedPrice;
        }
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

                    Size =
                        null,

                    PurchaseType =
                        purchaseType,

                    NegotiationId =
                        null
                });
        }

        SaveCartItems(cartItems);

        TempData["Success"] =
            "Product added to your cart.";

        return RedirectToAction("Cart");
    }

    if (purchaseType.Equals(
            "Negotiated",
            StringComparison.OrdinalIgnoreCase))
    {
        if (!product.Shop.OffersBuy ||
            !product.IsAvailableForBuy)
        {
            TempData["Error"] =
                "Negotiation is not available for this product.";

            return RedirectToAction(
                "ProductDetails",
                new { id = productId });
        }

        if (string.IsNullOrWhiteSpace(size))
        {
            TempData["Error"] =
                "Please select a size.";

            return RedirectToAction(
                "ProductDetails",
                new { id = productId });
        }

        var variant =
            product.ProductVariants
                .FirstOrDefault(v =>
                    string.Equals(
                        v.Size,
                        size,
                        StringComparison.OrdinalIgnoreCase));

        if (variant == null ||
            variant.StockQuantity <= 0)
        {
            TempData["Error"] =
                "The selected size is out of stock.";

            return RedirectToAction(
                "ProductDetails",
                new { id = productId });
        }

        int customerId =
            GetCustomerId()!.Value;

        var negotiation =
            await _context.Negotiations
                .Where(n =>
                    n.ProductId == productId &&
                    n.CustomerId == customerId &&
                    n.Status == "Accepted" &&
                    n.AgreedPrice.HasValue &&
                    n.AgreedPrice.Value > 0)
                .OrderByDescending(
                    n => n.RespondedDate ?? n.CreatedDate)
                .FirstOrDefaultAsync();

        if (negotiation == null)
        {
            TempData["Error"] =
                "No accepted negotiated price is available.";

            return RedirectToAction(
                "ProductDetails",
                new { id = productId });
        }

        bool alreadyInCart =
            cartItems.Any(x =>
                x.ProductId == productId &&
                x.PurchaseType == "Negotiated");

        if (alreadyInCart)
        {
            TempData["Error"] =
                "This negotiated product is already in your cart.";

            return RedirectToAction(
                "ProductDetails",
                new { id = productId });
        }

        cartItems.Add(
            new CustomerCartItem
            {
                ProductId =
                    product.ProductId,

                ProductName =
                    product.ProductName,

                Price =
                    negotiation.AgreedPrice!.Value,

                Quantity =
                    1,

                Size =
                    variant.Size,

                PurchaseType =
                    "Negotiated",

                NegotiationId =
                    negotiation.NegotiationId
            });

        SaveCartItems(cartItems);

        TempData["Success"] =
            "Negotiated product added to your cart.";

        return RedirectToAction("Cart");
    }

    return RedirectToAction(
        "ProductDetails",
        new { id = productId });
}

[HttpGet]
public async Task<IActionResult> Cart()
{
    if (!IsCustomerLoggedIn())
        return CustomerLogin();

    int customerId =
        GetCustomerId()!.Value;

    var cartItems =
        GetCartItems();

    if (!cartItems.Any())
    {
        ViewBag.CartItems =
            cartItems;

        return View(
            new List<Product>());
    }

    var productIds =
        cartItems
            .Select(x => x.ProductId)
            .Distinct()
            .ToList();

    var products =
        await _context.Products
            .Include(p => p.Shop)
            .Include(p => p.ProductVariants)
            .Include(p => p.ProductImages)
            .Where(p =>
                productIds.Contains(p.ProductId) &&
                p.Status)
            .ToListAsync();

    var validIds =
        products
            .Select(p => p.ProductId)
            .ToHashSet();

    cartItems =
        cartItems
            .Where(x =>
                validIds.Contains(x.ProductId))
            .ToList();

    var itemsToRemove =
        new List<CustomerCartItem>();

    foreach (var item in cartItems)
    {
        var product =
            products.FirstOrDefault(
                p => p.ProductId == item.ProductId);

        if (product == null)
        {
            itemsToRemove.Add(item);
            continue;
        }

        if (product.Shop == null ||
            !product.Shop.Status ||
            !product.Shop.IsApproved)
        {
            itemsToRemove.Add(item);
            continue;
        }

        item.ProductName =
            product.ProductName;

        string purchaseType =
            string.IsNullOrWhiteSpace(
                item.PurchaseType)
                ? "Buy"
                : item.PurchaseType.Trim();

        if (purchaseType.Equals(
                "Rent",
                StringComparison.OrdinalIgnoreCase))
        {
            if (!product.Shop.OffersRent ||
                !product.IsAvailableForRent)
            {
                itemsToRemove.Add(item);
                continue;
            }
        }
        else
        {
            if (!product.Shop.OffersBuy ||
                !product.IsAvailableForBuy)
            {
                itemsToRemove.Add(item);
                continue;
            }
        }

        bool hasVariants =
            product.ProductVariants != null &&
            product.ProductVariants.Any();

        ProductVariant? selectedVariant =
            null;

        if (hasVariants)
        {
            if (string.IsNullOrWhiteSpace(item.Size))
            {
                itemsToRemove.Add(item);
                continue;
            }

            selectedVariant =
                product.ProductVariants
                    .FirstOrDefault(v =>
                        string.Equals(
                            v.Size,
                            item.Size,
                            StringComparison.OrdinalIgnoreCase));

            if (selectedVariant == null ||
                selectedVariant.StockQuantity <= 0)
            {
                itemsToRemove.Add(item);
                continue;
            }

            if (item.Quantity >
                selectedVariant.StockQuantity)
            {
                item.Quantity =
                    selectedVariant.StockQuantity;
            }

            if (item.Quantity < 1)
            {
                itemsToRemove.Add(item);
                continue;
            }

            item.Size =
                selectedVariant.Size;
        }
        else
        {
            if (product.StockQuantity <= 0)
            {
                itemsToRemove.Add(item);
                continue;
            }

            if (item.Quantity >
                product.StockQuantity)
            {
                item.Quantity =
                    product.StockQuantity;
            }

            if (item.Quantity < 1)
            {
                itemsToRemove.Add(item);
                continue;
            }

            item.Size =
                null;
        }

        if (purchaseType.Equals(
                "Rent",
                StringComparison.OrdinalIgnoreCase))
        {
            if (product.Shop.OffersRent &&
                product.IsAvailableForRent &&
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

        if (purchaseType.Equals(
                "Negotiated",
                StringComparison.OrdinalIgnoreCase))
        {
            Negotiation? negotiation =
                null;

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
                            n.Status ==
                                "Accepted" &&
                            n.AgreedPrice.HasValue &&
                            n.AgreedPrice.Value > 0);
            }

            if (negotiation == null)
            {
                negotiation =
                    await _context.Negotiations
                        .Where(n =>
                            n.ProductId ==
                                product.ProductId &&
                            n.CustomerId ==
                                customerId &&
                            n.Status ==
                                "Accepted" &&
                            n.AgreedPrice.HasValue &&
                            n.AgreedPrice.Value > 0)
                        .OrderByDescending(n =>
                            n.RespondedDate ??
                            n.CreatedDate)
                        .FirstOrDefaultAsync();
            }

            if (negotiation != null &&
                negotiation.AgreedPrice.HasValue)
            {
                item.Price =
                    negotiation.AgreedPrice.Value;

                item.PurchaseType =
                    "Negotiated";

                item.NegotiationId =
                    negotiation.NegotiationId;

                item.Quantity =
                    1;
            }
            else
            {
                itemsToRemove.Add(item);
            }

            continue;
        }

        if (purchaseType.Equals(
                "Sale",
                StringComparison.OrdinalIgnoreCase))
        {
            if (product.Shop.OffersBuy &&
                product.IsAvailableForBuy &&
                product.IsOnSale &&
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
                itemsToRemove.Add(item);
            }

            continue;
        }

        item.Price =
            product.Price;

        item.PurchaseType =
            "Buy";

        item.NegotiationId =
            null;
    }

    foreach (var item in itemsToRemove)
    {
        cartItems.Remove(item);
    }

    SaveCartItems(cartItems);

    ViewBag.CartItems =
        cartItems;

    return View(products);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateCartQuantity(
            int productId,
            int quantity,
            string? size,
            string? purchaseType)
        {
            if (!IsCustomerLoggedIn())
                return CustomerLogin();

            if (quantity < 1)
                quantity = 1;

            var product =
                await _context.Products
                    .Include(p => p.ProductVariants)
                    .Include(p => p.Shop)
                    .FirstOrDefaultAsync(p =>
                        p.ProductId == productId &&
                        p.Status);

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

            string selectedPurchaseType =
                string.IsNullOrWhiteSpace(purchaseType)
                    ? "Buy"
                    : purchaseType.Trim();

            if (selectedPurchaseType.Equals(
                    "Rent",
                    StringComparison.OrdinalIgnoreCase))
            {
                if (!product.Shop.OffersRent ||
                    !product.IsAvailableForRent)
                {
                    TempData["Error"] =
                        "This product is no longer available for rent.";

                    return RedirectToAction("Cart");
                }
            }
            else
            {
                if (!product.Shop.OffersBuy ||
                    !product.IsAvailableForBuy)
                {
                    TempData["Error"] =
                        "Buying is not available for this product.";

                    return RedirectToAction("Cart");
                }
            }

            var cartItems =
                GetCartItems();

            var item =
                cartItems.FirstOrDefault(x =>
                    x.ProductId == productId &&
                    string.Equals(
                        x.Size ?? "",
                        size ?? "",
                        StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(
                        x.PurchaseType ?? "Buy",
                        selectedPurchaseType,
                        StringComparison.OrdinalIgnoreCase));

            if (item == null)
            {
                TempData["Error"] =
                    "The selected product is not in your cart.";

                return RedirectToAction("Cart");
            }

            bool hasVariants =
                product.ProductVariants != null &&
                product.ProductVariants.Any();

            if (hasVariants)
            {
                if (string.IsNullOrWhiteSpace(size))
                {
                    TempData["Error"] =
                        "Please select a size.";

                    return RedirectToAction("Cart");
                }

                var selectedVariant =
                    product.ProductVariants.FirstOrDefault(v =>
                        string.Equals(
                            v.Size,
                            size,
                            StringComparison.OrdinalIgnoreCase));

                if (selectedVariant == null)
                {
                    TempData["Error"] =
                        "The selected size is not available.";

                    return RedirectToAction("Cart");
                }

                if (selectedVariant.StockQuantity <= 0)
                {
                    TempData["Error"] =
                        $"Size {selectedVariant.Size} is currently out of stock.";

                    return RedirectToAction("Cart");
                }

                if (quantity > selectedVariant.StockQuantity)
                {
                    TempData["Error"] =
                        $"Only {selectedVariant.StockQuantity} item(s) are available in size {selectedVariant.Size}.";

                    return RedirectToAction("Cart");
                }

                item.Size =
                    selectedVariant.Size;
            }
            else
            {
                if (product.StockQuantity <= 0)
                {
                    TempData["Error"] =
                        "This product is currently out of stock.";

                    return RedirectToAction("Cart");
                }

                if (quantity > product.StockQuantity)
                {
                    TempData["Error"] =
                        $"Only {product.StockQuantity} item(s) are available.";

                    return RedirectToAction("Cart");
                }

                item.Size =
                    null;
            }

            item.Quantity =
                quantity;

            item.ProductName =
                product.ProductName;

            if (selectedPurchaseType.Equals(
                    "Rent",
                    StringComparison.OrdinalIgnoreCase))
            {
                if (product.Shop.OffersRent &&
                    product.IsAvailableForRent &&
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
                    TempData["Error"] =
                        "This product is no longer available for rent.";

                    return RedirectToAction("Cart");
                }
            }
            else if (selectedPurchaseType.Equals(
                         "Negotiated",
                         StringComparison.OrdinalIgnoreCase))
            {
                if (!product.Shop.OffersBuy ||
                    !product.IsAvailableForBuy)
                {
                    TempData["Error"] =
                        "Negotiation is not available for this product.";

                    return RedirectToAction("Cart");
                }

                int customerId =
                    GetCustomerId()!.Value;

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
                        "The accepted negotiated price is no longer available.";

                    return RedirectToAction("Cart");
                }

                item.Price =
                    negotiation.AgreedPrice!.Value;

                item.PurchaseType =
                    "Negotiated";

                item.NegotiationId =
                    negotiation.NegotiationId;

                item.Quantity =
                    1;
            }
            else if (selectedPurchaseType.Equals(
                         "Sale",
                         StringComparison.OrdinalIgnoreCase))
            {
                if (product.Shop.OffersBuy &&
                    product.IsAvailableForBuy &&
                    product.IsOnSale &&
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
                    TempData["Error"] =
                        "This sale price is no longer available.";

                    return RedirectToAction("Cart");
                }
            }
            else
            {
                if (!product.Shop.OffersBuy ||
                    !product.IsAvailableForBuy)
                {
                    TempData["Error"] =
                        "Buying is not available for this product.";

                    return RedirectToAction("Cart");
                }

                item.Price =
                    product.Price;

                item.PurchaseType =
                    "Buy";

                item.NegotiationId =
                    null;
            }

            SaveCartItems(cartItems);

            TempData["Success"] =
                "Cart quantity updated.";

            return RedirectToAction("Cart");
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RemoveFromCart(
             int productId,
             string? size,
             string? purchaseType)
        {
            if (!IsCustomerLoggedIn())
                return CustomerLogin();

            var cartItems = GetCartItems();

            var item = cartItems.FirstOrDefault(x =>
                x.ProductId == productId &&
                string.Equals(
                    x.Size ?? "",
                    size ?? "",
                    StringComparison.OrdinalIgnoreCase) &&
                string.Equals(
                    x.PurchaseType ?? "Buy",
                    purchaseType ?? "Buy",
                    StringComparison.OrdinalIgnoreCase));

            if (item == null)
            {
                TempData["Error"] =
                    "The selected product is not in your cart.";

                return RedirectToAction("Cart");
            }

            cartItems.Remove(item);

            SaveCartItems(cartItems);

            TempData["Success"] =
                "Product removed from cart.";

            return RedirectToAction("Cart");
        }

        [HttpGet]
        public async Task<IActionResult> CustomizeProduct(
     int id,
     string? size)
        {
            if (!IsCustomerLoggedIn())
                return CustomerLogin();

            var cartItems = GetCartItems();

            var cartItem = cartItems.FirstOrDefault(x =>
                x.ProductId == id &&
                string.Equals(
                    x.Size ?? "",
                    size ?? "",
                    StringComparison.OrdinalIgnoreCase));

            if (cartItem == null)
            {
                TempData["Error"] =
                    "Please add this product to your cart first.";

                return RedirectToAction("Cart");
            }

            var product = await _context.Products
                .Include(p => p.Shop)
                .FirstOrDefaultAsync(p =>
                    p.ProductId == id &&
                    p.Status);

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

            if (!product.Shop.OffersCustomization)
            {
                TempData["Error"] =
                    "Customization is not available from this shop.";

                return RedirectToAction("Cart");
            }

            if (!product.HasCustomMeasurement)
            {
                TempData["Error"] =
                    "Custom measurements are not available for this product.";

                return RedirectToAction("Cart");
            }

            if (!IsDressProduct(product))
            {
                TempData["Error"] =
                    "Custom measurements are only available for dress products.";

                return RedirectToAction("Cart");
            }

            ViewBag.SelectedSize = cartItem.Size;
            ViewBag.CustomMeasurements = cartItem.CustomMeasurements;

            return View(product);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveCustomMeasurements(
     int productId,
     string? size,
     string customMeasurements,
     string? customerNotes,
     List<IFormFile>? referenceImages)
        {
            if (!IsCustomerLoggedIn())
                return CustomerLogin();

            if (string.IsNullOrWhiteSpace(customMeasurements))
            {
                TempData["Error"] =
                    "Please add at least one measurement.";

                return RedirectToAction(
                    "CustomizeProduct",
                    new
                    {
                        id = productId,
                        size = size
                    });
            }

            var product = await _context.Products
                .Include(p => p.Shop)
                .FirstOrDefaultAsync(p =>
                    p.ProductId == productId &&
                    p.Status);

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

            if (!product.Shop.OffersCustomization)
            {
                TempData["Error"] =
                    "Customization is not available from this shop.";

                return RedirectToAction("Cart");
            }

            if (!product.HasCustomMeasurement)
            {
                TempData["Error"] =
                    "Custom measurements are not available for this product.";

                return RedirectToAction("Cart");
            }

            if (!IsDressProduct(product))
            {
                TempData["Error"] =
                    "Custom measurements are only available for dress products.";

                return RedirectToAction("Cart");
            }

            var cartItems = GetCartItems();

            var cartItem = cartItems.FirstOrDefault(x =>
                x.ProductId == productId &&
                string.Equals(
                    x.Size ?? "",
                    size ?? "",
                    StringComparison.OrdinalIgnoreCase));

            if (cartItem == null)
            {
                TempData["Error"] =
                    "This product is not in your cart.";

                return RedirectToAction("Cart");
            }

            var submittedMeasurements =
                customMeasurements
                    .Split(
                        '|',
                        StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToList();

            if (!submittedMeasurements.Any())
            {
                TempData["Error"] =
                    "Please add at least one measurement.";

                return RedirectToAction(
                    "CustomizeProduct",
                    new
                    {
                        id = productId,
                        size = size
                    });
            }

            var submittedNames =
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase);

            foreach (var measurement in submittedMeasurements)
            {
                int separatorIndex =
                    measurement.IndexOf(':');

                if (separatorIndex <= 0)
                {
                    TempData["Error"] =
                        "Invalid measurement format.";

                    return RedirectToAction(
                        "CustomizeProduct",
                        new
                        {
                            id = productId,
                            size = size
                        });
                }

                string measurementName =
                    measurement
                        .Substring(0, separatorIndex)
                        .Trim();

                string measurementValue =
                    measurement
                        .Substring(separatorIndex + 1)
                        .Trim();

                if (string.IsNullOrWhiteSpace(measurementName))
                {
                    TempData["Error"] =
                        "Measurement name cannot be empty.";

                    return RedirectToAction(
                        "CustomizeProduct",
                        new
                        {
                            id = productId,
                            size = size
                        });
                }

                if (string.IsNullOrWhiteSpace(measurementValue))
                {
                    TempData["Error"] =
                        "Please enter a value for " +
                        measurementName + ".";

                    return RedirectToAction(
                        "CustomizeProduct",
                        new
                        {
                            id = productId,
                            size = size
                        });
                }

                if (!submittedNames.Add(measurementName))
                {
                    TempData["Error"] =
                        "Duplicate measurement detected: " +
                        measurementName;

                    return RedirectToAction(
                        "CustomizeProduct",
                        new
                        {
                            id = productId,
                            size = size
                        });
                }
            }

            string? referenceImagePaths = null;

            if (referenceImages != null &&
                referenceImages.Count > 0)
            {
                var imagePaths =
                    new List<string>();

                var allowedExtensions =
                    new[]
                    {
                ".jpg",
                ".jpeg",
                ".png",
                ".webp"
                    };

                foreach (var image in referenceImages)
                {
                    if (image == null ||
                        image.Length == 0)
                        continue;

                    if (image.Length >
                        5 * 1024 * 1024)
                    {
                        TempData["Error"] =
                            "Each reference image must be 5 MB or smaller.";

                        return RedirectToAction(
                            "CustomizeProduct",
                            new
                            {
                                id = productId,
                                size = size
                            });
                    }

                    string extension =
                        Path.GetExtension(
                            image.FileName)
                            .ToLowerInvariant();

                    if (!allowedExtensions.Contains(
                            extension))
                    {
                        TempData["Error"] =
                            "Only JPG, JPEG, PNG and WEBP reference images are allowed.";

                        return RedirectToAction(
                            "CustomizeProduct",
                            new
                            {
                                id = productId,
                                size = size
                            });
                    }

                    string uploadsFolder =
                        Path.Combine(
                            Directory.GetCurrentDirectory(),
                            "wwwroot",
                            "uploads",
                            "customizations");

                    if (!Directory.Exists(
                            uploadsFolder))
                    {
                        Directory.CreateDirectory(
                            uploadsFolder);
                    }

                    string fileName =
                        Guid.NewGuid()
                        .ToString() +
                        extension;

                    string filePath =
                        Path.Combine(
                            uploadsFolder,
                            fileName);

                    using (
                        var stream =
                            new FileStream(
                                filePath,
                                FileMode.Create))
                    {
                        await image.CopyToAsync(stream);
                    }

                    imagePaths.Add(
                        "/uploads/customizations/" +
                        fileName);
                }

                if (imagePaths.Any())
                {
                    referenceImagePaths =
                        string.Join(
                            "|",
                            imagePaths);
                }
            }

            int customerId =
                GetCustomerId()!.Value;

            var customizationRequest =
                new CustomizationRequest
                {
                    ProductId = product.ProductId,

                    CustomerId = customerId,

                    ShopId = product.Shop.ShopId,

                    SelectedSize = size,

                    Measurements =
                        string.Join(
                            " | ",
                            submittedMeasurements),

                    ReferenceImages =
                        referenceImagePaths,

                    CustomerNotes =
                        string.IsNullOrWhiteSpace(
                            customerNotes)
                            ? null
                            : customerNotes.Trim(),

                    Status = "Pending",

                    CreatedDate = DateTime.Now
                };

            _context.CustomizationRequests.Add(
                customizationRequest);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Your customization request has been sent to the shopkeeper. You will receive the customized price after the shopkeeper reviews your request.";

            return RedirectToAction(
                "CustomizationRequests");
        }


       

        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            if (!IsCustomerLoggedIn())
                return CustomerLogin();

            int customerId =
                GetCustomerId()!.Value;

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

            decimal productTotal = 0m;

            var checkoutPrices =
                new Dictionary<int, decimal>();

            var checkoutPurchaseTypes =
                new Dictionary<int, string>();

            var checkoutNegotiationIds =
                new Dictionary<int, int?>();

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

                int? negotiationId = null;

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

                    purchaseType =
                        "Negotiated";
                }
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
                else
                {
                    finalUnitPrice =
                        product.Price;

                    purchaseType =
                        "Buy";
                }

                checkoutPrices[
                    product.ProductId] =
                    finalUnitPrice;

                checkoutPurchaseTypes[
                    product.ProductId] =
                    purchaseType;

                checkoutNegotiationIds[
                    product.ProductId] =
                    negotiationId;

                item.Price =
                    finalUnitPrice;

                item.PurchaseType =
                    purchaseType;

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

            decimal refundableSecurity = 0m;

            foreach (var item in cartItems)
            {
                if (!item.PurchaseType.Equals(
                    "Rent",
                    StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var product =
                    products.First(
                        p =>
                            p.ProductId ==
                            item.ProductId
                    );

                decimal securityPerItem =
                    product.RentalSecurity ?? 0m;

                refundableSecurity +=
                    securityPerItem *
                    item.Quantity;
            }

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
                serviceFee +
                refundableSecurity;

            decimal shopkeeperAmount =
                productTotal;

            ViewBag.CustomerName =
                customer.Name;

            ViewBag.CustomerPhone =
                customer.Phone;

            ViewBag.CustomerAddress =
                customer.Address;
            ViewBag.EasypaisaAccount =
               _configuration["PaymentSettings:EasypaisaAccount"];


            ViewBag.JazzCashAccount =
                _configuration["PaymentSettings:JazzCashAccount"];


            ViewBag.DeliveryCharges =
                deliveryCharges;

            ViewBag.ProductTotal =
                productTotal;

            ViewBag.ServiceFee =
                serviceFee;

            ViewBag.RefundableSecurity =
                refundableSecurity;

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

            ViewBag.CheckoutPrices =
                checkoutPrices;

            ViewBag.CheckoutPurchaseTypes =
                checkoutPurchaseTypes;

            ViewBag.CheckoutQuantities =
                cartItems.ToDictionary(
                    x => x.ProductId,
                    x => x.Quantity
                );
            ViewBag.CheckoutMeasurements =
                cartItems.ToDictionary(
                    x => x.ProductId,
                    x => x.CustomMeasurements ?? ""
                );
            ViewBag.CheckoutNegotiationIds =
                checkoutNegotiationIds;

            return View(products);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(
        string? cartData,
        string? deliveryCity,
        string deliveryAddress,
        string? notes,
        string paymentMethod,
        string? transactionId,
        IFormFile? transactionImage,
        string? refundPreferenceMethod,
        string? refundPreferenceAccount)
        {
            if (!IsCustomerLoggedIn())
                return CustomerLogin();

            int customerId = GetCustomerId()!.Value;

            var customer = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == customerId);

            if (customer == null)
            {
                HttpContext.Session.Clear();

                TempData["Error"] = "Customer account could not be found.";

                return RedirectToAction("Login", "Account");
            }

            if (!customer.Status)
            {
                TempData["Error"] =
                    "Your customer account has been disabled.";

                return RedirectToAction("Dashboard");
            }

            if (string.IsNullOrWhiteSpace(customer.Name))
            {
                TempData["Error"] =
                    "Your account name is missing. Please update your profile before placing an order.";

                return RedirectToAction("Profile", "Account");
            }

            if (string.IsNullOrWhiteSpace(customer.Phone))
            {
                TempData["Error"] =
                    "Your phone number is required before placing an order. Please update your profile.";

                return RedirectToAction("Profile", "Account");
            }

            if (string.IsNullOrWhiteSpace(customer.Address))
            {
                TempData["Error"] =
                    "Your account address is required before placing an order. Please update your profile.";

                return RedirectToAction("Profile", "Account");
            }

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

            deliveryCity = "Rawalpindi";

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

            if (string.IsNullOrWhiteSpace(paymentMethod))
            {
                TempData["Error"] =
                    "Please select a payment method.";

                return RedirectToAction("Checkout");
            }

            paymentMethod = paymentMethod.Trim();

            string? normalizedPaymentMethod =
                AllowedPaymentMethods
                    .FirstOrDefault(x =>
                        x.Equals(
                            paymentMethod,
                            StringComparison.OrdinalIgnoreCase));

            if (normalizedPaymentMethod == null)
            {
                TempData["Error"] =
                    "Invalid payment method.";

                return RedirectToAction("Checkout");
            }

            paymentMethod = normalizedPaymentMethod;

            var cartItems = GetCartItems();

            if (!cartItems.Any())
            {
                TempData["Error"] =
                    "Your cart is empty.";

                return RedirectToAction("Cart");
            }

            var productIds = cartItems
                .Select(x => x.ProductId)
                .Distinct()
                .ToList();

            var products = await _context.Products
                .Include(p => p.Shop)
                .Where(p =>
                    productIds.Contains(p.ProductId) &&
                    p.Status)
                .ToListAsync();

            if (products.Count != productIds.Count)
            {
                TempData["Error"] =
                    "One or more products in your cart are no longer available.";

                return RedirectToAction("Cart");
            }

            int shopId = products.First().ShopId;

            if (!products.All(p => p.ShopId == shopId))
            {
                TempData["Error"] =
                    "You can only place an order from one shop at a time.";

                return RedirectToAction("Cart");
            }

            var shop = products.First().Shop;

            if (shop == null ||
                !shop.Status ||
                !shop.IsApproved)
            {
                TempData["Error"] =
                    "The selected shop is currently unavailable.";

                return RedirectToAction("Shops");
            }

            decimal productTotal = 0m;

            decimal refundableSecurity = 0m;

            bool containsRental = false;

            var finalPrices =
                new Dictionary<int, decimal>();

            var finalPurchaseTypes =
                new Dictionary<int, string>();

            foreach (var item in cartItems)
            {
                if (item.Quantity < 1)
                {
                    TempData["Error"] =
                        "Invalid product quantity.";

                    return RedirectToAction("Cart");
                }

                var product = products.FirstOrDefault(
                    p => p.ProductId == item.ProductId);

                if (product == null)
                {
                    TempData["Error"] =
                        "A product in your cart could not be found.";

                    return RedirectToAction("Cart");
                }

                if (item.Quantity > product.StockQuantity)
                {
                    TempData["Error"] =
                        $"Only {product.StockQuantity} unit(s) of {product.ProductName} are available.";

                    return RedirectToAction("Cart");
                }

                string purchaseType =
     string.IsNullOrWhiteSpace(item.PurchaseType)
         ? "Buy"
         : item.PurchaseType.Trim();

                bool isCustomized =
                    !string.IsNullOrWhiteSpace(item.CustomMeasurements);

                decimal finalUnitPrice;

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

                    purchaseType = "Rent";

                    containsRental = true;

                    decimal securityPerItem =
                        product.RentalSecurity ?? 0m;

                    refundableSecurity +=
                        securityPerItem * item.Quantity;
                }
                else if (purchaseType.Equals(
                    "Negotiated",
                    StringComparison.OrdinalIgnoreCase))
                {
                    var negotiation =
                        await _context.Negotiations
                            .Where(n =>
                                n.ProductId == product.ProductId &&
                                n.CustomerId == customerId &&
                                n.Status == "Accepted" &&
                                n.AgreedPrice.HasValue)
                            .OrderByDescending(n =>
                                n.RespondedDate ??
                                n.CreatedDate)
                            .FirstOrDefaultAsync();

                    if (negotiation == null ||
                        !negotiation.AgreedPrice.HasValue ||
                        negotiation.AgreedPrice.Value <= 0)
                    {
                        TempData["Error"] =
                            $"The negotiated price for {product.ProductName} is no longer valid.";

                        return RedirectToAction("Checkout");
                    }

                    finalUnitPrice =
                        negotiation.AgreedPrice.Value;

                    purchaseType = "Negotiated";
                }
                else if (purchaseType.Equals(
                    "Sale",
                    StringComparison.OrdinalIgnoreCase))
                {
                    if (!product.IsOnSale ||
                        !product.SalePrice.HasValue ||
                        product.SalePrice.Value <= 0 ||
                        product.SalePrice.Value >= product.Price)
                    {
                        TempData["Error"] =
                            $"The sale price for {product.ProductName} is no longer available.";

                        return RedirectToAction("Cart");
                    }

                    finalUnitPrice =
                        product.SalePrice.Value;

                    purchaseType = "Sale";
                }
                else
                {
                    if (product.IsOnSale &&
                        product.SalePrice.HasValue &&
                        product.SalePrice.Value > 0 &&
                        product.SalePrice.Value < product.Price)
                    {
                        finalUnitPrice =
                            product.SalePrice.Value;

                        purchaseType = "Sale";
                    }
                    else
                    {
                        finalUnitPrice =
                            product.Price;

                        purchaseType = "Buy";
                    }
                }

                if (isCustomized)
                {
                    purchaseType =
                        "Customize/" + purchaseType;
                }

                finalPrices[product.ProductId] =
                    finalUnitPrice;

                finalPurchaseTypes[product.ProductId] =
                    purchaseType;

                productTotal +=
                    finalUnitPrice * item.Quantity;
            }

            if (productTotal <= 0)
            {
                TempData["Error"] =
                    "Invalid cart amount.";

                return RedirectToAction("Cart");
            }

            if (containsRental)
            {
                if (string.IsNullOrWhiteSpace(
                    refundPreferenceMethod))
                {
                    TempData["Error"] =
                        "Please select Easypaisa or JazzCash for your rental security refund.";

                    return RedirectToAction("Checkout");
                }

                refundPreferenceMethod =
                    refundPreferenceMethod.Trim();

                if (!refundPreferenceMethod.Equals(
                        "Easypaisa",
                        StringComparison.OrdinalIgnoreCase) &&
                    !refundPreferenceMethod.Equals(
                        "JazzCash",
                        StringComparison.OrdinalIgnoreCase))
                {
                    TempData["Error"] =
                        "Only Easypaisa and JazzCash are available for rental security refunds.";

                    return RedirectToAction("Checkout");
                }

                refundPreferenceMethod =
                    refundPreferenceMethod.Equals(
                        "Easypaisa",
                        StringComparison.OrdinalIgnoreCase)
                            ? "Easypaisa"
                            : "JazzCash";

                if (string.IsNullOrWhiteSpace(
                    refundPreferenceAccount))
                {
                    TempData["Error"] =
                        "Please enter your Easypaisa or JazzCash account number for the rental security refund.";

                    return RedirectToAction("Checkout");
                }

                refundPreferenceAccount =
                    refundPreferenceAccount.Trim();

                if (refundPreferenceAccount.Length < 10 ||
                    refundPreferenceAccount.Length > 20)
                {
                    TempData["Error"] =
                        "Please enter a valid Easypaisa or JazzCash account number.";

                    return RedirectToAction("Checkout");
                }
            }
            else
            {
                refundPreferenceMethod = null;
                refundPreferenceAccount = null;
                refundableSecurity = 0m;
            }

            decimal deliveryCharges =
                DeliveryCharge;

            decimal serviceFee =
                Math.Round(
                    productTotal * ServiceFeeRate,
                    2);

            decimal totalAmount =
                productTotal +
                deliveryCharges +
                serviceFee +
                refundableSecurity;

            decimal shopkeeperAmount =
                productTotal;

            bool paymentReceived = false;

            string paymentStatus = "Pending";

            string? savedTransactionImage = null;

            if (paymentMethod.Equals(
                "Cash on Delivery",
                StringComparison.OrdinalIgnoreCase))
            {
                paymentReceived = false;
                paymentStatus = "Pending";
                transactionId = null;
            }
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

                if (transactionId.Length < 3 ||
                    transactionId.Length > 100)
                {
                    TempData["Error"] =
                        "Please enter a valid transaction ID.";

                    return RedirectToAction("Checkout");
                }

                if (transactionImage == null ||
                    transactionImage.Length == 0)
                {
                    TempData["Error"] =
                        "Please upload the payment screenshot.";

                    return RedirectToAction("Checkout");
                }

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

                string uploadsFolder =
                    Path.Combine(
                        _environment.WebRootPath,
                        "uploads");

                if (!Directory.Exists(
                    uploadsFolder))
                {
                    Directory.CreateDirectory(
                        uploadsFolder);
                }

                string fileName =
                    "payment_" +
                    Guid.NewGuid().ToString("N") +
                    extension;

                string filePath =
                    Path.Combine(
                        uploadsFolder,
                        fileName);

                using (var stream =
                    new FileStream(
                        filePath,
                        FileMode.CreateNew))
                {
                    await transactionImage.CopyToAsync(
                        stream);
                }

                savedTransactionImage =
                    "/uploads/" + fileName;

                paymentReceived = false;
                paymentStatus = "Pending";
            }

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                var order = new Order
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
                        deliveryAddress,

                    Notes =
                        string.IsNullOrWhiteSpace(notes)
                            ? null
                            : notes.Trim(),

                    ProductTotal =
                        productTotal,

                    DeliveryCharges =
                        deliveryCharges,

                    RefundableSecurity =
                        refundableSecurity,

                    ServiceFee =
                        serviceFee,

                    TotalAmount =
                        totalAmount,

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

                foreach (var cartItem in cartItems)
                {
                    var product =
                        products.FirstOrDefault(
                            p => p.ProductId ==
                                 cartItem.ProductId);

                    if (product == null)
                    {
                        throw new InvalidOperationException(
                            "A product could not be found while creating the order.");
                    }

                    if (cartItem.Quantity >
                        product.StockQuantity)
                    {
                        throw new InvalidOperationException(
                            $"The available stock for {product.ProductName} changed while placing the order.");
                    }

                    decimal unitPrice =
                        finalPrices[
                            product.ProductId];

                    decimal subtotal =
                        unitPrice *
                        cartItem.Quantity;

                    string purchaseType =
                        finalPurchaseTypes[
                            product.ProductId];

                    bool isRental =
                        purchaseType.Contains(
                            "Rent",
                            StringComparison.OrdinalIgnoreCase);

                    var orderDetail =
                        new OrderDetail
                        {
                            OrderId =
                                order.OrderId,

                            ProductId =
                                product.ProductId,

                            Quantity =
                                cartItem.Quantity,

                            UnitPrice =
                                unitPrice,

                            SubTotal =
                                subtotal,

                            CustomMeasurements =
                                cartItem.CustomMeasurements,

                            PurchaseType =
                                purchaseType,

                            RentalSecurity =
                                isRental
                                    ? product.RentalSecurity ?? 0m
                                    : 0m,

                            RentalDuration =
                                isRental
                                    ? product.RentalDuration
                                    : null,

                            RentalConditions =
                                isRental
                                    ? product.RentalConditions
                                    : null,

                            RentalReturnStatus =
                                isRental
                                    ? "Awaiting Return"
                                    : "Not Required",

                            RefundPreferenceMethod =
                                isRental
                                    ? refundPreferenceMethod
                                    : null,

                            RefundPreferenceAccount =
                                isRental
                                    ? refundPreferenceAccount
                                    : null,

                            SecurityRefunded =
                                false,

                            SecurityRefundDate =
                                null,

                            SecurityRefundMethod =
                                null,

                            SecurityRefundAccount =
                                null,

                            SecurityRefundTransactionId =
                                null,

                            SecurityRefundNotes =
                                null
                        };

                    _context.OrderDetails.Add(
                        orderDetail);

                    product.StockQuantity -=
                        cartItem.Quantity;
                }

                await _context.SaveChangesAsync();

                await CreateNotification(
                    customerId,
                    "Order Placed",
                    $"Your order #{order.OrderId} has been placed successfully. Total amount is Rs. {totalAmount:N2}.",
                    "Order",
                    order.OrderId);

                if (shop.ShopkeeperId > 0)
                {
                    await CreateNotification(
                        shop.ShopkeeperId,
                        "New Order Received",
                        $"A new order #{order.OrderId} has been placed for your shop.",
                        "Order",
                        order.OrderId);
                }

                var admin =
                    await _context.Users
                        .Include(u => u.Role)
                        .FirstOrDefaultAsync(
                            u =>
                                u.Role != null &&
                                u.Role.RoleName == "Admin");

                if (admin != null)
                {
                    await CreateNotification(
                        admin.UserId,
                        "New Customer Order",
                        $"Customer {customer.Name} placed order #{order.OrderId}. Payment method: {paymentMethod}.",
                        "Order",
                        order.OrderId);
                }

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                ClearCart();

                TempData["Success"] =
                    $"Order #{order.OrderId} placed successfully.";

                return RedirectToAction(
                    "OrderDetails",
                    new
                    {
                        id = order.OrderId
                    });
            }
            catch (Exception ex)
            {
                try
                {
                    await transaction.RollbackAsync();
                }
                catch
                {
                }

                if (!string.IsNullOrWhiteSpace(
                    savedTransactionImage))
                {
                    string relativePath =
                        savedTransactionImage
                            .TrimStart('/')
                            .Replace(
                                '/',
                                Path.DirectorySeparatorChar);

                    string uploadedFilePath =
                        Path.Combine(
                            _environment.WebRootPath,
                            relativePath);

                    if (System.IO.File.Exists(
                        uploadedFilePath))
                    {
                        try
                        {
                            System.IO.File.Delete(
                                uploadedFilePath);
                        }
                        catch
                        {
                        }
                    }
                }

                string errorMessage =
                    ex.InnerException?.Message ??
                    ex.Message;

                TempData["Error"] =
                    "Order could not be placed: " +
                    errorMessage;

                return RedirectToAction("Checkout");
            }
        }

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
                        .ThenInclude(od => od.Product)
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
                        .ThenInclude(s => s!.Shopkeeper)
                    .Include(o => o.Delivery)
                    .Include(o => o.OrderDetails)
                        .ThenInclude(od => od.Product)
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestRentalReturn(
            int orderDetailId,
            string refundPreferenceMethod,
            string refundPreferenceAccount)
        {
            if (!IsCustomerLoggedIn())
                return CustomerLogin();

            int customerId = GetCustomerId()!.Value;

            var orderDetail = await _context.OrderDetails
                .Include(od => od.Order)
                .Include(od => od.Product)
                .FirstOrDefaultAsync(od =>
                    od.OrderDetailId == orderDetailId &&
                    od.Order.CustomerId == customerId);

            if (orderDetail == null)
            {
                TempData["Error"] = "Rental product not found.";

                return RedirectToAction("MyOrders");
            }

            if (!string.Equals(
                orderDetail.PurchaseType,
                "Rent",
                StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] =
                    "Only rental products can be returned.";

                return RedirectToAction(
                    "OrderDetails",
                    new { id = orderDetail.OrderId });
            }

            string orderStatus =
                orderDetail.Order.OrderStatus?.Trim() ?? "";

            if (!orderStatus.Equals(
                    "Delivered",
                    StringComparison.OrdinalIgnoreCase) &&
                !orderStatus.Equals(
                    "Completed",
                    StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] =
                    "The rental product can only be returned after the order has been delivered.";

                return RedirectToAction(
                    "OrderDetails",
                    new { id = orderDetail.OrderId });
            }

            string returnStatus =
                orderDetail.RentalReturnStatus?.Trim() ?? "";

            if (returnStatus.Equals(
                    "Return Requested",
                    StringComparison.OrdinalIgnoreCase) ||
                returnStatus.Equals(
                    "Return Picked Up",
                    StringComparison.OrdinalIgnoreCase) ||
                returnStatus.Equals(
                    "Returned",
                    StringComparison.OrdinalIgnoreCase) ||
                returnStatus.Equals(
                    "Inspected",
                    StringComparison.OrdinalIgnoreCase) ||
                returnStatus.Equals(
                    "Refunded",
                    StringComparison.OrdinalIgnoreCase) ||
                returnStatus.Equals(
                    "Security Forfeited",
                    StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] =
                    "A return request has already been submitted for this rental product.";

                return RedirectToAction(
                    "OrderDetails",
                    new { id = orderDetail.OrderId });
            }

            if (string.IsNullOrWhiteSpace(refundPreferenceMethod))
            {
                TempData["Error"] =
                    "Please select a security refund method.";

                return RedirectToAction(
                    "OrderDetails",
                    new { id = orderDetail.OrderId });
            }

            refundPreferenceMethod =
                refundPreferenceMethod.Trim();

            if (!refundPreferenceMethod.Equals(
                    "Easypaisa",
                    StringComparison.OrdinalIgnoreCase) &&
                !refundPreferenceMethod.Equals(
                    "JazzCash",
                    StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] =
                    "Please select either Easypaisa or JazzCash.";

                return RedirectToAction(
                    "OrderDetails",
                    new { id = orderDetail.OrderId });
            }

            if (string.IsNullOrWhiteSpace(refundPreferenceAccount))
            {
                TempData["Error"] =
                    "Please enter your Easypaisa or JazzCash account/mobile number.";

                return RedirectToAction(
                    "OrderDetails",
                    new { id = orderDetail.OrderId });
            }

            refundPreferenceAccount =
                refundPreferenceAccount.Trim();

            string accountNumber =
                new string(
                    refundPreferenceAccount
                        .Where(char.IsDigit)
                        .ToArray());

            if (accountNumber.Length != 11 ||
                !accountNumber.StartsWith("03"))
            {
                TempData["Error"] =
                    "Please enter a valid 11-digit mobile number starting with 03.";

                return RedirectToAction(
                    "OrderDetails",
                    new { id = orderDetail.OrderId });
            }

            orderDetail.RefundPreferenceMethod =
                refundPreferenceMethod.Equals(
                    "Easypaisa",
                    StringComparison.OrdinalIgnoreCase)
                    ? "Easypaisa"
                    : "JazzCash";

            orderDetail.RefundPreferenceAccount =
                accountNumber;

            orderDetail.RentalReturnStatus =
                "Return Requested";
            orderDetail.ReturnMethod = "Delivery Boy Pickup";

            orderDetail.ReturnRequestedDate = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"Return request for {orderDetail.Product?.ProductName ?? "rental product"} has been submitted successfully. Your security refund will be processed through {orderDetail.RefundPreferenceMethod} account {orderDetail.RefundPreferenceAccount} after the returned product is inspected.";

            return RedirectToAction(
                "OrderDetails",
                new { id = orderDetail.OrderId });
        }


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

        [HttpGet]
        public async Task<IActionResult> Feedback()
        {
            if (!IsCustomerLoggedIn())
                return CustomerLogin();

            int customerId =
                GetCustomerId()!.Value;

            var reviews = await _context.Reviews
                .Include(r => r.Product)
                .Include(r => r.Shop)
                .Where(r => r.UserId == customerId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

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


            ViewBag.CompletedOrders = completedOrders;


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
            if (!IsCustomerLoggedIn())
                return CustomerLogin();

            int customerId =
                GetCustomerId()!.Value;

            if (!orderId.HasValue ||
                orderId.Value <= 0)
            {
                TempData["Error"] =
                    "Please select a completed order.";

                return RedirectToAction("Feedback");
            }

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


            if (rating < 1 ||
                rating > 5)
            {
                TempData["Error"] =
                    "Please select a rating between 1 and 5.";

                return RedirectToAction("Feedback");
            }

            if (string.IsNullOrWhiteSpace(comment))
            {
                TempData["Error"] =
                    "Please enter your feedback.";

                return RedirectToAction("Feedback");
            }

            comment = comment.Trim();

            if (string.IsNullOrWhiteSpace(reviewType))
            {
                TempData["Error"] =
                    "Please select whether your feedback is for a shop or product.";

                return RedirectToAction("Feedback");
            }

            reviewType =
                reviewType.Trim();

            if (reviewType.Equals(
                "Shop",
                StringComparison.OrdinalIgnoreCase))
            {

                if (!shopId.HasValue ||
                    shopId.Value <= 0)
                {
                    TempData["Error"] =
                        "Please select a shop.";

                    return RedirectToAction("Feedback");
                }

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



                var shopReview =
                    new Review
                    {
                        UserId =
                            customerId,

                        ShopId =
                            shopId.Value,
                        ProductId =
                            null,

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

                await CreateNotification(
                    shop.ShopkeeperId,
                    "New Customer Review",
                    $"A customer submitted a {rating}-star review for your shop: {shop.ShopName}.",
                    "Review"
                );

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

                await _context.SaveChangesAsync();


                TempData["Success"] =
                    "Thank you! Your shop feedback has been submitted and is waiting for admin approval.";

                return RedirectToAction("Feedback");
            }


            if (reviewType.Equals(
                "Product",
                StringComparison.OrdinalIgnoreCase))
            {

                if (!productId.HasValue ||
                    productId.Value <= 0)
                {
                    TempData["Error"] =
                        "Please select a product.";

                    return RedirectToAction("Feedback");
                }

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

                await CreateNotification(
                    shop.ShopkeeperId,
                    "New Customer Review",
                    $"A customer submitted a {rating}-star review for your product: {product.ProductName}.",
                    "Review"
                );

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

                await _context.SaveChangesAsync();


                TempData["Success"] =
                    "Thank you! Your product feedback has been submitted and is waiting for admin approval.";

                return RedirectToAction("Feedback");
            }


            TempData["Error"] =
                "Invalid feedback type.";

            return RedirectToAction("Feedback");
        }

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

        [HttpGet]
        public async Task<IActionResult> Chat()
        {
            if (!IsCustomerLoggedIn())
                return CustomerLogin();

            int customerId =
                GetCustomerId()!.Value;

            var admin =
                await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(
                        u =>
                            u.Status &&
                            u.Role != null &&
                            u.Role.RoleName ==
                                "Admin");


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

        [HttpGet]
        public async Task<IActionResult>
            Conversation(int userId)
        {
            if (!IsCustomerLoggedIn())
                return CustomerLogin();

            int customerId =
                GetCustomerId()!.Value;

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

            bool allowed =
                false;



            if (role == "Admin")
            {
                allowed =
                    true;
            }

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

            var admin =
                await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(
                        u =>
                            u.Status &&
                            u.Role != null &&
                            u.Role.RoleName ==
                                "Admin");


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


            if (receiverId == customerId.Value)
            {
                TempData["Error"] =
                    "You cannot send a message to yourself.";

                return RedirectToAction(
                    nameof(Conversation),
                    new { userId = receiverId });
            }

            var receiver = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserId == receiverId);

            if (receiver == null)
            {
                TempData["Error"] =
                    "The selected user was not found.";

                return RedirectToAction(nameof(Chat));
            }

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


            string? customerName = await _context.Users
                .Where(u => u.UserId == customerId.Value)
                .Select(u => u.Name)
                .FirstOrDefaultAsync();

            customerName ??= "Customer";


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



    public class CustomerCartItem
    {
        public int ProductId { get; set; }

        public string? ProductName { get; set; }

        public decimal Price { get; set; }

        public int Quantity { get; set; }

        // Selected size for products that use ProductVariant
        public string? Size { get; set; }

        public string? CustomMeasurements { get; set; }

        public string PurchaseType { get; set; } = "Buy";

        public int? NegotiationId { get; set; }
    }

}