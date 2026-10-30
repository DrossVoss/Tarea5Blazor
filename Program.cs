using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.SignalR;
using Radzen;
using Tarea5Blazor.Components;
using Tarea5Blazor.Hubs;
using Tarea5Blazor.Servicios;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddRadzenComponents();
builder.Services.AddSignalR();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddSingleton<VerificacionStore>();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(opciones =>
    {
        opciones.LoginPath = "/registro";
        opciones.Cookie.Name = "Blazor.Sesion";
        opciones.Cookie.SameSite = SameSiteMode.Strict;
    });
builder.Services.AddAuthorization();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapPost("/api/registro", (RegistroDatos datos, HttpContext http,
    VerificacionStore store) =>
{
    if (string.IsNullOrWhiteSpace(datos.Nombre) ||
        datos.Nombre.Length > 80 ||
        string.IsNullOrWhiteSpace(datos.Email) ||
        !new EmailAddressAttribute().IsValid(datos.Email))
        return Results.BadRequest(new { mensaje = "Ingresá nombre y email válidos." });

    var solicitud = store.Crear(datos.Nombre.Trim(), datos.Email.Trim());
    http.Response.Cookies.Append("Blazor.Pendiente", solicitud.Secreto,
        new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Strict,
            Secure = http.Request.IsHttps,
            MaxAge = TimeSpan.FromMinutes(20)
        });
    return Results.Ok(new { id = solicitud.Id });
});

app.MapGet("/confirmar", async (string token, VerificacionStore store,
    IHubContext<VerificacionHub> hub) =>
{
    var solicitud = store.Confirmar(token);
    if (solicitud is null)
        return Results.BadRequest("Enlace inválido o vencido.");

    await hub.Clients.Group($"solicitud-{solicitud.Id}")
        .SendAsync("VerificacionCompletada");
    return Results.Text("Correo verificado. Revisá la pestaña de espera.");
});

app.MapPost("/api/finalizar", async (FinalizarDatos datos, HttpContext http,
    VerificacionStore store) =>
{
    var secreto = http.Request.Cookies["Blazor.Pendiente"];
    if (!store.Validar(datos.Id, secreto))
        return Results.Unauthorized();

    var solicitud = store.Buscar(datos.Id)!;
    if (!solicitud.Verificada)
        return Results.BadRequest(new { mensaje = "Todavía falta verificar el correo." });

    var identidad = new ClaimsIdentity(new[]
    {
        new Claim(ClaimTypes.Name, solicitud.Nombre),
        new Claim(ClaimTypes.Email, solicitud.Email)
    }, CookieAuthenticationDefaults.AuthenticationScheme);

    await http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
        new ClaimsPrincipal(identidad));

    http.Response.Cookies.Delete("Blazor.Pendiente");
    store.Eliminar(datos.Id);
    return Results.Ok();
});

app.MapPost("/api/salir", async (HttpContext http) =>
{
    await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Ok();
});

app.MapHub<VerificacionHub>("/verificacionHub");
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();

public sealed record RegistroDatos(string Nombre, string Email);
public sealed record FinalizarDatos(string Id);
