using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WeddingClosetHubs.Models;

namespace WeddingClosetHubs.Controllers
{
    public class AdminController : Controller
    {
        private readonly WeddingClosetHubsContext _context;

        // =====================================================
        // CONSTANTS
        // =====================================================

        private const string AdminRole = "Admin";
        private const string ShopkeeperRole = "Shopkeeper";
        private const string CustomerRole = "Customer";
        private const string DeliveryRole = "Delivery";

        private static readonly string[] ValidDeliveryZones =
        {
            "Saddar",
            "6th Road",
            "Askari 7"
        };

        // =====================================================
        // CONSTRUCTOR
        // =====================================================

        public AdminController(WeddingClosetHubsContext context)
        {
            _context = context;
        }

        // =====================================================
        // ADMIN AUTHORIZATION
        // =====================================================

        private bool IsAdmin()
        {
            int? userId =
                HttpContext.Session.GetInt32("UserId");

            string? roleName =
                HttpContext.Session.GetString("RoleName");

            return userId.HasValue &&
                   roleName == AdminRole;
        }

        // =====================================================
        // ADMIN LOGIN REDIRECT
        // =====================================================

        private IActionResult AdminLoginRedirect()
        {
            return RedirectToAction(
                "Login",
                "Account");
        }

        // =====================================================
        // ORDER HELPERS
        // =====================================================

        private static bool IsCancelledOrder(Order order)
        {
            return string.Equals(
                order.OrderStatus,
                "Cancelled",
                StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsCodOrder(Order order)
        {
            return string.Equals(
                       order.PaymentMethod,
                       "COD",
                       StringComparison.OrdinalIgnoreCase)
                   ||
                   string.Equals(
                       order.PaymentMethod,
                       "Cash on Delivery",
                       StringComparison.OrdinalIgnoreCase)
                   ||
                   string.Equals(
                       order.PaymentMethod,
                       "Cash",
                       StringComparison.OrdinalIgnoreCase);
        }

        

        // =====================================================
        // DASHBOARD
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            if (!IsAdmin())
            {
                return AdminLoginRedirect();
            }

            int adminId =
                HttpContext.Session.GetInt32("UserId") ?? 0;

            // =================================================
            // UNREAD CHAT MESSAGES
            // =================================================

            ViewBag.UnreadChats =
                await _context.ChatMessages
                    .CountAsync(c =>
                        c.ReceiverId == adminId &&
                        !c.IsRead);
            ViewBag.UnreadNotifications=
                await _context.Notifications
                     .CountAsync(n =>
                          n.UserId == adminId &&
                          !n.IsRead);
            
            ViewBag.UnreadFeedback =
                   await _context.Reviews
                         .CountAsync(r =>
                         !r.IsRead);

            // =================================================
            // USERS
            // =================================================

            ViewBag.TotalUsers =
                await _context.Users.CountAsync();

            ViewBag.ActiveUsers =
                await _context.Users
                    .CountAsync(u => u.Status);

            ViewBag.DisabledUsers =
                await _context.Users
                    .CountAsync(u => !u.Status);

            // =================================================
            // SHOPKEEPERS
            // =================================================

            ViewBag.TotalShopkeepers =
                await _context.Users
                    .Where(u =>
                        u.Role != null &&
                        u.Role.RoleName == ShopkeeperRole)
                    .CountAsync();

            ViewBag.PendingShopkeepers =
                await _context.Users
                    .Where(u =>
                        u.Role != null &&
                        u.Role.RoleName == ShopkeeperRole &&
                        !u.IsApproved)
                    .CountAsync();

            ViewBag.ActiveShopkeepers =
                await _context.Users
                    .Where(u =>
                        u.Role != null &&
                        u.Role.RoleName == ShopkeeperRole &&
                        u.IsApproved &&
                        u.Status)
                    .CountAsync();

            // =================================================
            // CUSTOMERS
            // =================================================

            ViewBag.TotalCustomers =
                await _context.Users
                    .Where(u =>
                        u.Role != null &&
                        u.Role.RoleName == CustomerRole)
                    .CountAsync();

            ViewBag.ActiveCustomers =
                await _context.Users
                    .Where(u =>
                        u.Role != null &&
                        u.Role.RoleName == CustomerRole &&
                        u.Status)
                    .CountAsync();

            // =================================================
            // DELIVERY
            // =================================================

            ViewBag.TotalDeliveryBoys =
                await _context.DeliveryBoys.CountAsync();

            ViewBag.PendingDeliveryBoys =
                await _context.DeliveryBoys
                    .Where(d =>
                        d.User != null &&
                        !d.User.IsApproved)
                    .CountAsync();

            ViewBag.ActiveDeliveryBoys =
                await _context.DeliveryBoys
                    .Where(d =>
                        d.User != null &&
                        d.User.IsApproved &&
                        d.User.Status)
                    .CountAsync();

            // =================================================
            // SHOPS
            // =================================================

            ViewBag.TotalShops =
                await _context.Shops.CountAsync();

            ViewBag.PendingShops =
                await _context.Shops
                    .CountAsync(s => !s.IsApproved);

            ViewBag.ActiveShops =
                await _context.Shops
                    .CountAsync(s =>
                        s.IsApproved &&
                        s.Status);

            ViewBag.DisabledShops =
                await _context.Shops
                    .CountAsync(s =>
                        s.IsApproved &&
                        !s.Status);

            // =================================================
            // ORDERS
            // =================================================

            // IMPORTANT:
            // Cancelled orders are not counted as active orders.

            ViewBag.TotalOrders =
                await _context.Orders
                    .CountAsync(o =>
                        o.OrderStatus != "Cancelled");

            ViewBag.PendingOrders =
                await _context.Orders
                    .CountAsync(o =>
                        o.OrderStatus == "Pending");

            ViewBag.CompletedOrders =
                await _context.Orders
                    .CountAsync(o =>
                        o.OrderStatus == "Completed");

            ViewBag.CancelledOrders =
                await _context.Orders
                    .CountAsync(o =>
                        o.OrderStatus == "Cancelled");

            // =================================================
            // PAYMENTS
            // =================================================

            // Cancelled orders are completely excluded
            // from payment calculations.

            ViewBag.TotalPayments =
                await _context.Orders
                    .CountAsync(o =>
                        o.OrderStatus != "Cancelled");

            ViewBag.ReceivedPayments =
                await _context.Orders
                    .CountAsync(o =>
                        o.OrderStatus != "Cancelled" &&
                        o.PaymentReceived);

            ViewBag.PendingPayments =
                await _context.Orders
                    .CountAsync(o =>
                        o.OrderStatus != "Cancelled" &&
                        !o.PaymentReceived);

            return View();
        }

        // =====================================================
        // SHOPKEEPERS
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Shopkeepers()
        {
            if (!IsAdmin())
            {
                return AdminLoginRedirect();
            }

            var shopkeepers =
                await _context.Users
                    .Include(u => u.Role)
                    .Include(u => u.Shop)
                    .Where(u =>
                        u.Role != null &&
                        u.Role.RoleName == ShopkeeperRole)
                    .OrderByDescending(u => u.CreatedDate)
                    .ToListAsync();

            return View(shopkeepers);
        }

        // =====================================================
        // SHOPKEEPER DETAILS
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> ShopkeeperDetails(int id)
        {
            if (!IsAdmin())
            {
                return AdminLoginRedirect();
            }

            var shopkeeper =
                await _context.Users
                    .Include(u => u.Role)
                    .Include(u => u.Shop)
                    .FirstOrDefaultAsync(u =>
                        u.UserId == id &&
                        u.Role != null &&
                        u.Role.RoleName == ShopkeeperRole);

            if (shopkeeper == null)
            {
                TempData["Error"] =
                    "Shopkeeper not found.";

                return RedirectToAction("Shopkeepers");
            }

            return View(shopkeeper);
        }

        // =====================================================
        // APPROVE SHOPKEEPER
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveShopkeeper(int id)
        {
            if (!IsAdmin())
            {
                return AdminLoginRedirect();
            }

            var shopkeeper =
                await _context.Users
                    .Include(u => u.Role)
                    .Include(u => u.Shop)
                    .FirstOrDefaultAsync(u =>
                        u.UserId == id &&
                        u.Role != null &&
                        u.Role.RoleName == ShopkeeperRole);

            if (shopkeeper == null)
            {
                TempData["Error"] =
                    "Shopkeeper not found.";

                return RedirectToAction("Shopkeepers");
            }

            shopkeeper.IsApproved = true;
            shopkeeper.Status = true;
            shopkeeper.ApprovedDate = DateTime.Now;

            if (shopkeeper.Shop != null)
            {
                shopkeeper.Shop.IsApproved = true;
                shopkeeper.Shop.Status = true;
                shopkeeper.Shop.ApprovedDate = DateTime.Now;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Shopkeeper and associated shop approved successfully.";

            return RedirectToAction("Shopkeepers");
        }

        // =====================================================
        // REJECT SHOPKEEPER
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectShopkeeper(int id)
        {
            if (!IsAdmin())
            {
                return AdminLoginRedirect();
            }

            var shopkeeper =
                await _context.Users
                    .Include(u => u.Role)
                    .Include(u => u.Shop)
                    .FirstOrDefaultAsync(u =>
                        u.UserId == id &&
                        u.Role != null &&
                        u.Role.RoleName == ShopkeeperRole);

            if (shopkeeper == null)
            {
                TempData["Error"] =
                    "Shopkeeper not found.";

                return RedirectToAction("Shopkeepers");
            }

            shopkeeper.IsApproved = false;
            shopkeeper.Status = false;
            shopkeeper.ApprovedDate = null;

            if (shopkeeper.Shop != null)
            {
                shopkeeper.Shop.IsApproved = false;
                shopkeeper.Shop.Status = false;
                shopkeeper.Shop.ApprovedDate = null;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Shopkeeper application rejected.";

            return RedirectToAction("Shopkeepers");
        }

        // =====================================================
        // ENABLE SHOPKEEPER
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnableShopkeeper(int id)
        {
            if (!IsAdmin())
            {
                return AdminLoginRedirect();
            }

            var shopkeeper =
                await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u =>
                        u.UserId == id &&
                        u.Role != null &&
                        u.Role.RoleName == ShopkeeperRole);

            if (shopkeeper == null)
            {
                TempData["Error"] =
                    "Shopkeeper not found.";

                return RedirectToAction("Shopkeepers");
            }

            if (!shopkeeper.IsApproved)
            {
                TempData["Error"] =
                    "Shopkeeper must be approved before enabling.";

                return RedirectToAction("Shopkeepers");
            }

            shopkeeper.Status = true;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Shopkeeper account enabled.";

            return RedirectToAction("Shopkeepers");
        }

        // =====================================================
        // DISABLE SHOPKEEPER
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DisableShopkeeper(int id)
        {
            if (!IsAdmin())
            {
                return AdminLoginRedirect();
            }

            var shopkeeper =
                await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u =>
                        u.UserId == id &&
                        u.Role != null &&
                        u.Role.RoleName == ShopkeeperRole);

            if (shopkeeper == null)
            {
                TempData["Error"] =
                    "Shopkeeper not found.";

                return RedirectToAction("Shopkeepers");
            }

            shopkeeper.Status = false;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Shopkeeper account disabled.";

            return RedirectToAction("Shopkeepers");
        }

        // =====================================================
        // SHOPS
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Shops()
        {
            if (!IsAdmin())
            {
                return AdminLoginRedirect();
            }

            var shops =
                await _context.Shops
                    .Include(s => s.Shopkeeper)
                    .OrderByDescending(s => s.CreatedDate)
                    .ToListAsync();

            return View(shops);
        }

        // =====================================================
        // SHOP DETAILS
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> ShopDetails(int id)
        {
            if (!IsAdmin())
            {
                return AdminLoginRedirect();
            }

            var shop =
                await _context.Shops
                    .Include(s => s.Shopkeeper)
                    .FirstOrDefaultAsync(s =>
                        s.ShopId == id);

            if (shop == null)
            {
                TempData["Error"] =
                    "Shop not found.";

                return RedirectToAction("Shops");
            }

            return View(shop);
        }

        // =====================================================
        // APPROVE SHOP
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveShop(int id)
        {
            if (!IsAdmin())
            {
                return AdminLoginRedirect();
            }

            var shop =
                await _context.Shops
                    .Include(s => s.Shopkeeper)
                    .FirstOrDefaultAsync(s =>
                        s.ShopId == id);

            if (shop == null)
            {
                TempData["Error"] =
                    "Shop not found.";

                return RedirectToAction("Shops");
            }

            shop.IsApproved = true;
            shop.Status = true;
            shop.ApprovedDate = DateTime.Now;

            if (shop.Shopkeeper != null)
            {
                shop.Shopkeeper.IsApproved = true;
                shop.Shopkeeper.Status = true;

                if (shop.Shopkeeper.ApprovedDate == null)
                {
                    shop.Shopkeeper.ApprovedDate =
                        DateTime.Now;
                }
            }

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Shop approved and activated successfully.";

            return RedirectToAction("Shops");
        }

        // =====================================================
        // REJECT SHOP
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectShop(int id)
        {
            if (!IsAdmin())
            {
                return AdminLoginRedirect();
            }

            var shop =
                await _context.Shops
                    .Include(s => s.Shopkeeper)
                    .FirstOrDefaultAsync(s =>
                        s.ShopId == id);

            if (shop == null)
            {
                TempData["Error"] =
                    "Shop not found.";

                return RedirectToAction("Shops");
            }

            shop.IsApproved = false;
            shop.Status = false;
            shop.ApprovedDate = null;

            if (shop.Shopkeeper != null)
            {
                shop.Shopkeeper.IsApproved = false;
                shop.Shopkeeper.Status = false;
                shop.Shopkeeper.ApprovedDate = null;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Shop application rejected.";

            return RedirectToAction("Shops");
        }

        // =====================================================
        // ENABLE SHOP
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnableShop(int id)
        {
            if (!IsAdmin())
            {
                return AdminLoginRedirect();
            }

            var shop =
                await _context.Shops
                    .FirstOrDefaultAsync(s =>
                        s.ShopId == id);

            if (shop == null)
            {
                TempData["Error"] =
                    "Shop not found.";

                return RedirectToAction("Shops");
            }

            if (!shop.IsApproved)
            {
                TempData["Error"] =
                    "Shop must be approved before it can be activated.";

                return RedirectToAction("Shops");
            }

            shop.Status = true;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Shop enabled successfully.";

            return RedirectToAction("Shops");
        }

        // =====================================================
        // DISABLE SHOP
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DisableShop(int id)
        {
            if (!IsAdmin())
            {
                return AdminLoginRedirect();
            }

            var shop =
                await _context.Shops
                    .FirstOrDefaultAsync(s =>
                        s.ShopId == id);

            if (shop == null)
            {
                TempData["Error"] =
                    "Shop not found.";

                return RedirectToAction("Shops");
            }

            shop.Status = false;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Shop disabled successfully.";

            return RedirectToAction("Shops");
        }

        // =====================================================
        // CUSTOMERS
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Customers()
        {
            if (!IsAdmin())
            {
                return AdminLoginRedirect();
            }

            var customers =
                await _context.Users
                    .Include(u => u.Role)
                    .Where(u =>
                        u.Role != null &&
                        u.Role.RoleName == CustomerRole)
                    .OrderByDescending(u => u.CreatedDate)
                    .ToListAsync();

            return View(customers);
        }

        // =====================================================
        // CUSTOMER DETAILS
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> CustomerDetails(int id)
        {
            if (!IsAdmin())
            {
                return AdminLoginRedirect();
            }

            var customer =
                await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u =>
                        u.UserId == id &&
                        u.Role != null &&
                        u.Role.RoleName == CustomerRole);

            if (customer == null)
            {
                TempData["Error"] =
                    "Customer not found.";

                return RedirectToAction("Customers");
            }

            return View(customer);
        }

        // =====================================================
        // ENABLE CUSTOMER
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnableCustomer(int id)
        {
            if (!IsAdmin())
            {
                return AdminLoginRedirect();
            }

            var customer =
                await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u =>
                        u.UserId == id &&
                        u.Role != null &&
                        u.Role.RoleName == CustomerRole);

            if (customer == null)
            {
                TempData["Error"] =
                    "Customer not found.";

                return RedirectToAction("Customers");
            }

            customer.Status = true;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Customer account enabled.";

            return RedirectToAction("Customers");
        }

