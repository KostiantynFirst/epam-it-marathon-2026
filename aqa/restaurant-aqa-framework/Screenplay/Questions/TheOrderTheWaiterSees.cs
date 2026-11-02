using Restaurant_Aqa_Framework.Api;
using Restaurant_Aqa_Framework.Api.Dtos.Orders;

namespace Restaurant_Aqa_Framework.Screenplay.Questions;

/// <summary>The booking's order as the restaurant's waiter sees it.</summary>
public sealed class TheOrderTheWaiterSees : IQuestion<GetOrderResponse>
{
    public async Task<GetOrderResponse> AnsweredByAsync(Actor actor)
    {
        var waiter = await actor.AsksForAsync(new TheWaiterSession());
        var response = await new OrdersApi(actor.UsingTheApi()).GetOrderAsync(actor.Recall<GroupDinner>().ReservationId, waiter.IdToken);
        if (!response.Ok)
            throw new InvalidOperationException(
                $"The waiter could not see the order: {response.Status}: {await response.ReadErrorMessageAsync()}");

        return await response.ReadAsAsync<GetOrderResponse>();
    }
}
