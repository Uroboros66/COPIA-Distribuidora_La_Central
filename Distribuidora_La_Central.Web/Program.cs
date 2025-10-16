using Distribuidora_La_Central.Web.Components;
using Distribuidora_La_Central.Shared.Services;
using Distribuidora_La_Central.Web.Services;
using System.Net.NetworkInformation;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Configuración para API y Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();  // 
builder.Services.AddSwaggerGen();

// Add device-specific services
builder.Services.AddSingleton<IFormFactor, FormFactor>();

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("http://localhost:5282") });


builder.Services.AddSingleton<AppState>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())  // 
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapControllers();  // Mapeo de API controllers

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(Distribuidora_La_Central.Shared._Imports).Assembly);

app.Run();