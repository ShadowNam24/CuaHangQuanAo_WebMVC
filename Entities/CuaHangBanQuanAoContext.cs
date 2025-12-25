using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace CuaHangQuanAo.Entities;

public partial class CuaHangBanQuanAoContext : DbContext
{
    public CuaHangBanQuanAoContext()
    {
    }

    public CuaHangBanQuanAoContext(DbContextOptions<CuaHangBanQuanAoContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Account> Accounts { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Customer> Customers { get; set; }

    public virtual DbSet<Employee> Employees { get; set; }

    public virtual DbSet<Item> Items { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrdersDetail> OrdersDetails { get; set; }

    public virtual DbSet<PasswordResetToken> PasswordResetTokens { get; set; }

    public virtual DbSet<ProductVariant> ProductVariants { get; set; }

    public virtual DbSet<Storage> Storages { get; set; }

    public virtual DbSet<Supplier> Suppliers { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlServer("Name=ChuoiKetNoi");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasKey(e => e.AccId).HasName("PK__Account__91CBC39834CE7F97");

            entity.ToTable("Account");

            entity.Property(e => e.AccId).HasColumnName("AccID");
            entity.Property(e => e.AccRole)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Pass)
                .HasMaxLength(256)
                .IsUnicode(false);
            entity.Property(e => e.Salt)
                .HasMaxLength(128)
                .IsUnicode(false);
            entity.Property(e => e.Username)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("pk_CategoryID");

            entity.ToTable("Category");

            entity.Property(e => e.CategoryId).HasColumnName("CategoryID");
            entity.Property(e => e.NameCategory)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.CustomerId).HasName("pk_CustomerID");

            entity.ToTable("Customer");

            entity.Property(e => e.CustomerId).HasColumnName("CustomerID");
            entity.Property(e => e.AccId).HasColumnName("AccID");
            entity.Property(e => e.AddressName)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.City)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.FirstName)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.LastName)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(10)
                .IsUnicode(false);

            entity.HasOne(d => d.Acc).WithMany(p => p.Customers)
                .HasForeignKey(d => d.AccId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_Customer_Account");
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.EmployeeId).HasName("pk_Employee_EmployeeID");

            entity.ToTable("Employee");

            entity.Property(e => e.EmployeeId).HasColumnName("EmployeeID");
            entity.Property(e => e.AccId).HasColumnName("AccID");
            entity.Property(e => e.Address)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.Firstname)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Lastname)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.Position)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.Acc).WithMany(p => p.Employees)
                .HasForeignKey(d => d.AccId)
                .HasConstraintName("FK_Employee_Account");
        });

        modelBuilder.Entity<Item>(entity =>
        {
            entity.HasKey(e => e.ItemsId).HasName("pk_ItemsID");

            entity.Property(e => e.ItemsId).HasColumnName("ItemsID");
            entity.Property(e => e.CategoryId).HasColumnName("CategoryID");
            entity.Property(e => e.CoverImage).HasMaxLength(200);
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.IsAvailable).HasDefaultValue(true);
            entity.Property(e => e.ItemsName)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Status).HasDefaultValue(1);

            entity.HasOne(d => d.Category).WithMany(p => p.Items)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_Items_CategoryID");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.OrdersId).HasName("pk_OrderID");

            entity.ToTable(tb => tb.HasTrigger("TRG_HoanKhoKhiHuyDonHang"));

            entity.Property(e => e.OrdersId).HasColumnName("OrdersID");
            entity.Property(e => e.CustomerId).HasColumnName("CustomerID");
            entity.Property(e => e.CustomerName).HasMaxLength(100);
            entity.Property(e => e.Discount).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.DiscountAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.DiscountCode).HasMaxLength(50);
            entity.Property(e => e.DiscountDescription).HasMaxLength(200);
            entity.Property(e => e.EmployeeId).HasColumnName("EmployeeID");
            entity.Property(e => e.PaymentMethod).HasMaxLength(50);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.ShippingAddress).HasMaxLength(500);
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.Total).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TransactionId)
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.HasOne(d => d.Customer).WithMany(p => p.Orders)
                .HasForeignKey(d => d.CustomerId)
                .HasConstraintName("fk_Orders_CustomerID");

            entity.HasOne(d => d.Employee).WithMany(p => p.Orders)
                .HasForeignKey(d => d.EmployeeId)
                .HasConstraintName("fk_Orders_EmployeeID");
        });

        modelBuilder.Entity<OrdersDetail>(entity =>
        {
            entity.HasKey(e => e.OrdersDetailsId).HasName("pk_OrdersDetails");

            entity.ToTable(tb => tb.HasTrigger("TRG_CapNhatKhoHang"));

            entity.Property(e => e.OrdersDetailsId).HasColumnName("OrdersDetailsID");
            entity.Property(e => e.Color).HasMaxLength(50);
            entity.Property(e => e.OrdersId).HasColumnName("OrdersID");
            entity.Property(e => e.ProductVariantId).HasColumnName("ProductVariantID");
            entity.Property(e => e.Size)
                .HasMaxLength(10)
                .IsUnicode(false);

            entity.HasOne(d => d.Orders).WithMany(p => p.OrdersDetails)
                .HasForeignKey(d => d.OrdersId)
                .HasConstraintName("fk_OrdersDetails_OrdersID");

            entity.HasOne(d => d.ProductVariant).WithMany(p => p.OrdersDetails)
                .HasForeignKey(d => d.ProductVariantId)
                .HasConstraintName("fk_ItemsID");
        });

        modelBuilder.Entity<PasswordResetToken>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Password__3214EC07508B5AEE");

            entity.ToTable("PasswordResetToken");

            entity.Property(e => e.ExpiryDate).HasColumnType("datetime");
            entity.Property(e => e.IsUsed).HasDefaultValue(false);
            entity.Property(e => e.Token)
                .HasMaxLength(256)
                .IsUnicode(false);

            entity.HasOne(d => d.Account).WithMany(p => p.PasswordResetTokens)
                .HasForeignKey(d => d.AccountId)
                .HasConstraintName("FK_RefreshToken_Account");
        });

        modelBuilder.Entity<ProductVariant>(entity =>
        {
            entity.HasKey(e => e.ProductVariantsId).HasName("PK__ProductV__980BBDC77E00FBBE");

            entity.Property(e => e.ProductVariantsId).HasColumnName("ProductVariantsID");
            entity.Property(e => e.Color).HasMaxLength(20);
            entity.Property(e => e.Image).HasMaxLength(100);
            entity.Property(e => e.PriceModifier).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.Size)
                .HasMaxLength(5)
                .IsUnicode(false);

            entity.HasOne(d => d.Product).WithMany(p => p.ProductVariants)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ProductVa__Produ__236943A5");
        });

        modelBuilder.Entity<Storage>(entity =>
        {
            entity.HasKey(e => e.StorageId).HasName("pk_Storage_Storage");

            entity.ToTable("Storage");

            entity.Property(e => e.StorageId).HasColumnName("StorageID");
            entity.Property(e => e.ImportDate).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.ProductVariantsId).HasColumnName("ProductVariantsID");
            entity.Property(e => e.SupplierId).HasColumnName("SupplierID");

            entity.HasOne(d => d.ProductVariants).WithMany(p => p.Storages)
                .HasForeignKey(d => d.ProductVariantsId)
                .HasConstraintName("fk_Storage_ItemsID");

            entity.HasOne(d => d.Supplier).WithMany(p => p.Storages)
                .HasForeignKey(d => d.SupplierId)
                .HasConstraintName("FK__Storage__Supplie__628FA481");
        });

        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.HasKey(e => e.SupplierId).HasName("pk_Supplier");

            entity.ToTable("Supplier");

            entity.Property(e => e.SupplierId).HasColumnName("SupplierID");
            entity.Property(e => e.AddressContact)
                .HasMaxLength(70)
                .IsUnicode(false);
            entity.Property(e => e.Email)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.SupplierName)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
