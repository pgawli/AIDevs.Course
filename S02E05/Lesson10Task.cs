using AIDevs.Common;
using Microsoft.Extensions.Configuration;
using OpenAI.Chat;

public class Lesson10Task : Lesson
{
  private readonly string workspaceFolder = Path.Combine(AppContext.BaseDirectory, "workspace");
  private readonly LinkDownloader linkDownloader;
  private readonly AudioTranscript audioTranscript;
  private readonly TextOrImageDescription textOrImageDescription;
  
  public Lesson10Task(ICentralaApi api, IConfiguration configuration) : base(api, configuration)
  {
    audioTranscript = new AudioTranscript(OpenAiToken);
    linkDownloader = new LinkDownloader(workspaceFolder);
    textOrImageDescription = new TextOrImageDescription(OpenAiToken);
  }

  public override async ValueTask Execute()
  {
    if (!Directory.Exists(workspaceFolder))
    {
      Directory.CreateDirectory(workspaceFolder);
    }

    var url = "https://c3ntrala.ag3nts.org/dane/arxiv-draft.html";
    var file = Path.Combine(workspaceFolder, "arxiv-draft.html");
    var htmlContent = await DownloadHtmDocument(file, url);

    var questions = await DownloadQuestions();

    Console.WriteLine("Extracting links ...");
    var links = HtmlTools.ExtractLinksFromHtml(htmlContent).ToArray();
    var downloadedLinks = await linkDownloader.DownloadLinks(links, url);

    await TranscriptAudioFiles(downloadedLinks);

    Console.WriteLine("Extracting images ...");
    var images = HtmlTools.ExtractImagesFromHtml(htmlContent).ToArray();
    var downloadedImages = await linkDownloader.DownloadLinks(images, url);
    var withPath = downloadedImages.Select(x => Path.Combine(workspaceFolder, x)).ToArray();
    var imageDescriptions = await textOrImageDescription.DescribePictures(withPath, workspaceFolder);

    var md = ConvertHtmlToMarkdown(htmlContent, url, new Dictionary<string, string>(),
      new Dictionary<string, string>());
    var updatedMd = ReplaceImagesWithDescriptions(md, imageDescriptions);
    updatedMd = ReplaceAudioWithTranscripts(updatedMd, downloadedLinks);

    var outputFile = Path.Combine(workspaceFolder, "arxiv-draft.md");
    await File.WriteAllTextAsync(outputFile, updatedMd);

    List<ChatMessage> prompt = CreatePrompt(updatedMd, questions);
    var answer = await textOrImageDescription.AnalyzeText(prompt);
    var answerFile = Path.Combine(workspaceFolder, $"answer_{Guid.CreateVersion7()}.txt");
    await File.WriteAllTextAsync(answerFile, answer);

    // var answers = new Dictionary<string, string>();
    // answers.Add("01", "Odpowiedź 1");
    // answers.Add("02", "Odpowiedź 2");
    // answers.Add("03", "Odpowiedź 3");
    //
    // var request = new AnswerArxiv("arxiv", apiKey, answers);
    // var response = await api.ReportArxiv(request);
    // if (!response.IsSuccessful)
    // {
    //   Console.WriteLine($"{response.StatusCode} {response.ReasonPhrase}\n{response.Error.Content}");
    //   return;
    // }
  }



  private List<ChatMessage> CreatePrompt(string updatedMd, Dictionary<string, string> questions)
  {
    var list = new List<ChatMessage>();
    list.Add(ChatMessage.CreateUserMessage("Przeanalizuj poniższy tekst w formacie Markdown:"));
    list.Add(ChatMessage.CreateUserMessage("początek tekstu"));
    list.Add(ChatMessage.CreateUserMessage(updatedMd));
    list.Add(ChatMessage.CreateUserMessage("koniec tekstu"));
    list.Add(ChatMessage.CreateUserMessage("oraz odpowiedz na poniższe pytania:"));
    foreach (var question in questions)
    {
      list.Add(ChatMessage.CreateUserMessage($"{question.Key}. {question.Value}"));  
    }

    return list;
  }

