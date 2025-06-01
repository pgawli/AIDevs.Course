using System.Text.Json;
using AIDevs.Common;
using Microsoft.Extensions.Configuration;
using OpenAI.Chat;

namespace S03E04;

public class Lesson14Task : Lesson
{
  private readonly ChatClient chatClient;
  private string note = string.Empty;
  
  private readonly Queue<string> queueNames = new();
  private readonly Queue<string> queueCities = new();
  private readonly HashSet<Person> peoples = new();
  
  private HashSet<string> names = new();
  private HashSet<string> cities = new();
  
  
  public Lesson14Task(ICentralaApi api, IConfiguration configuration) : base(api, configuration)
  {
    chatClient = CreateChatClient();
  }

  public override async ValueTask Execute()
  {
    await GetNote();
    await GetNamesAndCities();
    
    foreach (var name in names)
    {
      var person = new Person(name);
      peoples.Add(person);
      queueNames.Enqueue(person.ApiName);
    }

    cities = cities.Select(x => x.ReplacePolishCharacters()).ToHashSet();
    foreach (var city in cities)
    {
      queueCities.Enqueue(city.ReplacePolishCharacters());
    }
    
    while (queueNames.Count > 0 || queueCities.Count > 0)
    {
      Console.WriteLine($"Before: Names queue: {queueNames.Count}, Cities queue: {queueCities.Count}");
      await QueryNames();
      await QueryCities();
      Console.WriteLine($"After: Names queue: {queueNames.Count}, Cities queue: {queueCities.Count}");
    }
    var json = JsonSerializer.Serialize(peoples, new JsonSerializerOptions
    {
      WriteIndented = true,
      PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    });
    Console.WriteLine(json);
    
    var barbara = peoples.FirstOrDefault(p => p.ApiName == "BARBARA");
    var oneCity = barbara?.Cities.FirstOrDefault(x => x != "WARSZAWA" && x != "KRAKOW");
    
    // var citiesTested = new List<string>(cities);
    // var bannedCities = new List<string>();
    // do
    // {
    //   var oneCity = await FoundNewCity(cities);
    Console.WriteLine($"Found {oneCity} cities with 'BARBARA' in their name.");
    var answer = await Api.Report(new AnswerRequest("loop", ApiKey, oneCity));
    if (!answer.IsSuccessful)
    {
      Console.WriteLine($"Error reporting answer: {answer.Error?.Content ?? answer.StatusCode.ToString()}");
    }
    else
    {
      Console.WriteLine($"{answer.Content}");
    }
  }

  private async Task GetNote()
  {
    var response = await Api.GetBarbaraNote();
    if (!response.IsSuccessful)
    {
      Console.WriteLine($"Error getting note: {response.Error?.Content ?? response.StatusCode.ToString()}");
      throw new Exception("Error getting note");
    }
    note = response.Content;
  }
  private async Task QueryCities()
  {
    Console.WriteLine("Querying cities...");
    
    while (queueCities.TryDequeue(out var city))
    {
      var response = await Api.PostPlaces(new SimpleQuery(ApiKey, city));
      if (!response.IsSuccessful)
      {
        Console.WriteLine($"Error getting results from Centrala: {response.Error?.Content ?? response.StatusCode.ToString()}");
      }
      else
      {
        Console.WriteLine($"{city} - {response.Content}");

        if (response.Content.Message.Contains("RESTRICTED")) continue;

        var namesInCity = response.Content.Message.Split(' ');

        foreach (var name in namesInCity)
        {
          var person = peoples.FirstOrDefault(p => p.ApiName == name);
          if (person == null)
          {
            person = new Person(name);
            peoples.Add(person);
            queueNames.Enqueue(person.ApiName);
          }
          person.AddCity(city);          
        }
      }
    }
  }

  private async Task QueryNames()
  {
    Console.WriteLine("Querying names...");
    
    while (queueNames.TryDequeue(out var apiName))
    {
      var response = await Api.PostPeople(new SimpleQuery(ApiKey, apiName));
      if (!response.IsSuccessful)
      {
        Console.WriteLine($"Error getting results from Centrala: {response.Error?.Content ?? response.StatusCode.ToString()}");
      }
      else
      {
        if (response.Content.Message.Contains("RESTRICTED")) continue;
        var person = peoples.First(p => p.ApiName == apiName);
        var cityNames = response.Content.Message.Split(' ');  
        Console.WriteLine($"{person.ApiName} - {response.Content}");
        foreach (var name in cityNames)
        {
          person.AddCity(name);
          if (!cities.Contains(name))
          {
            queueCities.Enqueue(name);
            cities.Add(name);
          }
        }
      }
    }
  }

  private async Task GetNamesAndCities()
  {
    if (File.Exists("names_cities.txt"))  
    {
      var lines = await File.ReadAllLinesAsync("names_cities.txt");
      var names0 = lines[0].Split(',');
      var cities0 = lines[1].Split(',');
      names = new HashSet<string>(names0);
      cities = new HashSet<string>(cities0);
      return;
    }
    
    var prompt = new List<ChatMessage>()
    {
      ChatMessage.CreateUserMessage("Extract names and cities from the following text:"),
      ChatMessage.CreateUserMessage(note),
      ChatMessage.CreateUserMessage("Utwórz osobną listę imion i miast, oddzielając je przecinkami.\n" + 
                                    "Imiona i miasta muszą być w mianowniku. W odpowiedzi mają byc tylko listy, bez dodatkowego tekstu, pomiń nazwiska."),
    };
    var response = await chatClient.CompleteChatAsync(prompt);
    Console.WriteLine(response.Value.Content[0].Text);
    var answers = response.Value.Content[0].Text.Split('\n',StringSplitOptions.RemoveEmptyEntries)
                              .Select(line => line.Trim())
                              .Where(x => !string.IsNullOrWhiteSpace(x))
                              .ToArray();
    
    string line1 = string.Join(',', answers[0]);
    string line2 = string.Join(',', answers[1]);
    await File.WriteAllLinesAsync("names_cities.txt", [line1, line2]);
 
  }
}


public static class Latinaize
{
  public static string ReplacePolishCharacters(this string input) =>
    input.Trim()
      .ToLower()
      .Replace("ą", "a")
      .Replace("ć", "c")
      .Replace("ę", "e")
      .Replace("ł", "l")
      .Replace("ń", "n")
      .Replace("ó", "o")
      .Replace("ś", "s")
      .Replace("ź", "z")
      .Replace("ż", "z")
      .ToUpper();
}

public class Person
{
  private readonly string name;
  private readonly string apiName;
  private readonly HashSet<string> cities = new HashSet<string>();
  
  public Person(string name)
  {
    this.name = name.Trim();
    apiName = this.name.ReplacePolishCharacters();
  }
  
  public string Name => name;
  public string ApiName => apiName;
  public IReadOnlyCollection<string> Cities => cities;
  
  public void AddCity(string city)
  {
    cities.Add(city);
  }
};
