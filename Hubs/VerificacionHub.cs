using Microsoft.AspNetCore.SignalR;
using Tarea5Blazor.Servicios;

namespace Tarea5Blazor.Hubs;

public sealed class VerificacionHub(VerificacionStore store) : Hub
{
    private void ComprobarAcceso(string id)
    {
        var secreto = Context.GetHttpContext()?.Request.Cookies["Blazor.Pendiente"];
        if (!store.Validar(id, secreto))
            throw new HubException("No tenés acceso a esta solicitud.");
    }

    public Task Unirse(string id)
    {
        ComprobarAcceso(id);
        return Groups.AddToGroupAsync(Context.ConnectionId, $"solicitud-{id}");
    }

    public Task<bool> EstaVerificada(string id)
    {
        ComprobarAcceso(id);
        return Task.FromResult(store.Buscar(id)?.Verificada == true);
    }
}
