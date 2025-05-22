using System.Text.Json.Serialization;
using Refit;

namespace AIDevs.Common;

public interface ICentralaApi
{
  [Post("/report")]
  Task<ApiResponse<ResponseRequest>> Report([Body(BodySerializationMethod.Serialized)] AnswerRequest request);
  
  [Post("/report")]
  Task<ApiResponse<ResponseRequest>> Report9([Body(BodySerializationMethod.Serialized)] Answer9Request request);
  
  [Get("/data/{apiKey}/robotid.json")]
  Task<ApiResponse<RobotDescription>> GetRobot(string apiKey);
}

public sealed record RobotDescription([property: JsonPropertyName("description")] string RobotId);
