using Microsoft.Extensions.Configuration;
using Refit;

namespace PoligonApiTest;

public interface IPoligonApi
{
  [Get("/dane.txt")]
  Task<string> GetPoligonData();
  
  [Post("/verify")]
  Task<PoligonTask1Response> PostAnswer(PoligonTask1Request request);
}

public class PoligonTask1
{
  private readonly IPoligonApi poligonApi;
  private readonly IConfiguration configuration;

  public PoligonTask1(IPoligonApi poligonApi, IConfiguration configuration)
  {
    this.poligonApi = poligonApi;
    this.configuration = configuration;
  }

  public async Task Execute()
  {
    var data = await poligonApi.GetPoligonData();
    var apiKey = configuration["Api:Key"]!;
    var tablica = data.Split('\n').Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
    var request = new PoligonTask1Request("POLIGON", apiKey, tablica);
    var response = await poligonApi.PostAnswer(request);
    Console.WriteLine(response);
  }
}

public record PoligonTask1Request(string Task, string Apikey, string[] Answer);
public record PoligonTask1Response(int Code, string Message);