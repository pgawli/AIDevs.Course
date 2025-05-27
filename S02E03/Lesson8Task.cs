using System.Text;
using AIDevs.Common;
using Microsoft.Extensions.Configuration;
using OpenAI.Images;

namespace S02E03;

public class Lesson8Task : Lesson
{
  public Lesson8Task(ICentralaApi api, IConfiguration configuration) : base(api, configuration)
  {
  }

  public override async ValueTask Execute()
  {
    var robotDescription = await DownloadRobotDescription();
    var prompt = CreatePrompt(robotDescription);
    var robotUri = await GenerateImage(prompt);
    
    var answer = new AnswerRequest("robotid", ApiKey, robotUri.AbsoluteUri);
    var response = await Api.Report(answer);

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

  private string CreatePrompt(string robotDescription)
  {
    return new StringBuilder().Append("Używając opisu wygeneruj obraz robota,\n")
      .Append("pomiń wszystkie inne informacje, które nie są związane z wyglądem robotem,\n")
      .Append("Na obrazku ma być tylko robot, nie dodawaj żadnych dodatkowych opisów\n")
      .Append("Opis robota:")
      .Append(robotDescription)
      .ToString();
  }

  private async Task<Uri> GenerateImage(string robotDescription)
  {
    Console.WriteLine("Generating image...");
    ImageClient client = new("dall-e-3", OpenAiToken);
    ImageGenerationOptions options = new()
    {
      Quality = GeneratedImageQuality.Standard,
      Size = GeneratedImageSize.W1024xH1024,
      Style = GeneratedImageStyle.Natural,
      ResponseFormat = GeneratedImageFormat.Uri,
    };
    GeneratedImage image = await client.GenerateImageAsync(robotDescription, options);
    return image.ImageUri;
    
    // BinaryData bytes = image.ImageBytes;
    // var filename = $"c:\\temp\\{Guid.NewGuid()}.png";
    // await using FileStream stream = File.OpenWrite(filename);
    // await bytes.ToStream().CopyToAsync(stream);
    // Console.WriteLine($"Obrazek wygenerowany. {filename}");
    // return filename;
  }

  private async Task<string> DownloadRobotDescription()
  {
    var response = await Api.GetRobot(ApiKey);
    if (!response.IsSuccessful)
    {
      Console.WriteLine($"{response.StatusCode} {response.ReasonPhrase} - {response.Content}");
      throw new Exception("Robot description could not be retrieved.");
    }
    Console.WriteLine(response.Content.RobotId);
    return response.Content.RobotId;
  }
}