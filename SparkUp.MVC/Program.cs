using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.EntityFrameworkCore;
using SparkUp.Business;
using SparkUp.MVC.Models;
using SparkUp.MVC.Service;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
// Seed Database

// VnPay service
builder.Services.AddSingleton<IVnPayService, VnPayService>();


//read configuration info
builder.Services.Configure<EmailSenderDto>(
    builder.Configuration.GetSection("EmailSender"));
builder.Services.Configure<SettingsDto>(
    builder.Configuration.GetSection("Settings"));

//add authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
})
    .AddCookie()
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
builder.Services.AddSession(options => {
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.IsEssential = true;
});

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


app.UseSession();
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
