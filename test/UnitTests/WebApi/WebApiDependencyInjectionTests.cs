using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.ResiliencePipelines;
using MelloSilveiraTools.Database.Repositories;
using MelloSilveiraTools.WebApi;
using MelloSilveiraTools.WebApi.Application.Commands.Crud.Add;
using MelloSilveiraTools.WebApi.Application.Commands.Crud.Delete;
using MelloSilveiraTools.WebApi.Application.Commands.Crud.Read;
using MelloSilveiraTools.WebApi.Application.Commands.Crud.Update;
using MelloSilveiraTools.WebApi.Authentication;
using MelloSilveiraTools.WebApi.Authentication.Services;
using MelloSilveiraTools.WebApi.Infrastructure.ResiliencePipelines;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using System.Net;
using UnitTests.WebApi.Fixtures;

namespace UnitTests.WebApi;

public class WebApiDependencyInjectionTests
{
    [Fact]
    public void AddWebApiServices_RegistersPipelineAndOpenGenericCrudCommands()
    {
        ServiceCollection services = new();
        Mock<IRepository> repositoryMock = new();
        services.AddLogging();
        services.AddSingleton(repositoryMock.Object);

        ResiliencePipelineSettings settings = WebApiTestSettingsFactory.CreateResilienceSettings();
        services.AddWebApiServices(settings);

        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        Assert.NotNull(provider.GetRequiredService<ApiServiceAgentResiliencePipeline>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<AddEntity<WebApiTestEntity>>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ReadEntityById<WebApiTestEntity>>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ReadEntityPaged<WebApiTestEntity, WebApiTestFilter>>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<UpdateEntity<WebApiTestEntity>>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<DeleteEntity<WebApiTestEntity>>());
    }

    [Fact]
    public void AddJweAuthentication_RegistersJwtSettingsTokenServiceAndJwtBearerDefaults()
    {
        ServiceCollection services = new();
        services.AddSingleton(TimeProvider.System);

        JwtSettings jwtSettings = new()
        {
            Issuer = "issuer-test",
            Audience = "audience-test",
            EncryptionKey = "12345678901234567890123456789012",
            SigningKey = "signing-key-at-least-32-characters!",
            ClockSkewInSeconds = 10,
            SecurityKeyType = SecurityKeyType.Symmetric,
            TokenExperitationTimeInMinutes = 15
        };

        services.AddJweAuthentication(jwtSettings);

        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        Assert.Same(jwtSettings, provider.GetRequiredService<JwtSettings>());
        Assert.IsType<AuthenticationJweTokenService>(scope.ServiceProvider.GetRequiredService<IAuthenticationTokenService>());

        IOptions<AuthenticationOptions> authOptions = provider.GetRequiredService<IOptions<AuthenticationOptions>>();
        Assert.Equal(JwtBearerDefaults.AuthenticationScheme, authOptions.Value.DefaultAuthenticateScheme);
        Assert.Equal(JwtBearerDefaults.AuthenticationScheme, authOptions.Value.DefaultChallengeScheme);
        Assert.Equal(JwtBearerDefaults.AuthenticationScheme, authOptions.Value.DefaultScheme);

        IOptionsMonitor<JwtBearerOptions> bearerOptions = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>();
        JwtBearerOptions jwtBearer = bearerOptions.Get(JwtBearerDefaults.AuthenticationScheme);
        Assert.True(jwtBearer.SaveToken);
        Assert.Equal("issuer-test", jwtBearer.TokenValidationParameters.ValidIssuer);
        Assert.Equal("audience-test", jwtBearer.TokenValidationParameters.ValidAudience);
    }

    [Fact]
    public async Task AddSwaggerWithBearerSecurity_UseSwaggerDocs_And_UseCustomMiddlewares_ServeSwaggerAndHandleExceptions()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");

        ResiliencePipelineSettings settings = WebApiTestSettingsFactory.CreateResilienceSettings();
        Mock<IRepository> repositoryMock = new();
        builder.Services.AddSingleton(repositoryMock.Object);
        builder.Services.AddWebApiServices(settings);
        builder.Services.AddSwaggerWithBearerSecurity();

        await using WebApplication app = builder.Build();
        app.UseCustomMiddlewares();
        app.UseSwaggerDocs();
        app.MapGet("/boom", (Func<IResult>)(() => throw new KeyNotFoundException("Missing resource")));

        await app.StartAsync();
        string baseUrl = app.Urls.Single();

        Swashbuckle.AspNetCore.SwaggerGen.SwaggerGenOptions swaggerOptions = app.Services.GetRequiredService<IOptions<Swashbuckle.AspNetCore.SwaggerGen.SwaggerGenOptions>>().Value;
        string docName = swaggerOptions.SwaggerGeneratorOptions.SwaggerDocs.Keys.Single();
        Assert.NotEmpty(docName);

        using HttpClient client = new() { BaseAddress = new Uri(baseUrl) };

        HttpResponseMessage swaggerDocResponse = await client.GetAsync($"/swagger/{Uri.EscapeDataString(docName)}/swagger.json");
        Assert.Equal(HttpStatusCode.OK, swaggerDocResponse.StatusCode);

        HttpResponseMessage swaggerUiResponse = await client.GetAsync("/swagger/index.html");
        Assert.Equal(HttpStatusCode.OK, swaggerUiResponse.StatusCode);

        HttpResponseMessage boomResponse = await client.GetAsync("/boom");
        Assert.Equal(HttpStatusCode.NotFound, boomResponse.StatusCode);
        string body = await boomResponse.Content.ReadAsStringAsync();
        Assert.Contains(((int)StatusCode.NotFound).ToString(), body);

        await app.StopAsync();
    }
}