  private string ReplaceAudioWithTranscripts(string updated, string[] downloadedLinks)
  {
    var modified = updated;
    foreach (var descriptionFile in downloadedLinks)
    {
      var fullPath = Path.Combine(workspaceFolder, descriptionFile + ".txt");
      var originalFileName = Path.GetFileNameWithoutExtension(descriptionFile);
      Console.WriteLine($"Replacing {originalFileName} with {descriptionFile}");
      
      var audioStart = modified.IndexOf("<audio controls>", StringComparison.OrdinalIgnoreCase);
      var audioEnd = modified.IndexOf("</audio>", StringComparison.OrdinalIgnoreCase);
      if (audioStart == -1 || audioEnd == -1 || audioStart > audioEnd)
      {
        Console.WriteLine($"Audio tag not found for {originalFileName}");
        break;
      }
      modified = modified.Remove(audioStart, audioEnd - audioStart + "</audio>".Length);
      var descriptionContent = File.ReadAllText(fullPath);
      
      var link = $"[{descriptionFile}]";
      var linkStart = modified.IndexOf(link, StringComparison.OrdinalIgnoreCase);
      var linkEnd = modified.IndexOf(")", linkStart, StringComparison.OrdinalIgnoreCase);
      if (linkStart == -1 || linkEnd == -1 || linkStart > linkEnd)
      {
        Console.WriteLine($"Link tag not found for {originalFileName}");
        break;
      }
      modified = modified.Remove(linkStart, linkEnd - linkStart + 1);
      modified = modified.Insert(linkStart, $"Transkrypcja pliku audio:\n{descriptionContent}\nKoniec transkrypcji");
      
    }
    
    return modified;
  }


  private string ReplaceImagesWithDescriptions(string md, string[] imageDescriptions)
  {
    var modified = md;
    foreach (var description in imageDescriptions)
    {
      var originalFileName = Path.GetFileNameWithoutExtension(description);
      Console.WriteLine($"Replacing {originalFileName} with {description}");
      var figureStart = modified.IndexOf("<figure>", StringComparison.OrdinalIgnoreCase);
      var figureEnd = modified.IndexOf("</figure>", StringComparison.OrdinalIgnoreCase);
      if (figureStart == -1 || figureEnd == -1 || figureStart > figureEnd)
      {
        Console.WriteLine($"Figure tag not found for {originalFileName}");
        break;
      }
      var figure = modified.Substring(figureStart, figureEnd - figureStart + "</figure>".Length);
      var captionStart = figure.IndexOf("<figcaption>", StringComparison.OrdinalIgnoreCase);
      var captionEnd = figure.IndexOf("</figcaption>", StringComparison.OrdinalIgnoreCase);
      if (captionStart == -1 || captionEnd == -1 || captionStart > captionEnd)
      {
        Console.WriteLine($"Caption tag not found for {originalFileName}");
        break;
      }
      var descriptionContent = File.ReadAllText(description);
      var fix = "<figcaption>".Length;
      var caption = figure.Substring(captionStart + fix, captionEnd - captionStart - fix);
      modified = modified.Remove(figureStart, figureEnd - figureStart + "</figure>".Length);
      var content = $"Tytuł zdjęcia: {caption}\n" +
                    $"Opis zdjęcia: {descriptionContent}\n";
      Console.WriteLine($"Replacing {figure} with {content}");
      modified = modified.Insert(figureStart, content);
    }
    
    return modified;
  }

  private async ValueTask<string> DownloadHtmDocument(string file, string url)
  {
    var htmlContent = string.Empty;
    if (!File.Exists(file))
    {
      Console.WriteLine($"File {file} does not exist. Downloading ...");

      var response = await Api.GetArxivDraft();
      if (!response.IsSuccessStatusCode)
      {
        Console.WriteLine($"Failed to download file: {response.StatusCode}");
        return htmlContent;
      }
    
      htmlContent = response.Content;
      await File.WriteAllTextAsync(file, htmlContent);
      Console.WriteLine($"File downloaded to {file}");
    }
    else
    {
      Console.WriteLine($"File {file} already exists. Skipping download.");
      htmlContent = await File.ReadAllTextAsync(file);
    }

    return htmlContent;
  }

