using Restaurant_Aqa_Framework.Api;
using Restaurant_Aqa_Framework.Screenplay.Questions;

namespace Restaurant_Aqa_Framework.Screenplay.Tasks;

/// <summary>Someone asks the service to remove a participant from a group dinner (the current one, or the one named).
/// The participant is a guest the scenario knows by name, or — when <paramref name="guestName"/> is null — someone
/// who never joined. The outcome is the scenario's last response. <see cref="TheFirstDinner"/> names the dinner the
/// scenario booked first, whatever else it booked since.</summary>
public sealed class RemoveFromTheGroup(Party who, string? guestName, string? dinner = null) : ITask
{
    public const string TheFirstDinner = "the first dinner";

    public async Task PerformAsAsync(Actor actor)
    {
        var group = dinner == TheFirstDinner ? actor.RecallAll<GroupDinner>()[0] : actor.Recall<GroupDinner>(dinner);
        var groupId = group.GroupReservationId;
        var sessionId = guestName is null ? Guid.NewGuid().ToString() : actor.Recall<GroupGuest>(guestName).SessionId;
        var token = await actor.AsksForAsync(new TheCredentials(who));

        await actor.TheLastResponse().CaptureAsync(
            await new GroupPreOrdersApi(actor.UsingTheApi()).RemoveParticipantAsync(groupId, sessionId, token));
    }
}
