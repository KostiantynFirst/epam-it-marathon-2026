namespace Restaurant_Aqa_Framework.Screenplay;

/// <summary>A booking for a group: the reservation, its group pre-order and the invite the host shares.</summary>
public sealed record GroupDinner(string ReservationId, string GroupReservationId, string InviteToken);
