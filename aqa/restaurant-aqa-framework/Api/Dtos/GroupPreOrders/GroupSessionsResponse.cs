namespace Restaurant_Aqa_Framework.Api.Dtos.GroupPreOrders;

/// <summary>GET /guest/groups/{groupReservationId}/sessions (and the answer to removing a participant):
/// every participant with their cart, and the total over all of them.</summary>
public sealed record GroupSessionsResponse(IReadOnlyList<ParticipantResponse> Participants, decimal Total);
