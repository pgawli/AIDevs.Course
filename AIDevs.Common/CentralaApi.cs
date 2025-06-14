using System.Text.Json.Serialization;
using Refit;

namespace AIDevs.Common;

[Headers("User-Agent: RefitClient")]
public interface ICentralaApi
{
  [Get("/dane/barbara.txt")]
  Task<ApiResponse<string>> GetBarbaraNote();

  [Post("/report")]
  Task<ApiResponse<ResponseRequest>> Report([Body(BodySerializationMethod.Serialized)] AnswerRequest request);
  
  [Post("/report")]
  Task<ApiResponse<ResponseRequest>> Report9([Body(BodySerializationMethod.Serialized)] Answer9Request request);
  
  [Post("/report")]
  Task<ApiResponse<ResponseRequest>> ReportArxiv([Body(BodySerializationMethod.Serialized)] AnswerArxiv request);
  
  [Post("/report")]
  Task<ApiResponse<ResponseRequest>> ReportDocs([Body(BodySerializationMethod.Serialized)] AnswerDocuments request);
  
  [Get("/data/{apiKey}/robotid.json")]
  Task<ApiResponse<RobotDescription>> GetRobot(string apiKey);
  
  [Get("/data/arxiv-draft.html")]
  Task<ApiResponse<string>> GetArxivDraft();
  
  [Get("/data/{apiKey}/arxiv.txt")]
  Task<ApiResponse<string>> GetArxiv(string apiKey);
  
  [Post("/apidb")]
  Task<ApiResponse<string>> PostQuery([Body(BodySerializationMethod.Serialized)] Query apiKey);
  
  [Post("/report")]
  Task<ApiResponse<string>> PostAnswer([Body(BodySerializationMethod.Serialized)] ListAnswerRequest request);
  
  [Post("/people")] 
  Task<ApiResponse<ResponseRequest>> PostPeople([Body(BodySerializationMethod.Serialized)] SimpleQuery request);
  
  [Post("/places")] 
  Task<ApiResponse<ResponseRequest>> PostPlaces([Body(BodySerializationMethod.Serialized)] SimpleQuery request);

}

public sealed record SimpleQuery([property: JsonPropertyName("apikey")] string ApiKey, [property: JsonPropertyName("query")] string QueryText);
public sealed record RobotDescription([property: JsonPropertyName("description")] string RobotId);

