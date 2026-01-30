using BibliotecaAPI.Datos;
using BibliotecaAPI.DTOs;
using BibliotecaAPI.Entidades;
using BibliotecaAPI.Jobs;
using BibliotecaAPI.Servicios;
using BibliotecaAPI.Swagger;
using BibliotecaAPI.Utilidades;
using BibliotecaAPI.Utilidades.V1;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Área de Servicios

// Politica limitar peticiones por IP
builder.Services.AddRateLimiter(opciones =>
{
    // Politica global para limitar por IP
    //opciones.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    //    RateLimitPartition.GetFixedWindowLimiter(
    //        partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "desconocido",
    //        factory: _ => new FixedWindowRateLimiterOptions
    //        {
    //            PermitLimit = 5,
    //            Window = TimeSpan.FromSeconds(10)
    //        }
    //    ));

    // Politica "general" para limitar por IP
    opciones.AddPolicy("general", context =>
    {
        return RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "desconocido",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromSeconds(10)
                }
            );
    });

    // Politica "estricta" para limitar por IP
    opciones.AddPolicy("estricta", context =>
    {
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "desconocido",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 2,
                Window = TimeSpan.FromSeconds(5)
            });
    });

    // Politca "movil" para limitar por IP usando algoritmo "Sliding Window"
    opciones.AddPolicy("movil", context =>
    {
        return RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "desconocido",
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromSeconds(10),
                SegmentsPerWindow = 2,
                QueueLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            });
    });

    // Politca "cubeta" para limitar por IP usando algoritmo "Token Bucket"
    opciones.AddPolicy("cubeta", context =>
    {
        return RateLimitPartition.GetTokenBucketLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "desconocido",
            factory: _ => new TokenBucketRateLimiterOptions
            {
                TokenLimit = 5,
                TokensPerPeriod = 2,
                ReplenishmentPeriod = TimeSpan.FromSeconds(10)
            });
    });

    // Politca "concurrencia" para limitar por IP usando algoritmo "Currency Limited"
    opciones.AddPolicy("concurrencia", context =>
    {
        return RateLimitPartition.GetConcurrencyLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "desconocido",
            factory: _ => new ConcurrencyLimiterOptions
            {
                PermitLimit = 1
            });
    });

    // Politica "prueba-usuario" para limitar por "email" del token
    opciones.AddPolicy("prueba-usuario", context => {
        var emailClaim = context.User.Claims.Where(x => x.Type == "email").FirstOrDefault()!;
        var email = emailClaim.Value;

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: email,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 2,
                Window = TimeSpan.FromSeconds(20)
            });
    });

    // Retorna status 429 al ocurrir error de Rate Limiting
    opciones.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    // Se agrega información de respuesta: Mensaje de error y Tiempo de espera siguiente petición en Headers
    opciones.OnRejected = async (context, cancellationToken) =>
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter)) {
            context.HttpContext.Response.Headers["Retry-After"] = retryAfter.TotalSeconds.ToString();
        }
        await context.HttpContext.Response.WriteAsync("Limite excedido, intente mas tarde.", cancellationToken);
    };
});


builder.Services.AddOutputCache(opciones => {
    opciones.DefaultExpirationTimeSpan = TimeSpan.FromSeconds(60);
});
//builder.Services.AddStackExchangeRedisOutputCache(opciones => {
//    opciones.Configuration = builder.Configuration.GetConnectionString("redis");
//});

builder.Services.AddDataProtection();

var origenesPermitidos = builder.Configuration.GetSection("origenesPermitidos").Get<string[]>()!;

builder.Services.AddCors(opciones => {
    opciones.AddDefaultPolicy(opcionesCORS => {
        //opcionesCORS.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
        opcionesCORS.WithOrigins(origenesPermitidos).AllowAnyMethod().AllowAnyHeader().WithExposedHeaders("cantidad-total-registros");

    });
});

builder.Services.AddAutoMapper(typeof(Program));

builder.Services.AddControllers(opciones => {
    opciones.Conventions.Add(new ConversionAgrupaPorVersion());
}).AddNewtonsoftJson();

builder.Services.AddDbContext<ApplicationDbContext>(opciones =>
    opciones.UseSqlServer("name=DefaultConnection"));

builder.Services.AddIdentityCore<Usuario>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddScoped<UserManager<Usuario>>();
builder.Services.AddScoped<SignInManager<Usuario>>();
builder.Services.AddTransient<IServiciosUsuarios, ServiciosUsuarios>();
//builder.Services.AddTransient<IAlmacenadorArchivos, AlmacenadorArchivosAzure>();
builder.Services.AddTransient<IAlmacenadorArchivos, AlmacenadorArchivosLocal>();

