using System.Text.Json;
using AIDevs.Common;
using Microsoft.Extensions.Configuration;
using OpenAI.Audio;
using OpenAI.Chat;

namespace S02E04;

public class Lesson9Task : Lesson
{
  public Lesson9Task(ICentralaApi api, IConfiguration configuration) : base(api, configuration)
  {
  }

  public override async ValueTask Execute()
  {
    var categorized = await ReadAndCategorizeInformation();
    var peoples = new List<string>();
    var hardware = new List<string>();
    
    var textResults = await ProcessTextFiles(categorized.texts);
    peoples.AddRange(textResults.people);
    hardware.AddRange(textResults.hardware);
    
    var audioResults = await ProcessAudioFiles(categorized.mp3);
    peoples.AddRange(audioResults.people);
    hardware.AddRange(audioResults.hardware);
    
    var imageResults = await ProcessImageFiles(categorized.png);
    peoples.AddRange(imageResults.people);
    hardware.AddRange(imageResults.hardware);
    
    var request = new Answer9Request("kategorie", ApiKey, new Answer9Content(peoples.ToArray(), hardware.ToArray()));
    
    var json = JsonSerializer.Serialize(request);
    await File.WriteAllTextAsync($".\\dane\\{Guid.NewGuid()}.json", json);
    
    var response = await Api.Report9(request);
    if (!response.IsSuccessful)
    {
      Console.WriteLine($"{response.StatusCode} {response.ReasonPhrase} - {response.Content}");
      return;
    }
    Console.WriteLine(response.Content);
  }

  private async ValueTask<(IEnumerable<string> people, IEnumerable<string> hardware)> ProcessImageFiles(IEnumerable<string> images)
  {
    var people = new List<string>();
    var hardware = new List<string>();
    
    var chatClient = new ChatClient(model: "gpt-4o", apiKey: OpenAiToken);
    
    foreach (var image in images)
    {
      Console.WriteLine($"Input: {image}");
      var imageBytes = await File.ReadAllBytesAsync(image);
      var bytes = new BinaryData(imageBytes);
      var contentType = "image/png";
      var imagePart = ChatMessageContentPart.CreateImagePart(bytes, contentType, ChatImageDetailLevel.Low);
      
      var picture = ChatMessage.CreateUserMessage(
        "Przeanalizuj ten obraz i powiedz czy zawiera informacje o ludziach czy o sprzęcie.\n" +
        "Interesują mnie informacje o schwytanych ludziach lub o śladach ich obecności oraz o naprawionych usterkach sprzetowych\n" +
        "Nie interesują mnie aktualizacje sprzętu i oprogramowania a tylko i wyłącznie naprawy .\n" +
        "Odpowiedz jednym słowem jeśli to informacja o ludziach użyj słowa people, jeśli o sprzęcie odpowiedz hardware w innym przypadku odpowiedz nie.",
        imagePart
      );

      var response = await chatClient.CompleteChatAsync(picture);
      CategorizeAiAnswer(response, people, hardware, Path.GetFileName(image));
    }
    
    return (people, hardware);;
  }

  private async ValueTask<(IEnumerable<string> people, IEnumerable<string> hardware)> ProcessTextFiles(IEnumerable<string> texts)
  {
    var people = new List<string>();
    var hardware = new List<string>();
    
    var chatClient = new ChatClient("gpt-4o", OpenAiToken);    
    foreach (var text in texts)
    {
      Console.WriteLine($"Input: {text}");
      var content = await File.ReadAllTextAsync(text);
      
      var messages = new List<ChatMessage>()
      {
        ChatMessage.CreateUserMessage("Przeanalizuj ten plik i powiedz czy zawiera informacje o ludziach czy o sprzęcie."),
        ChatMessage.CreateUserMessage("Interesują mnie informacje tylko o schwytanych ludziach lub o śladach ich obecności oraz o naprawionych usterkach hardwarowych.\n" +
                                      "Ignore the file if contains information about the mood and pizza delivery and situation when nobody was meet.\n" +
                                      "Odpowiedz jednym słowem jeśli to informacja o ludziach użyj słowa people, jeśli o sprzęcie odpowiedz hardware w innym przypadku odpowiedz nie."),
        ChatMessage.CreateUserMessage(content)
      };
      
      Console.WriteLine($"Input: {content}");
      ChatCompletion aiResponse = await chatClient.CompleteChatAsync(messages);
      Console.WriteLine($"AI: {aiResponse.Content[0].Text}");
      
      CategorizeAiAnswer(aiResponse, people, hardware, Path.GetFileName(text));
    }
    
    return (people, hardware);
  }

  private static void CategorizeAiAnswer(ChatCompletion aiResponse, List<string> people, List<string> hardware, string filename)
  {
    if (aiResponse.Content[0].Text.Contains("people", StringComparison.OrdinalIgnoreCase))
    {
      people.Add(filename);
      Console.WriteLine($"People: {filename}");
    }
    else if (aiResponse.Content[0].Text.Contains("hardware", StringComparison.OrdinalIgnoreCase))
    {
      hardware.Add(filename);
      Console.WriteLine($"Hardware: {filename}");
    }
    else
    {
      Console.WriteLine($"Nie rozpoznano typu pliku: {filename}");
    }
  }

  private async ValueTask<(IEnumerable<string> people, IEnumerable<string> hardware)> ProcessAudioFiles(IEnumerable<string> audios)
  {
    var people = new List<string>();
    var hardware = new List<string>();
    
    var audioClient = new AudioClient(model: "whisper-1", OpenAiToken);
    
    foreach (var audio in audios)
    {
      Console.WriteLine($"Input: {audio}");
      await using var stream = File.OpenRead(audio);
      var result = await audioClient.TranscribeAudioAsync(stream, Path.GetFileName(audio));
      var tempFile = Path.GetTempFileName();
      await File.WriteAllTextAsync(tempFile, result.Value.Text);
      var analyzed = await ProcessTextFiles([tempFile]);
      if (analyzed.people.Any())
      {
        people.AddRange(Path.GetFileName(audio));
      }
      else if (analyzed.hardware.Any())
      {
        hardware.AddRange(Path.GetFileName(audio));
      }
    }
    
    return (people, hardware);
  }

  private async ValueTask<(IEnumerable<string> texts, IEnumerable<string> mp3, IEnumerable<string> png, IEnumerable<string> other)> ReadAndCategorizeInformation()
  {
    var png = Directory.EnumerateFiles(".\\dane", "*.png", SearchOption.TopDirectoryOnly);
    var texts = Directory.EnumerateFiles(".\\dane", "*.txt", SearchOption.TopDirectoryOnly);
    var mp3 = Directory.EnumerateFiles(".\\dane", "*.mp3", SearchOption.TopDirectoryOnly);
    var other = Directory.EnumerateFiles(".\\dane", "*.", SearchOption.TopDirectoryOnly);
    return await ValueTask.FromResult((texts, mp3, png, other));
  }
}