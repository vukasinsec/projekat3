using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using TaskManager.Data;
using TaskManager.Hubs;
using TaskManager.Models;
using TaskManager.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

builder.Services.AddSingleton<MongoDbContext>();

builder.Services.AddSingleton<ProjectService>();
builder.Services.AddSingleton<UserService>();
builder.Services.AddSingleton<TaskService>();
builder.Services.AddSingleton<NotificationService>();
builder.Services.AddSingleton<CommentService>();


builder.Services.AddSignalR();
builder.Services.AddSignalR().AddHubOptions<NotificationsHub>(options =>
{
    options.EnableDetailedErrors = true;
});






builder.Services.AddSingleton<IUserIdProvider, CustomUserIdProvider>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/Login";
    });



var app = builder.Build();


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.MapHub<NotificationsHub>("/notificationsHub");

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();


public class CustomUserIdProvider : IUserIdProvider
{
    public string GetUserId(HubConnectionContext connection)
    {
        var sub = connection.User?.FindFirst("sub")?.Value;
        var name = connection.User?.Identity?.Name;
        Console.WriteLine($"SignalR GetUserId called: sub={sub}, name={name}");
        return sub ?? name;
    }
}