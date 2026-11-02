using Restaurant_Aqa_Framework.Api;

namespace Restaurant_Aqa_Framework.Screenplay.Tasks;

/// <summary>A guest confirms their cart: from now on their dishes count towards the group's order.</summary>
public sealed class ConfirmTheCart(string guestName) : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var guest = actor.Recall<GroupGuest>(guestName);
        var response = actor.TheLastResponse();

        await response.CaptureAsync(await new GroupPreOrdersApi(actor.UsingTheApi()).SubmitCartAsync(guest.SessionId, guest.Token));
        if (!response.Succeeded)
            throw new InvalidOperationException($"{guestName} could not confirm the cart: {response.Status}: {response.Body}");
    }
}
