using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Refit;
using OpenAI.Chat;

namespace S01E01;

public class LoginTask
{
  private readonly ILoginApi loginApi;
  private readonly IConfiguration configuration;
  private readonly string openApiKey;

  public LoginTask(ILoginApi poligonApi, IConfiguration configuration)
  {
    this.loginApi = poligonApi;
    this.configuration = configuration;
    openApiKey = this.configuration["OpenAI:Token"]!;
  }
  public async Task Execute()
  {
    var question = await GetQuestion();
    var answer = await GetAnswer(question);
    var request = new Dictionary<string, object>
    {
      { "username", configuration["Login:Username"]! },
      { "password", configuration["Login:Password"]! },
      { "answer", answer }
    };
    var response = await loginApi.Login(request);
    Console.WriteLine(response);
  }

  private async Task<int> GetAnswer(string question)
  {
    ChatClient client = new(model: "gpt-4.1-nano", apiKey: openApiKey);

    ChatCompletion completion = await client.CompleteChatAsync(question);

    Console.WriteLine($"[ASSISTANT]: {completion.Content[0].Text}");
    var llmAnswer = completion.Content[0].Text;
    var match = Regex.Match(llmAnswer, @"\d+");
    if (match.Success)
      return int.Parse(match.Value);
    throw new Exception("Nie znaleziono liczby w odpowiedzi LLM.");
  }

  private async Task<string> GetQuestion()
  {
    var html = await loginApi.GetLoginPageContent();
    if (string.IsNullOrWhiteSpace(html))
      throw new Exception("Answer is empty");
    // <p id="human-question">Question:<br>Rok lądowania na Księżycu?</p>
    var id = "human-question";
    var match = Regex.Match(html, $@"<p[^>]*id\s*=\s*[""']{id}[""'][^>]*>.*?<br\s*/?>(.*?)</p>", RegexOptions.Singleline);
    if (match.Success)
    {
      return match.Groups[1].Value.Trim();
    }
    return string.Empty;
  }
}


public interface ILoginApi
{
  [Get("/")]
  Task<string> GetLoginPageContent();
  
  [Headers("Content-Type: application/x-www-form-urlencoded")]
  [Post("/")]
  Task<string> Login([Body(BodySerializationMethod.UrlEncoded)] Dictionary<string, object> data);
}

public record LoginRequest(string Username, string Password, string Answer);