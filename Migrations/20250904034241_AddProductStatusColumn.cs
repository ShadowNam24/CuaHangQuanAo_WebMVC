using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CuaHangQuanAo.Migrations
{
    /// <inheritdoc />
    public partial class AddProductStatusColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Account",
                columns: table => new
                {
                    AccID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    Pass = table.Column<string>(type: "varchar(256)", unicode: false, maxLength: 256, nullable: false),
                    Salt = table.Column<string>(type: "varchar(128)", unicode: false, maxLength: 128, nullable: false),
                    AccRole = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Account__91CBC39834CE7F97", x => x.AccID);
                });

            migrationBuilder.CreateTable(
                name: "Category",
                columns: table => new
                {
                    CategoryID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NameCategory = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_CategoryID", x => x.CategoryID);
                });

            migrationBuilder.CreateTable(
                name: "Supplier",
                columns: table => new
                {
                    SupplierID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SupplierName = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    PhoneNumber = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    AddressContact = table.Column<string>(type: "varchar(70)", unicode: false, maxLength: 70, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_Supplier", x => x.SupplierID);
                });

            migrationBuilder.CreateTable(
                name: "Customer",
                columns: table => new
                {
                    CustomerID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccID = table.Column<int>(type: "int", nullable: true),
                    FirstName = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    LastName = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    PhoneNumber = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: true),
                    AddressName = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: true),
                    City = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_CustomerID", x => x.CustomerID);
                    table.ForeignKey(
                        name: "FK_Customer_Account",
                        column: x => x.AccID,
                        principalTable: "Account",
                        principalColumn: "AccID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Employee",
                columns: table => new
                {
                    EmployeeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccID = table.Column<int>(type: "int", nullable: false),
                    Firstname = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    Lastname = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    Position = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    PhoneNumber = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: true),
                    Address = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_Employee_EmployeeID", x => x.EmployeeID);
                    table.ForeignKey(
                        name: "FK_Employee_Account",
                        column: x => x.AccID,
                        principalTable: "Account",
                        principalColumn: "AccID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PasswordResetToken",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccountId = table.Column<int>(type: "int", nullable: false),
                    Token = table.Column<string>(type: "varchar(256)", unicode: false, maxLength: 256, nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    IsUsed = table.Column<bool>(type: "bit", nullable: true, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Password__3214EC07127BE59B", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshToken_Account",
                        column: x => x.AccountId,
                        principalTable: "Account",
                        principalColumn: "AccID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Items",
                columns: table => new
                {
                    ItemsID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryID = table.Column<int>(type: "int", nullable: false),
                    ItemsName = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    SellPrice = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())"),
                    IsAvailable = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ItemsID", x => x.ItemsID);
                    table.ForeignKey(
                        name: "fk_Items_CategoryID",
                        column: x => x.CategoryID,
                        principalTable: "Category",
                        principalColumn: "CategoryID");
                });

            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    OrdersID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerID = table.Column<int>(type: "int", nullable: true),
                    Total = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    EmployeeID = table.Column<int>(type: "int", nullable: true),
                    OrderDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Discount = table.Column<decimal>(type: "decimal(5,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_OrderID", x => x.OrdersID);
                    table.ForeignKey(
                        name: "fk_Orders_CustomerID",
                        column: x => x.CustomerID,
                        principalTable: "Customer",
                        principalColumn: "CustomerID");
                    table.ForeignKey(
                        name: "fk_Orders_EmployeeID",
                        column: x => x.EmployeeID,
                        principalTable: "Employee",
                        principalColumn: "EmployeeID");
                });

            migrationBuilder.CreateTable(
                name: "ProductVariants",
                columns: table => new
                {
                    ProductVariantsID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductID = table.Column<int>(type: "int", nullable: false),
                    Size = table.Column<string>(type: "varchar(5)", unicode: false, maxLength: 5, nullable: false),
                    Color = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PriceModifier = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    StockQuantity = table.Column<int>(type: "int", nullable: false),
                    Image = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__ProductV__980BBDC77E00FBBE", x => x.ProductVariantsID);
                    table.ForeignKey(
                        name: "FK__ProductVa__Produ__236943A5",
                        column: x => x.ProductID,
                        principalTable: "Items",
                        principalColumn: "ItemsID");
                });

            migrationBuilder.CreateTable(
                name: "OrdersDetails",
                columns: table => new
                {
                    OrdersDetailsID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrdersID = table.Column<int>(type: "int", nullable: true),
                    ItemsID = table.Column<int>(type: "int", nullable: true),
                    Quantity = table.Column<int>(type: "int", nullable: true),
                    Price = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_OrdersDetails", x => x.OrdersDetailsID);
                    table.ForeignKey(
                        name: "fk_ItemsID",
                        column: x => x.ItemsID,
                        principalTable: "Items",
                        principalColumn: "ItemsID");
                    table.ForeignKey(
                        name: "fk_OrdersDetails_OrdersID",
                        column: x => x.OrdersID,
                        principalTable: "Orders",
                        principalColumn: "OrdersID");
                });

            migrationBuilder.CreateTable(
                name: "Storage",
                columns: table => new
                {
                    StorageID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SupplierID = table.Column<int>(type: "int", nullable: true),
                    ProductVariantsID = table.Column<int>(type: "int", nullable: true),
                    Quantity = table.Column<int>(type: "int", nullable: true),
                    ImportCost = table.Column<int>(type: "int", nullable: true),
                    ImportDate = table.Column<DateOnly>(type: "date", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_Storage_Storage", x => x.StorageID);
                    table.ForeignKey(
                        name: "FK__Storage__Supplie__5AEE82B9",
                        column: x => x.SupplierID,
                        principalTable: "Supplier",
                        principalColumn: "SupplierID");
                    table.ForeignKey(
                        name: "fk_Storage_ItemsID",
                        column: x => x.ProductVariantsID,
                        principalTable: "ProductVariants",
                        principalColumn: "ProductVariantsID");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Customer_AccID",
                table: "Customer",
                column: "AccID");

            migrationBuilder.CreateIndex(
                name: "IX_Employee_AccID",
                table: "Employee",
                column: "AccID");

            migrationBuilder.CreateIndex(
                name: "IX_Items_CategoryID",
                table: "Items",
                column: "CategoryID");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CustomerID",
                table: "Orders",
                column: "CustomerID");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_EmployeeID",
                table: "Orders",
                column: "EmployeeID");

            migrationBuilder.CreateIndex(
                name: "IX_OrdersDetails_ItemsID",
                table: "OrdersDetails",
                column: "ItemsID");

            migrationBuilder.CreateIndex(
                name: "IX_OrdersDetails_OrdersID",
                table: "OrdersDetails",
                column: "OrdersID");

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetToken_AccountId",
                table: "PasswordResetToken",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_ProductID",
                table: "ProductVariants",
                column: "ProductID");

            migrationBuilder.CreateIndex(
                name: "IX_Storage_ProductVariantsID",
                table: "Storage",
                column: "ProductVariantsID");

            migrationBuilder.CreateIndex(
                name: "IX_Storage_SupplierID",
                table: "Storage",
                column: "SupplierID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrdersDetails");

            migrationBuilder.DropTable(
                name: "PasswordResetToken");

            migrationBuilder.DropTable(
                name: "Storage");

            migrationBuilder.DropTable(
                name: "Orders");

            migrationBuilder.DropTable(
                name: "Supplier");

            migrationBuilder.DropTable(
                name: "ProductVariants");

            migrationBuilder.DropTable(
                name: "Customer");

            migrationBuilder.DropTable(
                name: "Employee");

            migrationBuilder.DropTable(
                name: "Items");

            migrationBuilder.DropTable(
                name: "Account");

            migrationBuilder.DropTable(
                name: "Category");
        }
    }
}
