using System.Security.Claims;
using System.Text.Json.Serialization;
using GestorIngresosEgresos.Api.Data;
using GestorIngresosEgresos.Api.Modelo;
using GestorIngresosEgresos.Api.Repository;
using GestorIngresosEgresos.Api.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

if (args.Contains("--selftest"))
{
    return PresupuestoResumen.SelfCheck() ? 0 : 1;
}

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "gie_session";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;
        // API, no redirects: responde 401/403 en vez de mandar a una pagina de login.
        options.Events.OnRedirectToLogin = ctx => { ctx.Response.StatusCode = 401; return Task.CompletedTask; };
        options.Events.OnRedirectToAccessDenied = ctx => { ctx.Response.StatusCode = 403; return Task.CompletedTask; };
    });
builder.Services.AddAuthorization();

builder.Services.AddSingleton<Db>();
builder.Services.AddScoped<UsuarioRepository>();
builder.Services.AddScoped<PeriodoRepository>();
builder.Services.AddScoped<IngresoRepository>();
builder.Services.AddScoped<GastoRepository>();
builder.Services.AddScoped<CategoriaRepository>();
builder.Services.AddScoped<PresupuestoRepository>();
builder.Services.AddScoped<DeudaRepository>();
builder.Services.AddScoped<UsuarioService>();
builder.Services.AddScoped<PeriodoService>();
builder.Services.AddScoped<IngresoService>();
builder.Services.AddScoped<GastoService>();
builder.Services.AddScoped<PresupuestoService>();
builder.Services.AddScoped<DeudaService>();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseExceptionHandler(handler => handler.Run(async ctx =>
{
    var ex = ctx.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
    ctx.Response.StatusCode = ex switch
    {
        ArgumentException => StatusCodes.Status400BadRequest,
        KeyNotFoundException => StatusCodes.Status404NotFound,
        InvalidOperationException => StatusCodes.Status409Conflict,
        _ => StatusCodes.Status500InternalServerError
    };
    await ctx.Response.WriteAsJsonAsync(new { error = ex?.Message ?? "Error inesperado." });
}));

app.UseAuthentication();
app.UseAuthorization();

int UsuarioId(HttpContext ctx) => int.Parse(ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)!);

var api = app.MapGroup("/api");

// ── Auth ────────────────────────────────────────────────────────────────
api.MapPost("/auth/login", async (LoginRequest req, HttpContext ctx, UsuarioService svc) =>
{
    var usuario = svc.Login(req.Username, req.Password);
    if (usuario is null) return Results.Unauthorized();

    var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
        new(ClaimTypes.Name, usuario.Username)
    };
    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    await ctx.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
    return Results.Ok(new { usuario.Id, usuario.Username });
});

api.MapPost("/auth/logout", async (HttpContext ctx) =>
{
    await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Ok();
}).RequireAuthorization();

api.MapGet("/auth/me", (HttpContext ctx) =>
    Results.Ok(new { Id = UsuarioId(ctx), Username = ctx.User.Identity!.Name })
).RequireAuthorization();

// ── Periodos ────────────────────────────────────────────────────────────
var periodos = api.MapGroup("/periodos").RequireAuthorization();

periodos.MapGet("/", (HttpContext ctx, PeriodoService svc) => svc.ObtenerTodos(UsuarioId(ctx)));

periodos.MapGet("/actual", (HttpContext ctx, PeriodoService svc, int? anio, int? mes) =>
{
    var hoy = DateTime.Now;
    var p = svc.ObtenerOCrearPeriodo(UsuarioId(ctx), anio ?? hoy.Year, mes ?? hoy.Month);
    return p is null ? Results.NotFound(new { error = "No hay periodo para ese mes." }) : Results.Ok(p);
});

periodos.MapGet("/{id:int}", (int id, HttpContext ctx, PeriodoService svc) =>
{
    var p = svc.ObtenerPorId(UsuarioId(ctx), id);
    return p is null ? Results.NotFound() : Results.Ok(p);
});

periodos.MapPut("/{id:int}/sueldo", (int id, SueldoRequest req, HttpContext ctx, PeriodoService svc) =>
{
    svc.ActualizarSueldoBase(UsuarioId(ctx), id, req.SueldoBase);
    return Results.NoContent();
});

periodos.MapPost("/{id:int}/cerrar", (int id, HttpContext ctx, PeriodoService svc) =>
{
    svc.CerrarPeriodo(UsuarioId(ctx), id);
    return Results.NoContent();
});

periodos.MapGet("/{id:int}/ingresos", (int id, HttpContext ctx, IngresoService svc) => svc.ObtenerPorPeriodo(UsuarioId(ctx), id));

periodos.MapPost("/{id:int}/ingresos", (int id, Ingreso ing, HttpContext ctx, IngresoService svc) =>
{
    ing.PeriodoId = id;
    return Results.Ok(svc.Guardar(UsuarioId(ctx), ing));
});

