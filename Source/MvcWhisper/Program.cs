using MvcWhisper.Services;

var builder = WebApplication.CreateBuilder(args);

// Für API-Controller wird mindestens AddControllers() benötigt.
// Wenn Sie ein reines MVC-Projekt haben, steht dort oft AddControllersWithViews(). 
// Beides ist okay, solange es registriert ist:
builder.Services.AddControllersWithViews(); 
builder.Services.AddScoped<TranscriptionService>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

//app.UseHttpsRedirection();
app.UseRouting();

//app.UseAuthorization();

app.MapStaticAssets();


// 1. Für Ihre API-Controller (das aktiviert [Route("api/[controller]")])
app.MapControllers(); 

// 2. Ihre bestehende MVC-Route (beibehalten für Home/Index)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
