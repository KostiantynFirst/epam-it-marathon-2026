using Microsoft.Playwright;

namespace Restaurant_Aqa_Framework.Ui;

/// <summary>The host's group pre-order screen (<c>/reservations/{reservationId}/group-order</c>): one card per
/// participant, the consolidated order, the grand total, and the submit / remove-guest dialogs.</summary>
public sealed class GroupOrderPage(IPage page) : BasePage(page)
{
    public ILocator Heading => Page.GetByRole(AriaRole.Heading, new() { Name = "Group Pre-Order", Exact = true, Level = 1 });
    public ILocator StatusBadge => Page.Locator("main .badge");
    public ILocator ParticipantCards => Page.Locator("app-participant-card article");
    public ILocator ConsolidatedLines => Page.Locator(".consolidated-list li");
    public ILocator GrandTotal => Page.Locator(".grand-total-value");
    public ILocator Notice => Page.Locator("main .state");
    public ILocator SubmitButton => Page.GetByRole(AriaRole.Button, new() { Name = "Submit Pre-Order", Exact = true });
    public ILocator RefreshButton => Page.GetByRole(AriaRole.Button, new() { Name = "Refresh", Exact = true });
    public ILocator Dialog => Page.GetByRole(AriaRole.Dialog);

    public Task NavigateAsync(string reservationId) => NavigateToAsync($"/reservations/{reservationId}/group-order");

    public static ILocator CardTitle(ILocator card) => card.Locator("h2.title");
    public static ILocator CardStatus(ILocator card) => card.Locator(".chip");
    public static ILocator CardSubtotal(ILocator card) => card.Locator(".subtotal span").Last;
    public static ILocator CardItems(ILocator card) => card.Locator("li.item");

    public ILocator RemoveButtonFor(string guestName) =>
        Page.GetByRole(AriaRole.Button, new() { Name = $"Remove {guestName}", Exact = true });

    public ILocator DialogButton(string name) =>
        Dialog.GetByRole(AriaRole.Button, new() { Name = name, Exact = true });

    /// <summary>The screen has finished loading: the status badge is shown (it appears once the group is loaded, even
    /// when nobody has joined), or a message other than "Loading…" (the group was not found, it failed to load).</summary>
    public Task WaitUntilLoadedAsync() =>
        StatusBadge.Or(Notice.Filter(new() { HasNotText = "Loading" })).First
            .WaitForAsync(new() { State = WaitForSelectorState.Visible });
}
