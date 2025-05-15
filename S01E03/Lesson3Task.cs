using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using OpenAI.Chat;
using Refit;

namespace S01E03;

public class Lesson3Task
{
  private readonly ILesson3Api chatApi;
  private readonly IConfiguration configuration;
  private readonly ChatClient client;
  private readonly string apiKey;

  public Lesson3Task(ILesson3Api chatApi,  IConfiguration configuration)
  {
    this.chatApi = chatApi;
    this.configuration = configuration;
    client = new ChatClient(model: "gpt-4.1-nano", apiKey: configuration["OpenAI:Token"]!);
    apiKey = configuration["ApiKey"]!;
  }
  
  public async Task Execute()
  {
    var llmCalls = 0;
    var input = await LoadInput();
    if (input == null) 
      throw new FileNotFoundException("Nie można wczytać danych wejściowych.");
    input = input with { ApiKey = apiKey };
    
    var calculation = new List<CalculationModel>();
    
    var i = 0;
    foreach (var model in input.TestData)
    {
      Console.Write("{0}: ", ++i);
      var calculated = Calculator.Calculate(model.Question);
      var updated = model with { Answer = calculated };
      
      if (updated.TestQuestion != null)
      {
        var answered = GetAnswerFromLlm(updated.TestQuestion);
        updated = updated with { TestQuestion = answered };
        llmCalls++;
      }
      Console.WriteLine($"{updated}");
      calculation.Add(updated);
    }
    
    Console.WriteLine($"Llm calls {llmCalls}");
    var answer = input with{ TestData = calculation };
    try
    {
      var request = new RequestModel("JSON", apiKey, answer);
      var response = await chatApi.Report(request);
      Console.WriteLine(response);
    }
    catch (ApiException ex)
    {
      Console.WriteLine($"Error: {ex.StatusCode} - {ex.ReasonPhrase}  {ex.Content}");
    }

  }

  private TestQuestion GetAnswerFromLlm(TestQuestion model)
  {
    var completion = client.CompleteChatAsync(model.Question).Result;
    var answer = completion.Value.Content[0].Text;
    if (answer == null)
      throw new ArgumentNullException("Nie można uzyskać odpowiedzi z modelu.");
    return model with { Answer = answer };
  }
  
  private async Task<DataModel?> LoadInput()
  {
    var jsonText = await File.ReadAllTextAsync("input.json");
    using var stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonText));
    return await JsonSerializer.DeserializeAsync<DataModel>(stream);
  }
}

internal static class Calculator
{
  public static int Calculate(string question)
  {
    var parts = question.Split([' '], StringSplitOptions.RemoveEmptyEntries);
    if (parts.Length < 3)
      throw new ArgumentException("Niepoprawne pytanie.");
    var first = int.Parse(parts[0]);
    var second = int.Parse(parts[2]);
    var operation = parts[1].Trim();
    return operation == "+" ? first + second : first - second;
  }
}

public interface ILesson3Api
{
  [Post("/report")]
  Task<string> Report(RequestModel input);
}


public sealed record TestQuestion(
  [property: JsonPropertyName("q")] string Question,
  [property: JsonPropertyName("a")] string Answer
 
);
public sealed record CalculationModel(
  [property: JsonPropertyName("question")] string Question, 
  [property: JsonPropertyName("answer")] int Answer,
  [property: JsonPropertyName("test")] TestQuestion? TestQuestion
);

public sealed record DataModel (
  [property: JsonPropertyName("apikey")]
  string ApiKey,
  [property: JsonPropertyName("description")]
  string Description,
  [property: JsonPropertyName("copyright")]
  string Copyright,
  [property: JsonPropertyName("test-data")]
  List<CalculationModel> TestData);

public sealed record RequestModel (
  [property: JsonPropertyName("task")]
  string Task,
  [property: JsonPropertyName("apikey")]
  string ApiKey,
  [property: JsonPropertyName("answer")]
  DataModel Answer);

