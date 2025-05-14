using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Refit;
using S01E01;

var builder = new HostBuilder()
  .ConfigureAppConfiguration((hostingContext, config) =>
  {
    config.AddUserSecrets<Program>(); // Dodaje sekcję secrets
  })
  .ConfigureServices((context, services) =>
  {
    var apiUrl = context.Configuration["Login:BaseUrl"]!;
    // var apiKey = context.Configuration["Api:Key"];

    services.AddRefitClient<ILoginApi>()
      .ConfigureHttpClient(client =>
      {
        client.BaseAddress = new Uri(apiUrl);
      })
      .AddDefaultLogger();
    services.AddTransient<LoginTask>();
  });

var app = builder.Build();
await app.StartAsync();
try
{
  var task1 = app.Services.GetRequiredService<LoginTask>();
  await task1.Execute();
}
finally
{
  await app.StopAsync();
};

