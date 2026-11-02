using Restaurant_Aqa_Framework.Ui;

namespace Restaurant_Aqa_Framework.Screenplay.Tasks;

/// <summary>The host signs in on the storefront and opens the group pre-order of their booking.</summary>
public sealed class OpenTheGroupOrderPage : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var host = actor.Recall<CustomerAccount>(SetUpAGroupDinner.Host);
        await actor.AttemptsToAsync(new SignInThroughTheLoginForm(host.Email, host.Password));

        var page = new GroupOrderPage(actor.LookingAtThePage());
        await page.NavigateAsync(actor.Recall<GroupDinner>().ReservationId);
        await page.Heading.WaitForAsync();
        await page.WaitUntilLoadedAsync();
    }
}
