using Microsoft.Playwright;
using Restaurant_Aqa_Framework.Ui;

namespace Restaurant_Aqa_Framework.Screenplay.Tasks;

/// <summary>The host removes a guest from the group on the screen and confirms it in the dialog.</summary>
public sealed class RemoveAGuestOnThePage(string guestName) : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var page = new GroupOrderPage(actor.LookingAtThePage());

        await page.RemoveButtonFor(guestName).ClickAsync();
        await page.DialogButton("Remove Guest").ClickAsync();
        await page.Dialog.WaitForAsync(new() { State = WaitForSelectorState.Hidden });
        await page.RemoveButtonFor(guestName).WaitForAsync(new() { State = WaitForSelectorState.Detached });
    }
}
