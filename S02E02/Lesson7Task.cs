using AIDevs.Common;
using Microsoft.Extensions.Configuration;
using OpenAI.Chat;

public class Lesson7Task : Lesson
{
  private readonly ChatClient chatClient;

  protected Lesson7Task(ICentralaApi api, IConfiguration configuration) : base(api, configuration)
  {
    chatClient = new ChatClient(model: "gpt-4o", apiKey: openAiToken);
  }

  public override async ValueTask Execute()
  {
    throw new NotImplementedException();
  }
}