using Restaurant_Aqa_Framework.Api.Dtos.GroupPreOrders;

namespace Restaurant_Aqa_Framework.Screenplay.Questions;

/// <summary>The consolidated lines the last successful submit returned.</summary>
public sealed class TheOrderSentToTheRestaurant : IQuestion<IReadOnlyList<ConsolidatedItemResponse>>
{
    public Task<IReadOnlyList<ConsolidatedItemResponse>> AnsweredByAsync(Actor actor) =>
        Task.FromResult<IReadOnlyList<ConsolidatedItemResponse>>(actor.TheLastResponse().Json<List<ConsolidatedItemResponse>>());
}
