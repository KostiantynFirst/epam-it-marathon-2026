using Restaurant_Aqa_Framework.Api;
using Restaurant_Aqa_Framework.Api.Dtos.GroupPreOrders;

namespace Restaurant_Aqa_Framework.Screenplay.Tasks;

/// <summary>A guest opens the invite of a group dinner (the current one, or the one named) and joins under their
/// name. The guest is remembered under that name, with the token for their own later calls.</summary>
public sealed class JoinTheGroup(string guestName, string? dinner = null) : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var group = actor.Recall<GroupDinner>(dinner);
        var response = actor.TheLastResponse();

        await response.CaptureAsync(await new GroupPreOrdersApi(actor.UsingTheApi()).JoinAsync(group.GroupReservationId, guestName));
        if (!response.Succeeded)
            throw new InvalidOperationException($"{guestName} could not join the group: {response.Status}: {response.Body}");

        var session = response.Json<GuestSessionCreatedResponse>();
        actor.Remember(guestName, new GroupGuest(guestName, session.SessionId, session.SessionToken));
    }
}
