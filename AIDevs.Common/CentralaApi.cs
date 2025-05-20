using Refit;

namespace AIDevs.Common;

public interface ICentralaApi
{
  [Post("/report")]
  Task<ApiResponse<ResponseRequest>> Report([Body(BodySerializationMethod.Serialized)] AnswerRequest request);
}