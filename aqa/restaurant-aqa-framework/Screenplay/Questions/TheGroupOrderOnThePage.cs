using Restaurant_Aqa_Framework.Ui;

namespace Restaurant_Aqa_Framework.Screenplay.Questions;

/// <summary>One participant's card as the host sees it.</summary>
public sealed record ParticipantCardView(string Title, string Status, string Subtotal, int Dishes);

/// <summary>What the host's group pre-order screen shows right now.</summary>
public sealed record GroupOrderView(
    IReadOnlyList<ParticipantCardView> Participants,
    IReadOnlyList<string> ConsolidatedOrder,
    string GrandTotal,
    string Status,
    string Notice,
    bool CanSubmit);

/// <summary>Reads the host's group pre-order screen. Waits for it to finish loading first, so the answer is the
/// screen's settled state, not a half-rendered one.</summary>
public sealed class TheGroupOrderOnThePage : IQuestion<GroupOrderView>
{
    public async Task<GroupOrderView> AnsweredByAsync(Actor actor)
    {
        var page = new GroupOrderPage(actor.LookingAtThePage());
        await page.WaitUntilLoadedAsync();

        var participants = new List<ParticipantCardView>();
        foreach (var card in await page.ParticipantCards.AllAsync())
        {
            participants.Add(new ParticipantCardView(
                Title: await TextAsync(GroupOrderPage.CardTitle(card)),
                Status: await TextAsync(GroupOrderPage.CardStatus(card)),
                Subtotal: await TextAsync(GroupOrderPage.CardSubtotal(card)),
                Dishes: await GroupOrderPage.CardItems(card).CountAsync()));
        }

        var consolidated = (await page.ConsolidatedLines.AllInnerTextsAsync())
            .Select(Squash)
            .ToList();

        return new GroupOrderView(
            participants,
            consolidated,
            GrandTotal: await page.GrandTotal.CountAsync() > 0 ? await TextAsync(page.GrandTotal) : "",
            Status: await page.StatusBadge.CountAsync() > 0 ? await TextAsync(page.StatusBadge) : "",
            Notice: await page.Notice.CountAsync() > 0 ? Squash(string.Join(" ", await page.Notice.AllInnerTextsAsync())) : "",
            CanSubmit: await page.SubmitButton.CountAsync() > 0 && await page.SubmitButton.IsEnabledAsync());
    }

    private static async Task<string> TextAsync(Microsoft.Playwright.ILocator locator) => Squash(await locator.InnerTextAsync());

    /// <summary>Collapses the line breaks and runs of spaces the layout puts between a dish and its price.</summary>
    private static string Squash(string text) => string.Join(" ", text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}
