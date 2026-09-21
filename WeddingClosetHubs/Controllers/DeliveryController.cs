using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WeddingClosetHubs.Models;

namespace WeddingClosetHubs.Controllers
{
    public class DeliveryController : Controller
    {
        private readonly WeddingClosetHubsContext _context;

        public DeliveryController(WeddingClosetHubsContext context)
        {
            _context = context;
        }

        private bool IsDeliveryLoggedIn()
        {
            int? userId =
                HttpContext.Session.GetInt32("UserId");

            string? role =
                HttpContext.Session.GetString("RoleName");

            return userId.HasValue &&
                   userId.Value > 0 &&
                   !string.IsNullOrWhiteSpace(role) &&
                   role.Equals(
                       "Delivery",
                       StringComparison.OrdinalIgnoreCase);
        }

        private int? GetDeliveryUserId()
        {
            return HttpContext.Session.GetInt32("UserId");
        }

        private IActionResult DeliveryLogin()
        {
            HttpContext.Session.Clear();

            return RedirectToAction(
                "Login",
                "Account");
        }

        private async Task<DeliveryBoy?> GetDeliveryBoy()
        {
            int? userId =
                GetDeliveryUserId();

            if (!userId.HasValue)
                return null;

            return await _context.DeliveryBoys
                .Include(d => d.User)
                    .ThenInclude(u => u!.Role)
                .FirstOrDefaultAsync(d =>
                    d.UserId == userId.Value);
        }

