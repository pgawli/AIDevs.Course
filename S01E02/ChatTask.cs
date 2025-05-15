using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using OpenAI.Chat;
using Refit;

namespace S01E02;

public class ChatTask
{
  private readonly IChatApi chatApi;
  private readonly ChatClient client;
  
  public ChatTask(IChatApi chatApi,  IConfiguration configuration)
  {
    this.chatApi = chatApi;
    client = new(model: "gpt-4.1-nano", apiKey: configuration["OpenAI:Token"]!);
  }
  
  public async Task Execute()
  {
    var context = new List<ChatMessage>()
    {
      ChatMessage.CreateSystemMessage("stolicą Polski jest Kraków" ),
      ChatMessage.CreateSystemMessage("znana liczba z książki Autostopem przez Galaktykę to 69" ),
      ChatMessage.CreateSystemMessage("Aktualny rok to 1999" ),
      ChatMessage.CreateSystemMessage("Odpowiadaj tylko w języku polskim" ),
    };

    
    var ready = new VerificationRequest("READY", 0);
    Console.WriteLine($"User: {ready}");
    var response = await chatApi.Verify(ready);
    Console.WriteLine("Verify: {response}");
    
    while (true)
    {
      var msgId = response.MsgId;
      var messages = new List<ChatMessage>(context)
      {
        ChatMessage.CreateUserMessage(response.Text)
      };
      
      ChatCompletion aiResponse = await client.CompleteChatAsync(messages);
      Console.WriteLine($"AI: {aiResponse.Content[0].Text}");
      
      var request = new VerificationRequest(aiResponse.Content[0].Text, msgId);
      response = await chatApi.Verify(request);
      Console.WriteLine($"Verify {response.Text}");
    }
  }
}

public interface IChatApi
{
    [Post("/verify")]
    Task<VerificationResponse> Verify([Body] VerificationRequest request);
}

public sealed record VerificationRequest([property: JsonPropertyName("text")] string Text, [property: JsonPropertyName("msgID")]int MsgId);
public sealed record VerificationResponse([property: JsonPropertyName("text")] string Text, [property: JsonPropertyName("msgID")]int MsgId); 

