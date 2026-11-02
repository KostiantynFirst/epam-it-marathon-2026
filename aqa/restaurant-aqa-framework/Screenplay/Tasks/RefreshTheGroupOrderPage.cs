using Microsoft.Playwright;
using Restaurant_Aqa_Framework.Ui;

namespace Restaurant_Aqa_Framework.Screenplay.Tasks;

/// <summary>The host asks the screen to catch up with the guests now, instead of waiting for the next automatic
/// refresh.</summary>
public sealed class RefreshTheGroupOrderPage : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var browser = actor.LookingAtThePage();
        var page = new GroupOrderPage(browser);

        var refreshed = browser.WaitForResponseAsync(r =>
            r.Url.Contains("/sessions", StringComparison.Ordinal) && r.Request.Method == "GET");
        await page.RefreshButton.ClickAsync();
        await refreshed;
        await browser.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }
}
