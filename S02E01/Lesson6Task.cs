using System.Net.Http.Json;
using System.Text.Json.Serialization;
using AIDevs.Common;
using Microsoft.Extensions.Configuration;
using OpenAI.Audio;
using OpenAI.Chat;
using Refit;

namespace S02E01;

public class Lesson6Task
{
  private readonly ICentralaApi lesson6Api;
  private readonly AudioClient audioClient;
  private readonly ChatClient chatClient;
  private readonly string apiKey;
  private List<ChatMessage> context = new ();
  
  public Lesson6Task(IConfiguration configuration, ICentralaApi lesson6Api)
  {
    this.lesson6Api = lesson6Api;
    audioClient = new AudioClient(model: "whisper-1", apiKey: configuration["OpenAI:Token"]!);
    chatClient = new ChatClient(model: "gpt-4o", apiKey: configuration["OpenAI:Token"]!);
    apiKey = configuration["ApiKey"]!;
  }
  
  public async ValueTask Execute()
  {
    // await TranscriptAudioFiles();
    await QueryAi();
  }

  private async ValueTask QueryAi()
  {
    context.AddRange(
     new UserChatMessage("Na podstawie transkrypcji ustal, na jakiej ulicy znajduje się instytut uczelni, w którym wykłada profesor Andrzej Maj."),
      new UserChatMessage("Analizuj dokładnie treść transkrypcji i wyciągaj wnioski."),
      new UserChatMessage("Jeśli w transkrypcjach nie ma bezpośredniej odpowiedzi, użyj swojej wiedzy na temat tej konkretnej uczelni, aby ustalić nazwę ulicy."), 
      new UserChatMessage("Pamiętaj, że chodzi o ulicę, na której znajduje się instytut, a nie główna siedziba uczelni. Odpowiedz tylko nazwą ulicy."),
      new UserChatMessage("Transkrypcja do analizy znajduje sie poniżej.")
    );
    CreateContext();
    
    var chatResponse = await chatClient.CompleteChatAsync(context);
    if (chatResponse == null)
    {
      throw new Exception("The chat response could not be retrieved.");
    }
    
    var answer = chatResponse.Value.Content[0].Text!;
    
    var request = new AnswerRequest("mp3", apiKey, answer);
    var response = await lesson6Api.Report(request);
    if (!response.IsSuccessful)
    {
      Console.WriteLine($"{response.StatusCode} {response.ReasonPhrase} - {response.Content}");
      throw new Exception("The answer could not be posted.");
    }
    if (response.Content == null)
    {
      throw new Exception("Empty response.");
    }
    
    if (response.Content.Code == 0)
    {
      Console.WriteLine($"Answer: {response.Content.Message}");
    }
    else
    {
      Console.WriteLine($"Answer: {response.Content}");
    }
  }

  private void CreateContext()
  {
    var dataDir = Path.Combine(AppContext.BaseDirectory, "data", "text");
    if (!Directory.Exists(dataDir))
    {
      Directory.CreateDirectory(dataDir);
    }
    
    var textFiles = Directory.GetFiles(dataDir, "*.txt", SearchOption.TopDirectoryOnly)
      .ToList();
    
    foreach (var file in textFiles)
    {
      var content = File.ReadAllText(file);
      context.Add(ChatMessage.CreateUserMessage(content));
    }
  }

  private async ValueTask TranscriptAudioFiles()
  {
    var dataDir = Path.Combine(AppContext.BaseDirectory, "data");
    var audioFiles = Directory.GetFiles(dataDir, "*.m4a", SearchOption.TopDirectoryOnly)
      .ToList();

    foreach (var file in audioFiles)
    {
      var transcriptFile = Path.Combine(dataDir, "text", $"{Path.GetFileNameWithoutExtension(file)}.txt");
      if (File.Exists(transcriptFile))
      {
        continue;
      }
      Console.WriteLine($"Źródło: {file}");
      await using var stream = File.OpenRead(file);
      var result = await audioClient.TranscribeAudioAsync(stream, Path.GetFileName(file));
      Console.WriteLine($"Wynik: {transcriptFile}");
      File.WriteAllText(transcriptFile, result.Value.Text);
    }
  }
}