        // =====================================================
        // DISABLE CUSTOMER
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DisableCustomer(int id)
        {
            if (!IsAdmin())
            {
                return AdminLoginRedirect();
            }

            var customer =
                await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u =>
                        u.UserId == id &&
                        u.Role != null &&
                        u.Role.RoleName == CustomerRole);

            if (customer == null)
            {
                TempData["Error"] =
                    "Customer not found.";

                return RedirectToAction("Customers");
            }

            customer.Status = false;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Customer account disabled.";

            return RedirectToAction("Customers");
        }

        // =====================================================
        // DELIVERY BOYS
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> DeliveryBoys()
        {
            if (!IsAdmin())
            {
                return AdminLoginRedirect();
            }

            var deliveryBoys =
                await _context.DeliveryBoys
                    .Include(d => d.User)
                    .ThenInclude(u => u.Role)
                    .OrderByDescending(d => d.CreatedDate)
                    .ToListAsync();

            return View(deliveryBoys);
        }

        // =====================================================
        // DELIVERY DETAILS
        // id = DeliveryBoyId
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> DeliveryDetails(int id)
        {
            if (!IsAdmin())
            {
                return AdminLoginRedirect();
            }

            var delivery =
                await _context.DeliveryBoys
                    .Include(d => d.User)
                    .ThenInclude(u => u.Role)
                    .FirstOrDefaultAsync(d =>
                        d.DeliveryBoyId == id);

            if (delivery == null)
            {
                TempData["Error"] =
                    "Delivery account not found.";

                return RedirectToAction("DeliveryBoys");
            }

            return View(delivery);
        }

