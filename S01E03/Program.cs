using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Refit;
using S01E03;

var builder = new HostBuilder()
  .ConfigureAppConfiguration((hostingContext, config) =>
  {
    config.AddUserSecrets<Program>(); // Dodaje sekcję secrets
  })
  .ConfigureServices((context, services) =>
  {
    var apiUrl = context.Configuration["BaseUrl"]!;

    var jsonSettings = new RefitSettings
    {
      ContentSerializer = new SystemTextJsonContentSerializer(
        new JsonSerializerOptions
        {
          RespectNullableAnnotations = true,
          PropertyNamingPolicy = null
        }
      )
    };
    
    services.AddRefitClient<ILesson3Api>(jsonSettings)
      .ConfigureHttpClient(client =>
      {
        client.BaseAddress = new Uri(apiUrl);
      });
    services.AddTransient<Lesson3Task>();
  });

var app = builder.Build();
await app.StartAsync();
try
{
  var lesson = app.Services.GetRequiredService<Lesson3Task>();
  await lesson.Execute();
}
finally
{
  await app.StopAsync();
};