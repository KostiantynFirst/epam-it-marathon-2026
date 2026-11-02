namespace Restaurant_Aqa_Framework.Api.Dtos.GroupPreOrders;

/// <summary>Only the fields the group pre-order is about; the rest of the dish is ignored.</summary>
public sealed record ParticipantDishResponse(string Id, string Name, decimal Price);
