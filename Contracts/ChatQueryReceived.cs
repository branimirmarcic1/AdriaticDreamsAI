// U projektu Contracts -> ChatQueryReceived.cs
namespace Contracts;

public record ChatQueryReceived(Guid CorrelationId, string QueryText, string UserId);