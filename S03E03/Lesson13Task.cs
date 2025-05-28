using System.Security.AccessControl;
using System.Text.Json;
using System.Text.Json.Serialization;
using AIDevs.Common;
using Microsoft.Extensions.Configuration;
using OpenAI.Chat;

public class Lesson13Task : Lesson
{
  private readonly ChatClient chatClient;
  
  public Lesson13Task(ICentralaApi api, IConfiguration configuration) : base(api, configuration)
  {
    chatClient = CreateChatClient();
  }

  public override async ValueTask Execute()
  {
    var tableNames = await GetTableNames();
    var tableStructures = await GetTableStructures(tableNames);
    var sql = await GenerateQuery(tableStructures);
    string[] queryResult = await FetchResults(sql);
    
    var answer = new ListAnswerRequest("database", ApiKey, queryResult);
    var response = await Api.PostAnswer(answer);
    if (response.IsSuccessful)
    {
      Console.WriteLine("Results successfully saved.");
      Console.WriteLine(response.Content);
    }
    else
    {
      Console.WriteLine($"Error saving results: {response.Error?.Content ?? response.StatusCode.ToString()}");
    }
  }

  private async Task<string[]> FetchResults(string sql)
  {
    var response = await Api.PostQuery(new Query(
      Task: "database",
      ApiKey: ApiKey,
      QueryText: sql
    ));

    if (response.IsSuccessful)
    {
      Console.WriteLine(response.Content);      
      var reply = JsonSerializer.Deserialize<DatabaseQueryResponse>(response.Content)!;
      return reply.Reply.Select(x => x.DcId).ToArray();    
    }
    Console.WriteLine($"Error fetching results: {response.Error?.Content ?? response.StatusCode.ToString()}");
    return [];
  }

  private async Task<string> GenerateQuery(Dictionary<string, ColumnInfo[]> tableStructures)
  {
    var managerQueryFile = Path.Combine(AppContext.BaseDirectory, "managers.sql");
    if (File.Exists(managerQueryFile))
    {
      Console.WriteLine("managers.sql exists");
      var content = await File.ReadAllTextAsync(managerQueryFile);
      return content;
    }
    
    var structures = JsonSerializer.Serialize(tableStructures);
    var prompt = new List<ChatMessage>()
    {
      ChatMessage.CreateUserMessage("Znając strukturę tabel bazy danych wygeneruj zapytanie SQL które zwróci kolumnę DC_ID których użytkownicy są nieaktywni."),
      ChatMessage.CreateUserMessage(structures),
      ChatMessage.CreateUserMessage("Zwróć tylko sam tekst zapytania SQL bez żadnych dodatkowych informacji ani formatowania Markdown.")
    };
    
    var result = await chatClient.CompleteChatAsync(prompt);
    Console.WriteLine(result.Value.Content[0].Text);

    var sql = result.Value.Content[0].Text;

    await File.WriteAllTextAsync(managerQueryFile, sql);
    
    return sql;
  }

  private async Task<Dictionary<string, ColumnInfo[]>> GetTableStructures(string[] tableNames)
  {
    var tableStructuresFile = Path.Combine(AppContext.BaseDirectory, "tables_structures.json");
    if (File.Exists(tableStructuresFile))
    {
      Console.WriteLine("tables_structures.json exists");
      var content = await File.ReadAllTextAsync(tableStructuresFile);
      var result = JsonSerializer.Deserialize<Dictionary<string, ColumnInfo[]>>(content);
      if (result != null && result.Count == tableNames.Length)
      {
        Console.WriteLine("Loaded table structures from file.");
        return result;
      }
    }

    var newResult = new Dictionary<string, ColumnInfo[]>();

    foreach (var tableName in tableNames)
    {

      var response = await Api.PostQuery(new Query(
        Task: "database",
        ApiKey: ApiKey,
        QueryText: $"SHOW CREATE TABLE {tableName};"
      ));
      
      if (response.IsSuccessStatusCode)
      {
        Console.WriteLine(response.Content);
        var prompt = new List<ChatMessage>()
        {
          ChatMessage.CreateUserMessage("Z danych w formacie JSON, wybierz nazwy i typy kolumn w bazie dabych."),
          ChatMessage.CreateUserMessage(response.Content),
          ChatMessage.CreateUserMessage(
            "Zwróć wynik w tablicy formacie JSON zawierającej nazwę i typ kolumny wg schematu [{ col: nazwa_kolumny, type: typ_kolumny }].\n" +
            "W typie kolumny ważna jest tylko informacja o typie kolumny, bez dodatkowych informacji."),
          ChatMessage.CreateUserMessage(
          "Zwróć tylko sam json bez żadnych dodatkowych informacji.")
        };
        var result = await chatClient.CompleteChatAsync(prompt);
        Console.WriteLine(result.Value.Content[0].Text);
        var json = result.Value.Content[0].Text.Replace("```json", "").Replace("```", "").Trim();
        var fields = JsonSerializer.Deserialize<ColumnInfo[]>(json);
        newResult.Add(tableName, fields!);
      }
      else
      {
        Console.WriteLine($"Error retrieving structure for table {tableName}: {response.Error?.Content ?? response.StatusCode.ToString()}");
      }
    }
    
    await File.WriteAllTextAsync(tableStructuresFile, JsonSerializer.Serialize(newResult));

    return newResult;
  }

  private async Task<string[]> GetTableNames()
  {
    var tableListFile = Path.Combine(AppContext.BaseDirectory, "tables.txt");
    if (File.Exists(tableListFile))
    {
      Console.WriteLine("tables.txt exists");
      var content = await File.ReadAllTextAsync(tableListFile);
      return content.Split(',', StringSplitOptions.RemoveEmptyEntries)
        .Select(t => t.Trim()).ToArray();
    }
    
    var response = await Api.PostQuery(new Query(
      Task: "database",
      ApiKey: ApiKey,
      QueryText: "SHOW TABLES;"
    ));

    if (response.IsSuccessStatusCode)
    {
      var prompt = new List<ChatMessage>()
      {
        ChatMessage.CreateUserMessage("Z danych w formacie JSON, wypisz nazwy tabel w bazie danych."),
        ChatMessage.CreateUserMessage(response.Content),
        ChatMessage.CreateUserMessage(
          "Zwróć wynik w formie ciągu tekstowego gdzie nazwey tabel będą oddzielone przecinkami.")
      };
      var result = await chatClient.CompleteChatAsync(prompt);
      if (result != null)
      {
        Console.WriteLine("Response from OpenAI:");
        Console.WriteLine(result.Value.Content[0].Text);
        var tables = result.Value.Content[0].Text.Split(',', StringSplitOptions.RemoveEmptyEntries);
        await File.WriteAllTextAsync(tableListFile, result.Value.Content[0].Text);
        return tables;
      }
    }
    Console.WriteLine(response.Error?.Content ?? $"Error retrieving table names. Status code: {response.StatusCode}");
    return [];
  }
}

public record ColumnInfo([property: JsonPropertyName("col")]string Name, [property: JsonPropertyName("type")]string Type);

public record DatabaseQueryResponse(
  [property: JsonPropertyName("reply")] DcIdRecord[] Reply,
  [property: JsonPropertyName("error")] string Error
);

public record DcIdRecord(
  [property: JsonPropertyName("dc_id")] string DcId
);