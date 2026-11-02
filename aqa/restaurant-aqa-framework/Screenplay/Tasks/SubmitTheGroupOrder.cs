using Restaurant_Aqa_Framework.Api;
using Restaurant_Aqa_Framework.Screenplay.Questions;

namespace Restaurant_Aqa_Framework.Screenplay.Tasks;

/// <summary>Where the submit goes wrong on purpose.</summary>
public enum SubmitTarget
{
    TheDinner,
    AGroupThatDoesNotExist,
    ABookingThatDoesNotExist,
}

/// <summary>Someone asks the service to send the group's consolidated pre-order to the restaurant. The outcome is the
/// scenario's last response, so a scenario can check both a success and a refusal.</summary>
public sealed class SubmitTheGroupOrder(Party who, SubmitTarget target = SubmitTarget.TheDinner) : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var dinner = actor.Recall<GroupDinner>();
        var groupId = target == SubmitTarget.AGroupThatDoesNotExist ? Guid.NewGuid().ToString() : dinner.GroupReservationId;
        var reservationId = target == SubmitTarget.ABookingThatDoesNotExist ? Guid.NewGuid().ToString() : dinner.ReservationId;
        var token = await actor.AsksForAsync(new TheCredentials(who));

        await actor.TheLastResponse().CaptureAsync(
            await new GroupPreOrdersApi(actor.UsingTheApi()).SubmitAsync(groupId, reservationId, token));
    }
}
