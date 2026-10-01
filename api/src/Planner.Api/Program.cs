using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Planner.Api.Authentication;
using Planner.Api.Errors;
using Planner.Application;
using Planner.Application.Common;
using Planner.Infrastructure;
using Planner.Infrastructure.Development;

var builder = WebApplication.CreateBuilder(args);

// O build gera o openapi/planner.json executando esta configuração sem servir pedidos.
var generatingOpenApiDocument = Assembly.GetEntryAssembly()?.GetName().Name == "GetDocument.Insider";

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Autenticação: por enquanto só existe o esquema de desenvolvimento, e ele nunca pode valer em outro ambiente.
if(!builder.Environment.IsDevelopment() && !generatingOpenApiDocument) {
    throw new InvalidOperationException(
        "No authentication is configured outside Development: the development scheme must never run there.");
}
builder.Services
    .AddAuthentication(DevelopmentAuthenticationHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, DevelopmentAuthenticationHandler>(
        DevelopmentAuthenticationHandler.SchemeName, configureOptions: null);
// Todo endpoint exige um usuário autenticado, sem precisar marcar um por um.
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, ClaimsCurrentUser>();

// Erros: Problem Details com o campo "code" (docs/api-design.md, seção Errors).
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ProblemDetailsExceptionHandler>();
// JSON estrito: números só como números ("3" é recusado), para o contrato não virar "number | string".
builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.NumberHandling = JsonNumberHandling.Strict)
    .ConfigureApiBehaviorOptions(options => {
        var defaultFactory = options.InvalidModelStateResponseFactory;
        options.InvalidModelStateResponseFactory = context => {
            var result = defaultFactory(context);
            if(result is Microsoft.AspNetCore.Mvc.ObjectResult { Value: Microsoft.AspNetCore.Mvc.ProblemDetails problem }) {
                problem.Extensions["code"] = "validation";
            }
            return result;
        };
    });
builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.NumberHandling = JsonNumberHandling.Strict);
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseAuthentication();
app.UseAuthorization();

if(app.Environment.IsDevelopment()) {
    app.MapOpenApi().AllowAnonymous();
    await DevelopmentUser.EnsureCreatedAsync(app.Services);
}

app.MapControllers();

await app.RunAsync();

/// <summary>Exposto para os testes de integração da API (WebApplicationFactory).</summary>
public partial class Program;