        private async Task<bool> IsApprovedDeliveryBoy()
        {
            var deliveryBoy =
                await GetDeliveryBoy();

            if (deliveryBoy == null)
                return false;

            if (string.IsNullOrWhiteSpace(
                    deliveryBoy.VerificationStatus))
            {
                return false;
            }

            if (!deliveryBoy.VerificationStatus.Equals(
                    "Approved",
                    StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (deliveryBoy.User == null)
                return false;

            if (!deliveryBoy.User.IsApproved)
                return false;

            if (!deliveryBoy.User.Status)
                return false;

            return true;
        }


        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            if (!IsDeliveryLoggedIn())
                return DeliveryLogin();

            if (!await IsApprovedDeliveryBoy())
            {
                HttpContext.Session.Clear();

                TempData["Error"] =
                    "Your delivery account has not been approved by admin.";

                return RedirectToAction(
                    "Login",
                    "Account");
            }

            var deliveryBoy =
                await GetDeliveryBoy();

            if (deliveryBoy == null)
                return DeliveryLogin();

            int deliveryUserId =
                deliveryBoy.UserId;


            int totalAssigned =
                await _context.Orders
                    .CountAsync(o =>
                        o.DeliveryId == deliveryUserId &&
                        o.OrderStatus != "Cancelled");


            int activeOrders =
                await _context.Orders
                    .CountAsync(o =>
                        o.DeliveryId == deliveryUserId &&
                        o.OrderStatus != "Cancelled" &&
                        o.OrderStatus != "Delivered" &&
                        o.OrderStatus != "Completed" &&
                        (
                            o.OrderStatus == "Pending" ||
                            o.OrderStatus == "Ready" ||
                            o.OrderStatus == "Confirmed" ||
                            o.OrderStatus == "Assigned" ||
                            o.OrderStatus == "Picked Up" ||
                            o.OrderStatus == "Out for Delivery"
                        ));


            int deliveredOrders =
                await _context.Orders
                    .CountAsync(o =>
                        o.DeliveryId == deliveryUserId &&
                        o.OrderStatus != "Cancelled" &&
                        (
                            o.OrderStatus == "Delivered" ||
                            o.OrderStatus == "Completed"
                        ));


            int codOrders =
                await _context.Orders
                    .CountAsync(o =>
                        o.DeliveryId == deliveryUserId &&
                        o.OrderStatus != "Cancelled" &&
                        (
                            o.PaymentMethod == "COD" ||
                            o.PaymentMethod == "Cash on Delivery"
                        ));


            decimal codCollected =
                await _context.Orders
                    .Where(o =>
                        o.DeliveryId == deliveryUserId &&
                        o.OrderStatus != "Cancelled" &&
                        (
                            o.PaymentMethod == "COD" ||
                            o.PaymentMethod == "Cash on Delivery"
                        ) &&
                        o.DeliveryCollectedAmount > 0)
                    .SumAsync(o =>
                        o.DeliveryCollectedAmount);


            decimal totalEarnings =
                await _context.Orders
                    .Where(o =>
                        o.DeliveryId == deliveryUserId &&
                        o.OrderStatus != "Cancelled" &&
                        (
                            o.OrderStatus == "Delivered" ||
                            o.OrderStatus == "Completed"
                        ))
                    .SumAsync(o =>
                        o.DeliveryCharges);


            decimal paidEarnings =
                await _context.Orders
                    .Where(o =>
                        o.DeliveryId == deliveryUserId &&
                        o.OrderStatus != "Cancelled" &&
                        (
                            o.OrderStatus == "Delivered" ||
                            o.OrderStatus == "Completed"
                        ) &&
                        o.DeliveryPaymentStatus == "Paid")
                    .SumAsync(o =>
                        o.DeliveryPaidAmount);


            decimal pendingEarnings =
                await _context.Orders
                    .Where(o =>
                        o.DeliveryId == deliveryUserId &&
                        o.OrderStatus != "Cancelled" &&
                        (
                            o.OrderStatus == "Delivered" ||
                            o.OrderStatus == "Completed"
                        ) &&
                        o.DeliveryPaymentStatus != "Paid")
                    .SumAsync(o =>
                        o.DeliveryCharges);


            ViewBag.PaymentMethod =
                deliveryBoy.PaymentMethod;

            ViewBag.PaymentAccount =
                deliveryBoy.PaymentAccount;


            string accountStatus;

            if (deliveryBoy.User != null &&
                deliveryBoy.User.IsApproved &&
                deliveryBoy.User.Status &&
                string.Equals(
                    deliveryBoy.VerificationStatus,
                    "Approved",
                    StringComparison.OrdinalIgnoreCase))
            {
                accountStatus = "Active";
            }
            else
            {
                accountStatus = "Pending";
            }

            ViewBag.VerificationStatus =
                accountStatus;

            
            ViewBag.DeliveryBoy =
                deliveryBoy;

            ViewBag.TotalAssigned =
                totalAssigned;

            ViewBag.ActiveOrders =
                activeOrders;

            ViewBag.PendingOrders =
                activeOrders;

            ViewBag.DeliveredOrders =
                deliveredOrders;

            ViewBag.CompletedOrders =
                deliveredOrders;

            

            ViewBag.CodOrders =
                codOrders;

            ViewBag.CodCollected =
                codCollected;

            ViewBag.TotalEarnings =
                totalEarnings;

            ViewBag.PaidEarnings =
                paidEarnings;

            ViewBag.PendingEarnings =
                pendingEarnings;

            ViewBag.PendingPayment =
                pendingEarnings;

            ViewBag.AssignedZone =
                string.IsNullOrWhiteSpace(
                    deliveryBoy.AssignedZone)
                    ? "Not Assigned"
                    : deliveryBoy.AssignedZone.Trim();

            ViewBag.DeliveryName =
                deliveryBoy.User?.Name ??
                "Delivery Boy";

            ViewBag.CurrentUserId =
                deliveryUserId;

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Orders()
        {
            if (!IsDeliveryLoggedIn())
                return DeliveryLogin();

            if (!await IsApprovedDeliveryBoy())
                return DeliveryLogin();

            var deliveryBoy =
                await GetDeliveryBoy();

            if (deliveryBoy == null)
                return DeliveryLogin();

            int deliveryUserId =
                deliveryBoy.UserId;

            string assignedZone =
                deliveryBoy.AssignedZone?.Trim() ?? "";

            var orders =
                await _context.Orders
                    .Include(o => o.Customer)
                    .Include(o => o.Shop)
                    .Include(o => o.OrderDetails)
                        .ThenInclude(od => od.Product)
                    .Where(o =>
                        o.OrderStatus != "Cancelled" &&
                        (
                            o.DeliveryId == deliveryUserId
                            ||
                            (
                                o.DeliveryId == null &&
                                (
                                    o.OrderStatus == "Ready" ||
                                    o.OrderStatus == "Confirmed"
                                ) &&
                                !string.IsNullOrWhiteSpace(
                                    assignedZone) &&
                                !string.IsNullOrWhiteSpace(
                                    o.DeliveryAddress) &&
                                o.DeliveryAddress.Contains(
                                    assignedZone)
                            )
                        ))
                    .OrderByDescending(
                        o => o.CreatedDate)
                    .ToListAsync();

            ViewBag.DeliveryName =
                deliveryBoy.User?.Name ??
                "Delivery Boy";

            ViewBag.AssignedZone =
                string.IsNullOrWhiteSpace(
                    assignedZone)
                    ? "Not Assigned"
                    : assignedZone;

            ViewBag.CurrentUserId =
                deliveryUserId;

            return View(orders);
        }

        [HttpGet]
        public async Task<IActionResult> MyOrders()
        {
            if (!IsDeliveryLoggedIn())
                return DeliveryLogin();

            if (!await IsApprovedDeliveryBoy())
                return DeliveryLogin();

            return RedirectToAction(
                "Orders");
        }

        [HttpGet]
        public async Task<IActionResult> OrderDetails(
            int id)
        {
            if (!IsDeliveryLoggedIn())
                return DeliveryLogin();

            if (!await IsApprovedDeliveryBoy())
                return DeliveryLogin();

            var deliveryBoy =
                await GetDeliveryBoy();

            if (deliveryBoy == null)
                return DeliveryLogin();

            int deliveryUserId =
                deliveryBoy.UserId;

            string assignedZone =
                deliveryBoy.AssignedZone?.Trim() ?? "";

            var order =
                await _context.Orders
                    .Include(o => o.Customer)
                    .Include(o => o.Shop)
                    .Include(o => o.Delivery)
                    .Include(o => o.OrderDetails)
                        .ThenInclude(od => od.Product)
                    .FirstOrDefaultAsync(o =>
                        o.OrderId == id);

            if (order == null)
            {
                TempData["Error"] =
                    "Order not found.";

                return RedirectToAction(
                    "Orders");
            }

            if (string.Equals(
                    order.OrderStatus,
                    "Cancelled",
                    StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] =
                    "Cancelled orders are not available.";

                return RedirectToAction(
                    "Orders");
            }

            string orderStatus =
                string.IsNullOrWhiteSpace(
                    order.OrderStatus)
                    ? "Pending"
                    : order.OrderStatus.Trim();

            bool belongsToDeliveryBoy =
                order.DeliveryId.HasValue &&
                order.DeliveryId.Value ==
                deliveryUserId;

            bool isReady =
                orderStatus.Equals(
                    "Ready",
                    StringComparison.OrdinalIgnoreCase);

            bool isConfirmed =
                orderStatus.Equals(
                    "Confirmed",
                    StringComparison.OrdinalIgnoreCase);

            bool availableInZone =
                !order.DeliveryId.HasValue &&
                (isReady || isConfirmed) &&
                !string.IsNullOrWhiteSpace(
                    assignedZone) &&
                !string.IsNullOrWhiteSpace(
                    order.DeliveryAddress) &&
                order.DeliveryAddress.Contains(
                    assignedZone,
                    StringComparison.OrdinalIgnoreCase);

            if (!belongsToDeliveryBoy &&
                !availableInZone)
            {
                TempData["Error"] =
                    "You are not authorized to view this order.";

                return RedirectToAction(
                    "Orders");
            }

            ViewBag.CurrentUserId =
                deliveryUserId;

            ViewBag.DeliveryName =
                deliveryBoy.User?.Name ??
                "Delivery Boy";

            ViewBag.AssignedZone =
                string.IsNullOrWhiteSpace(
                    assignedZone)
                    ? "Not Assigned"
                    : assignedZone;

            ViewBag.OrderStatus =
                orderStatus;

            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptOrder(
            int id)
        {
            if (!IsDeliveryLoggedIn())
                return DeliveryLogin();

            if (!await IsApprovedDeliveryBoy())
                return DeliveryLogin();

            var deliveryBoy =
                await GetDeliveryBoy();

            if (deliveryBoy == null)
                return DeliveryLogin();

            int deliveryUserId =
                deliveryBoy.UserId;

            var order =
                await _context.Orders
                    .Include(o => o.Customer)
                    .Include(o => o.Shop)
                    .FirstOrDefaultAsync(o =>
                        o.OrderId == id);

            if (order == null)
            {
                TempData["Error"] =
                    "Order not found.";

                return RedirectToAction(
                    "Orders");
            }

            if (string.Equals(
                    order.OrderStatus,
                    "Cancelled",
                    StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] =
                    "Cancelled orders cannot be accepted.";

                return RedirectToAction(
                    "Orders");
            }

            string currentStatus =
                string.IsNullOrWhiteSpace(
                    order.OrderStatus)
                    ? "Pending"
                    : order.OrderStatus.Trim();

            if (order.DeliveryId.HasValue)
            {
                if (order.DeliveryId.Value ==
                    deliveryUserId)
                {
                    if (currentStatus.Equals(
                            "Ready",
                            StringComparison.OrdinalIgnoreCase)
                        ||
                        currentStatus.Equals(
                            "Confirmed",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        order.OrderStatus =
                            "Assigned";

                        await CreateNotification(
                            order.CustomerId,
                            "Delivery Assignment Confirmed",
                            $"Delivery boy {deliveryBoy.User?.Name ?? "Delivery Boy"} has confirmed delivery of Order #{order.OrderId}.",
                            "Order",
                            order.OrderId);

                        var admin =
                            await GetAdmin();

                        if (admin != null)
                        {
                            await CreateNotification(
                                admin.UserId,
                                "Delivery Assignment Confirmed",
                                $"Delivery boy {deliveryBoy.User?.Name ?? "Delivery Boy"} confirmed Order #{order.OrderId}.",
                                "Order",
                                order.OrderId);
                        }

                        await _context.SaveChangesAsync();

                        TempData["Success"] =
                            $"Order #{order.OrderId} is now assigned to you.";

                        return RedirectToAction(
                            "OrderDetails",
                            new
                            {
                                id = order.OrderId
                            });
                    }

                    TempData["Error"] =
                        "This order is already assigned to you.";

                    return RedirectToAction(
                        "OrderDetails",
                        new
                        {
                            id = order.OrderId
                        });
                }

                TempData["Error"] =
                    "This order has already been assigned to another delivery boy.";

                return RedirectToAction(
                    "Orders");
            }

            if (!currentStatus.Equals(
                    "Ready",
                    StringComparison.OrdinalIgnoreCase)
                &&
                !currentStatus.Equals(
                    "Confirmed",
                    StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] =
                    "This order is not available for delivery.";

                return RedirectToAction(
                    "Orders");
            }

            string assignedZone =
                deliveryBoy.AssignedZone?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(
                    assignedZone))
            {
                TempData["Error"] =
                    "Admin has not assigned a delivery zone to you.";

                return RedirectToAction(
                    "Orders");
            }

            if (string.IsNullOrWhiteSpace(
                    order.DeliveryAddress))
            {
                TempData["Error"] =
                    "This order does not have a delivery address.";

                return RedirectToAction(
                    "Orders");
            }

            if (!order.DeliveryAddress.Contains(
                    assignedZone,
                    StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] =
                    "This order is outside your assigned delivery zone.";

                return RedirectToAction(
                    "Orders");
            }

            order.DeliveryId =
                deliveryUserId;

            order.OrderStatus =
                "Assigned";

            await CreateNotification(
                order.CustomerId,
                "Delivery Boy Assigned",
                $"Delivery boy {deliveryBoy.User?.Name ?? "Delivery Boy"} has been assigned to your Order #{order.OrderId}.",
                "Order",
                order.OrderId);

            var assignedAdmin =
                await GetAdmin();

            if (assignedAdmin != null)
            {
                await CreateNotification(
                    assignedAdmin.UserId,
                    "Order Accepted by Delivery Boy",
                    $"Delivery boy {deliveryBoy.User?.Name ?? "Delivery Boy"} has accepted Order #{order.OrderId}.",
                    "Order",
                    order.OrderId);
            }

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"Order #{order.OrderId} has been assigned to you.";

            return RedirectToAction(
                "OrderDetails",
                new
                {
                    id = order.OrderId
                });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateDeliveryStatus(
            int id,
            string status)
        {
            if (!IsDeliveryLoggedIn())
                return DeliveryLogin();

            if (!await IsApprovedDeliveryBoy())
                return DeliveryLogin();

            int? userId =
                GetDeliveryUserId();

            if (!userId.HasValue)
                return DeliveryLogin();

            int deliveryUserId =
                userId.Value;

            var order =
                await _context.Orders
                    .Include(o => o.Customer)
                    .Include(o => o.Shop)
                    .FirstOrDefaultAsync(o =>
                        o.OrderId == id &&
                        o.DeliveryId == deliveryUserId);

            if (order == null)
            {
                TempData["Error"] =
                    "Order not found or it is not assigned to you.";

                return RedirectToAction(
                    "Orders");
            }

            if (string.Equals(
                    order.OrderStatus,
                    "Cancelled",
                    StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] =
                    "Cancelled orders cannot be updated.";

                return RedirectToAction(
                    "Orders");
            }

            if (string.IsNullOrWhiteSpace(status))
            {
                TempData["Error"] =
                    "Invalid delivery status.";

                return RedirectToAction(
                    "OrderDetails",
                    new { id });
            }

            status =
                status.Trim();

            string currentStatus =
                string.IsNullOrWhiteSpace(
                    order.OrderStatus)
                    ? "Pending"
                    : order.OrderStatus.Trim();

           
            if (status.Equals(
                    "Picked Up",
                    StringComparison.OrdinalIgnoreCase))
            {
                if (!currentStatus.Equals(
                        "Assigned",
                        StringComparison.OrdinalIgnoreCase))
                {
                    TempData["Error"] =
                        $"Order must be Assigned before it can be marked as Picked Up. Current status: {currentStatus}.";

                    return RedirectToAction(
                        "OrderDetails",
                        new { id });
                }

                order.OrderStatus =
                    "Picked Up";

                await CreateNotification(
                    order.CustomerId,
                    "Order Picked Up",
                    $"Your Order #{order.OrderId} has been picked up by the delivery boy.",
                    "Order",
                    order.OrderId);
            }

            else if (status.Equals(
                         "Out for Delivery",
                         StringComparison.OrdinalIgnoreCase))
            {
                if (!currentStatus.Equals(
                        "Picked Up",
                        StringComparison.OrdinalIgnoreCase))
                {
                    TempData["Error"] =
                        $"Order must be Picked Up before it can be marked as Out for Delivery. Current status: {currentStatus}.";

                    return RedirectToAction(
                        "OrderDetails",
                        new { id });
                }

                order.OrderStatus =
                    "Out for Delivery";

                await CreateNotification(
                    order.CustomerId,
                    "Order Out for Delivery",
                    $"Your Order #{order.OrderId} is now out for delivery.",
                    "Order",
                    order.OrderId);
            }

            else if (status.Equals(
                         "Delivered",
                         StringComparison.OrdinalIgnoreCase))
            {
                if (!currentStatus.Equals(
                        "Out for Delivery",
                        StringComparison.OrdinalIgnoreCase))
                {
                    TempData["Error"] =
                        $"Order must be Out for Delivery before it can be marked as Delivered. Current status: {currentStatus}.";

                    return RedirectToAction(
                        "OrderDetails",
                        new { id });
                }

                bool isCod =
                    IsCashOnDelivery(
                        order.PaymentMethod);

                if (isCod &&
                    order.DeliveryCollectedAmount <
                    order.TotalAmount)
                {
                    TempData["Error"] =
                        $"Please collect the full COD amount of Rs. {order.TotalAmount:N2} before completing this delivery.";

                    return RedirectToAction(
                        "OrderDetails",
                        new { id });
                }

                order.OrderStatus =
                    "Delivered";

                order.DeliveryPaymentStatus =
                    "Pending";

                await CreateNotification(
                    order.CustomerId,
                    "Order Delivered",
                    $"Your Order #{order.OrderId} has been delivered successfully.",
                    "Order",
                    order.OrderId);

                if (order.Shop != null)
                {
                    await CreateNotification(
                        order.Shop.ShopkeeperId,
                        "Order Delivered",
                        $"Order #{order.OrderId} has been delivered to the customer.",
                        "Order",
                        order.OrderId);
                }

                var admin =
                    await GetAdmin();

                if (admin != null)
                {
                    await CreateNotification(
                        admin.UserId,
                        "Order Delivered",
                        $"Order #{order.OrderId} has been delivered. Delivery payout of Rs. {order.DeliveryCharges:N2} is pending.",
                        "Payment",
                        order.OrderId);

                    await CreateNotification(
                        deliveryUserId,
                        "Delivery Completed",
                        $"Order #{order.OrderId} was delivered successfully. Your delivery payment of Rs. {order.DeliveryCharges:N2} is now pending Admin payment.",
                        "Payment",
                        order.OrderId);
                }
            }

            else
            {
                TempData["Error"] =
                    $"Invalid delivery status: {status}.";

                return RedirectToAction(
                    "OrderDetails",
                    new { id });
            }

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"Order #{order.OrderId} status updated to {order.OrderStatus}.";

            return RedirectToAction(
                "OrderDetails",
                new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StartDelivery(
            int id)
        {
            if (!IsDeliveryLoggedIn())
                return DeliveryLogin();

            if (!await IsApprovedDeliveryBoy())
                return DeliveryLogin();

            int? userId =
                GetDeliveryUserId();

            if (!userId.HasValue)
                return DeliveryLogin();

            var order =
                await _context.Orders
                    .FirstOrDefaultAsync(o =>
                        o.OrderId == id &&
                        o.DeliveryId ==
                        userId.Value);

            if (order == null)
            {
                TempData["Error"] =
                    "Order not found or it is not assigned to you.";

                return RedirectToAction(
                    "Orders");
            }

            if (string.Equals(
                    order.OrderStatus,
                    "Cancelled",
                    StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] =
                    "Cancelled orders cannot be started.";

                return RedirectToAction(
                    "Orders");
            }

            string currentStatus =
                order.OrderStatus?.Trim() ?? "";

            if (currentStatus.Equals(
                    "Assigned",
                    StringComparison.OrdinalIgnoreCase))
            {
                return await UpdateDeliveryStatus(
                    id,
                    "Picked Up");
            }

            if (currentStatus.Equals(
                    "Picked Up",
                    StringComparison.OrdinalIgnoreCase))
            {
                return await UpdateDeliveryStatus(
                    id,
                    "Out for Delivery");
            }

            TempData["Error"] =
                "The order must be Assigned or Picked Up before starting delivery.";

            return RedirectToAction(
                "OrderDetails",
                new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeliverOrder(
            int id)
        {
            return await UpdateDeliveryStatus(
                id,
                "Delivered");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CollectCOD(
            int id)
        {
            if (!IsDeliveryLoggedIn())
                return DeliveryLogin();

            if (!await IsApprovedDeliveryBoy())
                return DeliveryLogin();

            int? userId =
                GetDeliveryUserId();

            if (!userId.HasValue)
                return DeliveryLogin();

            int deliveryUserId =
                userId.Value;

            var order =
                await _context.Orders
                    .Include(o => o.Customer)
                    .FirstOrDefaultAsync(o =>
                        o.OrderId == id &&
                        o.DeliveryId == deliveryUserId);

            if (order == null)
            {
                TempData["Error"] =
                    "Order not found or it is not assigned to you.";

                return RedirectToAction(
                    "Orders");
            }

            if (string.Equals(
                    order.OrderStatus,
                    "Cancelled",
                    StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] =
                    "COD cannot be collected for a cancelled order.";

                return RedirectToAction(
                    "OrderDetails",
                    new { id });
            }

            if (!IsCashOnDelivery(
                    order.PaymentMethod))
            {
                TempData["Error"] =
                    "This order is not a Cash on Delivery order.";

                return RedirectToAction(
                    "OrderDetails",
                    new { id });
            }

            if (order.DeliveryCollectedAmount > 0)
            {
                TempData["Error"] =
                    "COD payment has already been collected.";

                return RedirectToAction(
                    "OrderDetails",
                    new { id });
            }

            string currentStatus =
                order.OrderStatus?.Trim() ??
                "Pending";

            if (!currentStatus.Equals(
                    "Picked Up",
                    StringComparison.OrdinalIgnoreCase)
                &&
                !currentStatus.Equals(
                    "Out for Delivery",
                    StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] =
                    "COD can only be collected after the order has been picked up.";

                return RedirectToAction(
                    "OrderDetails",
                    new { id });
            }

            decimal collectedAmount =
                order.TotalAmount;

            if (collectedAmount <= 0)
            {
                TempData["Error"] =
                    "The order total is invalid.";

                return RedirectToAction(
                    "OrderDetails",
                    new { id });
            }

            order.DeliveryCollectedAmount =
                collectedAmount;

            order.DeliveryCollectionDate =
                DateTime.Now;

            order.DeliveryCashHandedToAdmin =
                false;

            order.DeliveryCashHandoverDate =
                null;

            order.DeliveryCashHandoverNotes =
                null;

            order.PaymentStatus =
                "Pending";

            order.PaymentReceived =
                false;

            order.PaymentDate =
                null;

            await CreateNotification(
                order.CustomerId,
                "COD Cash Collected",
                $"The delivery boy has collected Rs. {collectedAmount:N2} for Order #{order.OrderId}. Payment is awaiting Admin confirmation.",
                "Payment",
                order.OrderId);

            var admin =
                await GetAdmin();

            if (admin != null)
            {
                await CreateNotification(
                    admin.UserId,
                    "COD Cash Awaiting Handover",
                    $"Delivery boy collected Rs. {collectedAmount:N2} from the customer for Order #{order.OrderId}. The cash has not yet been handed over to Admin.",
                    "Payment",
                    order.OrderId);
            }

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"COD cash of Rs. {collectedAmount:N2} has been recorded. Payment will remain Pending until Admin receives the cash.";

            return RedirectToAction(
                "OrderDetails",
                new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CollectCodPayment(
            int id,
            decimal collectedAmount)
        {
            if (!IsDeliveryLoggedIn())
                return DeliveryLogin();

            if (!await IsApprovedDeliveryBoy())
                return DeliveryLogin();

            int? userId =
                GetDeliveryUserId();

            if (!userId.HasValue)
                return DeliveryLogin();

            int deliveryUserId =
                userId.Value;

            var order =
                await _context.Orders
                    .Include(o => o.Customer)
                    .FirstOrDefaultAsync(o =>
                        o.OrderId == id &&
                        o.DeliveryId == deliveryUserId);

            if (order == null)
            {
                TempData["Error"] =
                    "Order not found or it is not assigned to you.";

                return RedirectToAction(
                    "Orders");
            }

            if (string.Equals(
                    order.OrderStatus,
                    "Cancelled",
                    StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] =
                    "COD cannot be collected for a cancelled order.";

                return RedirectToAction(
                    "OrderDetails",
                    new { id });
            }

            if (!IsCashOnDelivery(
                    order.PaymentMethod))
            {
                TempData["Error"] =
                    "This order is not a Cash on Delivery order.";

                return RedirectToAction(
                    "OrderDetails",
                    new { id });
            }

            if (order.DeliveryCollectedAmount > 0)
            {
                TempData["Error"] =
                    "COD payment has already been collected.";

                return RedirectToAction(
                    "OrderDetails",
                    new { id });
            }

            string currentStatus =
                order.OrderStatus?.Trim() ??
                "Pending";

            if (!currentStatus.Equals(
                    "Picked Up",
                    StringComparison.OrdinalIgnoreCase)
                &&
                !currentStatus.Equals(
                    "Out for Delivery",
                    StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] =
                    "COD can only be collected after pickup.";

                return RedirectToAction(
                    "OrderDetails",
                    new { id });
            }

            if (collectedAmount <= 0)
            {
                TempData["Error"] =
                    "Please enter a valid collected amount.";

                return RedirectToAction(
                    "OrderDetails",
                    new { id });
            }

            if (collectedAmount != order.TotalAmount)
            {
                TempData["Error"] =
                    $"Customer must pay the exact order total of Rs. {order.TotalAmount:N2}.";

                return RedirectToAction(
                    "OrderDetails",
                    new { id });
            }

            order.DeliveryCollectedAmount =
                collectedAmount;

            order.DeliveryCollectionDate =
                DateTime.Now;

            order.DeliveryCashHandedToAdmin =
                false;

            order.DeliveryCashHandoverDate =
                null;

            order.DeliveryCashHandoverNotes =
                null;

            order.PaymentStatus =
                "Pending";

            order.PaymentReceived =
                false;

            order.PaymentDate =
                null;

            await CreateNotification(
                order.CustomerId,
                "COD Cash Collected",
                $"The delivery boy has collected Rs. {collectedAmount:N2} for Order #{order.OrderId}. Payment is awaiting Admin confirmation.",
                "Payment",
                order.OrderId);

            var admin =
                await GetAdmin();

            if (admin != null)
            {
                await CreateNotification(
                    admin.UserId,
                    "COD Cash Awaiting Handover",
                    $"Delivery boy collected Rs. {collectedAmount:N2} for Order #{order.OrderId}. Please confirm when the cash is handed over.",
                    "Payment",
                    order.OrderId);
            }

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"COD cash of Rs. {collectedAmount:N2} has been recorded. Payment remains Pending until Admin confirms receipt.";

            return RedirectToAction(
                "OrderDetails",
                new { id });
        }

        [HttpGet]
        public async Task<IActionResult> Collections()
        {
            if (!IsDeliveryLoggedIn())
                return DeliveryLogin();

            if (!await IsApprovedDeliveryBoy())
                return DeliveryLogin();

            int? userId =
                GetDeliveryUserId();

            if (!userId.HasValue)
                return DeliveryLogin();

            int deliveryUserId =
                userId.Value;

            var orders =
                await _context.Orders
                    .Include(o => o.Customer)
                    .Where(o =>
                        o.DeliveryId == deliveryUserId &&
                        o.OrderStatus != "Cancelled" &&
                        o.DeliveryCollectedAmount > 0)
                    .OrderByDescending(
                        o => o.DeliveryCollectionDate)
                    .ToListAsync();

            var deliveryBoy =
                await GetDeliveryBoy();

            ViewBag.DeliveryName =
                deliveryBoy?.User?.Name ??
                "Delivery Boy";

            ViewBag.AssignedZone =
                string.IsNullOrWhiteSpace(
                    deliveryBoy?.AssignedZone)
                    ? "Not Assigned"
                    : deliveryBoy!.AssignedZone!.Trim();

            ViewBag.CurrentUserId =
                deliveryUserId;

            return View(orders);
        }

       
        [HttpGet]
        public async Task<IActionResult> Payments()
        {
            if (!IsDeliveryLoggedIn())
                return DeliveryLogin();

            if (!await IsApprovedDeliveryBoy())
                return DeliveryLogin();

            int? userId =
                GetDeliveryUserId();

            if (!userId.HasValue)
                return DeliveryLogin();

            int deliveryUserId =
                userId.Value;

            var deliveryBoy =
                await _context.DeliveryBoys
                    .Include(d => d.User)
                    .FirstOrDefaultAsync(d =>
                        d.UserId == deliveryUserId);

            if (deliveryBoy == null)
                return DeliveryLogin();

            ViewBag.DeliveryName =
                deliveryBoy.User?.Name ??
                "Delivery Boy";

            ViewBag.AssignedZone =
                string.IsNullOrWhiteSpace(
                    deliveryBoy.AssignedZone)
                    ? "Not Assigned"
                    : deliveryBoy.AssignedZone.Trim();

            ViewBag.CurrentUserId =
                deliveryUserId;

            ViewBag.PaymentMethod =
                deliveryBoy.PaymentMethod;

            ViewBag.PaymentAccount =
                deliveryBoy.PaymentAccount;

            var orders =
                await _context.Orders
                    .Include(o => o.Customer)
                    .Include(o => o.Shop)
                    .Where(o =>
                        o.DeliveryId == deliveryUserId &&
                        o.OrderStatus != "Cancelled" &&
                        (
                            o.OrderStatus == "Delivered" ||
                            o.OrderStatus == "Completed"
                        ))
                    .OrderByDescending(
                        o => o.DeliveryPaymentDate ??
                             o.CreatedDate)
                    .ToListAsync();

            return View(orders);
        }

        [HttpGet]
        public async Task<IActionResult> PaymentDetails(
            int id)
        {
            if (!IsDeliveryLoggedIn())
                return DeliveryLogin();

            if (!await IsApprovedDeliveryBoy())
                return DeliveryLogin();

            int? userId =
                GetDeliveryUserId();

            if (!userId.HasValue)
                return DeliveryLogin();

            int deliveryUserId =
                userId.Value;

            var order =
                await _context.Orders
                    .Include(o => o.Customer)
                    .Include(o => o.Shop)
                    .FirstOrDefaultAsync(o =>
                        o.OrderId == id &&
                        o.DeliveryId == deliveryUserId &&
                        o.OrderStatus != "Cancelled");

            if (order == null)
            {
                TempData["Error"] =
                    "Payment record not found.";

                return RedirectToAction(
                    "Payments");
            }

            var deliveryBoy =
                await GetDeliveryBoy();

            ViewBag.DeliveryName =
                deliveryBoy?.User?.Name ??
                "Delivery Boy";

            ViewBag.AssignedZone =
                string.IsNullOrWhiteSpace(
                    deliveryBoy?.AssignedZone)
                    ? "Not Assigned"
                    : deliveryBoy!.AssignedZone!.Trim();

            ViewBag.PaymentMethod =
                deliveryBoy?.PaymentMethod;

            ViewBag.PaymentAccount =
                deliveryBoy?.PaymentAccount;

            ViewBag.CurrentUserId =
                deliveryUserId;

            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SavePaymentSettings(
            string paymentMethod,
            string paymentAccount)
        {
            if (!IsDeliveryLoggedIn())
                return DeliveryLogin();

            if (!await IsApprovedDeliveryBoy())
                return DeliveryLogin();

            int? userId =
                GetDeliveryUserId();

            if (!userId.HasValue)
                return DeliveryLogin();

            string method =
                paymentMethod?.Trim() ?? "";

            if (!method.Equals(
                    "Easypaisa",
                    StringComparison.OrdinalIgnoreCase)
                &&
                !method.Equals(
                    "JazzCash",
                    StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] =
                    "Please select either Easypaisa or JazzCash.";

                return RedirectToAction(
                    "Payments");
            }

            method =
                method.Equals(
                    "JazzCash",
                    StringComparison.OrdinalIgnoreCase)
                    ? "JazzCash"
                    : "Easypaisa";

            string account =
                paymentAccount?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(account))
            {
                TempData["Error"] =
                    "Please enter your Easypaisa/JazzCash account number.";

                return RedirectToAction(
                    "Payments");
            }

            if (account.Length < 10 ||
                account.Length > 15)
            {
                TempData["Error"] =
                    "Please enter a valid Easypaisa/JazzCash account number.";

                return RedirectToAction(
                    "Payments");
            }

            var deliveryBoy =
                await _context.DeliveryBoys
                    .FirstOrDefaultAsync(d =>
                        d.UserId == userId.Value);

            if (deliveryBoy == null)
            {
                TempData["Error"] =
                    "Delivery profile not found.";

                return RedirectToAction(
                    "Payments");
            }

            deliveryBoy.PaymentMethod =
                method;

            deliveryBoy.PaymentAccount =
                account;

            await _context.SaveChangesAsync();

            await CreateNotification(
                userId.Value,
                "Payment Settings Updated",
                $"Your delivery payment receiving method has been set to {method}. Admin can use this account for your delivery payouts.",
                "Payment");

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"{method} payment receiving method and account saved successfully.";

            return RedirectToAction(
                "Payments");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetPaymentMethod(
            int id,
            string paymentMethod,
            string paymentAccount)
        {
            if (!IsDeliveryLoggedIn())
                return DeliveryLogin();

            if (!await IsApprovedDeliveryBoy())
                return DeliveryLogin();

            int? userId =
                GetDeliveryUserId();

            if (!userId.HasValue)
                return DeliveryLogin();

            int deliveryUserId =
                userId.Value;

            var order =
                await _context.Orders
                    .FirstOrDefaultAsync(o =>
                        o.OrderId == id &&
                        o.DeliveryId == deliveryUserId);

            if (order == null)
            {
                TempData["Error"] =
                    "Payment record not found.";

                return RedirectToAction(
                    "Payments");
            }

            if (string.Equals(
                    order.OrderStatus,
                    "Cancelled",
                    StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] =
                    "Payment cannot be updated for a cancelled order.";

                return RedirectToAction(
                    "Payments");
            }

            if (string.Equals(
                    order.DeliveryPaymentStatus,
                    "Paid",
                    StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] =
                    "Payment method cannot be changed because Admin has already paid this order.";

                return RedirectToAction(
                    "PaymentDetails",
                    new { id });
            }

            string method =
                paymentMethod?.Trim() ?? "";

            if (!method.Equals(
                    "Easypaisa",
                    StringComparison.OrdinalIgnoreCase)
                &&
                !method.Equals(
                    "JazzCash",
                    StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] =
                    "Delivery payment can only be received through Easypaisa or JazzCash.";

                return RedirectToAction(
                    "PaymentDetails",
                    new { id });
            }

            method =
                method.Equals(
                    "JazzCash",
                    StringComparison.OrdinalIgnoreCase)
                    ? "JazzCash"
                    : "Easypaisa";

            string account =
                paymentAccount?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(account))
            {
                TempData["Error"] =
                    "Please enter the Easypaisa/JazzCash account number.";

                return RedirectToAction(
                    "PaymentDetails",
                    new { id });
            }

            if (account.Length < 10 ||
                account.Length > 15)
            {
                TempData["Error"] =
                    "Please enter a valid Easypaisa/JazzCash account number.";

                return RedirectToAction(
                    "PaymentDetails",
                    new { id });
            }

            var deliveryBoy =
                await _context.DeliveryBoys
                    .FirstOrDefaultAsync(d =>
                        d.UserId == deliveryUserId);

            if (deliveryBoy == null)
            {
                TempData["Error"] =
                    "Delivery profile not found.";

                return RedirectToAction(
                    "Payments");
            }

            deliveryBoy.PaymentMethod =
                method;

            deliveryBoy.PaymentAccount =
                account;

            order.DeliveryPaymentMethod =
                method;

            order.DeliveryPaymentAccount =
                account;

            if (string.IsNullOrWhiteSpace(
                    order.DeliveryPaymentStatus))
            {
                order.DeliveryPaymentStatus =
                    "Pending";
            }

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"{method} payment receiving information has been saved.";

            return RedirectToAction(
                "PaymentDetails",
                new { id });
        }

        [HttpGet]
        public async Task<IActionResult> Notifications()
        {
            if (!IsDeliveryLoggedIn())
                return DeliveryLogin();

            if (!await IsApprovedDeliveryBoy())
                return DeliveryLogin();

            int? userId =
                GetDeliveryUserId();

            if (!userId.HasValue)
                return DeliveryLogin();

            var deliveryBoy =
                await _context.DeliveryBoys
                    .Include(d => d.User)
                    .FirstOrDefaultAsync(d =>
                        d.UserId == userId.Value);

            if (deliveryBoy == null)
                return DeliveryLogin();

            var notifications =
                await _context.Notifications
                    .Where(n =>
                        n.UserId == userId.Value)
                    .OrderByDescending(
                        n => n.CreatedDate)
                    .ToListAsync();

            ViewBag.UnreadNotificationCount =
                notifications.Count(n =>
                    !n.IsRead);

            ViewBag.ReadNotificationCount =
                notifications.Count(n =>
                    n.IsRead);

            ViewBag.TotalNotificationCount =
                notifications.Count;

            ViewBag.DeliveryName =
                deliveryBoy.User?.Name ??
                "Delivery Boy";

            ViewBag.AssignedZone =
                string.IsNullOrWhiteSpace(
                    deliveryBoy.AssignedZone)
                    ? "Not Assigned"
                    : deliveryBoy.AssignedZone.Trim();

            ViewBag.CurrentUserId =
                userId.Value;

            return View(notifications);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkNotificationRead(
            int id)
        {
            if (!IsDeliveryLoggedIn())
                return DeliveryLogin();

            int? userId =
                GetDeliveryUserId();

            if (!userId.HasValue)
                return DeliveryLogin();

            var notification =
                await _context.Notifications
                    .FirstOrDefaultAsync(n =>
                        n.NotificationId == id &&
                        n.UserId == userId.Value);

            if (notification != null)
            {
                notification.IsRead =
                    true;

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(
                "Notifications");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllNotificationsRead()
        {
            if (!IsDeliveryLoggedIn())
                return DeliveryLogin();

            int? userId =
                GetDeliveryUserId();

            if (!userId.HasValue)
                return DeliveryLogin();

            var notifications =
                await _context.Notifications
                    .Where(n =>
                        n.UserId == userId.Value &&
                        !n.IsRead)
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
                "Notifications");
        }

        [HttpGet]
        public async Task<IActionResult> Chat(int? userId)
        {
            if (!IsDeliveryLoggedIn())
                return DeliveryLogin();

            if (!await IsApprovedDeliveryBoy())
                return DeliveryLogin();

            int? currentUserId =
                GetDeliveryUserId();

            if (!currentUserId.HasValue)
                return DeliveryLogin();

            int deliveryUserId =
                currentUserId.Value;

            var deliveryBoy =
                await _context.DeliveryBoys
                    .Include(d => d.User)
                    .FirstOrDefaultAsync(d =>
                        d.UserId == deliveryUserId);

            if (deliveryBoy == null)
                return DeliveryLogin();

            var adminUsers =
                await _context.Users
                    .Include(u => u.Role)
                    .Where(u =>
                        u.UserId != deliveryUserId &&
                        u.Role != null &&
                        u.Role.RoleName == "Admin")
                    .ToListAsync();

            var deliveryOrders =
                await _context.Orders
                    .Where(o =>
                        o.DeliveryId == deliveryUserId &&
                        o.OrderStatus != "Cancelled")
                    .Select(o => new
                    {
                        o.CustomerId,

                        ShopkeeperId =
                            o.Shop != null
                                ? o.Shop.ShopkeeperId
                                : 0
                    })
                    .ToListAsync();

            var customerIds =
                deliveryOrders
                    .Where(x =>
                        x.CustomerId > 0)
                    .Select(x =>
                        x.CustomerId)
                    .Distinct()
                    .ToList();

            var shopkeeperIds =
                deliveryOrders
                    .Where(x =>
                        x.ShopkeeperId > 0)
                    .Select(x =>
                        x.ShopkeeperId)
                    .Distinct()
                    .ToList();

            var customerUsers =
                await _context.Users
                    .Include(u => u.Role)
                    .Where(u =>
                        u.UserId != deliveryUserId &&
                        customerIds.Contains(u.UserId))
                    .ToListAsync();

            var shopkeeperUsers =
                await _context.Users
                    .Include(u => u.Role)
                    .Where(u =>
                        u.UserId != deliveryUserId &&
                        shopkeeperIds.Contains(u.UserId))
                    .ToListAsync();

            var chatUsers =
                adminUsers
                    .Concat(customerUsers)
                    .Concat(shopkeeperUsers)
                    .GroupBy(u => u.UserId)
                    .Select(g => g.First())
                    .OrderBy(u => u.Name)
                    .ToList();

            ViewBag.ChatUsers =
                chatUsers;

            ViewBag.SelectedUserId =
                userId;

            ViewBag.CurrentUserId =
                deliveryUserId;

            ViewBag.DeliveryName =
                deliveryBoy.User?.Name ??
                "Delivery Boy";

            ViewBag.AssignedZone =
                string.IsNullOrWhiteSpace(
                    deliveryBoy.AssignedZone)
                    ? "Not Assigned"
                    : deliveryBoy.AssignedZone.Trim();

            ViewBag.PaymentMethod =
                deliveryBoy.PaymentMethod;

            ViewBag.PaymentAccount =
                deliveryBoy.PaymentAccount;

            var chatUserIds =
                chatUsers
                    .Select(u => u.UserId)
                    .ToList();

            var chatMessages =
                await _context.ChatMessages
                    .Where(m =>
                        (
                            m.SenderId == deliveryUserId &&
                            chatUserIds.Contains(
                                m.ReceiverId)
                        )
                        ||
                        (
                            m.ReceiverId == deliveryUserId &&
                            chatUserIds.Contains(
                                m.SenderId)
                        ))
                    .OrderByDescending(
                        m => m.SentDate)
                    .ToListAsync();

            var conversations =
                chatUsers
                    .Select(u =>
                    {
                        var userMessages =
                            chatMessages
                                .Where(m =>
                                    (
                                        m.SenderId ==
                                        deliveryUserId &&
                                        m.ReceiverId ==
                                        u.UserId
                                    )
                                    ||
                                    (
                                        m.SenderId ==
                                        u.UserId &&
                                        m.ReceiverId ==
                                        deliveryUserId
                                    ))
                                .OrderByDescending(
                                    m => m.SentDate)
                                .ToList();

                        var lastMessage =
                            userMessages.FirstOrDefault();

                        return new
                        {
                            UserId =
                                u.UserId,

                            UserName =
                                u.Name ??
                                "User",

                            RoleName =
                                u.Role?.RoleName ??
                                "User",

                            LastMessage =
                                lastMessage?.Message ??
                                "No messages yet",

                            LastMessageDate =
                                lastMessage?.SentDate ??
                                DateTime.MinValue,

                            UnreadCount =
                                userMessages.Count(m =>
                                    m.ReceiverId ==
                                        deliveryUserId &&
                                    !m.IsRead)
                        };
                    })
                    .OrderByDescending(
                        c => c.LastMessageDate)
                    .ThenBy(
                        c => c.UserName)
                    .ToList();

            ViewBag.Conversations =
                conversations;

            ViewBag.ChatPlaceholder =
                userId.HasValue
                    ? "Type your message..."
                    : "Select a user to start chatting...";

            List<ChatMessage> messages =
                new List<ChatMessage>();

            if (userId.HasValue &&
                userId.Value > 0)
            {
                var selectedUser =
                    chatUsers.FirstOrDefault(u =>
                        u.UserId == userId.Value);

                if (selectedUser == null)
                {
                    TempData["Error"] =
                        "You can only chat with Admin, customers or shopkeepers connected to your deliveries.";

                    return RedirectToAction(
                        "Chat");
                }

                ViewBag.SelectedUserName =
                    selectedUser.Name ??
                    "User";

                ViewBag.SelectedUserRole =
                    selectedUser.Role?.RoleName ??
                    "User";

                ViewBag.ChatPlaceholder =
                    $"Message {selectedUser.Name}...";

                messages =
                    await _context.ChatMessages
                        .Include(m => m.Sender)
                        .Include(m => m.Receiver)
                        .Where(m =>
                            (
                                m.SenderId ==
                                deliveryUserId &&
                                m.ReceiverId ==
                                userId.Value
                            )
                            ||
                            (
                                m.SenderId ==
                                userId.Value &&
                                m.ReceiverId ==
                                deliveryUserId
                            ))
                        .OrderBy(
                            m => m.SentDate)
                        .ToListAsync();

                var unreadMessages =
                    messages
                        .Where(m =>
                            m.ReceiverId ==
                                deliveryUserId &&
                            !m.IsRead)
                        .ToList();

                foreach (var msg in unreadMessages)
                {
                    msg.IsRead =
                        true;
                }

                if (unreadMessages.Any())
                {
                    await _context.SaveChangesAsync();
                }
            }
            else
            {
                ViewBag.SelectedUserName =
                    "";

                ViewBag.SelectedUserRole =
                    "";

                ViewBag.ChatPlaceholder =
                    "Select Admin, customer or shopkeeper...";
            }

            return View(messages);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessage(
            int receiverId,
            string message)
        {
            if (!IsDeliveryLoggedIn())
                return DeliveryLogin();

            if (!await IsApprovedDeliveryBoy())
                return DeliveryLogin();

            int? currentUserId =
                GetDeliveryUserId();

            if (!currentUserId.HasValue)
                return DeliveryLogin();

            int senderId =
                currentUserId.Value;

            if (receiverId <= 0)
            {
                TempData["Error"] =
                    "Please select a user.";

                return RedirectToAction(
                    nameof(Chat));
            }

            if (receiverId == senderId)
            {
                TempData["Error"] =
                    "You cannot send a message to yourself.";

                return RedirectToAction(
                    nameof(Chat));
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                TempData["Error"] =
                    "Message cannot be empty.";

                return RedirectToAction(
                    nameof(Chat),
                    new
                    {
                        userId = receiverId
                    });
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
                    "Receiver not found or disabled.";

                return RedirectToAction(
                    nameof(Chat));
            }

            bool receiverIsAdmin =
                receiver.Role != null &&
                string.Equals(
                    receiver.Role.RoleName,
                    "Admin",
                    StringComparison.OrdinalIgnoreCase);

            if (!receiverIsAdmin)
            {
                bool receiverAllowed =
                    await _context.Orders
                        .Where(o =>
                            o.DeliveryId == senderId &&
                            o.OrderStatus != "Cancelled")
                        .AnyAsync(o =>
                            o.CustomerId == receiverId
                            ||
                            (
                                o.Shop != null &&
                                o.Shop.ShopkeeperId ==
                                    receiverId
                            ));

                if (!receiverAllowed)
                {
                    TempData["Error"] =
                        "You can only chat with Admin, customers or shopkeepers connected to your deliveries.";

                    return RedirectToAction(
                        nameof(Chat));
                }
            }

            string senderName =
                await GetCurrentDeliveryName();

            var chatMessage =
                new ChatMessage
                {
                    SenderId =
                        senderId,

                    ReceiverId =
                        receiverId,

                    Message =
                        message.Trim(),

                    IsRead =
                        false,

                    SentDate =
                        DateTime.Now
                };

            _context.ChatMessages.Add(
                chatMessage);

            string notificationTitle;
            string notificationMessage;

            if (receiverIsAdmin)
            {
                notificationTitle =
                    "New Chat Message from Delivery";

                notificationMessage =
                    $"{senderName} sent you a new chat message.";
            }
            else
            {
                notificationTitle =
                    "New Chat Message";

                notificationMessage =
                    $"You received a new message from {senderName}.";
            }

            await CreateNotification(
                receiverId,
                notificationTitle,
                notificationMessage,
                "Chat");

            await _context.SaveChangesAsync();

            return RedirectToAction(
                nameof(Chat),
                new
                {
                    userId = receiverId
                });
        }
       

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMessage(
            int messageId,
            int receiverId)
        {
            if (!IsDeliveryLoggedIn())
                return DeliveryLogin();

            if (!await IsApprovedDeliveryBoy())
                return DeliveryLogin();

            int? currentUserId = GetDeliveryUserId();

            if (!currentUserId.HasValue)
                return DeliveryLogin();

            int deliveryUserId = currentUserId.Value;

            if (messageId <= 0 || receiverId <= 0)
            {
                TempData["Error"] = "Invalid message.";
                return RedirectToAction(nameof(Chat));
            }

           
            var chatMessage = await _context.ChatMessages
                .FirstOrDefaultAsync(m =>
                    m.ChatMessageId == messageId);

            if (chatMessage == null)
            {
                TempData["Error"] = "Message not found.";

                return RedirectToAction(
                    nameof(Chat),
                    new
                    {
                        userId = receiverId
                    });
            }

          
            if (chatMessage.SenderId != deliveryUserId)
            {
                TempData["Error"] =
                    "You can only delete your own messages.";

                return RedirectToAction(
                    nameof(Chat),
                    new
                    {
                        userId = receiverId
                    });
            }

            
            if (chatMessage.ReceiverId != receiverId)
            {
                TempData["Error"] = "Invalid conversation.";

                return RedirectToAction(
                    nameof(Chat),
                    new
                    {
                        userId = receiverId
                    });
            }

           
            chatMessage.IsDeleted = true;
            chatMessage.Message = "This message was deleted.";

            await _context.SaveChangesAsync();

            return RedirectToAction(
                nameof(Chat),
                new
                {
                    userId = receiverId
                });
        }
       
        [HttpGet]
        public async Task<IActionResult> GetMessages(
            int userId)
        {
            if (!IsDeliveryLoggedIn())
                return Unauthorized();

            if (!await IsApprovedDeliveryBoy())
                return Unauthorized();

            int? currentUserId =
                GetDeliveryUserId();

            if (!currentUserId.HasValue)
                return Unauthorized();

            int deliveryUserId =
                currentUserId.Value;

            if (userId <= 0)
                return BadRequest();

            bool isAdmin =
                await _context.Users
                    .Include(u => u.Role)
                    .AnyAsync(u =>
                        u.UserId == userId &&
                        u.UserId != deliveryUserId &&
                        u.Role != null &&
                        u.Role.RoleName == "Admin");

            bool isConnectedUser =
                await _context.Orders
                    .Where(o =>
                        o.DeliveryId ==
                            deliveryUserId &&
                        o.OrderStatus !=
                            "Cancelled")
                    .AnyAsync(o =>
                        o.CustomerId ==
                            userId
                        ||
                        (
                            o.Shop != null &&
                            o.Shop.ShopkeeperId ==
                                userId
                        ));

            if (!isAdmin &&
                !isConnectedUser)
            {
                return Forbid();
            }

            var unreadMessages =
                await _context.ChatMessages
                    .Where(m =>
                        m.SenderId ==
                            userId &&
                        m.ReceiverId ==
                            deliveryUserId &&
                        !m.IsRead)
                    .ToListAsync();

            foreach (var msg in unreadMessages)
            {
                msg.IsRead =
                    true;
            }

            if (unreadMessages.Any())
            {
                await _context.SaveChangesAsync();
            }

            var messages =
                await _context.ChatMessages
                    .Where(m =>
                        (
                            m.SenderId ==
                                deliveryUserId &&
                            m.ReceiverId ==
                                userId
                        )
                        ||
                        (
                            m.SenderId ==
                                userId &&
                            m.ReceiverId ==
                                deliveryUserId
                        ))
                    .OrderBy(
                        m => m.SentDate)
                    .Select(m => new
                    {
                        m.ChatMessageId,
                        m.SenderId,
                        m.ReceiverId,
                        m.Message,
                        m.IsRead,
                        m.IsDeleted,
                        m.SentDate
                    })
                    .ToListAsync();

            return Json(messages);
        }

        [HttpGet]
        public async Task<IActionResult> UnreadChatCount()
        {
            if (!IsDeliveryLoggedIn())
                return Unauthorized();

            int? userId =
                GetDeliveryUserId();

            if (!userId.HasValue)
                return Unauthorized();

            int count =
                await _context.ChatMessages
                    .CountAsync(m =>
                        m.ReceiverId ==
                            userId.Value &&
                        !m.IsRead);

            return Json(new
            {
                count
            });
        }

        [HttpGet]
        public async Task<IActionResult> UnreadNotificationCount()
        {
            if (!IsDeliveryLoggedIn())
                return Unauthorized();

            int? userId =
                GetDeliveryUserId();

            if (!userId.HasValue)
                return Unauthorized();

            int count =
                await _context.Notifications
                    .CountAsync(n =>
                        n.UserId ==
                            userId.Value &&
                        !n.IsRead);

            return Json(new
            {
                count
            });
        }

        private async Task<User?> GetAdmin()
        {
            return await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u =>
                    u.Role != null &&
                    u.Role.RoleName == "Admin");
        }

        private async Task<string> GetCurrentDeliveryName()
        {
            int? userId =
                GetDeliveryUserId();

            if (!userId.HasValue)
                return "Delivery Boy";

            var user =
                await _context.Users
                    .FirstOrDefaultAsync(u =>
                        u.UserId == userId.Value);

            return user?.Name ??
                   "Delivery Boy";
        }

        private bool IsCashOnDelivery(
            string? paymentMethod)
        {
            if (string.IsNullOrWhiteSpace(
                    paymentMethod))
            {
                return false;
            }

            string method =
                paymentMethod.Trim();

            return method.Equals(
                       "COD",
                       StringComparison.OrdinalIgnoreCase)
                   ||
                   method.Equals(
                       "Cash on Delivery",
                       StringComparison.OrdinalIgnoreCase)
                   ||
                   method.Contains(
                       "Cash",
                       StringComparison.OrdinalIgnoreCase);
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

            bool userExists =
                await _context.Users
                    .AnyAsync(u =>
                        u.UserId == userId);

            if (!userExists)
                return;

            var notification =
                new Notification
                {
                    UserId =
                        userId,

                    Title =
                        string.IsNullOrWhiteSpace(
                            title)
                            ? "Notification"
                            : title.Trim(),

                    Message =
                        string.IsNullOrWhiteSpace(
                            message)
                            ? ""
                            : message.Trim(),

                    Type =
                        string.IsNullOrWhiteSpace(
                            type)
                            ? "General"
                            : type.Trim(),

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
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();

            return RedirectToAction(
                "Login",
                "Account");
        }
    }
}