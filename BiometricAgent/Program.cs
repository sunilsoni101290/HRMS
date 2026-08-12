using BiometricAgent;
using BiometricAgent.Services;

var builder = Host.CreateApplicationBuilder(args);

// Lets `dotnet run` / a console window work for local testing, and
// `sc create` / the Services MMC control it as a real Windows Service
// when published and installed at the client site.
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "ERP Biometric Agent";
});

builder.Services.Configure<AgentOptions>(
    builder.Configuration.GetSection(AgentOptions.SectionName));

builder.Services.AddHttpClient<ApiClient>();
builder.Services.AddSingleton<OfflineQueue>();
builder.Services.AddSingleton<SyncState>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
