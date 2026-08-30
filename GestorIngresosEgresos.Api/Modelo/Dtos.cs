namespace GestorIngresosEgresos.Api.Modelo;

// Contrato HTTP, separado de las entidades. Atar el body directo a la entidad deja que
// el cliente escriba campos que solo decide el servidor (DeudaId, MontoPagado, Estado,
// UsuarioId, GastoId), y devolver la entidad expone columnas internas. Cada request trae
// solo lo que el usuario puede elegir; cada response, solo lo que la vista usa.

// ── Entrada ─────────────────────────────────────────────────────────────
public record LoginRequest(string Username, string Password);
public record RegistroRequest(string Username, string Password, string? Email);
public record CodigoRequest(string Codigo);
public record DesactivarTotpRequest(string Password, string Codigo);
public record PerfilRequest(string? Email);
public record PasswordRequest(string Actual, string Nueva);
public record AbonoRequest(int PeriodoId, int? CategoriaId, decimal Monto, string? Descripcion);

public record IngresoRequest(decimal Monto, DateTime Fecha, string? Descripcion, TipoIngreso Tipo)
{
    // Sin DeudaId: un ingreso ligado a deuda solo lo crea RegistrarPago.
    public Ingreso AEntidad() => new()
    {
        Monto = Monto,
        Fecha = Fecha,
        Descripcion = Descripcion ?? "",
        Tipo = Tipo
    };
}

public record GastoRequest(decimal Monto, DateTime Fecha, string? Descripcion, int? CategoriaId, bool EsSobre)
{
    // Sin DeudaId: si viniera del body, un gasto falso podria apuntar a cualquier deuda
    // y al borrarlo se le restaria monto_pagado a una deuda que nunca abono nada.
    public Gasto AEntidad() => new()
    {
        Monto = Monto,
        Fecha = Fecha,
        Descripcion = Descripcion ?? "",
        CategoriaId = CategoriaId,
        EsSobre = EsSobre
    };
}

public record ConsumoRequest(decimal Monto, DateTime Fecha, string? Descripcion)
{
    // Sin GastoId: en el alta lo pone la ruta y en la edicion se lee de la base.
    public Consumo AEntidad() => new()
    {
        Monto = Monto,
        Fecha = Fecha,
        Descripcion = Descripcion ?? ""
    };
}

public record DeudaRequest(
    TipoDeuda Tipo, string? Nombre, string? Acreedor, decimal MontoOriginal,
    DateTime FechaInicio, DateTime? FechaVencimiento, string? Descripcion)
{
    // Sin MontoPagado ni Estado: los mueve RegistrarPago, no el cliente.
    public Deuda AEntidad() => new()
    {
        Tipo = Tipo,
        Nombre = Nombre ?? "",
        Acreedor = Acreedor ?? "",
        MontoOriginal = MontoOriginal,
        FechaInicio = FechaInicio,
        FechaVencimiento = FechaVencimiento,
        Descripcion = Descripcion ?? ""
    };
}

// ── Salida ──────────────────────────────────────────────────────────────
public record UsuarioDto(int Id, string Username, string? Email, string? Avatar, bool DobleFactor)
{
    // Nunca PasswordHash ni TotpSecret.
    public static UsuarioDto De(Usuario u) => new(u.Id, u.Username, u.Email, u.Avatar, u.TotpActivo);
}

public record PeriodoDto(
    int Id, string Nombre, DateTime FechaInicio, DateTime FechaFin,
    decimal SaldoInicial, EstadoPeriodo Estado, bool EsActual)
{
    public static PeriodoDto De(Periodo p) =>
        new(p.Id, p.Nombre, p.FechaInicio, p.FechaFin, p.SaldoInicial, p.Estado, p.EsActual);
}

public record IngresoDto(
    int Id, int PeriodoId, decimal Monto, DateTime Fecha,
    string Descripcion, TipoIngreso Tipo, bool EsCobro)
{
    public static IngresoDto De(Ingreso i) =>
        new(i.Id, i.PeriodoId, i.Monto, i.Fecha, i.Descripcion, i.Tipo, i.EsCobro);
}

public record GastoDto(
    int Id, int PeriodoId, int? CategoriaId, string CategoriaNombre, decimal Monto,
    DateTime Fecha, string Descripcion, bool EsSobre, bool EsAbono)
{
    public static GastoDto De(Gasto g) =>
        new(g.Id, g.PeriodoId, g.CategoriaId, g.CategoriaNombre, g.Monto, g.Fecha, g.Descripcion, g.EsSobre, g.EsAbono);
}

public record ConsumoDto(int Id, int GastoId, decimal Monto, DateTime Fecha, string Descripcion)
{
    public static ConsumoDto De(Consumo c) => new(c.Id, c.GastoId, c.Monto, c.Fecha, c.Descripcion);
}

public record DeudaDto(
    int Id, TipoDeuda Tipo, string Nombre, string Acreedor, decimal MontoOriginal, decimal MontoPagado,
    DateTime FechaInicio, DateTime? FechaVencimiento, EstadoDeuda Estado, string Descripcion,
    decimal SaldoPendiente, decimal PorcentajePagado)
{
    public static DeudaDto De(Deuda d) =>
        new(d.Id, d.Tipo, d.Nombre, d.Acreedor, d.MontoOriginal, d.MontoPagado, d.FechaInicio,
            d.FechaVencimiento, d.Estado, d.Descripcion, d.SaldoPendiente, d.PorcentajePagado);
}
