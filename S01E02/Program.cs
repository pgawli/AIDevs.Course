using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Refit;
using S01E02;

var builder = new HostBuilder()
  .ConfigureAppConfiguration((hostingContext, config) =>
  {
    config.AddUserSecrets<Program>(); // Dodaje sekcję secrets
  })
  .ConfigureServices((context, services) =>
  {
    var apiUrl = context.Configuration["BaseUrl"]!;

    services.AddRefitClient<IChatApi>()
      .ConfigureHttpClient(client =>
      {
        client.BaseAddress = new Uri(apiUrl);
      })
      .AddDefaultLogger();
    services.AddTransient<ChatTask>();
  });

var app = builder.Build();
await app.StartAsync();
try
{
  var task1 = app.Services.GetRequiredService<ChatTask>();
  await task1.Execute();
}
finally
{
  await app.StopAsync();
};