using Restaurant_Aqa_Framework.Api;
using Restaurant_Aqa_Framework.Screenplay.Questions;

namespace Restaurant_Aqa_Framework.Screenplay.Tasks;

/// <summary>Someone asks the service for the participants of a group dinner (the current one, or the one named), or
/// of a group that does not exist. The outcome is the scenario's last response: it may well be a refusal.</summary>
public sealed class LookAtTheGroupOrder(Party who, string? dinner = null, bool ofAGroupThatDoesNotExist = false) : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var groupId = ofAGroupThatDoesNotExist
            ? Guid.NewGuid().ToString()
            : actor.Recall<GroupDinner>(dinner).GroupReservationId;
        var token = await actor.AsksForAsync(new TheCredentials(who));

        await actor.TheLastResponse().CaptureAsync(
            await new GroupPreOrdersApi(actor.UsingTheApi()).GetParticipantsAsync(groupId, token));
    }
}
