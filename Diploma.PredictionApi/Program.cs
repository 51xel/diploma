using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using Diploma.Application;
using Diploma.Dal.EntityFramework;
using Diploma.Dal.PythonRunTime;
using Diploma.Dal.PythonRunTime.Common;
using Diploma.Dal.RedisCache;
using Diploma.Dal.Storage;
using Diploma.PredictionApi.HostedServices;

var builder = WebApplication.CreateBuilder(args);

var isDevelopment = builder.Environment.IsDevelopment();

builder.Configuration.AddAzureKeyVault(
    new Uri(builder.Configuration["AzureKeyVaultUri"]!),
    new DefaultAzureCredential(),
    new AzureKeyVaultConfigurationOptions
    {
        Manager = new KeyVaultSecretManager()
    });

builder.Services
    .AddApplication()
    .AddDalEntityFramework(builder.Configuration)
    .AddDalStorage(builder.Configuration)
    .AddDalPythonRunTime()
    .AddDalRedisCache(builder.Configuration, isDevelopment);

if (isDevelopment)
{
    builder.Services.AddHostedService<RedisContainerHostedService>();
}

builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();

lifetime.ApplicationStopping.Register(() =>
{
    PythonEngineControl.Shutdown();
});

if (isDevelopment)
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
