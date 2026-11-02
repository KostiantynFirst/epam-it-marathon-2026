using Microsoft.Playwright;
using Restaurant_Aqa_Framework.Ui;

namespace Restaurant_Aqa_Framework.Screenplay.Tasks;

/// <summary>The host submits the group's pre-order on the screen and confirms it in the dialog.</summary>
public sealed class SendThePreOrderToTheRestaurant : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var page = new GroupOrderPage(actor.LookingAtThePage());

        await page.SubmitButton.ClickAsync();
        await page.DialogButton("Submit").ClickAsync();
        await page.Dialog.WaitForAsync(new() { State = WaitForSelectorState.Hidden });
    }
}
