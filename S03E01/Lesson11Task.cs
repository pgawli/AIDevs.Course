using System.Text;
using AIDevs.Common;
using Microsoft.Extensions.Configuration;
using OpenAI.Chat;

public class Lesson11Task : Lesson
{
  public Lesson11Task(ICentralaApi api, IConfiguration configuration) : base(api, configuration)
  {
  }

  public override async ValueTask Execute()
  {
    await AnalyzeTextFiles();
  }

  private async ValueTask AnalyzeTextFiles()
  {
    var folderPath = Configuration["FactoryFilesFolder"]!;
    var reports = Directory.EnumerateFiles(folderPath, "*.txt", SearchOption.TopDirectoryOnly);
    var facts = Directory.EnumerateFiles(Path.Combine(folderPath, "facts"), "*.txt", SearchOption.TopDirectoryOnly);

    var builder = new StringBuilder(); 
    foreach (var fact in facts)
    {
      var fileName = Path.GetFileName(fact);
      var content = await File.ReadAllTextAsync(fact);
      builder.AppendLine($"Fakt {fileName} \n {content} \n");
    }
    
    var fakty = builder.ToString();

    builder.Clear();
    builder.AppendLine(fakty);

    var analiza = new Dictionary<string, string>();
    var saveFolder = Path.Combine(AppContext.BaseDirectory, "analiza");
    if (!Directory.Exists(saveFolder))
    {
      Directory.CreateDirectory(saveFolder);
    }
    
    foreach (var report in reports) 
    {
      var fileName = Path.GetFileName(report);
      var resultFile = Path.Combine(saveFolder, fileName);
      if (File.Exists(resultFile))
      {
        Console.WriteLine($"Plik {resultFile} już istnieje. Pomijam.");
        continue;
      }
      
      var content = await File.ReadAllTextAsync(report);
      var promptText = 
        $"Przeanalizuj tekst raportu oraz załaczonych faktów." +
        $"Znajdź fakty powiązane z raportem i wykorzystaj je do tworzenia listy słów kluczowych. " +
        $"Najczęstszym łącznikiem raprotu i faktów będą osoby wymienione w raporcie i w faktach. " +
        $"Wygeneruj listę słów kluczowych" +
        "- Słowa kluczowe muszą być w języku polskim" +
        "- Muszą być w mianowniku (np. \"nauczyciel\", \"programista\", a nie \"nauczyciela\", \"programistów\"" +
        "- Słowa powinny być oddzielone przecinkami (np. słowo1,słowo2,słowo3)." +
        "- Lista powinna precyzyjnie opisywać raport, uwzględniając treść raportu, powiązane fakty oraz informacje z nazwy pliku takie jak nazwa sektora." +
        "- Nazwa sektora która znajduje się w nazwie pliku to litera i cyfra np C4." +
        "- Umieść nazwe sektora na liście słów kluczowych go na liscie słów kluczowych." +
        "- Słów kluczowych może być dowolnie wiele dla danego raportu." +
        "Przedstaw wyniki w formie ciągu słów kluczonych oddzielonych przecinkami." +
        $"Raport {fileName} \n {content} \n" +
        $"Fakty:\n {fakty} \n";
      
      var prompt = new List<ChatMessage> { ChatMessage.CreateUserMessage(promptText) };
      var analyzer = new TextOrImageDescription(OpenAiToken);
      var response = await analyzer.AnalyzeText(prompt);
      if (string.IsNullOrEmpty(response))
      {
        Console.WriteLine($"Nie udało się przeanalizować raportu: {fileName}");
        continue;
      }
      
      await File.WriteAllTextAsync(resultFile, response);
      
      Console.WriteLine($"Analiza raportu {fileName} zakończona.");
      Console.WriteLine("====================================================");
      Console.WriteLine(response);
      
      analiza.Add(fileName, response);
    }
    
    var request = new AnswerDocuments( "dokumenty", ApiKey, analiza);
    var apiResponse = await Api.ReportDocs(request);
    if (apiResponse.IsSuccessStatusCode)
    {
      Console.WriteLine($"Dokumenty zostały pomyślnie przesłane do API. {apiResponse.Content}");
    }
    else
    {
      Console.WriteLine($"Błąd podczas przesyłania dokumentów: {apiResponse.Error.Content}");
    }
  }
}