        // =====================================================
        // APPROVE DELIVERY
        // id = UserId
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveDelivery(int id)
        {
            if (!IsAdmin())
            {
                return AdminLoginRedirect();
            }

            var delivery =
                await _context.Users
                    .Include(u => u.Role)
                    .Include(u => u.DeliveryBoy)
                    .FirstOrDefaultAsync(u =>
                        u.UserId == id &&
                        u.Role != null &&
                        u.Role.RoleName == DeliveryRole);

            if (delivery == null)
            {
                TempData["Error"] =
                    "Delivery account not found.";

                return RedirectToAction("DeliveryBoys");
            }

            delivery.IsApproved = true;
            delivery.Status = true;
            delivery.ApprovedDate = DateTime.Now;

            if (delivery.DeliveryBoy != null)
            {
                delivery.DeliveryBoy.VerificationStatus =
                    "Approved";

                delivery.DeliveryBoy.ApprovedDate =
                    DateTime.Now;

                delivery.DeliveryBoy.RejectionReason =
                    null;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Delivery account approved successfully.";

            return RedirectToAction("DeliveryBoys");
        }

        // =====================================================
        // REJECT DELIVERY
        // id = UserId
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectDelivery(
            int id,
            string? rejectionReason,
            string? adminNotes)
        {
            if (!IsAdmin())
            {
                return AdminLoginRedirect();
            }

            var delivery =
                await _context.Users
                    .Include(u => u.Role)
                    .Include(u => u.DeliveryBoy)
                    .FirstOrDefaultAsync(u =>
                        u.UserId == id &&
                        u.Role != null &&
                        u.Role.RoleName == DeliveryRole);

            if (delivery == null)
            {
                TempData["Error"] =
                    "Delivery account not found.";

                return RedirectToAction("DeliveryBoys");
            }

            delivery.IsApproved = false;
            delivery.Status = false;
            delivery.ApprovedDate = null;

            if (delivery.DeliveryBoy != null)
            {
                delivery.DeliveryBoy.VerificationStatus =
                    "Rejected";

                delivery.DeliveryBoy.RejectionReason =
                    string.IsNullOrWhiteSpace(rejectionReason)
                        ? "Application rejected by administrator."
                        : rejectionReason.Trim();

                delivery.DeliveryBoy.AdminNotes =
                    string.IsNullOrWhiteSpace(adminNotes)
                        ? null
                        : adminNotes.Trim();
            }

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Delivery application rejected.";

            if (delivery.DeliveryBoy != null)
            {
                return RedirectToAction(
                    "DeliveryDetails",
                    new
                    {
                        id = delivery.DeliveryBoy.DeliveryBoyId
                    });
            }

            return RedirectToAction("DeliveryBoys");
        }

        // =====================================================
        // ASSIGN DELIVERY ZONE + ADMIN NOTES
        // id = DeliveryBoyId
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignDeliveryZone(
            int id,
            string? assignedZone,
            string? adminNotes)
        {
            if (!IsAdmin())
            {
                return AdminLoginRedirect();
            }

            var delivery =
                await _context.DeliveryBoys
                    .FirstOrDefaultAsync(d =>
                        d.DeliveryBoyId == id);

            if (delivery == null)
            {
                TempData["Error"] =
                    "Delivery account not found.";

                return RedirectToAction("DeliveryBoys");
            }

            if (string.IsNullOrWhiteSpace(assignedZone))
            {
                TempData["Error"] =
                    "Please select a delivery zone.";

                return RedirectToAction(
                    "DeliveryDetails",
                    new
                    {
                        id = delivery.DeliveryBoyId
                    });
            }

            assignedZone =
                assignedZone.Trim();

            if (!ValidDeliveryZones.Contains(
                    assignedZone,
                    StringComparer.OrdinalIgnoreCase))
            {
                TempData["Error"] =
                    "Invalid delivery zone selected.";

                return RedirectToAction(
                    "DeliveryDetails",
                    new
                    {
                        id = delivery.DeliveryBoyId
                    });
            }

            assignedZone =
                ValidDeliveryZones.First(z =>
                    string.Equals(
                        z,
                        assignedZone,
                        StringComparison.OrdinalIgnoreCase));

            delivery.AssignedZone =
                assignedZone;

            delivery.AdminNotes =
                string.IsNullOrWhiteSpace(adminNotes)
                    ? null
                    : adminNotes.Trim();

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Delivery zone and admin notes saved successfully.";

            return RedirectToAction(
                "DeliveryDetails",
                new
                {
                    id = delivery.DeliveryBoyId
                });
        }

        // =====================================================
        // UPDATE DELIVERY DETAILS
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateDeliveryDetails(
            int UserId,
            string? AssignedZone,
            string? AdminNotes)
        {
            if (!IsAdmin())
            {
                return AdminLoginRedirect();
            }

            var delivery =
                await _context.DeliveryBoys
                    .Include(d => d.User)
                    .FirstOrDefaultAsync(d =>
                        d.UserId == UserId);

            if (delivery == null)
            {
                TempData["Error"] =
                    "Delivery account not found.";

                return RedirectToAction("DeliveryBoys");
            }

            if (!string.IsNullOrWhiteSpace(AssignedZone))
            {
                AssignedZone =
                    AssignedZone.Trim();

                if (!ValidDeliveryZones.Contains(
                        AssignedZone,
                        StringComparer.OrdinalIgnoreCase))
                {
                    TempData["Error"] =
                        "Invalid delivery zone selected.";

                    return RedirectToAction(
                        "DeliveryDetails",
                        new
                        {
                            id = delivery.DeliveryBoyId
                        });
                }

                AssignedZone =
                    ValidDeliveryZones.First(z =>
                        string.Equals(
                            z,
                            AssignedZone,
                            StringComparison.OrdinalIgnoreCase));
            }

            delivery.AssignedZone =
                string.IsNullOrWhiteSpace(AssignedZone)
                    ? null
                    : AssignedZone;

            delivery.AdminNotes =
                string.IsNullOrWhiteSpace(AdminNotes)
                    ? null
                    : AdminNotes.Trim();

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Delivery details updated successfully.";

            return RedirectToAction(
                "DeliveryDetails",
                new
                {
                    id = delivery.DeliveryBoyId
                });
        }

        // =====================================================
        // ENABLE DELIVERY
        // id = UserId
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnableDelivery(int id)
        {
            if (!IsAdmin())
            {
                return AdminLoginRedirect();
            }

            var delivery =
                await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u =>
                        u.UserId == id &&
                        u.Role != null &&
                        u.Role.RoleName == DeliveryRole);

            if (delivery == null)
            {
                TempData["Error"] =
                    "Delivery account not found.";

                return RedirectToAction("DeliveryBoys");
            }

            if (!delivery.IsApproved)
            {
                TempData["Error"] =
                    "Delivery account must be approved before enabling.";

                return RedirectToAction("DeliveryBoys");
            }

            delivery.Status = true;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Delivery account enabled.";

            return RedirectToAction("DeliveryBoys");
        }

        // =====================================================
        // DISABLE DELIVERY
        // id = UserId
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DisableDelivery(int id)
        {
            if (!IsAdmin())
            {
                return AdminLoginRedirect();
            }

            var delivery =
                await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u =>
                        u.UserId == id &&
                        u.Role != null &&
                        u.Role.RoleName == DeliveryRole);

            if (delivery == null)
            {
                TempData["Error"] =
                    "Delivery account not found.";

                return RedirectToAction("DeliveryBoys");
            }

            delivery.Status = false;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Delivery account disabled.";

            return RedirectToAction("DeliveryBoys");
        }

        // =====================================================
        // ORDERS
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Orders()
        {
            if (!IsAdmin())
            {
                return AdminLoginRedirect();
            }

            // IMPORTANT:
            // Cancelled orders are not shown on Admin Orders page.

            var orders =
                await _context.Orders
                    .Include(o => o.Customer)
                    .Include(o => o.Shop)
                    .Include(o => o.Delivery)
                    .Where(o =>
                        o.OrderStatus != "Cancelled")
                    .OrderByDescending(o => o.CreatedDate)
                    .ToListAsync();

            return View(orders);
        }

        // =====================================================
        // ORDER DETAILS
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> OrderDetails(int id)
        {
            if (!IsAdmin())
            {
                return AdminLoginRedirect();
            }

            var order = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.Shop)
                    .ThenInclude(s => s.Shopkeeper)
                .Include(o => o.Delivery)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o =>
                    o.OrderId == id &&
                    o.OrderStatus != "Cancelled");

            if (order == null)
            {
                TempData["Error"] =
                    "Order not found or the order has been cancelled.";

                return RedirectToAction("Orders");
            }

            // =================================================
            // APPROVED + ACTIVE DELIVERY BOYS
            // =================================================

            var deliveryBoys =
                await _context.Users
                    .Include(u => u.Role)
                    .Include(u => u.DeliveryBoy)
                    .Where(u =>
                        u.Role != null &&
                        u.Role.RoleName == DeliveryRole &&
                        u.IsApproved &&
                        u.Status &&
                        u.DeliveryBoy != null &&
                        u.DeliveryBoy.VerificationStatus == "Approved" &&
                        u.DeliveryBoy.AssignedZone != null &&
                        u.DeliveryBoy.AssignedZone != "")
                    .OrderBy(u => u.Name)
                    .ToListAsync();

            ViewBag.DeliveryBoys =
                deliveryBoys;

            return View(order);
        }

        // =====================================================
        // ASSIGN DELIVERY BOY TO ORDER
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignDeliveryBoy(
            int id,
            int deliveryId)
        {
            if (!IsAdmin())
            {
                return AdminLoginRedirect();
            }

            // =================================================
            // FIND ORDER
            // =================================================

            var order =
                await _context.Orders
                    .FirstOrDefaultAsync(o =>
                        o.OrderId == id);

            if (order == null)
            {
                TempData["Error"] =
                    "Order not found.";

                return RedirectToAction("Orders");
            }

            // =================================================
            // CANCELLED ORDER CHECK
            // =================================================

            if (IsCancelledOrder(order))
            {
                TempData["Error"] =
                    "A cancelled order cannot be assigned to a delivery boy.";

                return RedirectToAction("Orders");
            }

            // =================================================
            // FIND DELIVERY BOY
            // =================================================

            var deliveryBoy =
                await _context.Users
                    .Include(u => u.Role)
                    .Include(u => u.DeliveryBoy)
                    .FirstOrDefaultAsync(u =>
                        u.UserId == deliveryId &&
                        u.Role != null &&
                        u.Role.RoleName == DeliveryRole &&
                        u.IsApproved &&
                        u.Status &&
                        u.DeliveryBoy != null &&
                        u.DeliveryBoy.VerificationStatus == "Approved");

            if (deliveryBoy == null)
            {
                TempData["Error"] =
                    "Selected delivery boy is not approved or active.";

                return RedirectToAction(
                    "OrderDetails",
                    new { id });
            }

            // =================================================
            // CHECK DELIVERY ZONE
            // =================================================

            if (deliveryBoy.DeliveryBoy == null ||
                string.IsNullOrWhiteSpace(
                    deliveryBoy.DeliveryBoy.AssignedZone))
            {
                TempData["Error"] =
                    "This delivery boy does not have an assigned zone.";

                return RedirectToAction(
                    "OrderDetails",
                    new { id });
            }

            // =================================================
            // ASSIGN DELIVERY BOY
            // =================================================

            order.DeliveryId =
                deliveryBoy.UserId;

            // =================================================
            // UPDATE STATUS
            // =================================================

            if (string.Equals(
                    order.OrderStatus,
                    "Ready for Delivery",
                    StringComparison.OrdinalIgnoreCase))
            {
                order.OrderStatus =
                    "Assigned";
            }
            else if (string.Equals(
                         order.OrderStatus,
                         "Ready",
                         StringComparison.OrdinalIgnoreCase))
            {
                order.OrderStatus =
                    "Assigned";
            }

            // =================================================
            // CREATE DELIVERY NOTIFICATION
            // =================================================

            await CreateNotification(
                deliveryBoy.UserId,
                "New Order Assigned",
                $"Order #{order.OrderId} has been assigned to you for delivery.",
                "Order",
                order.OrderId);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"Order #{order.OrderId} assigned to {deliveryBoy.Name} successfully.";

            return RedirectToAction(
                "OrderDetails",
                new { id });
        }

  
  // =====================================================
// PAYMENTS
// =====================================================

[HttpGet]
public async Task<IActionResult> Payments()
{
    if (!IsAdmin())
    {
        return AdminLoginRedirect();
    }

    // -------------------------------------------------
    // IMPORTANT:
    // Cancelled orders are NOT payment records.
    // -------------------------------------------------

    var payments = await _context.Orders
        .Include(o => o.Customer)
        .Include(o => o.Shop)
            .ThenInclude(s => s.Shopkeeper)
        .Include(o => o.Delivery)
        .Where(o =>o.OrderStatus!="Cancelled")
               
        .OrderByDescending(o => o.CreatedDate)
        .ToListAsync();

    return View(payments);
}


// =====================================================
// PAYMENT DETAILS
// =====================================================

