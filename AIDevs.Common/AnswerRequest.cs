using System.Text.Json.Serialization;

namespace AIDevs.Common;

public record AnswerRequest(
  [property: JsonPropertyName("task")] string Task, 
  [property: JsonPropertyName("apikey")] string ApiKey, 
  [property: JsonPropertyName("answer")] string Answer);
  
public record ResponseRequest(
  [property: JsonPropertyName("code")] int Code, 
  [property: JsonPropertyName("message")] string Message);  