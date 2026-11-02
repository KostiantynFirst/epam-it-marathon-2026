using Restaurant_Aqa_Framework.Api.Dtos.GroupPreOrders;

namespace Restaurant_Aqa_Framework.Screenplay.Questions;

/// <summary>The participants and total the last group pre-order request returned (the list, or the list after a
/// participant was removed).</summary>
public sealed class TheGroupOrderInTheService : IQuestion<GroupSessionsResponse>
{
    public Task<GroupSessionsResponse> AnsweredByAsync(Actor actor) =>
        Task.FromResult(actor.TheLastResponse().Json<GroupSessionsResponse>());
}
