namespace Restaurant_Aqa_Framework.Api.Dtos.GroupPreOrders;

/// <summary>GET /guest/invites/{token} — what a guest sees before joining (PascalCase on the wire; the
/// case-insensitive <see cref="JsonDefaults"/> reads it as is).</summary>
public sealed record GuestInviteResponse(string GroupReservationId, string LocationAddress, int MaxGuestCount);
