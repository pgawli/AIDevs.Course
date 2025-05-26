using System.Text.Json.Serialization;

namespace AIDevs.Common;

public record AnswerRequest(
  [property: JsonPropertyName("task")] string Task, 
  [property: JsonPropertyName("apikey")] string ApiKey, 
  [property: JsonPropertyName("answer")] string Answer);
  
public record ResponseRequest(
  [property: JsonPropertyName("code")] int Code, 
  [property: JsonPropertyName("message")] string Message);  
  
public record Answer9Request(
  [property: JsonPropertyName("task")] string Task, 
  [property: JsonPropertyName("apikey")] string ApiKey, 
  [property: JsonPropertyName("answer")] Answer9Content Answer);

public sealed record Answer9Content(
  [property: JsonPropertyName("people")] string[] Peoples, [property: JsonPropertyName("hardware")] string[] Hardware);

public sealed record AnswerArxiv(
  [property: JsonPropertyName("task")] string Task, 
  [property: JsonPropertyName("apikey")] string ApiKey, 
  [property: JsonPropertyName("answer")] Dictionary<string, string> Answer
);  