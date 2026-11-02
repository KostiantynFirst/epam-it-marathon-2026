namespace Restaurant_Aqa_Framework.Api.Dtos.GroupPreOrders;

public sealed record ParticipantResponse(
    string Id,
    string Type,
    string Name,
    IReadOnlyList<ParticipantItemResponse> Items,
    decimal Total,
    bool IsSubmitted);