[HttpGet]
public async Task<IActionResult> PaymentDetails(int id)
{
    if (!IsAdmin())
    {
        return AdminLoginRedirect();
    }

    // -------------------------------------------------
    // Get order and related information
    // -------------------------------------------------

    var order = await _context.Orders
        .Include(o => o.Customer)

        .Include(o => o.Shop)
            .ThenInclude(s => s.Shopkeeper)

        .Include(o => o.Delivery)

        .Include(o => o.OrderDetails)
            .ThenInclude(od => od.Product)

        .FirstOrDefaultAsync(o =>
            o.OrderId == id &&
            o.OrderStatus != "Cancelled");

    // -------------------------------------------------
    // Order not found
    // -------------------------------------------------

    if (order == null)
    {
        TempData["Error"] =
            "Payment record not found or the order has been cancelled.";

        return RedirectToAction(nameof(Payments));
    }

    // -------------------------------------------------
    // GET DELIVERY BOY PAYMENT SETTINGS
    //
    // Order.DeliveryId points to User.UserId.
    // The actual delivery payment settings are stored
    // in DeliveryBoy.PaymentMethod and
    // DeliveryBoy.PaymentAccount.
    // -------------------------------------------------

    if (order.DeliveryId.HasValue)
    {
        var deliveryBoy = await _context.DeliveryBoys
            .FirstOrDefaultAsync(d =>
                d.UserId == order.DeliveryId.Value);

        if (deliveryBoy != null)
        {
            ViewBag.DeliveryPaymentMethod =
                deliveryBoy.PaymentMethod;

            ViewBag.DeliveryPaymentAccount =
                deliveryBoy.PaymentAccount;
        }
        else
        {
            ViewBag.DeliveryPaymentMethod = null;
            ViewBag.DeliveryPaymentAccount = null;
        }
    }
    else
    {
        ViewBag.DeliveryPaymentMethod = null;
        ViewBag.DeliveryPaymentAccount = null;
    }

    // -------------------------------------------------
    // RETURN PAYMENT DETAILS VIEW
    // -------------------------------------------------

    return View(order);
}

// =====================================================
// RECEIVE CUSTOMER PAYMENT
// =====================================================

