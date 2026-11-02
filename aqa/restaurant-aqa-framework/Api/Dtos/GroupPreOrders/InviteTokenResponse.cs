namespace Restaurant_Aqa_Framework.Api.Dtos.GroupPreOrders;

/// <summary>GET /reservations/{reservationId}/invite-token — the token the host shares with the guests.</summary>
public sealed record InviteTokenResponse(string Token);
