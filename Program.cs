using BookingAppV2.Connection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<BookingAppV2.Services.UsersService>();
builder.Services.AddScoped<BookingAppV2.Services.BookingService>();
builder.Services.AddScoped<BookingAppV2.Services.StockService>();
builder.Services.AddScoped<BookingAppV2.Services.ItemService>();
builder.Services.AddScoped<BookingAppV2.Services.DepartmentService>();


builder.Services.AddScoped<BookingAppV2.Helpers.GetRolesUsers>();
builder.Services.AddScoped<BookingAppV2.Helpers.GetUserBookingStatus>();

builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSession();
builder.Services.AddHttpContextAccessor();



// register dbAccess as a service (choose AddSingleton/AddScoped/AddTransient as appropriate)
builder.Services.AddSingleton<dbAccess>();

var app = builder.Build();

var configuration = builder.Configuration;


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseSession();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Login}/{action=Index}/{id?}");

app.Run();