[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> ReceivePayment(int id)
{
    if (!IsAdmin())
    {
        return AdminLoginRedirect();
    }

    var order = await _context.Orders
        .Include(o => o.Delivery)
        .FirstOrDefaultAsync(o => o.OrderId == id);

    if (order == null)
    {
        TempData["Error"] = "Order not found.";

        return RedirectToAction(nameof(Payments));
    }

    // -------------------------------------------------
    // CANCELLED ORDER
    // -------------------------------------------------

    if (IsCancelledOrder(order))
    {
        TempData["Error"] =
            "Customer payment cannot be received for a cancelled order.";

        return RedirectToAction(
            nameof(PaymentDetails),
            new { id });
    }

    // -------------------------------------------------
    // DUPLICATE PAYMENT
    // -------------------------------------------------

    if (order.PaymentReceived)
    {
        TempData["Error"] =
            "Customer payment has already been received.";

        return RedirectToAction(
            nameof(PaymentDetails),
            new { id });
    }

    // -------------------------------------------------
    // VALIDATE TOTAL
    // -------------------------------------------------

    if (order.TotalAmount <= 0)
    {
        TempData["Error"] =
            "Customer payment amount must be greater than zero.";

        return RedirectToAction(
            nameof(PaymentDetails),
            new { id });
    }

    // =================================================
    // COD PAYMENT
    // =================================================

    if (IsCodOrder(order))
    {
        // -------------------------------------------------
        // COD MUST BE COLLECTED
        // -------------------------------------------------

        if (order.DeliveryCollectedAmount < order.TotalAmount)
        {
            TempData["Error"] =
                "The full COD amount has not been recorded as collected by the delivery boy.";

            return RedirectToAction(
                nameof(PaymentDetails),
                new { id });
        }

        // -------------------------------------------------
        // COD CASH MUST BE HANDED TO ADMIN
        // -------------------------------------------------

        if (!order.DeliveryCashHandedToAdmin)
        {
            TempData["Error"] =
                "COD cash has been collected by the delivery boy, but it has not yet been handed over to Admin.";

            return RedirectToAction(
                nameof(PaymentDetails),
                new { id });
        }

        // -------------------------------------------------
        // COD HANDOVER IS ALREADY THE CONFIRMATION
        // -------------------------------------------------

        order.PaymentReceived = true;
        order.PaymentStatus = "Paid";
        order.PaymentDate =
            order.DeliveryCashHandoverDate ?? DateTime.Now;

        await _context.SaveChangesAsync();

        TempData["Success"] =
            "COD customer payment confirmed successfully after cash handover.";

        return RedirectToAction(
            nameof(PaymentDetails),
            new { id });
    }

    // =================================================
    // ONLINE PAYMENT
    // =================================================

    bool hasTransactionId =
        !string.IsNullOrWhiteSpace(order.TransactionId);

    bool hasTransactionImage =
        !string.IsNullOrWhiteSpace(order.TransactionImage);

    if (!hasTransactionId && !hasTransactionImage)
    {
        TempData["Error"] =
            "A transaction ID or payment screenshot is required before confirming online payment.";

        return RedirectToAction(
            nameof(PaymentDetails),
            new { id });
    }

    // Admin manually verifies the online transaction
    // before confirming payment.

    order.PaymentReceived = true;
    order.PaymentStatus = "Paid";
    order.PaymentDate = DateTime.Now;

    await _context.SaveChangesAsync();

    TempData["Success"] =
        "Customer online payment confirmed successfully.";

    return RedirectToAction(
        nameof(PaymentDetails),
        new { id });
}
        // =====================================================
// CONFIRM COD CASH HANDOVER
// =====================================================

[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> ConfirmCODHandover(
    int id,
    string? notes)
{
    if (!IsAdmin())
    {
        return AdminLoginRedirect();
    }

    var order = await _context.Orders
        .Include(o => o.Delivery)
            
        .Include(o => o.Customer)
        .FirstOrDefaultAsync(o => o.OrderId == id);

    if (order == null)
    {
        TempData["Error"] = "Order not found.";

        return RedirectToAction(nameof(Payments));
    }

    // -------------------------------------------------
    // CANCELLED ORDER
    // -------------------------------------------------

    if (IsCancelledOrder(order))
    {
        TempData["Error"] =
            "COD cash cannot be confirmed for a cancelled order.";

        return RedirectToAction(
            nameof(PaymentDetails),
            new { id });
    }

    // -------------------------------------------------
    // MUST BE COD
    // -------------------------------------------------

    if (!IsCodOrder(order))
    {
        TempData["Error"] =
            "This order is not a Cash on Delivery order.";

        return RedirectToAction(
            nameof(PaymentDetails),
            new { id });
    }

    // -------------------------------------------------
    // MUST HAVE COLLECTION
    // -------------------------------------------------

    if (order.DeliveryCollectedAmount <= 0)
    {
        TempData["Error"] =
            "The delivery boy has not recorded any COD cash collection yet.";

        return RedirectToAction(
            nameof(PaymentDetails),
            new { id });
    }

    // -------------------------------------------------
    // FULL AMOUNT MUST BE COLLECTED
    // -------------------------------------------------

    if (order.DeliveryCollectedAmount < order.TotalAmount)
    {
        TempData["Error"] =
            "The full COD amount has not been collected.";

        return RedirectToAction(
            nameof(PaymentDetails),
            new { id });
    }

    // -------------------------------------------------
    // PREVENT DUPLICATE HANDOVER
    // -------------------------------------------------

    if (order.DeliveryCashHandedToAdmin)
    {
        TempData["Error"] =
            "This COD cash handover has already been confirmed.";

        return RedirectToAction(
            nameof(PaymentDetails),
            new { id });
    }

    // =================================================
    // CONFIRM CASH HANDOVER
    // =================================================

    order.DeliveryCashHandedToAdmin = true;

    order.DeliveryCashHandoverDate =
        DateTime.Now;

    order.DeliveryCashHandoverNotes =
        string.IsNullOrWhiteSpace(notes)
            ? null
            : notes.Trim();

    // -------------------------------------------------
    // NOW CUSTOMER PAYMENT BECOMES PAID
    // -------------------------------------------------

    order.PaymentReceived = true;
    order.PaymentStatus = "Paid";
    order.PaymentDate = DateTime.Now;

    await _context.SaveChangesAsync();

    // =================================================
    // NOTIFY CUSTOMER
    // =================================================

    if (order.Customer != null)
    {
        await CreateNotification(
            order.CustomerId,
            "COD Payment Confirmed",
            $"Your COD payment of Rs. {order.TotalAmount:N0} for Order #{order.OrderId} has been received and confirmed by Admin.",
            "Payment",
            order.OrderId);
    }

    // =================================================
    // NOTIFY DELIVERY BOY
    // =================================================

    if (order.Delivery != null)
    {
        await CreateNotification(
            order.Delivery.UserId,
            "COD Cash Handover Confirmed",
            $"Admin has confirmed receipt of the COD cash of Rs. {order.DeliveryCollectedAmount:N0} for Order #{order.OrderId}.",
            "Payment",
            order.OrderId);
    }

    await _context.SaveChangesAsync();

    TempData["Success"] =
        $"COD cash of Rs. {order.DeliveryCollectedAmount:N0} has been received and customer payment is now marked Paid.";

    return RedirectToAction(
        nameof(PaymentDetails),
        new { id });
}
// =====================================================
// RECORD SHOPKEEPER MANUAL PAYMENT
// =====================================================

[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> RecordShopkeeperPayment(
    int id,
    string transactionId,
    string? paymentMethod,
    string? paymentAccount,
    string? paymentNotes)
{
    if (!IsAdmin())
    {
        return AdminLoginRedirect();
    }

    // -------------------------------------------------
    // LOAD ORDER
    // -------------------------------------------------

    var order = await _context.Orders
        .Include(o => o.Shop)
            .ThenInclude(s => s.Shopkeeper)
        .FirstOrDefaultAsync(o => o.OrderId == id);

    if (order == null)
    {
        TempData["Error"] = "Order not found.";

        return RedirectToAction(nameof(Payments));
    }

    // -------------------------------------------------
    // CANCELLED ORDER
    // -------------------------------------------------

    if (IsCancelledOrder(order))
    {
        TempData["Error"] =
            "Shopkeeper payment cannot be recorded for a cancelled order.";

        return RedirectToAction(
            nameof(PaymentDetails),
            new { id });
    }

    // -------------------------------------------------
    // CUSTOMER PAYMENT MUST BE RECEIVED
    // -------------------------------------------------

    if (!order.PaymentReceived)
    {
        TempData["Error"] =
            "Customer payment must be received before recording the shopkeeper payout.";

        return RedirectToAction(
            nameof(PaymentDetails),
            new { id });
    }

    // -------------------------------------------------
    // ORDER MUST BE DELIVERED
    // -------------------------------------------------

    bool isDeliveryCompleted =
        string.Equals(
            order.OrderStatus,
            "Delivered",
            StringComparison.OrdinalIgnoreCase)
        ||
        string.Equals(
            order.OrderStatus,
            "Completed",
            StringComparison.OrdinalIgnoreCase);

    if (!isDeliveryCompleted)
    {
        TempData["Error"] =
            "Shopkeeper payout can only be recorded after the order has been delivered.";

        return RedirectToAction(
            nameof(PaymentDetails),
            new { id });
    }

    // -------------------------------------------------
    // SHOP VALIDATION
    // -------------------------------------------------

    if (order.Shop == null)
    {
        TempData["Error"] =
            "This order is not associated with a valid shop.";

        return RedirectToAction(
            nameof(PaymentDetails),
            new { id });
    }

    // -------------------------------------------------
    // SHOPKEEPER VALIDATION
    // -------------------------------------------------

    var shopkeeper = order.Shop.Shopkeeper;

    if (shopkeeper == null)
    {
        TempData["Error"] =
            "Shopkeeper information is not available.";

        return RedirectToAction(
            nameof(PaymentDetails),
            new { id });
    }

    // -------------------------------------------------
    // PREVENT DUPLICATE PAYMENT
    // -------------------------------------------------

    if (string.Equals(
        order.ShopkeeperPaymentStatus,
        "Paid",
        StringComparison.OrdinalIgnoreCase))
    {
        TempData["Error"] =
            "Shopkeeper payment has already been recorded as paid.";

        return RedirectToAction(
            nameof(PaymentDetails),
            new { id });
    }

    // -------------------------------------------------
    // VALIDATE PRODUCT AMOUNT
    // -------------------------------------------------

    if (order.ProductTotal <= 0)
    {
        TempData["Error"] =
            "Shopkeeper payment amount must be greater than zero.";

        return RedirectToAction(
            nameof(PaymentDetails),
            new { id });
    }

    // -------------------------------------------------
    // VALIDATE PAYMENT METHOD
    // -------------------------------------------------

    string selectedPaymentMethod =
        paymentMethod?.Trim() ?? "";

    bool validPaymentMethod =
        string.Equals(
            selectedPaymentMethod,
            "Easypaisa",
            StringComparison.OrdinalIgnoreCase)
        ||
        string.Equals(
            selectedPaymentMethod,
            "JazzCash",
            StringComparison.OrdinalIgnoreCase);

    if (!validPaymentMethod)
    {
        TempData["Error"] =
            "Select either Easypaisa or JazzCash.";

        return RedirectToAction(
            nameof(PaymentDetails),
            new { id });
    }

    // -------------------------------------------------
    // VALIDATE PAYMENT ACCOUNT
    // -------------------------------------------------

    string selectedAccount =
        paymentAccount?.Trim() ?? "";

    if (string.IsNullOrWhiteSpace(selectedAccount))
    {
        TempData["Error"] =
            "Shopkeeper payment account is required.";

        return RedirectToAction(
            nameof(PaymentDetails),
            new { id });
    }

    // -------------------------------------------------
    // VALIDATE TRANSACTION ID
    // -------------------------------------------------

    string manualTransactionId =
        transactionId?.Trim() ?? "";

    if (string.IsNullOrWhiteSpace(manualTransactionId))
    {
        TempData["Error"] =
            "Enter the Easypaisa/JazzCash transaction or reference ID.";

        return RedirectToAction(
            nameof(PaymentDetails),
            new { id });
    }

    // -------------------------------------------------
    // SHOPKEEPER AMOUNT
    // -------------------------------------------------
    //
    // ProductTotal is the final agreed product price.
    //
    // ServiceFee is a separate platform fee.
    //
    // Shopkeeper receives the FULL ProductTotal.
    // -------------------------------------------------

    decimal shopkeeperAmount =
        order.ProductTotal;

    // -------------------------------------------------
    // RECORD MANUAL PAYOUT
    // -------------------------------------------------

    order.ShopkeeperAmount =
        shopkeeperAmount;

    order.ShopkeeperPaidAmount =
        shopkeeperAmount;

    order.ShopkeeperPaymentStatus =
        "Paid";

    order.ShopkeeperPaymentMethod =
        selectedPaymentMethod;

    order.ShopkeeperPaymentAccount =
        selectedAccount;

    order.ShopkeeperTransactionId =
        manualTransactionId;

    order.ShopkeeperPaymentNotes =
        string.IsNullOrWhiteSpace(paymentNotes)
            ? null
            : paymentNotes.Trim();

    order.ShopkeeperPaymentDate =
        DateTime.Now;

    // -------------------------------------------------
    // SAVE
    // -------------------------------------------------

    await _context.SaveChangesAsync();

    TempData["Success"] =
        $"Manual shopkeeper payment of Rs. {shopkeeperAmount:N2} has been recorded successfully.";

    return RedirectToAction(
        nameof(PaymentDetails),
        new { id });
}

// =====================================================
// =====================================================
// RECORD DELIVERY BOY MANUAL PAYMENT
// =====================================================

[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> RecordDeliveryPayment(
    int id,
    string transactionId,
    string? paymentNotes)
{
    if (!IsAdmin())
    {
        return AdminLoginRedirect();
    }

    // -------------------------------------------------
    // LOAD ORDER + DELIVERY USER
    // -------------------------------------------------

    var order = await _context.Orders
        .Include(o => o.Delivery)
        .FirstOrDefaultAsync(o => o.OrderId == id);

    if (order == null)
    {
        TempData["Error"] = "Order not found.";

        return RedirectToAction(nameof(Payments));
    }

    // -------------------------------------------------
    // CANCELLED ORDER
    // -------------------------------------------------

    if (IsCancelledOrder(order))
    {
        TempData["Error"] =
            "Delivery payment cannot be recorded for a cancelled order.";

        return RedirectToAction(
            nameof(PaymentDetails),
            new { id });
    }

    // -------------------------------------------------
    // DELIVERY BOY MUST BE ASSIGNED
    // -------------------------------------------------

    if (order.DeliveryId == null ||
        order.Delivery == null)
    {
        TempData["Error"] =
            "A delivery person must be assigned before recording the delivery payout.";

        return RedirectToAction(
            nameof(PaymentDetails),
            new { id });
    }

    // -------------------------------------------------
    // LOAD DELIVERY BOY DETAILS
    // -------------------------------------------------

    var deliveryBoy = await _context.DeliveryBoys
        .FirstOrDefaultAsync(d =>
            d.UserId == order.Delivery.UserId);

    if (deliveryBoy == null)
    {
        TempData["Error"] =
            "Delivery person payment settings were not found.";

        return RedirectToAction(
            nameof(PaymentDetails),
            new { id });
    }

    // -------------------------------------------------
    // CUSTOMER PAYMENT MUST BE RECEIVED
    // -------------------------------------------------

    if (!order.PaymentReceived)
    {
        TempData["Error"] =
            "Customer payment must be received before recording the delivery payout.";

        return RedirectToAction(
            nameof(PaymentDetails),
            new { id });
    }

    // -------------------------------------------------
    // ORDER MUST BE DELIVERED
    // -------------------------------------------------

    bool isDeliveryCompleted =
        string.Equals(
            order.OrderStatus,
            "Delivered",
            StringComparison.OrdinalIgnoreCase)
        ||
        string.Equals(
            order.OrderStatus,
            "Completed",
            StringComparison.OrdinalIgnoreCase);

    if (!isDeliveryCompleted)
    {
        TempData["Error"] =
            "Delivery payout can only be recorded after the order has been delivered.";

        return RedirectToAction(
            nameof(PaymentDetails),
            new { id });
    }

    // -------------------------------------------------
    // PREVENT DUPLICATE PAYMENT
    // -------------------------------------------------

    if (string.Equals(
        order.DeliveryPaymentStatus,
        "Paid",
        StringComparison.OrdinalIgnoreCase))
    {
        TempData["Error"] =
            "Delivery person payment has already been recorded as paid.";

        return RedirectToAction(
            nameof(PaymentDetails),
            new { id });
    }

    // -------------------------------------------------
    // VALIDATE DELIVERY CHARGES
    // -------------------------------------------------

    if (order.DeliveryCharges <= 0)
    {
        TempData["Error"] =
            "Delivery charges must be greater than zero.";

        return RedirectToAction(
            nameof(PaymentDetails),
            new { id });
    }

    // -------------------------------------------------
    // GET SAVED DELIVERY BOY PAYMENT METHOD
    // -------------------------------------------------
    //
    // DeliveryBoy.PaymentMethod is saved when the
    // delivery person sets their payment settings.
    //
    // Example:
    // Easypaisa
    // JazzCash
    // -------------------------------------------------

    string selectedPaymentMethod =
        deliveryBoy.PaymentMethod?.Trim() ?? "";

    bool validPaymentMethod =
        string.Equals(
            selectedPaymentMethod,
            "Easypaisa",
            StringComparison.OrdinalIgnoreCase)
        ||
        string.Equals(
            selectedPaymentMethod,
            "JazzCash",
            StringComparison.OrdinalIgnoreCase);

    if (!validPaymentMethod)
    {
        TempData["Error"] =
            "The delivery person has not set a valid Easypaisa or JazzCash payment method.";

        return RedirectToAction(
            nameof(PaymentDetails),
            new { id });
    }

    // -------------------------------------------------
    // GET SAVED DELIVERY BOY PAYMENT ACCOUNT
    // -------------------------------------------------

    string selectedAccount =
        deliveryBoy.PaymentAccount?.Trim() ?? "";

    if (string.IsNullOrWhiteSpace(selectedAccount))
    {
        TempData["Error"] =
            "The delivery person has not added a payment account.";

        return RedirectToAction(
            nameof(PaymentDetails),
            new { id });
    }

    // -------------------------------------------------
    // VALIDATE TRANSACTION ID
    // -------------------------------------------------

    string manualTransactionId =
        transactionId?.Trim() ?? "";

    if (string.IsNullOrWhiteSpace(manualTransactionId))
    {
        TempData["Error"] =
            "Enter the Easypaisa/JazzCash transaction or reference ID.";

        return RedirectToAction(
            nameof(PaymentDetails),
            new { id });
    }

    // -------------------------------------------------
    // COD VALIDATION
    // -------------------------------------------------

    if (IsCodOrder(order) &&
        order.DeliveryCollectedAmount < order.TotalAmount)
    {
        TempData["Error"] =
            "The full COD amount must be recorded as collected before recording the delivery payout.";

        return RedirectToAction(
            nameof(PaymentDetails),
            new { id });
    }

    // -------------------------------------------------
    // DELIVERY PAYOUT
    // -------------------------------------------------
    //
    // Delivery person receives the fixed delivery charge.
    // Example:
    // DeliveryCharges = Rs.300
    // DeliveryPaidAmount = Rs.300
    // -------------------------------------------------

    decimal deliveryAmount =
        order.DeliveryCharges;

    // -------------------------------------------------
    // RECORD MANUAL PAYOUT
    // -------------------------------------------------

    order.DeliveryPaidAmount =
        deliveryAmount;

    order.DeliveryPaymentStatus =
        "Paid";

    // IMPORTANT:
    // Save the payment method/account that was actually
    // used for this particular payout.
    //
    // This creates a historical record even if the
    // delivery person changes their payment settings later.

    order.DeliveryPaymentMethod =
        selectedPaymentMethod;

    order.DeliveryPaymentAccount =
        selectedAccount;

    order.DeliveryTransactionId =
        manualTransactionId;

    order.DeliveryPaymentNotes =
        string.IsNullOrWhiteSpace(paymentNotes)
            ? null
            : paymentNotes.Trim();

    order.DeliveryPaymentDate =
        DateTime.Now;

    // -------------------------------------------------
    // SAVE
    // -------------------------------------------------

    await _context.SaveChangesAsync();

    TempData["Success"] =
        $"Manual delivery payment of Rs. {deliveryAmount:N2} has been recorded successfully through {selectedPaymentMethod}.";

    return RedirectToAction(
        nameof(PaymentDetails),
        new { id });
}
      
// =====================================================
// REPORTS
// =====================================================

[HttpGet]
public async Task<IActionResult> Reports()
{
    if (!IsAdmin())
    {
        return AdminLoginRedirect();
    }

    // =====================================================
    // USERS
    // =====================================================

    ViewBag.TotalUsers =
        await _context.Users.CountAsync();

    ViewBag.ActiveUsers =
        await _context.Users
            .CountAsync(u => u.Status);

    ViewBag.DisabledUsers =
        await _context.Users
            .CountAsync(u => !u.Status);


    // =====================================================
    // SHOPKEEPERS
    // =====================================================

    ViewBag.TotalShopkeepers =
        await _context.Users
            .Where(u =>
                u.Role != null &&
                u.Role.RoleName == ShopkeeperRole)
            .CountAsync();

    ViewBag.ApprovedShopkeepers =
        await _context.Users
            .Where(u =>
                u.Role != null &&
                u.Role.RoleName == ShopkeeperRole &&
                u.IsApproved)
            .CountAsync();

    ViewBag.PendingShopkeepers =
        await _context.Users
            .Where(u =>
                u.Role != null &&
                u.Role.RoleName == ShopkeeperRole &&
                !u.IsApproved)
            .CountAsync();


    // =====================================================
    // CUSTOMERS
    // =====================================================

    ViewBag.TotalCustomers =
        await _context.Users
            .Where(u =>
                u.Role != null &&
                u.Role.RoleName == CustomerRole)
            .CountAsync();

    ViewBag.ActiveCustomers =
        await _context.Users
            .Where(u =>
                u.Role != null &&
                u.Role.RoleName == CustomerRole &&
                u.Status)
            .CountAsync();


    // =====================================================
    // DELIVERY
    // =====================================================

    ViewBag.TotalDelivery =
        await _context.Users
            .Where(u =>
                u.Role != null &&
                u.Role.RoleName == DeliveryRole)
            .CountAsync();

    ViewBag.ApprovedDelivery =
        await _context.Users
            .Where(u =>
                u.Role != null &&
                u.Role.RoleName == DeliveryRole &&
                u.IsApproved)
            .CountAsync();

    ViewBag.PendingDelivery =
        await _context.Users
            .Where(u =>
                u.Role != null &&
                u.Role.RoleName == DeliveryRole &&
                !u.IsApproved)
            .CountAsync();


    // =====================================================
    // SHOPS
    // =====================================================

    ViewBag.TotalShops =
        await _context.Shops.CountAsync();

    ViewBag.ApprovedShops =
        await _context.Shops
            .CountAsync(s => s.IsApproved);

    ViewBag.PendingShops =
        await _context.Shops
            .CountAsync(s => !s.IsApproved);

    ViewBag.ActiveShops =
        await _context.Shops
            .CountAsync(s =>
                s.IsApproved &&
                s.Status);

    ViewBag.DisabledShops =
        await _context.Shops
            .CountAsync(s =>
                s.IsApproved &&
                !s.Status);


    // =====================================================
    // ORDERS
    // =====================================================

    var allOrders =
        _context.Orders.AsQueryable();

    var activeOrders =
        allOrders.Where(o =>
            o.OrderStatus != "Cancelled");


    ViewBag.TotalOrders =
        await activeOrders.CountAsync();

    ViewBag.PendingOrders =
        await activeOrders
            .CountAsync(o =>
                o.OrderStatus == "Pending");

    ViewBag.CompletedOrders =
        await activeOrders
            .CountAsync(o =>
                o.OrderStatus == "Completed");

    ViewBag.CancelledOrders =
        await allOrders
            .CountAsync(o =>
                o.OrderStatus == "Cancelled");


    // =====================================================
    // CUSTOMER PAYMENTS
    // =====================================================

    ViewBag.TotalPayments =
        await activeOrders.CountAsync();

    ViewBag.ReceivedPayments =
        await activeOrders
            .CountAsync(o =>
                o.PaymentReceived);

    ViewBag.PendingPayments =
        await activeOrders
            .CountAsync(o =>
                !o.PaymentReceived);


    // =====================================================
    // FINANCIAL TOTALS
    // =====================================================

    ViewBag.TotalProductAmount =
        await activeOrders
            .SumAsync(o =>
                (decimal?)o.ProductTotal) ?? 0m;


    ViewBag.TotalDeliveryCharges =
        await activeOrders
            .SumAsync(o =>
                (decimal?)o.DeliveryCharges) ?? 0m;


    ViewBag.TotalServiceFee =
        await activeOrders
            .SumAsync(o =>
                (decimal?)o.ServiceFee) ?? 0m;


    ViewBag.TotalRevenue =
        await activeOrders
            .SumAsync(o =>
                (decimal?)o.TotalAmount) ?? 0m;


    // =====================================================
    // SHOPKEEPER FINANCIALS
    // =====================================================

    ViewBag.TotalShopkeeperAmount =
        await activeOrders
            .SumAsync(o =>
                (decimal?)o.ShopkeeperAmount) ?? 0m;


    ViewBag.TotalShopkeeperPaid =
        await activeOrders
            .Where(o =>
                o.ShopkeeperPaymentStatus == "Paid")
            .SumAsync(o =>
                (decimal?)o.ShopkeeperPaidAmount) ?? 0m;


    ViewBag.PendingShopkeeperPayments =
        await activeOrders
            .Where(o =>
                o.PaymentReceived &&
                o.ShopkeeperPaymentStatus != "Paid")
            .SumAsync(o =>
                (decimal?)o.ProductTotal) ?? 0m;


    // =====================================================
    // DELIVERY FINANCIALS
    // =====================================================

    ViewBag.TotalDeliveryPaid =
        await activeOrders
            .Where(o =>
                o.DeliveryPaymentStatus == "Paid")
            .SumAsync(o =>
                (decimal?)o.DeliveryPaidAmount) ?? 0m;


    ViewBag.PendingDeliveryPayments =
        await activeOrders
            .Where(o =>
                o.PaymentReceived &&
                o.DeliveryId != null &&
                o.DeliveryPaymentStatus != "Paid")
            .SumAsync(o =>
                (decimal?)o.DeliveryCharges) ?? 0m;


    // =====================================================
    // PAYMENT METHODS
    // =====================================================

    ViewBag.CODPayments =
        await activeOrders
            .CountAsync(o =>
                o.PaymentMethod == "COD" ||
                o.PaymentMethod == "Cash on Delivery");


    ViewBag.JazzCashPayments =
        await activeOrders
            .CountAsync(o =>
                o.PaymentMethod == "JazzCash");


    ViewBag.EasypaisaPayments =
        await activeOrders
            .CountAsync(o =>
                o.PaymentMethod == "Easypaisa");


    // =====================================================
    // RETURN VIEW
    // =====================================================

    return View();
}


        // =====================================================
        // USER ACCOUNTS
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> UserAccounts()
        {
            if (!IsAdmin())
            {
                return AdminLoginRedirect();
            }

            var users =
                await _context.Users
                    .Include(u => u.Role)
                    .OrderByDescending(u => u.CreatedDate)
                    .ToListAsync();

            return View(users);
        }

        // =====================================================
        // USER DETAILS
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> UserDetails(int id)
        {
            if (!IsAdmin())
            {
                return AdminLoginRedirect();
            }

            var user =
                await _context.Users
                    .Include(u => u.Role)
                    .Include(u => u.Shop)
                    .Include(u => u.DeliveryBoy)
                    .FirstOrDefaultAsync(u =>
                        u.UserId == id);

            if (user == null)
            {
                TempData["Error"] =
                    "User not found.";

                return RedirectToAction("UserAccounts");
            }

            return View(user);
        }

        // =====================================================
        // DISABLE USER
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DisableUser(int id)
        {
            if (!IsAdmin())
            {
                return AdminLoginRedirect();
            }

            var user =
                await _context.Users
                    .FirstOrDefaultAsync(u =>
                        u.UserId == id);

            if (user == null)
            {
                TempData["Error"] =
                    "User not found.";

                return RedirectToAction("UserAccounts");
            }

            int? currentUserId =
                HttpContext.Session.GetInt32("UserId");

            if (currentUserId == id)
            {
                TempData["Error"] =
                    "You cannot disable your own admin account.";

                return RedirectToAction(
                    "UserDetails",
                    new { id });
            }

            user.Status = false;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "User account disabled successfully.";

            return RedirectToAction(
                "UserDetails",
                new { id });
        }

        // =====================================================
        // ENABLE USER
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnableUser(int id)
        {
            if (!IsAdmin())
            {
                return AdminLoginRedirect();
            }

            var user =
                await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u =>
                        u.UserId == id);

            if (user == null)
            {
                TempData["Error"] =
                    "User not found.";

                return RedirectToAction("UserAccounts");
            }

            if (user.Role != null &&
                (user.Role.RoleName == ShopkeeperRole ||
                 user.Role.RoleName == DeliveryRole) &&
                !user.IsApproved)
            {
                TempData["Error"] =
                    "This account must be approved before it can be enabled.";

                return RedirectToAction(
                    "UserDetails",
                    new { id });
            }

            user.Status = true;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "User account enabled successfully.";

            return RedirectToAction(
                "UserDetails",
                new { id });
        }

        // =====================================================
        // ADMIN PROFILE
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> AdminProfile()
        {
            if (!IsAdmin())
            {
                return AdminLoginRedirect();
            }

            int? userId =
                HttpContext.Session.GetInt32("UserId");

            if (!userId.HasValue)
            {
                return AdminLoginRedirect();
            }

            var admin =
                await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u =>
                        u.UserId == userId.Value &&
                        u.Role != null &&
                        u.Role.RoleName == AdminRole);

            if (admin == null)
            {
                TempData["Error"] =
                    "Admin profile not found.";

                return RedirectToAction("Dashboard");
            }

            return View(admin);
        }

        // =====================================================
        // UPLOAD ADMIN PROFILE IMAGE
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadProfileImage(
            IFormFile? profileImage)
        {
            if (!IsAdmin())
            {
                return AdminLoginRedirect();
            }

            int? userId =
                HttpContext.Session.GetInt32("UserId");

            if (!userId.HasValue)
            {
                return AdminLoginRedirect();
            }

            if (profileImage == null ||
                profileImage.Length == 0)
            {
                TempData["Error"] =
                    "Please select a profile picture.";

                return RedirectToAction("AdminProfile");
            }

            string[] allowedExtensions =
            {
                ".jpg",
                ".jpeg",
                ".png",
                ".webp"
            };

            string extension =
                Path.GetExtension(
                    profileImage.FileName)
                .ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
            {
                TempData["Error"] =
                    "Only JPG, JPEG, PNG and WEBP images are allowed.";

                return RedirectToAction("AdminProfile");
            }

            const long maxFileSize =
                5 * 1024 * 1024;

            if (profileImage.Length > maxFileSize)
            {
                TempData["Error"] =
                    "Profile picture must be less than 5 MB.";

                return RedirectToAction("AdminProfile");
            }

            var admin =
                await _context.Users
                    .FirstOrDefaultAsync(u =>
                        u.UserId == userId.Value);

            if (admin == null)
            {
                TempData["Error"] =
                    "Admin profile not found.";

                return RedirectToAction("Dashboard");
            }

            string profileFolder =
                Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    "profiles");

            if (!Directory.Exists(profileFolder))
            {
                Directory.CreateDirectory(profileFolder);
            }

            string fileName =
                $"{Guid.NewGuid()}{extension}";

            string filePath =
                Path.Combine(
                    profileFolder,
                    fileName);

            using (var stream =
                new FileStream(
                    filePath,
                    FileMode.Create))
            {
                await profileImage.CopyToAsync(stream);
            }

            // =================================================
            // DELETE OLD IMAGE
            // =================================================

            if (!string.IsNullOrWhiteSpace(
                    admin.ProfileImage))
            {
                string oldImagePath =
                    Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot",
                        admin.ProfileImage
                            .TrimStart('/')
                            .Replace(
                                '/',
                                Path.DirectorySeparatorChar));

                if (System.IO.File.Exists(oldImagePath))
                {
                    try
                    {
                        System.IO.File.Delete(
                            oldImagePath);
                    }
                    catch
                    {
                        // Ignore old image deletion errors
                    }
                }
            }

            admin.ProfileImage =
                "/profiles/" + fileName;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Profile picture uploaded successfully.";

            return RedirectToAction("AdminProfile");
        }

       // =====================================================
