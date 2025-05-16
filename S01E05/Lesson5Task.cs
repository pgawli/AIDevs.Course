using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using OpenAI.Chat;
using Refit;

namespace S01E05;

public class Lesson5Task
{
  private readonly ILesson5Api lesson5Api;
  private readonly IConfiguration configuration;
  private readonly ChatClient client;
  private readonly string apiKey;  

  public Lesson5Task(ILesson5Api lesson5Api, IConfiguration configuration)
  {
    this.lesson5Api = lesson5Api;
    this.configuration = configuration;
    client = new ChatClient(model: "gpt-4.1-nano", apiKey: configuration["OpenAI:Token"]!);
    apiKey = configuration["ApiKey"]!;
  }
  public async Task Execute()
  {
    var cenzura = await lesson5Api.GetCenzuraTxt(apiKey);
    if (!cenzura.IsSuccessful)
    {
      Console.WriteLine($"{cenzura.StatusCode} {cenzura.ReasonPhrase} - {cenzura.Content}");
      throw new Exception("The cenzura.txt could not be retrieved.");
    }
    var originalText = cenzura.Content;
    Console.WriteLine($"Cenzura.txt: {originalText}");
    
    var prompt = $"To jest tekst: '{originalText}' który należy ocenzurować. Zamień wszystkie dane dotyczące imienia, nazwiska, adresu oraz wieku na słowo CENZURA.";
    
    var context = new List<ChatMessage>
    {
      ChatMessage.CreateSystemMessage("Imię i nazwisko muszą być zastąpione jednym słowem CENZURA" ),
      ChatMessage.CreateSystemMessage("W adresie zastęp tylko nazwy miast i ulic ale zachowaj określenia miejsc takie jak 'ulica' i podobne" ),
      ChatMessage.CreateSystemMessage(prompt)
    };
    
    var chatResponse = await client.CompleteChatAsync(context);
    if (chatResponse == null)
    {
      throw new Exception("The chat response could not be retrieved.");
    }
    var answer = chatResponse.Value.Content[0].Text!;
    Console.WriteLine(answer);
    
    var request = new AnswerRequest("CENZURA", apiKey, answer);
    var response = await lesson5Api.PostCenzura(request);
    if (!response.IsSuccessful)
    {
      Console.WriteLine($"{cenzura.StatusCode} {cenzura.ReasonPhrase} - {cenzura.Content}");
      throw new Exception("The answer could not be posted.");
    }
    Console.WriteLine($"Answer: {response.Content}");
  }
  
}

public interface ILesson5Api
{
  [Get("/data/{apikey}/cenzura.txt")]
  Task<ApiResponse<string>> GetCenzuraTxt(string apikey);
  
  [Post("/report")]
  Task<ApiResponse<string>> PostCenzura(AnswerRequest request);
}

public record AnswerRequest(
  [property: JsonPropertyName("task")] string Task, 
  [property: JsonPropertyName("apikey")] string ApiKey, 
  [property: JsonPropertyName("answer")] string Answer);