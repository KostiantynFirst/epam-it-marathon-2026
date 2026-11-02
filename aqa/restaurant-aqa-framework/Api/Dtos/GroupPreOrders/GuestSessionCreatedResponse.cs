namespace Restaurant_Aqa_Framework.Api.Dtos.GroupPreOrders;

/// <summary>POST /guest/sessions — the guest's session and the token every later guest call must bear.</summary>
public sealed record GuestSessionCreatedResponse(string SessionId, string SessionToken);
