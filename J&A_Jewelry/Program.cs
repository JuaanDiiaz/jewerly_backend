using J_A_Jewelry.Middleware;
using J_A_Jewelry.Models;
using J_A_Jewelry.Services;
using J_A_Jewelry.Services.Identity;
using J_A_Jewelry.Serilog;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text;

var serverVersion = new MySqlServerVersion(new Version(8, 0, 39));

var builder = WebApplication.CreateBuilder(args);

// Serilog is the logging pipeline: Microsoft/EF category logging flows
// through it too (levels from the Serilog config section). The MySQL sink is
// wired in code so it reuses the same myDBConn connection string — never
// duplicated into appsettings.
builder.Host.UseSerilog((context, loggerConfiguration) => loggerConfiguration
    .ReadFrom.Configuration(context.Configuration)
    // Required for LogContext (TraceMiddleware's per-request TraceId
    // property) to reach any sink.
    .Enrich.FromLogContext()
    .WriteTo.Sink(
        new Serilog.Sinks.PeriodicBatching.PeriodicBatchingSink(
            new MySqlLogEventSink(context.Configuration.GetConnectionString("myDBConn")!),
            new Serilog.Sinks.PeriodicBatching.PeriodicBatchingSinkOptions
            {
                BatchSizeLimit = 200,
                Period = TimeSpan.FromSeconds(5),
                EagerlyEmitFirstEvent = true,
                QueueLimit = 10000
            })));

// Add services to the container.
builder.Services.AddRazorPages();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
// Bearer button in Swagger so endpoints are testable from the UI itself.
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Description = "Paste the JWT from POST /api/Auth/login (no 'Bearer ' prefix needed)"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve;
});

// Add database context
// MySqlServerVersion is declared up front (instead of ServerVersion.AutoDetect)
// so startup and design-time tooling never require a live database ping.
builder.Services.AddDbContext<IsetechcJewelryInventoryContext>(options =>
    options.UseMySql(builder.Configuration.GetConnectionString("myDBConn"), serverVersion));

// Log-viewer support: client-log ingest rate limiting + retention options.
builder.Services.AddMemoryCache();
builder.Services.Configure<LogRetentionOptions>(builder.Configuration.GetSection("LogRetention"));
builder.Services.AddHostedService<LogRetentionService>();
builder.Services.AddHostedService<IdentitySeedService>();

// Identity: JWT bearer + one policy per permission code (Services/Identity/
// Permissions). Controllers opt in with [Authorize(Policy = <code>)]; a
// 'permission' claim of '*' passes every policy (shipped Administrator).
var jwtSigningKey = builder.Configuration["Jwt:SigningKey"]
    ?? throw new InvalidOperationException(
        "Jwt:SigningKey is not configured (user-secrets in dev / Jwt__SigningKey env var in prod)");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "jajewelry-api";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "jajewelry-app";

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSigningKey)),
            ValidateLifetime = true,
        };
        // Answer 401s with a JSON body the client can read (default is an
        // empty body + WWW-Authenticate header).
        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(
                    "{\"message\":\"Not authenticated\",\"status\":401}");
            }
        };
    });
builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
builder.Services.AddAuthorization(options =>
{
    foreach (var code in Permissions.All)
    {
        options.AddPolicy(code, policy =>
            policy.Requirements.Add(new PermissionRequirement(code)));
    }
});

// Add CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins",
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader()
                   // The app reads X-TraceId off responses to pair its log
                   // events with the server's request lines; without this,
                   // cross-origin XHR cannot see custom headers.
                   .WithExposedHeaders("X-TraceId");
        });
});

var app = builder.Build();

// Diagnostics pipeline. TraceMiddleware runs first so the traceId is on
// every response header and in every LogContext below it; request logging
// wraps the whole chain (5xx=Error, 4xx=Warning, 2xx=Debug); exceptions get
// the JSON+traceId middleware before any other handler can answer.
app.UseMiddleware<TraceMiddleware>();
app.UseSerilogRequestLogging(options =>
{
    options.GetLevel = (httpContext, elapsed, ex) => httpContext.Response.StatusCode switch
    {
        >= 500 => Serilog.Events.LogEventLevel.Error,
        >= 400 => Serilog.Events.LogEventLevel.Warning,
        _ => Serilog.Events.LogEventLevel.Debug
    };
});
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Use CORS policy - Aseg�rate de que esto est� antes de UseAuthorization y MapControllers
app.UseCors("AllowAllOrigins");

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Quiosco API V1");
    c.RoutePrefix = string.Empty; // Set Swagger UI at app's root
});

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Authentication resolves the JWT → claims; Authorize polices them. Both
// sit after routing so endpoint attributes decide which policies apply.
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.MapControllers();

app.Run();
