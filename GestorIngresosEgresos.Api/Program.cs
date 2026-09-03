using System.Security.Claims;
using System.Text.Json.Serialization;
using GestorIngresosEgresos.Api.Data;
using GestorIngresosEgresos.Api.Modelo;
using GestorIngresosEgresos.Api.Repository;
using GestorIngresosEgresos.Api.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;

if (args.Contains("--selftest"))
{
    return PresupuestoResumen.SelfCheck() & Periodo.SelfCheck() & Deuda.SelfCheck() & TotpService.SelfCheck() ? 0 : 1;
}

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "gie_session";
        options.Cookie.HttpOnly = true;
        // Lax evita que un sitio externo dispare POST/PUT/DELETE con la cookie del
        // usuario: es lo que protege de CSRF a los endpoints que mutan datos.
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;
        // API, no redirects: responde 401/403 en vez de mandar a una pagina de login.
        options.Events.OnRedirectToLogin = ctx => { ctx.Response.StatusCode = 401; return Task.CompletedTask; };
        options.Events.OnRedirectToAccessDenied = ctx => { ctx.Response.StatusCode = 403; return Task.CompletedTask; };
    });

// Entre la contraseña y el segundo factor la sesion existe pero esta a medias: lleva
// la marca "2fa_pendiente" y esta politica la rechaza en todo lo que no sea el paso 2.
const string Pendiente2fa = "2fa_pendiente";
builder.Services.AddAuthorizationBuilder()
    .SetDefaultPolicy(new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .RequireAssertion(ctx => !ctx.User.HasClaim(Pendiente2fa, "1"))
        .Build());

builder.Services.AddSingleton<Db>();
builder.Services.AddSingleton<AvatarService>();
builder.Services.AddSingleton<TotpService>();
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

// Detras de "tailscale serve" la app recibe HTTP plano; sin esto no se entera de que
// el usuario entro por HTTPS y la cookie de sesion sale sin el flag Secure.
var forwarded = new ForwardedHeadersOptions { ForwardedHeaders = ForwardedHeaders.XForwardedProto };
// El proxy corre en el host y entra por el puerto publicado, asi que la peticion llega
// desde el gateway del bridge de Docker, no desde loopback: con la lista por defecto
// (solo loopback) la cabecera se descartaria. Falsificarla solo consigue que el propio
// cliente reciba su cookie marcada como Secure, no afecta a nadie mas.
forwarded.KnownNetworks.Clear();
forwarded.KnownProxies.Clear();
app.UseForwardedHeaders(forwarded);

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

async Task IniciarSesion(HttpContext ctx, Usuario usuario, bool pendiente2fa)
{
    var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
        new(ClaimTypes.Name, usuario.Username)
    };
    if (pendiente2fa) claims.Add(new Claim(Pendiente2fa, "1"));

    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    // La sesion a medias caduca pronto: es solo para completar el segundo paso.
    var props = pendiente2fa
        ? new AuthenticationProperties { IsPersistent = false, ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(5) }
        : new AuthenticationProperties { IsPersistent = true };

    await ctx.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity), props);
}

var api = app.MapGroup("/api");

// ── Auth ────────────────────────────────────────────────────────────────
api.MapPost("/auth/login", async (LoginRequest req, HttpContext ctx, UsuarioService svc) =>
{
    var usuario = svc.VerificarCredenciales(req.Username, req.Password);
    if (usuario is null) return Results.Unauthorized();

    await IniciarSesion(ctx, usuario, pendiente2fa: usuario.TotpActivo);
    return usuario.TotpActivo
        ? Results.Ok(new { requiere2fa = true })
        : Results.Ok(UsuarioDto.De(usuario));
});

// Paso 2 del login: la cookie a medias identifica al usuario, falta el codigo.
api.MapPost("/auth/login/2fa", async (CodigoRequest req, HttpContext ctx, UsuarioService svc) =>
{
    if (ctx.User.FindFirstValue(ClaimTypes.NameIdentifier) is not string id)
        return Results.Unauthorized();

    var usuario = svc.Obtener(int.Parse(id));
    if (!svc.VerificarCodigoTotp(usuario, req.Codigo))
        return Results.Json(new { error = "Codigo incorrecto." }, statusCode: 401);

    await IniciarSesion(ctx, usuario, pendiente2fa: false);
    return Results.Ok(UsuarioDto.De(usuario));
}).RequireAuthorization(p => p.RequireAuthenticatedUser());

api.MapPost("/auth/registro", async (RegistroRequest req, HttpContext ctx, UsuarioService svc) =>
{
    var usuario = svc.Registrar(req.Username, req.Password, req.Email);
    await IniciarSesion(ctx, usuario, pendiente2fa: false);
    return Results.Ok(UsuarioDto.De(usuario));
});

api.MapPost("/auth/logout", async (HttpContext ctx) =>
{
    await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Ok();
}).RequireAuthorization(p => p.RequireAuthenticatedUser());

api.MapGet("/auth/me", (HttpContext ctx, UsuarioService svc) =>
    Results.Ok(UsuarioDto.De(svc.Obtener(UsuarioId(ctx))))
).RequireAuthorization();

