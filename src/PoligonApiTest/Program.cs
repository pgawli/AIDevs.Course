using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PoligonApiTest;
using Refit;

var builder = new HostBuilder()
    .ConfigureAppConfiguration((hostingContext, config) =>
    {
        config.AddUserSecrets<Program>(); // Dodaje sekcję secrets
    })
    .ConfigureServices((context, services) =>
    {
        var apiUrl = context.Configuration["Api:BaseUrl"]!;
        // var apiKey = context.Configuration["Api:Key"];

        services.AddRefitClient<IPoligonApi>()
            .ConfigureHttpClient(client =>
        {
            client.BaseAddress = new Uri(apiUrl);
        });
        services.AddTransient<PoligonTask1>();
    });

var app = builder.Build();
await app.StartAsync();
try
{
    var task1 = app.Services.GetRequiredService<PoligonTask1>();
    await task1.Execute();
}
finally
{
    await app.StopAsync();
};