using Microsoft.Playwright;
using Restaurant_Aqa_Framework.Api.Dtos.GroupPreOrders;

namespace Restaurant_Aqa_Framework.Api;

/// <summary>
/// The group pre-order: the host's invite, the guests' own sessions and carts, and the host's view of the
/// whole group (list, submit, remove a participant).
///
/// Two kinds of credentials meet here. The host's calls (sessions list, submit, remove, owner session id, invite
/// token) are behind the Cognito authorizer and take the host's id token. A guest's calls (add a dish, submit the
/// cart) take the guest-session token returned when the guest joined — also sent as <c>Authorization: Bearer</c>.
/// </summary>
public sealed class GroupPreOrdersApi(IAPIRequestContext api) : ApiClientBase(api)
{
    public Task<IAPIResponse> GetInviteTokenAsync(string reservationId, string? bearerToken = null) =>
        GetAsync(Url($"/reservations/{reservationId}/invite-token"), new APIRequestContextOptions
        {
            Headers = Headers(bearerToken),
        });

    public Task<IAPIResponse> GetInviteAsync(string inviteToken) =>
        GetAsync(Url($"/guest/invites/{inviteToken}"));

    public Task<IAPIResponse> JoinAsync(string groupReservationId, string guestName) =>
        Api.PostAsync(Url("/guest/sessions"), new APIRequestContextOptions
        {
            DataString = Serialize(new CreateGuestSessionRequest(groupReservationId, guestName)),
            Headers = Headers(),
        });

    public Task<IAPIResponse> AddDishAsync(string guestSessionId, string dishId, int quantity, string guestToken) =>
        Api.PostAsync(Url($"/guest/sessions/{guestSessionId}/dishes"), new APIRequestContextOptions
        {
            DataString = Serialize(new AddGuestDishRequest(dishId, quantity)),
            Headers = Headers(guestToken),
        });

    public Task<IAPIResponse> SubmitCartAsync(string guestSessionId, string guestToken) =>
        Api.PostAsync(Url($"/guest/sessions/{guestSessionId}/submit"), new APIRequestContextOptions
        {
            Headers = Headers(guestToken),
        });

    public Task<IAPIResponse> GetParticipantsAsync(string groupReservationId, string? bearerToken = null) =>
        GetAsync(Url($"/guest/groups/{groupReservationId}/sessions"), new APIRequestContextOptions
        {
            Headers = Headers(bearerToken),
        });

    public Task<IAPIResponse> GetOwnerSessionIdAsync(string reservationId, string? bearerToken = null) =>
        GetAsync(Url($"/reservations/{reservationId}/guest-session-id"), new APIRequestContextOptions
        {
            Headers = Headers(bearerToken),
        });

    public Task<IAPIResponse> SubmitAsync(string groupReservationId, string reservationId, string? bearerToken = null) =>
        Api.PostAsync(Url($"/group-reservations/{groupReservationId}/submit"), new APIRequestContextOptions
        {
            DataString = Serialize(new SubmitGroupPreOrderRequest(reservationId)),
            Headers = Headers(bearerToken),
        });

    public Task<IAPIResponse> RemoveParticipantAsync(string groupReservationId, string guestSessionId, string? bearerToken = null) =>
        DeleteAsync(Url($"/guest/groups/{groupReservationId}/sessions/{guestSessionId}"), new APIRequestContextOptions
        {
            Headers = Headers(bearerToken),
        });
}