  private async ValueTask<Dictionary<string, string>> DownloadQuestions()
  {
    Console.WriteLine("Downloading questions...");
    var result = new Dictionary<string, string>();  
    var response = await Api.GetArxiv(ApiKey);
    if (!response.IsSuccessful)
    {
      Console.WriteLine($"{response.StatusCode} {response.ReasonPhrase} - {response.Content}");
      return result;
    }
    var questions = response.Content;
    Console.WriteLine(questions);
    var questionsList = questions.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
    foreach (var question in questionsList)
    {
      var parts = question.Split(['='], 2);
      if (parts.Length == 2)
      {
        var key = parts[0].Trim();
        var value = parts[1].Trim();
        result[key] = value;
      }
    }
    return result;
  }

  private async Task TranscriptAudioFiles(string[] links)
  {
    foreach (var link in links)
    {
      var ext = Path.GetExtension(link);
      if (ext != ".mp3" && ext != ".wav")
      {
        Console.WriteLine($"Skipping audio transcript... {link}");
        continue;
      }
      var filename = Path.GetFileName(link);
      var outputFile = Path.Combine(workspaceFolder, $"{filename}.txt");
      if (File.Exists(outputFile))
      {
        Console.WriteLine($"File {outputFile} already exists. Skipping.");
        continue; 
      }

      var inputFile = Path.Combine(workspaceFolder, Path.GetFileName(link));
      var result = await audioTranscript.Convert(inputFile);
      if (!string.IsNullOrEmpty(result))
      {
        await File.WriteAllTextAsync(outputFile, result);
        Console.WriteLine($"File {outputFile} saved.");
      }
    }
  }

  private string ConvertHtmlToMarkdown(string htmlContent, string baseUrl, 
    Dictionary<string, string> imageMappings, Dictionary<string, string> linkMappings)
  {
    // Załaduj HTML do dokumentu
    var htmlDoc = new HtmlAgilityPack.HtmlDocument();
    htmlDoc.LoadHtml(htmlContent);

    // Znajdź główną treść (możesz dostosować selektor)
    var mainContent = htmlDoc.DocumentNode.SelectSingleNode("//body") ?? htmlDoc.DocumentNode;

    // Zamień odniesienia do obrazów na lokalne ścieżki
    var imgNodes = mainContent.SelectNodes("//img[@src]");
    if (imgNodes != null)
    {
      foreach (var imgNode in imgNodes)
      {
        var src = imgNode.GetAttributeValue("src", "");
        if (!string.IsNullOrEmpty(src) && imageMappings.TryGetValue(src, out var localPath))
        {
          // Używamy relatywnej ścieżki
          imgNode.SetAttributeValue("src", Path.GetFileName(localPath));
        }
      }
    }

    // Zamień odniesienia do linków na lokalne ścieżki
    var linkNodes = mainContent.SelectNodes("//a[@href]");
    if (linkNodes != null)
    {
      foreach (var linkNode in linkNodes)
      {
        var href = linkNode.GetAttributeValue("href", "");
        if (!string.IsNullOrEmpty(href) && linkMappings.TryGetValue(href, out var localPath))
        {
          // Używamy relatywnej ścieżki
          linkNode.SetAttributeValue("href", Path.GetFileName(localPath));
        }
      }
    }

    // Użyj ReverseMarkdown do konwersji HTML na Markdown
    // Możesz dodać NuGet package: dotnet add package ReverseMarkdown
    var converter = new ReverseMarkdown.Converter();
    var markdown = converter.Convert(mainContent.OuterHtml);
    
    return markdown;
  }
}