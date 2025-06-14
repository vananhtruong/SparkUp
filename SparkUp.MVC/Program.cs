﻿using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.EntityFrameworkCore;
using SparkUp.Business;
using SparkUp.MVC.Models;
using SparkUp.MVC.Service;
using Net.payOS;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
// Seed Database


//read configuration info
builder.Services.Configure<EmailSenderDto>(
    builder.Configuration.GetSection("EmailSender"));
builder.Services.Configure<SettingsDto>(
    builder.Configuration.GetSection("Settings"));

// Register Services
builder.Services.AddTransient<IEmailSender, EmailSender>();
builder.Services.AddTransient<INotificationService, NotificationService>();

// Register PayOsService

var configuration = builder.Configuration;
string clientId = configuration["Environment:PAYOS_CLIENT_ID"] ?? throw new Exception("Missing PAYOS_CLIENT_ID");
string apiKey = configuration["Environment:PAYOS_API_KEY"] ?? throw new Exception("Missing PAYOS_API_KEY");
string checksumKey = configuration["Environment:PAYOS_CHECKSUM_KEY"] ?? throw new Exception("Missing PAYOS_CHECKSUM_KEY");

// --- KH?I T?O VÀ ??NG KÝ payOS VÀO DI CONTAINER ---
PayOS payOSClient = new PayOS(clientId, apiKey, checksumKey);
builder.Services.AddSingleton(payOSClient);





//add authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "CookieAuth";
    options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
})
    .AddCookie("CookieAuth", options =>
    {
        options.LoginPath = "/Authentication/Index";
        options.LogoutPath = "/Authentication/Logout";
        options.ExpireTimeSpan = TimeSpan.FromDays(1);
    })
    .AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
    {
        options.ClientId = builder.Configuration["GoogleKeys:ClientId"];
        options.ClientSecret = builder.Configuration["GoogleKeys:ClientSecret"];
        options.CallbackPath = "/signin-google"; // Standard path
        options.SaveTokens = true; // Save tokens for later use

        // If you need additional scopes:
        options.Scope.Add("profile");
        options.Scope.Add("email");
    })
    .AddFacebook(FacebookDefaults.AuthenticationScheme, options =>
    {
        options.AppId = builder.Configuration["FacebookKeys:AppId"];
        options.AppSecret = builder.Configuration["FacebookKeys:AppSecret"];
        options.CallbackPath = "/Authentication/FacebookResponse";
    });

//additional services
builder.Services.AddScoped<IEmailSender, EmailSender>();

//enable cache
builder.Services.AddMemoryCache();

var app = builder.Build();


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // Tạo một scope để lấy DbContext
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            var context = services.GetRequiredService<AppDbContext>();
            bool canConnect = context.Database.CanConnect();
            Console.WriteLine($"Kết nối Database: {(canConnect ? "Thành công" : "Thất bại")}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Lỗi khi kết nối đến database: {ex.Message}");
        }
    }
}
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

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
