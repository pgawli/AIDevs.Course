using Microsoft.Extensions.Configuration;
using Qdrant.Client;

namespace AIDevs.Common;

public abstract class Lesson
{
  protected readonly ICentralaApi Api;
  protected readonly IConfiguration Configuration;
  protected readonly string ApiKey;
  protected readonly string OpenAiToken;
  protected readonly string? QdrantUrl;
  protected readonly int QdrantPort = 6333;
  protected readonly string? QdrantApiKey;
  
  protected Lesson(ICentralaApi api, IConfiguration configuration)
  {
    Api = api;
    Configuration = configuration;
    ApiKey = configuration["ApiKey"]!;
    OpenAiToken = configuration["OpenAI:Token"]!;
    
    QdrantUrl = configuration["Qdrant:Url"];
    QdrantPort = configuration.GetValue("Qdrant:Port", 6333);
    QdrantApiKey = configuration["Qdrant:ApiKey"];
  }

  public abstract ValueTask Execute();
  
  protected QdrantClient CreateQdrantClient()
  {
    if (string.IsNullOrEmpty(QdrantUrl))
      throw new InvalidOperationException("Qdrant URL is not configured.");
    if (string.IsNullOrEmpty(QdrantApiKey))
      throw new InvalidOperationException("Qdrant ApiKey is not configured.");

    return new QdrantClient("localhost"); //QdrantUrl, QdrantPort, false, QdrantApiKey);
  }
}