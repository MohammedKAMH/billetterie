using Billetterie.Api.Auth;
using Billetterie.Api.BackgroundServices;
using Billetterie.Application.Interfaces;
using Billetterie.Infrastructure.Data;
using Billetterie.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using Scalar.AspNetCore;

// %APPDATA%\Microsoft\UserSecrets\
var builder = WebApplication.CreateBuilder(args);

var front = "NomPolitiqueFront";

var origins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? [];

builder.Services.AddCors(options =>
options.AddPolicy(front, policy => policy
    .WithOrigins(origins)
    .AllowAnyHeader()
    .AllowAnyMethod()));

// Add services to the container.
builder.Services.AddDbContext<BilletterieDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("BilletterieDb")));

JsonWebTokenHandler.DefaultInboundClaimTypeMap.Clear();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Keycloak:Authority"];
        options.Audience = builder.Configuration["Keycloak:Audience"];
        options.RequireHttpsMetadata = false;
    });

builder.Services.AddAuthorization(options =>
    options.AddPolicy(Policies.OrganisateurValide, policy => policy
        .RequireAuthenticatedUser()
        .AddRequirements(new OrganisateurValideRequirement())));

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(Policies.OrganisateurValide, policy => policy
        .RequireAuthenticatedUser()
        .AddRequirements(new OrganisateurValideRequirement()));

    options.AddPolicy(Policies.Admin, policy => policy
        .RequireRole("Admin"));
});

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddTransient<IClaimsTransformation, KeycloakRolesClaimsTransformation>();

builder.Services.AddScoped<IUtilisateurService,UtilisateurService>();
builder.Services.AddScoped<IEvenementService, EvenementService>();
builder.Services.AddScoped<ITypeBilletService, TypeBilletService>(); 
builder.Services.AddScoped<IReservationService, ReservationService>();
builder.Services.AddHostedService<ExpirationReservationsService>();
builder.Services.AddScoped<IOrganisateurService, OrganisateurService>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUtilisateurCourant, UtilisateurCourant>();
builder.Services.AddScoped<IAuthorizationHandler, OrganisateurValideHandler>();

var app = builder.Build();

app.UseCors(front);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();

}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
