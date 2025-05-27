using System.Runtime.InteropServices;
using AIDevs.Common;
using Google.Protobuf.Collections;
using Microsoft.Extensions.Configuration;
using OpenAI.Embeddings;
using Qdrant.Client;
using Qdrant.Client.Grpc;

public class Lesson12Task : Lesson
{
  private QdrantClient qdrantClient;
  private readonly string collectionName = "weapon_reports";
  private List<WeaponReportEmbedding>? embedding = new List<WeaponReportEmbedding>();
  
  public Lesson12Task(ICentralaApi api, IConfiguration configuration) : base(api, configuration)
  {
    qdrantClient = CreateQdrantClient();
  }

  public override async ValueTask Execute()
  {
    await CreateOrLoadEmbedding();
    const string question = "W raporcie, z którego dnia znajduje się wzmianka o kradzieży prototypu broni?";
    var model = new EmbeddingClient("text-embedding-3-large", OpenAiToken);
    var questionEmbedding = await model.GenerateEmbeddingAsync(question);
    var questionVector = questionEmbedding.Value.ToFloats().ToArray();


    var results = await qdrantClient.SearchAsync(collectionName, questionVector, limit: 1);
          
    Console.WriteLine($"Znaleziono {results.Count} wyników:");
    foreach (var result in results)
    {
      var filename = result.Payload["filename"].ToString();
      var date = result.Payload["date"].StringValue;
      
      Console.WriteLine($"Plik: {filename}, Data: {date}");
      
      var response = await Api.Report(new AnswerRequest("wektory", ApiKey, date));
      if (response.IsSuccessStatusCode)
      {
        var content = response.Content;
        Console.WriteLine($"Zawartość raportu: {content}");
      }
      else
      {
        Console.WriteLine($"Błąd podczas pobierania raportu: {response.Error.Content}");
      }
    }
  }

  private async ValueTask CreateOrLoadEmbedding()
  {
    var embeddingPath = Path.Combine(AppContext.BaseDirectory, "weapon_reports_embedding.json");
    if (File.Exists(embeddingPath))
    {
      var embeddingJson = await File.ReadAllTextAsync(embeddingPath);
      embedding = System.Text.Json.JsonSerializer.Deserialize<List<WeaponReportEmbedding>>(embeddingJson);
      if (embedding != null && embedding.Count > 0)
      {
        Console.WriteLine($"Wczytano {embedding.Count} raportów z pliku {embeddingPath}.");
        
        await qdrantClient.DeleteCollectionAsync(collectionName);
        await qdrantClient.CreateCollectionAsync(collectionName,  new VectorParams { Size = 3072, Distance = Distance.Dot });

        var points = new List<PointStruct>();
        ulong id = 1;
        foreach (var file in embedding)
        {
          var point = new PointStruct
          {
            Id = id++,
            Vectors = file.Vector,
            Payload =
            {
              ["filename"] = file.Name,
              ["date"] = file.Date
            }
          };

          points.Add(point);
        }
        
        await qdrantClient.UpsertAsync(
          collectionName: collectionName,
          points: points
        );
        
        return;
      }
    }
    
    CollectionInfo collection;
    
    // collection = await qdrantClient.GetCollectionInfoAsync(collectionName);
    // await qdrantClient.DeleteCollectionAsync(collectionName);
    // await qdrantClient.CreateCollectionAsync(collectionName,  new VectorParams { Size = 3072, Distance = Distance.Dot });
    
    // if (collection.Status == null)
    // {
    //   await qdrantClient.CreateCollectionAsync(collectionName,  new VectorParams { Size = 100, Distance = Distance.Cosine });
    // }
    
    var weaponReports = new List<WeaponReportEmbedding>();
    var reports = Directory.EnumerateFiles(Configuration["WeaponsFolder"]!);
    
    var batchPoints = new List<PointStruct>();
    ulong pointIdCounter = 1;
    
    foreach (var report in reports)
    {
      Console.WriteLine($"Czytam {report} ...");
      var content = await File.ReadAllTextAsync(report);
      var fileName = Path.GetFileName(report);
      
      var date = Path.GetFileName(report).Substring(0,10);
      date = date.Replace('_', '-');
      

      var model = new EmbeddingClient("text-embedding-3-large", OpenAiToken);
      var embeddingResult = await model.GenerateEmbeddingAsync(content);
      var vector = embeddingResult.Value.ToFloats().ToArray();
      
      var id = new WeaponReportEmbedding(fileName, date, vector);
      weaponReports.Add(id);
      
      var point = new PointStruct()
      {
        Id = pointIdCounter++,
        Vectors = vector,
      };

      point.Payload["filename"] = fileName;
      point.Payload["date"] = date;
      batchPoints.Add(point);
    }
    
    var json = System.Text.Json.JsonSerializer.Serialize(weaponReports);
    
    await File.WriteAllTextAsync(embeddingPath, json);
    
    await qdrantClient.UpsertAsync(
      collectionName: collectionName,
      points: batchPoints
    );
    Console.WriteLine($"Dodano {batchPoints.Count} wektorów do kolekcji");
    batchPoints.Clear();
  }
}

public sealed class WeaponReportEmbedding
{
  public WeaponReportEmbedding(string name, string date, float[] vector)
  {
    Name = name;
    Date = date;
    Vector = vector;
  }
  
  public string Name { get; }
  public string Date { get; }
  public float[] Vector { get; }
}
