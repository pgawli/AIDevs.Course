using System.Text.Json.Serialization;
using Refit;

namespace AIDevs.Common;

[Headers("User-Agent: RefitClient")]
public interface ICentralaApi
{
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
}

public sealed record RobotDescription([property: JsonPropertyName("description")] string RobotId);