builder.Services.AddScoped<FiltroValidacionLibro>();

builder.Services.AddScoped<BibliotecaAPI.Servicios.V1.IServicioAutores, BibliotecaAPI.Servicios.V1.ServicioAutores>();

builder.Services.AddScoped<BibliotecaAPI.Servicios.V1.IGeneradorEnlaces, BibliotecaAPI.Servicios.V1.GeneradorEnlaces>();
builder.Services.AddScoped<HATEOASAutorAttribute>();
builder.Services.AddScoped<HATEOASAutoresAttribute>();

builder.Services.AddHostedService<FacturasBackgroundService>();

builder.Services.AddScoped<IServicioLlaves, ServicioLlaves>();

builder.Services.AddHttpContextAccessor();

builder.Services.AddAuthentication().AddJwtBearer(opciones => {
    opciones.MapInboundClaims = false;

    opciones.TokenValidationParameters = new TokenValidationParameters {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["llavejwt"]!)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization(opciones =>
{
    opciones.AddPolicy("esadmin", politica => politica.RequireClaim("esadmin"));
});

builder.Services.AddSwaggerGen(opciones => {
    opciones.SwaggerDoc("v1", new OpenApiInfo {
        Version = "v1",
        Title = "Biblioteca API - Hola, GitHub Actions",
        Description = "Este es un web api para trabajar con datos de autores y libros",
        Contact = new OpenApiContact {
            Name = "Alan Aguilar",
            Email = "alan2304@gmail.com",
            Url = new Uri("https://opensource.org/license/mit")
        }
    });

    opciones.SwaggerDoc("v2", new OpenApiInfo {
        Version = "v2",
        Title = "Biblioteca API",
        Description = "Este es un web api para trabajar con datos de autores y libros",
        Contact = new OpenApiContact
        {
            Name = "Alan Aguilar",
            Email = "alan2304@gmail.com",
            Url = new Uri("https://opensource.org/license/mit")
        }
    });

    opciones.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header
    });

    opciones.OperationFilter<FiltroAutorizacion>();
    //opciones.AddSecurityRequirement(new OpenApiSecurityRequirement {
    //    {
    //        new OpenApiSecurityScheme {
    //            Reference = new OpenApiReference {
    //                Type = ReferenceType.SecurityScheme,
    //                Id = "Bearer"
    //            }
    //        },
    //        new string[]{ }
    //    }
    //});

});

builder.Services.AddOptions<LimitarPeticionesDTO>()
    .Bind(builder.Configuration.GetSection(LimitarPeticionesDTO.Seccion))
    .ValidateDataAnnotations()
    .ValidateOnStart();

var app = builder.Build();

// Se ímplementan todas las migraciones en la BD de Azure la primera vez que se ejecuta el proyecto
using (var scope = app.Services.CreateScope()) {
    var dbContext = scope.ServiceProvider.GetService<ApplicationDbContext>();
    if (dbContext!.Database.IsRelational()) {
        dbContext.Database.Migrate();
    }
}

    // Área de Middlewares
    app.UseExceptionHandler(exceptionHandlerApp => exceptionHandlerApp.Run(async context => {
        // Instancias para manejar el error y obtenerlo del sistema
        var exceptionHanlderFeature = context.Features.Get<IExceptionHandlerFeature>();
        var exception = exceptionHanlderFeature?.Error!;

        // Se crea el objeto del error
        var error = new Error() {
            MensajeDeError = exception.Message,
            StackTrace = exception.StackTrace,
            Fecha = DateTime.UtcNow
        };

        // Obtener error del context y guardarlo en BD
        var dbContext = context.RequestServices.GetRequiredService<ApplicationDbContext>();
        dbContext.Add(error);
        await dbContext.SaveChangesAsync();
        await Results.InternalServerError(new {
            tipo = "error",
            mensaje = "Ha ocurrido un error inesperado",
            estatus = 500
        }).ExecuteAsync(context);
    }));

app.UseSwagger();
app.UseSwaggerUI(opciones => {
    opciones.SwaggerEndpoint("/swagger/v1/swagger.json", "Biblioteca API V1");
    opciones.SwaggerEndpoint("/swagger/v2/swagger.json", "Biblioteca API V2");
});

app.UseStaticFiles();

app.UseRateLimiter();

app.UseCors();

app.UseLimitarPeticiones();

app.UseOutputCache();

app.MapControllers();

app.Run();

public partial class Program { }
