using Microsoft.EntityFrameworkCore;

namespace WeddingClosetHubs.Models
{
    public class WeddingClosetHubsContext : DbContext
    {
        public WeddingClosetHubsContext(
            DbContextOptions<WeddingClosetHubsContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }

        public DbSet<Role> Roles { get; set; }

        public DbSet<Shop> Shops { get; set; }

        public DbSet<Product> Products { get; set; }

        public DbSet<Order> Orders { get; set; }

        public DbSet<OrderDetail> OrderDetails { get; set; }

        public DbSet<DeliveryBoy> DeliveryBoys { get; set; }

        public DbSet<Payment> Payments { get; set; }

        public DbSet<ChatMessage> ChatMessages { get; set; }

        public DbSet<Notification> Notifications { get; set; }

        public DbSet<Review> Reviews { get; set; }
        public virtual DbSet<Negotiation> Negotiations { get; set; }


        protected override void OnModelCreating(
            ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);


         
            modelBuilder.Entity<Role>().HasData(

                new Role
                {
                    RoleId = 1,
                    RoleName = "Admin"
                },

                new Role
                {
                    RoleId = 2,
                    RoleName = "Shopkeeper"
                },

                new Role
                {
                    RoleId = 3,
                    RoleName = "Customer"
                },

                new Role
                {
                    RoleId = 4,
                    RoleName = "Delivery"
                }
            );


            modelBuilder.Entity<Order>()
                .HasOne(o => o.Customer)
                .WithMany()
                .HasForeignKey(o => o.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);


            modelBuilder.Entity<Order>()
                .HasOne(o => o.Shop)
                .WithMany()
                .HasForeignKey(o => o.ShopId)
                .OnDelete(DeleteBehavior.Restrict);


            modelBuilder.Entity<Order>()
                .HasOne(o => o.Delivery)
                .WithMany()
                .HasForeignKey(o => o.DeliveryId)
                .OnDelete(DeleteBehavior.SetNull);


            modelBuilder.Entity<OrderDetail>()
                .HasOne(od => od.Order)
                .WithMany(o => o.OrderDetails)
                .HasForeignKey(od => od.OrderId)
                .OnDelete(DeleteBehavior.Cascade);


            modelBuilder.Entity<OrderDetail>()
                .HasOne(od => od.Product)
                .WithMany()
                .HasForeignKey(od => od.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            
             modelBuilder.Entity<Shop>()
                 .HasOne(s => s.Shopkeeper)
                 .WithOne(u => u.Shop)
                 .HasForeignKey<Shop>(s => s.ShopkeeperId)
                 .OnDelete(DeleteBehavior.Cascade);


           modelBuilder.Entity<DeliveryBoy>()
               .HasOne(d => d.User)
               .WithOne(u => u.DeliveryBoy)
               .HasForeignKey<DeliveryBoy>(d => d.UserId)
               .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Payment>()
              .HasOne(p => p.Order)
              .WithMany()
              .HasForeignKey(p => p.OrderId)
              .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Payment>()
              .HasOne(p => p.Customer)
              .WithMany()
              .HasForeignKey(p => p.CustomerId)
              .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Payment>()
               .HasOne(p => p.Shop)
               .WithMany()
               .HasForeignKey(p => p.ShopId)
               .OnDelete(DeleteBehavior.Restrict); 

            modelBuilder.Entity<ChatMessage>()
                .HasOne(c => c.Sender)
                .WithMany()
                .HasForeignKey(c => c.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ChatMessage>()
                .HasOne(c => c.Receiver)
                .WithMany()
                .HasForeignKey(c => c.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Review>()
                 .HasOne(r => r.Product)
                 .WithMany()
                 .HasForeignKey(r => r.ProductId)
                 .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Review>()
                .HasOne(r => r.Shop)
                .WithMany()
                .HasForeignKey(r => r.ShopId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Review>()
                .HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.NoAction);
                       
            modelBuilder.Entity<Negotiation>()
                .HasOne(n => n.Product)
                .WithMany()
                .HasForeignKey(n => n.ProductId)
                .OnDelete(DeleteBehavior.NoAction);


            modelBuilder.Entity<Negotiation>()
                .HasOne(n => n.Customer)
                .WithMany()
                .HasForeignKey(n => n.CustomerId)
                .OnDelete(DeleteBehavior.NoAction);


            modelBuilder.Entity<Negotiation>()
                .HasOne(n => n.Shopkeeper)
                .WithMany()
                .HasForeignKey(n => n.ShopkeeperId)
                .OnDelete(DeleteBehavior.NoAction);


            modelBuilder.Entity<Negotiation>()
                .Property(n => n.OriginalPrice)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Negotiation>()
                .Property(n => n.RequestedPrice)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Negotiation>()
                .Property(n => n.CounterPrice)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Negotiation>()
                .Property(n => n.AgreedPrice)
                .HasPrecision(18, 2);


            modelBuilder.Entity<Product>()
                .Property(p => p.SalePrice)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Product>()
                .Property(p => p.RentPrice)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Product>()
                .Property(p => p.RentalSecurity)
                .HasPrecision(18, 2);

            modelBuilder.Entity<OrderDetail>()
                .Property(od => od.RentalSecurity)
                .HasPrecision(18, 2);
        }
    }
}