// ── Perfil ──────────────────────────────────────────────────────────────
var perfil = api.MapGroup("/perfil").RequireAuthorization();

perfil.MapGet("/", (HttpContext ctx, UsuarioService svc) => Results.Ok(UsuarioDto.De(svc.Obtener(UsuarioId(ctx)))));

perfil.MapPut("/", (PerfilRequest req, HttpContext ctx, UsuarioService svc) =>
{
    svc.ActualizarPerfil(UsuarioId(ctx), req.Email);
    return Results.Ok(UsuarioDto.De(svc.Obtener(UsuarioId(ctx))));
});

perfil.MapPut("/password", (PasswordRequest req, HttpContext ctx, UsuarioService svc) =>
{
    svc.CambiarPassword(UsuarioId(ctx), req.Actual, req.Nueva);
    return Results.NoContent();
});

perfil.MapGet("/avatar", (HttpContext ctx, UsuarioService svc, AvatarService avatares) =>
{
    var usuario = svc.Obtener(UsuarioId(ctx));
    if (usuario.Avatar is null || !avatares.Existe(usuario.Avatar)) return Results.NotFound();
    return Results.File(avatares.Ruta(usuario.Avatar), AvatarService.TipoMime(usuario.Avatar));
});

// Antiforgery deshabilitado a proposito: la cookie es SameSite=Lax, asi que un sitio
// externo no puede disparar este POST con la sesion del usuario.
perfil.MapPost("/avatar", async (IFormFile archivo, HttpContext ctx, UsuarioService svc) =>
    Results.Ok(new { avatar = await svc.CambiarAvatarAsync(UsuarioId(ctx), archivo) })
).DisableAntiforgery();

perfil.MapDelete("/avatar", (HttpContext ctx, UsuarioService svc) =>
{
    svc.QuitarAvatar(UsuarioId(ctx));
    return Results.NoContent();
});

perfil.MapPost("/2fa/iniciar", (HttpContext ctx, UsuarioService svc) =>
{
    var alta = svc.IniciarAltaTotp(UsuarioId(ctx));
    return Results.Ok(new { alta.Secret, qr = $"data:image/png;base64,{alta.QrPngBase64}" });
});

perfil.MapPost("/2fa/confirmar", (CodigoRequest req, HttpContext ctx, UsuarioService svc) =>
{
    svc.ConfirmarAltaTotp(UsuarioId(ctx), req.Codigo);
    return Results.NoContent();
});

perfil.MapPost("/2fa/desactivar", (DesactivarTotpRequest req, HttpContext ctx, UsuarioService svc) =>
{
    svc.DesactivarTotp(UsuarioId(ctx), req.Password, req.Codigo);
    return Results.NoContent();
});

// ── Periodos ────────────────────────────────────────────────────────────
var periodos = api.MapGroup("/periodos").RequireAuthorization();

periodos.MapGet("/", (HttpContext ctx, PeriodoService svc) =>
    svc.ObtenerTodos(UsuarioId(ctx)).Select(PeriodoDto.De));

periodos.MapGet("/actual", (HttpContext ctx, PeriodoService svc, int? anio, int? mes) =>
{
    var hoy = DateTime.Now;
    var p = svc.ObtenerOCrearPeriodo(UsuarioId(ctx), anio ?? hoy.Year, mes ?? hoy.Month);
    return p is null ? Results.NotFound(new { error = "No hay periodo para ese mes." }) : Results.Ok(PeriodoDto.De(p));
});

periodos.MapGet("/{id:int}", (int id, HttpContext ctx, PeriodoService svc) =>
{
    var p = svc.ObtenerPorId(UsuarioId(ctx), id);
    return p is null ? Results.NotFound() : Results.Ok(PeriodoDto.De(p));
});

periodos.MapPost("/{id:int}/cerrar", (int id, HttpContext ctx, PeriodoService svc) =>
{
    svc.CerrarPeriodo(UsuarioId(ctx), id);
    return Results.NoContent();
});

periodos.MapPost("/{id:int}/reabrir", (int id, HttpContext ctx, PeriodoService svc) =>
{
    svc.ReabrirPeriodo(UsuarioId(ctx), id);
    return Results.NoContent();
});

periodos.MapGet("/config", (HttpContext ctx, PeriodoService svc) => svc.ObtenerConfig(UsuarioId(ctx)));

periodos.MapPut("/config", (ConfigPeriodosRequest req, HttpContext ctx, PeriodoService svc) =>
{
    svc.GuardarConfig(UsuarioId(ctx), req.DiaCorte, req.DiasGracia);
    return Results.NoContent();
});

periodos.MapGet("/{id:int}/ingresos", (int id, HttpContext ctx, IngresoService svc) =>
    svc.ObtenerPorPeriodo(UsuarioId(ctx), id).Select(IngresoDto.De));

