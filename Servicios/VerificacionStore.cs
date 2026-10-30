using System.Collections.Concurrent;
using System.Threading;

namespace Tarea5Blazor.Servicios;

public sealed class Solicitud
{
    public string Id { get; } = Guid.NewGuid().ToString("N");
    public string Token { get; } = Guid.NewGuid().ToString("N");
    public string Secreto { get; } = Guid.NewGuid().ToString("N");
    public string Nombre { get; init; } = "";
    public string Email { get; init; } = "";
    public DateTimeOffset Vence { get; } = DateTimeOffset.UtcNow.AddMinutes(20);

    private int _verificada;
    public bool Verificada => Volatile.Read(ref _verificada) == 1;
    public void MarcarVerificada() => Interlocked.Exchange(ref _verificada, 1);
}

public sealed class VerificacionStore
{
    private readonly ConcurrentDictionary<string, Solicitud> _solicitudes = new();
    private readonly ConcurrentDictionary<string, string> _idsPorToken = new();

    public Solicitud Crear(string nombre, string email)
    {
        var solicitud = new Solicitud { Nombre = nombre, Email = email };
        _solicitudes[solicitud.Id] = solicitud;
        _idsPorToken[solicitud.Token] = solicitud.Id;
        return solicitud;
    }

    public Solicitud? Buscar(string? id) =>
        !string.IsNullOrWhiteSpace(id) && _solicitudes.TryGetValue(id, out var s)
            && DateTimeOffset.UtcNow <= s.Vence ? s : null;

    public bool Validar(string? id, string? secreto)
    {
        var solicitud = Buscar(id);
        return solicitud is not null &&
            !string.IsNullOrWhiteSpace(secreto) &&
            solicitud.Secreto == secreto;
    }

    public Solicitud? Confirmar(string? token)
    {
        if (string.IsNullOrWhiteSpace(token) ||
            !_idsPorToken.TryGetValue(token, out var id)) return null;
        var solicitud = Buscar(id);
        solicitud?.MarcarVerificada();
        return solicitud;
    }

    public void Eliminar(string id)
    {
        if (_solicitudes.TryRemove(id, out var solicitud))
            _idsPorToken.TryRemove(solicitud.Token, out _);
    }
}
