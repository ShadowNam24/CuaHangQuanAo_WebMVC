using CuaHangQuanAo.DesignPatterns;
using CuaHangQuanAo.Entities;
using CuaHangQuanAo.Factory;
using CuaHangQuanAo.Models;
using CuaHangQuanAo.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<CuaHangBanQuanAoContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ChuoiKetNoi")));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(1);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    });

builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("EmployeeOnly", policy => policy.RequireRole("Employee"));
        options.AddPolicy("CustomerOnly", policy => policy.RequireRole("Customer"));
        options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    });

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IProductFactoryProvider, ProductFactoryProvider>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IStorageFactoryProvider, StorageFactoryProvider>();
builder.Services.AddScoped<IStorageService, StorageService>();

// Add session support (optional)
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddTransient<IEmailService, EmailService>();

builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();//
builder.Services.AddScoped<CartService>();
builder.Services.AddSession();

var app = builder.Build();
app.UseSession(); //

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");


app.Run();