periodos.MapPost("/{id:int}/ingresos", (int id, IngresoRequest req, HttpContext ctx, IngresoService svc) =>
{
    var ing = req.AEntidad();
    ing.PeriodoId = id;
    return Results.Ok(IngresoDto.De(svc.Guardar(UsuarioId(ctx), ing)));
});

periodos.MapGet("/{id:int}/gastos", (int id, HttpContext ctx, GastoService svc) =>
    svc.ObtenerPorPeriodo(UsuarioId(ctx), id).Select(GastoDto.De));

periodos.MapPost("/{id:int}/gastos", (int id, GastoRequest req, HttpContext ctx, GastoService svc) =>
{
    var g = req.AEntidad();
    g.PeriodoId = id;
    return Results.Ok(GastoDto.De(svc.Guardar(UsuarioId(ctx), g)));
});

periodos.MapGet("/{id:int}/sobres", (int id, HttpContext ctx, PresupuestoService svc) => svc.ObtenerSobres(UsuarioId(ctx), id));

// ── Ingresos ────────────────────────────────────────────────────────────
var ingresos = api.MapGroup("/ingresos").RequireAuthorization();

ingresos.MapPut("/{id:int}", (int id, IngresoRequest req, HttpContext ctx, IngresoService svc) =>
{
    var ing = req.AEntidad();
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

gastos.MapPut("/{id:int}", (int id, GastoRequest req, HttpContext ctx, GastoService svc) =>
{
    var g = req.AEntidad();
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

gastos.MapGet("/{id:int}/consumos", (int id, HttpContext ctx, PresupuestoService svc) =>
    svc.ObtenerConsumos(UsuarioId(ctx), id).Select(ConsumoDto.De));

gastos.MapPost("/{id:int}/consumos", (int id, ConsumoRequest req, HttpContext ctx, PresupuestoService svc) =>
{
    var c = req.AEntidad();
    c.GastoId = id;
    return Results.Ok(svc.Guardar(UsuarioId(ctx), c));
});

// ── Consumos ────────────────────────────────────────────────────────────
var consumos = api.MapGroup("/consumos").RequireAuthorization();

consumos.MapPut("/{id:int}", (int id, ConsumoRequest req, HttpContext ctx, PresupuestoService svc) =>
{
    var c = req.AEntidad();
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

deudas.MapGet("/", (HttpContext ctx, DeudaService svc) => svc.ObtenerTodas(UsuarioId(ctx)).Select(DeudaDto.De));
deudas.MapGet("/activas", (HttpContext ctx, DeudaService svc) => svc.ObtenerActivas(UsuarioId(ctx)).Select(DeudaDto.De));
deudas.MapGet("/resumen", (HttpContext ctx, DeudaService svc) => svc.Resumen(UsuarioId(ctx)));

deudas.MapPost("/", (DeudaRequest req, HttpContext ctx, DeudaService svc) =>
    Results.Ok(DeudaDto.De(svc.Guardar(UsuarioId(ctx), req.AEntidad()))));

// El Tipo del body se ignora: lo fija la creacion y cambiarlo dejaria los pagos, que viven
// en gastos o en ingresos segun la direccion, colgados de la tabla equivocada.
deudas.MapPut("/{id:int}", (int id, DeudaRequest req, HttpContext ctx, DeudaService svc) =>
{
    var d = req.AEntidad();
    d.Id = id;
    svc.Actualizar(UsuarioId(ctx), d);
    return Results.NoContent();
});

deudas.MapDelete("/{id:int}", (int id, HttpContext ctx, DeudaService svc) =>
{
    svc.Eliminar(UsuarioId(ctx), id);
    return Results.NoContent();
});

// Historial completo: pagos y ampliaciones mezclados.
deudas.MapGet("/{id:int}/movimientos", (int id, HttpContext ctx, DeudaService svc) => svc.ObtenerMovimientos(UsuarioId(ctx), id));

deudas.MapPost("/{id:int}/pagos", (int id, AbonoRequest req, HttpContext ctx, DeudaService svc) =>
    Results.Ok(svc.RegistrarPago(UsuarioId(ctx), id, req.PeriodoId, req.CategoriaId, req.Monto, req.Descripcion)));

// Prestar mas sobre una deuda que ya existe, en vez de crear otra a la misma persona.
deudas.MapPost("/{id:int}/ampliaciones", (int id, AmpliacionRequest req, HttpContext ctx, DeudaService svc) =>
    Results.Ok(svc.Ampliar(UsuarioId(ctx), id, req.Monto, req.Fecha, req.Descripcion)));

deudas.MapPut("/ampliaciones/{ampliacionId:int}", (int ampliacionId, AmpliacionRequest req, HttpContext ctx, DeudaService svc) =>
{
    svc.ActualizarAmpliacion(UsuarioId(ctx), ampliacionId, req.Monto, req.Fecha, req.Descripcion);
    return Results.NoContent();
});

deudas.MapDelete("/ampliaciones/{ampliacionId:int}", (int ampliacionId, HttpContext ctx, DeudaService svc) =>
{
    svc.EliminarAmpliacion(UsuarioId(ctx), ampliacionId);
    return Results.NoContent();
});

app.Run();
return 0;
