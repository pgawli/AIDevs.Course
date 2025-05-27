using Microsoft.Extensions.Configuration;

namespace AIDevs.Common;

public abstract class Lesson
{
  protected readonly ICentralaApi Api;
  protected readonly IConfiguration Configuration;
  protected readonly string ApiKey;
  protected readonly string OpenAiToken;
  
  protected Lesson(ICentralaApi api, IConfiguration configuration)
  {
    Api = api;
    Configuration = configuration;
    ApiKey = configuration["ApiKey"]!;
    OpenAiToken = configuration["OpenAI:Token"]!;
  }

  public abstract ValueTask Execute();
}