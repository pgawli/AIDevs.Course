using AIDevs.Common;
using Microsoft.Extensions.Configuration;
using OpenAI.Chat;

namespace S02E02;

public class Lesson7Task : Lesson
{
  private readonly ChatClient chatClient;
  

  public Lesson7Task(ICentralaApi api, IConfiguration configuration) : base(api, configuration)
  {
    chatClient = new ChatClient(model: "gpt-4o", apiKey: OpenAiToken);
  }

  public override async ValueTask Execute()
  {
    await RecognizePictures();
  }

  private async Task RecognizePictures()
  {
    var pictures = Directory
      .EnumerateFiles(Path.Combine(AppContext.BaseDirectory, "dane"), "*.png", SearchOption.TopDirectoryOnly).ToList();

    var prompt = new List<ChatMessage>();
    prompt.Add(ChatMessage.CreateUserMessage("Poniżej załaczam opisy czterech fragmentów mapy."));
    prompt.Add(ChatMessage.CreateUserMessage("Jeden z nich nie będzie pasował do reszty i należy go pominąć."));
    prompt.Add(ChatMessage.CreateUserMessage("Na podstawie pasujących do siebie fragmentów mapy podaj nazwę miasta którego ta mapa dotyczy."));
    prompt.Add(ChatMessage.CreateUserMessage("Odpowiedz tylko nazwą miasta."));

    var i = 1;
    foreach (var file in pictures)
    {
      var imageBytes = await File.ReadAllBytesAsync(file);
      var d = new BinaryData(imageBytes);
      var imagePart = ChatMessageContentPart.CreateImagePart(d, "image/png", ChatImageDetailLevel.Low);;
      
      var message = ChatMessage.CreateUserMessage(
        "Co znajduje się na tym obrazku? Opisz dokładnie zawartość.",
        imagePart
      );

      var context = new List<ChatMessage> { message };
      context.AddRange(message);
      var response = await chatClient.CompleteChatAsync(context);

      if (response != null)
      {
        prompt.Add($"To jest fragment nr {i}");
        prompt.Add(ChatMessage.CreateUserMessage(response.Value.Content[0].Text));
        var result = response.Value.Content[0].Text;
        Console.WriteLine($"Analiza obrazu {Path.GetFileName(file)}: {result}");
      }
      i++;
    }
    
    var final = await chatClient.CompleteChatAsync(prompt);
    if (final != null)
    {
      Console.WriteLine($"Odpowiedź: {final.Value.Content[0].Text}");
    }    
  }
}