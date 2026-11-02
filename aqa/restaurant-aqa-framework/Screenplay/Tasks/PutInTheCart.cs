using Restaurant_Aqa_Framework.Api;
using Restaurant_Aqa_Framework.Api.Dtos.Dishes;

namespace Restaurant_Aqa_Framework.Screenplay.Tasks;

/// <summary>A guest puts some of a dish (one the dinner was set up with) into their own cart.</summary>
public sealed class PutInTheCart(string guestName, int quantity, string dish) : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var guest = actor.Recall<GroupGuest>(guestName);
        var dishId = actor.Recall<DishResponse>(dish).Id;
        var response = actor.TheLastResponse();

        await response.CaptureAsync(
            await new GroupPreOrdersApi(actor.UsingTheApi()).AddDishAsync(guest.SessionId, dishId, quantity, guest.Token));
        if (!response.Succeeded)
            throw new InvalidOperationException($"{guestName} could not add the {dish}: {response.Status}: {response.Body}");
    }
}
