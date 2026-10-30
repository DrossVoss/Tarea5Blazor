# Tarea Blazor 04 — Consigna 3

Implementación educativa de registro con verificación simulada mediante **Blazor Web App (.NET 8, Interactive Server)**, **Radzen** y **SignalR**.

## Crear la base

```powershell
cd "$HOME\Desktop"
mkdir TareaBlazor04
cd TareaBlazor04
dotnet new blazor -n Tarea5Blazor -f net8.0 -int Server -ai
cd Tarea5Blazor
dotnet add package Radzen.Blazor
dotnet tool install --global Microsoft.Web.LibraryManager.Cli
libman install @microsoft/signalr@8.0.0 -p unpkg -d wwwroot/lib/signalr --files dist/browser/signalr.min.js
```

Extraer el ZIP de archivos de la solución sobre esta carpeta (`Tarea5Blazor`), **no sobre la carpeta padre**, conservando los otros archivos del template Blazor.

```powershell
dotnet restore
dotnet build
dotnet run --urls http://localhost:5074
```

Abrir `http://localhost:5074/registro`, ingresar nombre y correo, abrir el enlace de verificación en una segunda pestaña. La primera recibe el evento SignalR y realiza el inicio de sesión y la redirección automáticamente.

## Arquitectura

- `Components/Pages/Registro.razor`: formulario Radzen.
- `Components/Pages/Espera.razor`: interfaz Blazor, recepción de evento vía JS interop.
- `Hubs/VerificacionHub.cs`: grupo de SignalR para una solicitud específica.
- `Servicios/VerificacionStore.cs`: almacenamiento temporal en memoria y token.
- `Program.cs`: DI, endpoints HTTP, cookies y Hub.
- `wwwroot/js/verificacion.js`: cliente SignalR JavaScript y solicitudes HTTP.
- `Components/Pages/Home.razor`: página de inicio según autenticación.

## Límites de la demostración

No se envían emails ni se verifica que el estudiante sea propietario del email; no existe DB ni manejo de contraseñas y el almacenamiento se pierde al reiniciar. Se muestra el enlace de confirmación en la propia pantalla y la confirmación usa GET solo para simular un clic. Debe usarse HTTPS y protección CSRF adecuada, enlaces de un solo uso, expiración/limpieza de solicitudes y ASP.NET Core Identity o sistema de autenticación robusto antes de usar en producción.

La aplicación utiliza una conexión SignalR para la interactividad de Blazor y otra para el Hub didáctico; ambas cumplen roles distintos.