// CHAT
// =====================================================

[HttpGet]
public async Task<IActionResult> Chat()
{
    if (!IsAdmin())
    {
        return AdminLoginRedirect();
    }

    int adminId =
        HttpContext.Session.GetInt32("UserId") ?? 0;

    if (adminId <= 0)
    {
        return AdminLoginRedirect();
    }

    // -------------------------------------------------
    // GET USERS WHO HAVE A CHAT WITH ADMIN
    // -------------------------------------------------

    var userIds =
        await _context.ChatMessages
            .Where(c =>
                c.SenderId == adminId ||
                c.ReceiverId == adminId)
            .Select(c =>
                c.SenderId == adminId
                    ? c.ReceiverId
                    : c.SenderId)
            .Distinct()
            .ToListAsync();

    var users =
        await _context.Users
            .Include(u => u.Role)
            .Where(u =>
                u.UserId != adminId &&
                userIds.Contains(u.UserId) &&
                u.Status)
            .ToListAsync();

    // -------------------------------------------------
    // CONVERSATIONS
    // -------------------------------------------------

    var conversations = new List<object>();

    foreach (var user in users)
    {
        var lastMessage =
            await _context.ChatMessages
                .Where(c =>
                    (c.SenderId == adminId &&
                     c.ReceiverId == user.UserId) ||

                    (c.SenderId == user.UserId &&
                     c.ReceiverId == adminId))
                .OrderByDescending(c => c.SentDate)
                .FirstOrDefaultAsync();

        int unreadCount =
            await _context.ChatMessages
                .CountAsync(c =>
                    c.SenderId == user.UserId &&
                    c.ReceiverId == adminId &&
                    !c.IsRead);

        conversations.Add(new
        {
            UserId = user.UserId,

            UserName = user.Name,

            Role = user.Role != null
                ? user.Role.RoleName
                : "User",

            LastMessage =
                lastMessage?.Message ?? "",

            LastMessageDate =
                lastMessage?.SentDate,

            UnreadCount =
                unreadCount
        });
    }

    ViewBag.Conversations =
        conversations
            .OrderByDescending(c =>
                ((dynamic)c).LastMessageDate
                ?? DateTime.MinValue)
            .ToList();

    ViewBag.SelectedUserId = null;
    ViewBag.SelectedUserName = null;
    ViewBag.SelectedUserRole = null;
    ViewBag.CurrentUserId = adminId;

    // -------------------------------------------------
    // BOTH COUNTS
    // -------------------------------------------------

    ViewBag.UnreadChatCount =
        await GetAdminUnreadChatCount();

    ViewBag.UnreadNotificationCount =
        await GetAdminUnreadNotificationCount();

    return View(new List<ChatMessage>());
}     
        // =====================================================
