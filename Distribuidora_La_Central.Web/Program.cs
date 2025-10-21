using Distribuidora_La_Central.Web.Components;
using Distribuidora_La_Central.Shared.Services;
using Distribuidora_La_Central.Web.Services;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Configuración para API y Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add device-specific services
builder.Services.AddSingleton<IFormFactor, FormFactor>();

// Configurar HttpClient para usar la IP correcta
builder.Services.AddScoped(sp =>
{
    var baseUrl = "http://192.168.56.1:5282";
    return new HttpClient { BaseAddress = new Uri(baseUrl) };
});

builder.Services.AddSingleton<AppState>();

var app = builder.Build();

// Función mejorada para obtener IPs locales
List<string> GetLocalIPAddresses()
{
    var ips = new List<string>();

    try
    {
        // Obtener nombre del host
        var hostName = Dns.GetHostName();
        Console.WriteLine($"🔍 Buscando IPs para: {hostName}");

        // Obtener todas las entradas del host
        var hostEntry = Dns.GetHostEntry(hostName);

        foreach (var ip in hostEntry.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                string ipString = ip.ToString();
                ips.Add(ipString);

                // Mostrar IPs específicas de redes comunes
                if (ipString.StartsWith("192.168.56."))
                {
                    Console.WriteLine($"🎯 IP VirtualBox (Recomendada): {ipString}");
                }
                else if (ipString.StartsWith("192.168.1.") || ipString.StartsWith("192.168.0."))
                {
                    Console.WriteLine($"🏠 IP Red Local: {ipString}");
                }
                else if (ipString.StartsWith("10."))
                {
                    Console.WriteLine($"🏢 IP Red Corporativa: {ipString}");
                }
                else
                {
                    Console.WriteLine($"📡 Otra IP: {ipString}");
                }
            }
        }

        // Si no encontramos IPs, usar métodos alternativos
        if (ips.Count == 0)
        {
            Console.WriteLine("⚠️ No se encontraron IPs IPv4, usando método alternativo...");

            // Método alternativo para Linux/Windows
            try
            {
                using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
                {
                    socket.Connect("8.8.8.8", 65530);
                    if (socket.LocalEndPoint is IPEndPoint endPoint)
                    {
                        ips.Add(endPoint.Address.ToString());
                        Console.WriteLine($"🔧 IP por método alternativo: {endPoint.Address}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error método alternativo: {ex.Message}");
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Error al obtener IPs: {ex.Message}");
    }

    // Siempre incluir localhost
    ips.Add("127.0.0.1");
    return ips;
}

// Obtener todas las IPs
var localIPs = GetLocalIPAddresses();
string localhostUrl = "http://localhost:5282";
var ipUrls = localIPs.Select(ip => $"http://{ip}:5282").ToList();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Distribuidora La Central API V1");
        c.ConfigObject.AdditionalItems["persistAuthorization"] = true;
    });
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapControllers();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(Distribuidora_La_Central.Shared._Imports).Assembly);

// Configurar todas las URLs
app.Urls.Add(localhostUrl);
foreach (var ipUrl in ipUrls)
{
    if (!ipUrl.Contains("127.0.0.1")) // Evitar duplicados
    {
        app.Urls.Add(ipUrl);
    }
}

// Mostrar información de conexión
Console.WriteLine("\n" + new string('=', 50));
Console.WriteLine("🚀 SERVIDOR INICIADO - DISTRIBUIDORA LA CENTRAL");
Console.WriteLine(new string('=', 50));
Console.WriteLine($"📍 Localhost: {localhostUrl}");
Console.WriteLine($"📚 Swagger Local: {localhostUrl}/swagger/index.html");

foreach (var ipUrl in ipUrls)
{
    if (!ipUrl.Contains("127.0.0.1"))
    {
        Console.WriteLine($"🌐 IP Disponible: {ipUrl}");
        Console.WriteLine($"📚 Swagger desde red: {ipUrl}/swagger/index.html");
    }
}

Console.WriteLine("\n🔗 URLs para acceso desde Linux:");
Console.WriteLine($"   http://192.168.56.1:5282/swagger/index.html");
Console.WriteLine($"   http://[cualquier-ip-arriba]:5282/swagger/index.html");
Console.WriteLine(new string('=', 50));
Console.WriteLine("⏹️  Presiona Ctrl+C para detener el servidor");
Console.WriteLine(new string('=', 50) + "\n");

// Intentar abrir navegador automáticamente (solo en Windows)
try
{
    if (OperatingSystem.IsWindows())
    {
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = localhostUrl + "/swagger/index.html",
            UseShellExecute = true
        });
        Console.WriteLine("✅ Navegador abierto automáticamente en Windows");
    }
    else
    {
        Console.WriteLine("💡 En Linux, abre manualmente el navegador con las URLs mostradas arriba");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"⚠️ No se pudo abrir el navegador: {ex.Message}");
}

app.Run();