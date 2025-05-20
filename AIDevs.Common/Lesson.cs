using Microsoft.Extensions.Configuration;

namespace AIDevs.Common;

public abstract class Lesson
{
  protected readonly ICentralaApi api;
  protected readonly string apiKey;
  protected readonly string openAiToken;
  
  protected Lesson(ICentralaApi api, IConfiguration configuration)
  {
    this.api = api;
    apiKey = configuration["ApiKey"]!;
    openAiToken = configuration["OpenAI:Token"]!;
  }

  public abstract ValueTask Execute();
}