using OpenAI.Chat;

namespace AIDevs.Common;

public class TextOrImageDescription
{
  private readonly ChatClient chatClient;

  public TextOrImageDescription(string openAiToken, string model = "gpt-4o")
  {
    chatClient = new ChatClient(model, apiKey: openAiToken);
  }

  public async Task<string[]> DescribePictures(string[] imageFilePaths, string outputFolder)
  {
    var result = new List<string>();
    foreach (var imageFilePath in imageFilePaths)
    {
      var fileName = Path.GetFileName(imageFilePath);
      var outputPath = Path.Combine(outputFolder, fileName + ".txt");
      if (File.Exists(outputPath))
      {
        Console.WriteLine($"File {outputPath} already exists. Skipping.");
        result.Add(outputPath);
        continue;
      }
      
      var desc = await Describe(imageFilePath);
      if (!string.IsNullOrEmpty(desc))
      {
        await File.WriteAllTextAsync(outputPath, desc);
        Console.WriteLine($"Description saved to {outputPath}");
        result.Add(outputPath);
      }
      else
      {
        Console.WriteLine($"Failed to describe image: {imageFilePath}");
      }
    }
    return result.ToArray();
  }
  
  public async Task<string> AnalyzeText(List<ChatMessage> prompt)
  {
    var response = await chatClient.CompleteChatAsync(prompt);
    return response != null ? response.Value.Content[0].Text : string.Empty;  
  }
  
  private async Task<string> Describe(string audioFilePath)
  {
    Console.WriteLine($"Image: {audioFilePath}");
    var imageBytes = await File.ReadAllBytesAsync(audioFilePath);
    var bytes = new BinaryData(imageBytes);
    var extension = Path.GetExtension(audioFilePath);
    var contentType = "image/" + extension.TrimStart('.');
    var imagePart = ChatMessageContentPart.CreateImagePart(bytes, contentType, ChatImageDetailLevel.Low);
    
    var prompt = ChatMessage.CreateUserMessage(
      "Opisz co znajduje sie na załączonym obrazie",
      imagePart
    );

    var response = await chatClient.CompleteChatAsync(prompt);
    return response != null ? response.Value.Content[0].Text : string.Empty;
  }
}