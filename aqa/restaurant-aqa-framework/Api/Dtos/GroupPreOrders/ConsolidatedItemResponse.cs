namespace Restaurant_Aqa_Framework.Api.Dtos.GroupPreOrders;

/// <summary>POST /group-reservations/{groupReservationId}/submit — one line of the order sent to the restaurant.</summary>
public sealed record ConsolidatedItemResponse(string DishId, int Quantity);