periodos.MapGet("/{id:int}/gastos", (int id, HttpContext ctx, GastoService svc) => svc.ObtenerPorPeriodo(UsuarioId(ctx), id));

periodos.MapPost("/{id:int}/gastos", (int id, Gasto g, HttpContext ctx, GastoService svc) =>
{
    g.PeriodoId = id;
    return Results.Ok(svc.Guardar(UsuarioId(ctx), g));
});

periodos.MapGet("/{id:int}/sobres", (int id, HttpContext ctx, PresupuestoService svc) => svc.ObtenerSobres(UsuarioId(ctx), id));

// ── Ingresos ────────────────────────────────────────────────────────────
var ingresos = api.MapGroup("/ingresos").RequireAuthorization();

ingresos.MapPut("/{id:int}", (int id, Ingreso ing, HttpContext ctx, IngresoService svc) =>
{
    ing.Id = id;
    svc.Actualizar(UsuarioId(ctx), ing);
    return Results.NoContent();
});

ingresos.MapDelete("/{id:int}", (int id, HttpContext ctx, IngresoService svc) =>
{
    svc.Eliminar(UsuarioId(ctx), id);
    return Results.NoContent();
});

// ── Gastos ──────────────────────────────────────────────────────────────
var gastos = api.MapGroup("/gastos").RequireAuthorization();

gastos.MapPut("/{id:int}", (int id, Gasto g, HttpContext ctx, GastoService svc) =>
{
    g.Id = id;
    svc.Actualizar(UsuarioId(ctx), g);
    return Results.NoContent();
});

gastos.MapDelete("/{id:int}", (int id, HttpContext ctx, GastoService svc) =>
{
    svc.Eliminar(UsuarioId(ctx), id);
    return Results.NoContent();
});

gastos.MapGet("/{id:int}/resumen", (int id, HttpContext ctx, PresupuestoService svc) =>
{
    var r = svc.ObtenerResumen(UsuarioId(ctx), id);
    return r is null ? Results.NotFound() : Results.Ok(r);
});

gastos.MapGet("/{id:int}/consumos", (int id, HttpContext ctx, PresupuestoService svc) => svc.ObtenerConsumos(UsuarioId(ctx), id));

gastos.MapPost("/{id:int}/consumos", (int id, Consumo c, HttpContext ctx, PresupuestoService svc) =>
{
    c.GastoId = id;
    return Results.Ok(svc.Guardar(UsuarioId(ctx), c));
});

// ── Consumos ────────────────────────────────────────────────────────────
var consumos = api.MapGroup("/consumos").RequireAuthorization();

consumos.MapPut("/{id:int}", (int id, Consumo c, HttpContext ctx, PresupuestoService svc) =>
{
    c.Id = id;
    return Results.Ok(svc.Actualizar(UsuarioId(ctx), c));
});

consumos.MapDelete("/{id:int}", (int id, HttpContext ctx, PresupuestoService svc) =>
{
    svc.Eliminar(UsuarioId(ctx), id);
    return Results.NoContent();
});

// ── Categorias ──────────────────────────────────────────────────────────
api.MapGet("/categorias", (GastoService svc) => svc.ObtenerCategorias()).RequireAuthorization();

// ── Deudas ──────────────────────────────────────────────────────────────
var deudas = api.MapGroup("/deudas").RequireAuthorization();

deudas.MapGet("/", (HttpContext ctx, DeudaService svc) => svc.ObtenerTodas(UsuarioId(ctx)));
deudas.MapGet("/activas", (HttpContext ctx, DeudaService svc) => svc.ObtenerActivas(UsuarioId(ctx)));
deudas.MapGet("/total-pendiente", (HttpContext ctx, DeudaService svc) => new { total = svc.TotalPendiente(UsuarioId(ctx)) });

deudas.MapPost("/", (Deuda d, HttpContext ctx, DeudaService svc) => Results.Ok(svc.Guardar(UsuarioId(ctx), d)));

deudas.MapDelete("/{id:int}", (int id, HttpContext ctx, DeudaService svc) =>
{
    svc.Eliminar(UsuarioId(ctx), id);
    return Results.NoContent();
});

deudas.MapGet("/{id:int}/abonos", (int id, HttpContext ctx, GastoService svc) => svc.ObtenerAbonosPorDeuda(UsuarioId(ctx), id));

deudas.MapPost("/{id:int}/abonos", (int id, AbonoRequest req, HttpContext ctx, DeudaService svc) =>
    Results.Ok(svc.RegistrarAbono(UsuarioId(ctx), id, req.PeriodoId, req.CategoriaId, req.Monto, req.Descripcion)));

app.Run();
return 0;

record LoginRequest(string Username, string Password);
record SueldoRequest(decimal SueldoBase);
record AbonoRequest(int PeriodoId, int? CategoriaId, decimal Monto, string? Descripcion);