// OPEN CONVERSATION
// =====================================================

[HttpGet]
public async Task<IActionResult> Conversation(int userId)
{
    if (!IsAdmin())
    {
        return AdminLoginRedirect();
    }

    int adminId =
        HttpContext.Session.GetInt32("UserId") ?? 0;

    if (adminId <= 0)
    {
        return AdminLoginRedirect();
    }

    // -------------------------------------------------
    // FIND SELECTED USER
    // -------------------------------------------------

    var selectedUser =
        await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u =>
                u.UserId == userId &&
                u.UserId != adminId &&
                u.Status);

    if (selectedUser == null)
    {
        TempData["Error"] =
            "User not found.";

        return RedirectToAction(nameof(Chat));
    }

    // -------------------------------------------------
    // GET CONVERSATION MESSAGES
    // -------------------------------------------------

    var messages =
        await _context.ChatMessages
            .Include(c => c.Sender)
                .ThenInclude(u => u.Role)
            .Include(c => c.Receiver)
                .ThenInclude(u => u.Role)
            .Where(c =>
                (c.SenderId == adminId &&
                 c.ReceiverId == userId) ||

                (c.SenderId == userId &&
                 c.ReceiverId == adminId))
            .OrderBy(c => c.SentDate)
            .ToListAsync();

    // -------------------------------------------------
    // MARK ONLY RECEIVED MESSAGES AS READ
    // -------------------------------------------------

    var unreadMessages =
        messages
            .Where(c =>
                c.SenderId == userId &&
                c.ReceiverId == adminId &&
                !c.IsRead)
            .ToList();

    if (unreadMessages.Any())
    {
        foreach (var message in unreadMessages)
        {
            message.IsRead = true;
        }

        await _context.SaveChangesAsync();
    }

    // -------------------------------------------------
    // REBUILD CONVERSATIONS
    // -------------------------------------------------

    var userIds =
        await _context.ChatMessages
            .Where(c =>
                c.SenderId == adminId ||
                c.ReceiverId == adminId)
            .Select(c =>
                c.SenderId == adminId
                    ? c.ReceiverId
                    : c.SenderId)
            .Distinct()
            .ToListAsync();

    var users =
        await _context.Users
            .Include(u => u.Role)
            .Where(u =>
                u.UserId != adminId &&
                userIds.Contains(u.UserId) &&
                u.Status)
            .ToListAsync();

    var conversations = new List<object>();

    foreach (var user in users)
    {
        var lastMessage =
            await _context.ChatMessages
                .Where(c =>
                    (c.SenderId == adminId &&
                     c.ReceiverId == user.UserId) ||

                    (c.SenderId == user.UserId &&
                     c.ReceiverId == adminId))
                .OrderByDescending(c => c.SentDate)
                .FirstOrDefaultAsync();

        int unreadCount =
            await _context.ChatMessages
                .CountAsync(c =>
                    c.SenderId == user.UserId &&
                    c.ReceiverId == adminId &&
                    !c.IsRead);

        conversations.Add(new
        {
            UserId = user.UserId,

            UserName = user.Name,

            Role = user.Role != null
                ? user.Role.RoleName
                : "User",

            LastMessage =
                lastMessage?.Message ?? "",

            LastMessageDate =
                lastMessage?.SentDate,

            UnreadCount =
                unreadCount
        });
    }

    ViewBag.Conversations =
        conversations
            .OrderByDescending(c =>
                ((dynamic)c).LastMessageDate
                ?? DateTime.MinValue)
            .ToList();

    // -------------------------------------------------
    // SELECTED USER
    // -------------------------------------------------

    ViewBag.SelectedUserId =
        userId;

    ViewBag.SelectedUserName =
        selectedUser.Name;

    ViewBag.SelectedUserRole =
        selectedUser.Role?.RoleName;

    ViewBag.CurrentUserId =
        adminId;

    // -------------------------------------------------
    // BOTH COUNTS AFTER MARKING CHAT AS READ
    // -------------------------------------------------

    ViewBag.UnreadChatCount =
        await GetAdminUnreadChatCount();

    ViewBag.UnreadNotificationCount =
        await GetAdminUnreadNotificationCount();

    return View(
        "Chat",
        messages);
}
        // =====================================================
// SEND MESSAGE
// =====================================================

[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> SendMessage(
    int receiverId,
    string message)
{
    if (!IsAdmin())
    {
        return AdminLoginRedirect();
    }

    int adminId =
        HttpContext.Session.GetInt32("UserId") ?? 0;

    if (adminId <= 0)
    {
        return AdminLoginRedirect();
    }

    if (receiverId == adminId)
    {
        TempData["Error"] =
            "You cannot send a message to yourself.";

        return RedirectToAction(
            nameof(Conversation),
            new { userId = receiverId });
    }

    if (string.IsNullOrWhiteSpace(message))
    {
        TempData["Error"] =
            "Message cannot be empty.";

        return RedirectToAction(
            nameof(Conversation),
            new { userId = receiverId });
    }

    var receiver =
        await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u =>
                u.UserId == receiverId &&
                u.Status);

    if (receiver == null)
    {
        TempData["Error"] =
            "User not found or disabled.";

        return RedirectToAction(nameof(Chat));
    }

    // -------------------------------------------------
    // CHAT MESSAGE
    // -------------------------------------------------

    var chatMessage =
        new ChatMessage
        {
            SenderId = adminId,
            ReceiverId = receiverId,
            Message = message.Trim(),
            IsRead = false,
            SentDate = DateTime.Now
        };

    _context.ChatMessages.Add(chatMessage);

    // -------------------------------------------------
    // NOTIFICATION
    // -------------------------------------------------

    await CreateNotification(
        receiverId,
        "New Message from Admin",
        $"Admin sent you a new message: {message.Trim()}",
        "Chat");

    // -------------------------------------------------
    // SAVE BOTH TO DATABASE
    // -------------------------------------------------

    await _context.SaveChangesAsync();

    return RedirectToAction(
        nameof(Conversation),
        new { userId = receiverId });
}
        // =====================================================
        // DELETE CHAT MESSAGE
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteChatMessage(
            int id)
        {
            if (!IsAdmin())
            {
                return AdminLoginRedirect();
            }

            var message =
                await _context.ChatMessages
                    .FirstOrDefaultAsync(c =>
                        c.ChatMessageId == id);

            if (message == null)
            {
                TempData["Error"] =
                    "Chat message not found.";

                return RedirectToAction("Chat");
            }

            _context.ChatMessages.Remove(
                message);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Chat message deleted successfully.";

            return RedirectToAction("Chat");
        }

    [HttpGet]
public async Task<IActionResult> Notifications()
{
    if (!IsAdmin())
        return AdminLoginRedirect();

    int adminId = HttpContext.Session.GetInt32("UserId") ?? 0;

    if (adminId <= 0)
        return AdminLoginRedirect();

    // =====================================================
    // GET ADMIN NOTIFICATIONS
    // =====================================================

    var notifications = await _context.Notifications
        .Include(n => n.User)
            .ThenInclude(u => u.Role)
        .Include(n => n.Order)
        .Where(n => n.UserId == adminId)
        .OrderByDescending(n => n.CreatedDate)
        .ToListAsync();

    // =====================================================
    // GET CURRENT UNREAD COUNT FROM DATABASE
    // =====================================================

    int unreadCount = await GetAdminUnreadNotificationCount();

    ViewBag.UnreadNotificationCount = unreadCount;

    // =====================================================
    // USERS FOR SEND NOTIFICATION
    // =====================================================

    ViewBag.Users = await _context.Users
        .Include(u => u.Role)
        .Where(u =>
            u.UserId != adminId &&
            u.Role != null &&
            u.Role.RoleName != AdminRole &&
            u.Status)
        .OrderBy(u => u.Name)
        .ToListAsync();

    return View(notifications);
}
        // =====================================================
        // SEND NOTIFICATION FROM ADMIN
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendNotification(
            int userId,
            string title,
            string message,
            string? type)
        {
            if (!IsAdmin())
            {
                return AdminLoginRedirect();
            }

            if (userId <= 0)
            {
                TempData["Error"] =
                    "Please select a user.";

                return RedirectToAction(
                    nameof(Notifications));
            }

            if (string.IsNullOrWhiteSpace(title))
            {
                TempData["Error"] =
                    "Notification title is required.";

                return RedirectToAction(
                    nameof(Notifications));
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                TempData["Error"] =
                    "Notification message is required.";

                return RedirectToAction(
                    nameof(Notifications));
            }

            var user =
                await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u =>
                        u.UserId == userId);

            if (user == null)
            {
                TempData["Error"] =
                    "User not found.";

                return RedirectToAction(
                    nameof(Notifications));
            }

            var notification =
                new Notification
                {
                    UserId = user.UserId,
                    Title = title.Trim(),
                    Message = message.Trim(),
                    Type =
                        string.IsNullOrWhiteSpace(type)
                            ? "General"
                            : type.Trim(),
                    IsRead = false,
                    CreatedDate = DateTime.Now,
                    OrderId = null
                };

            _context.Notifications.Add(
                notification);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"Notification sent successfully to {user.Name}.";

            return RedirectToAction(
                nameof(Notifications));
        }

        // =====================================================
        // DELETE NOTIFICATION
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteNotification(
            int id)
        {
            if (!IsAdmin())
            {
                return AdminLoginRedirect();
            }

            var notification =
                await _context.Notifications
                    .FirstOrDefaultAsync(n =>
                        n.NotificationId == id);

            if (notification == null)
            {
                TempData["Error"] =
                    "Notification not found.";

                return RedirectToAction(
                    nameof(Notifications));
            }

            _context.Notifications.Remove(
                notification);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Notification deleted successfully.";

            return RedirectToAction(
                nameof(Notifications));
        }
// =====================================================
// ADMIN UNREAD NOTIFICATION COUNT
// =====================================================

private async Task<int> GetAdminUnreadNotificationCount()
{
    int adminId =
        HttpContext.Session.GetInt32("UserId") ?? 0;

    if (adminId <= 0)
    {
        return 0;
    }

    return await _context.Notifications
        .CountAsync(n =>
            n.UserId == adminId &&
            !n.IsRead);
}


// =====================================================
// ADMIN UNREAD CHAT COUNT
// =====================================================

private async Task<int> GetAdminUnreadChatCount()
{
    int adminId =
        HttpContext.Session.GetInt32("UserId") ?? 0;

    if (adminId <= 0)
    {
        return 0;
    }

    return await _context.ChatMessages
        .CountAsync(c =>
            c.ReceiverId == adminId &&
            !c.IsRead);
}
        // =====================================================
// CREATE NOTIFICATION
// =====================================================

private async Task CreateNotification(
    int userId,
    string title,
    string message,
    string type = "General",
    int? orderId = null)
{
    if (userId <= 0)
        return;

    if (string.IsNullOrWhiteSpace(title))
        return;

    if (string.IsNullOrWhiteSpace(message))
        return;

    bool userExists = await _context.Users
        .AnyAsync(u => u.UserId == userId);

    if (!userExists)
        return;

    var notification = new Notification
    {
        UserId = userId,
        Title = title.Trim(),
        Message = message.Trim(),
        Type = string.IsNullOrWhiteSpace(type)
            ? "General"
            : type.Trim(),
        OrderId = orderId,
        IsRead = false,
        CreatedDate = DateTime.Now
    };

    _context.Notifications.Add(notification);
}

       // =====================================================
// MARK SINGLE NOTIFICATION AS READ
// =====================================================

[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> MarkNotificationRead(int id)
{
    if (!IsAdmin())
    {
        return AdminLoginRedirect();
    }

    int adminId =
        HttpContext.Session.GetInt32("UserId") ?? 0;

    if (adminId <= 0)
    {
        return AdminLoginRedirect();
    }

    var notification =
        await _context.Notifications
            .FirstOrDefaultAsync(n =>
                n.NotificationId == id &&
                n.UserId == adminId);

    if (notification == null)
    {
        TempData["Error"] =
            "Notification not found.";

        return RedirectToAction(
            nameof(Notifications));
    }

    notification.IsRead = true;

    await _context.SaveChangesAsync();

    return RedirectToAction(
        nameof(Notifications));
}
        // =====================================================
// MARK ALL ADMIN NOTIFICATIONS AS READ
// =====================================================

[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> MarkAllNotificationsRead()
{
    if (!IsAdmin())
    {
        return AdminLoginRedirect();
    }

    int adminId =
        HttpContext.Session.GetInt32("UserId") ?? 0;

    if (adminId <= 0)
    {
        return AdminLoginRedirect();
    }

    var unreadNotifications =
        await _context.Notifications
            .Where(n =>
                n.UserId == adminId &&
                !n.IsRead)
            .ToListAsync();

    foreach (var notification in unreadNotifications)
    {
        notification.IsRead = true;
    }

    await _context.SaveChangesAsync();

    TempData["Success"] =
        "All notifications marked as read.";

    return RedirectToAction(
        nameof(Notifications));
}
        // =====================================================
        // FEEDBACK / REVIEWS
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Feedback()
        {
            if (!IsAdmin())
            {
                return AdminLoginRedirect();
            }

            var feedback =
                await _context.Reviews
                    .Include(r => r.User)
                    .Include(r => r.Product)
                    .Include(r => r.Shop)
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync();

            return View(feedback);
        }

        // =====================================================
        // APPROVE FEEDBACK
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveFeedback(
            int id)
        {
            if (!IsAdmin())
            {
                return AdminLoginRedirect();
            }

            var review =
                await _context.Reviews
                    .FirstOrDefaultAsync(r =>
                        r.ReviewId == id);

            if (review == null)
            {
                TempData["Error"] =
                    "Feedback not found.";

                return RedirectToAction("Feedback");
            }

            review.IsApproved = true;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Feedback approved successfully.";

            return RedirectToAction("Feedback");
        }

        // =====================================================
        // REJECT / HIDE FEEDBACK
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectFeedback(
            int id)
        {
            if (!IsAdmin())
            {
                return AdminLoginRedirect();
            }

            var review =
                await _context.Reviews
                    .FirstOrDefaultAsync(r =>
                        r.ReviewId == id);

            if (review == null)
            {
                TempData["Error"] =
                    "Feedback not found.";

                return RedirectToAction("Feedback");
            }

            review.IsApproved = false;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Feedback hidden successfully.";

            return RedirectToAction("Feedback");
        }


        // =====================================================
        // LOGOUT
        // =====================================================

        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();

            return RedirectToAction(
                "Login",
                "Account");
        }
    }
}