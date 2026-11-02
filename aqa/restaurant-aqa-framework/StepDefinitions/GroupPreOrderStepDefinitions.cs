using System.Globalization;
using Restaurant_Aqa_Framework.Api.Dtos.Dishes;
using Restaurant_Aqa_Framework.Api.Dtos.Orders;
using Restaurant_Aqa_Framework.Screenplay;
using Restaurant_Aqa_Framework.Screenplay.Questions;
using Restaurant_Aqa_Framework.Screenplay.Tasks;

namespace Restaurant_Aqa_Framework.StepDefinitions;

/// <summary>
/// The group pre-order: a host books a table for a group, guests join through the invite and fill their own carts,
/// and the host reviews everyone's choices, removes a participant and sends one consolidated pre-order to the
/// restaurant. The same sentence works on both stages: on <c>@ui</c> the host uses the storefront, on <c>@api</c> the
/// service — guests and the set-up always go through the service.
/// </summary>
[Binding]
public sealed class GroupPreOrderStepDefinitions(Actor actor) : StepDefinitionBase(actor)
{
    // ---- set-up ----

    [Given("a host has booked a table for {int} guests and shared the invite")]
    public Task GivenAHostHasBookedATable(int guests) => Actor.AttemptsToAsync(new SetUpAGroupDinner(guests));

    [Given("a host has booked a table for {int} guests without a pre-order of their own")]
    public Task GivenAHostHasBookedATableWithoutPreOrder(int guests) =>
        Actor.AttemptsToAsync(new SetUpAGroupDinner(guests, withTheHostsOwnPreOrder: false));

    [Given("the host has also booked a table for {int} guests for another party, {string}")]
    public Task GivenTheHostHasAnotherParty(int guests, string party) =>
        Actor.AttemptsToAsync(new SetUpAGroupDinner(guests, name: party));

    [Given("{string} has joined the group")]
    public Task GivenGuestHasJoined(string guest) => Actor.AttemptsToAsync(new JoinTheGroup(guest));

    [Given("{string} has joined the group of {string}")]
    public Task GivenGuestHasJoinedTheParty(string guest, string party) => Actor.AttemptsToAsync(new JoinTheGroup(guest, party));

    [Given("{string} has put {int} of the {dish} in the cart")]
    [When("{string} puts {int} of the {dish} in the cart")]
    public Task GivenGuestHasPutADishInTheCart(string guest, int quantity, string dish) =>
        Actor.AttemptsToAsync(new PutInTheCart(guest, quantity, dish));

    [Given("{string} has confirmed the cart")]
    [When("{string} confirms the cart")]
    public Task GivenGuestHasConfirmedTheCart(string guest) => Actor.AttemptsToAsync(new ConfirmTheCart(guest));

    // ---- the host at work: on the storefront ----

    [Given("the host has opened the group pre-order")]
    [When("the host opens the group pre-order")]
    [Scope(Tag = "ui")]
    public Task WhenTheHostOpensTheGroupPreOrderOnTheStorefront() => Actor.AttemptsToAsync(new OpenTheGroupOrderPage());

    [When("the host refreshes the group pre-order")]
    [Scope(Tag = "ui")]
    public Task WhenTheHostRefreshesTheGroupPreOrderOnTheStorefront() => Actor.AttemptsToAsync(new RefreshTheGroupOrderPage());

    [When("the host sends the pre-order to the restaurant")]
    [Scope(Tag = "ui")]
    public Task WhenTheHostSendsThePreOrderOnTheStorefront() => Actor.AttemptsToAsync(new SendThePreOrderToTheRestaurant());

    [When("the host removes {string} from the group")]
    [Scope(Tag = "ui")]
    public Task WhenTheHostRemovesAGuestOnTheStorefront(string guest) => Actor.AttemptsToAsync(new RemoveAGuestOnThePage(guest));

    // ---- the host (or someone else) at work: through the service ----

    [Given("the host has opened the group pre-order")]
    [When("the host opens the group pre-order")]
    [When("the host refreshes the group pre-order")]
    [Scope(Tag = "api")]
    public Task WhenTheHostLooksAtTheGroupPreOrder() => Actor.AttemptsToAsync(new LookAtTheGroupOrder(Party.Host));

    [When("the {party} tries to open the group pre-order")]
    public Task WhenSomeoneTriesToOpenTheGroupPreOrder(Party who) => Actor.AttemptsToAsync(new LookAtTheGroupOrder(who));

    [When("the host opens a group pre-order that does not exist")]
    public Task WhenTheHostOpensAGroupThatDoesNotExist() =>
        Actor.AttemptsToAsync(new LookAtTheGroupOrder(Party.Host, ofAGroupThatDoesNotExist: true));

    [When("the host sends the pre-order to the restaurant")]
    [When("the host tries to send the pre-order to the restaurant again")]
    [Scope(Tag = "api")]
    public Task WhenTheHostSendsThePreOrder() => Actor.AttemptsToAsync(new SubmitTheGroupOrder(Party.Host));

    [Given("the host has sent the pre-order to the restaurant")]
    public async Task GivenTheHostHasSentThePreOrder()
    {
        await Actor.AttemptsToAsync(new SubmitTheGroupOrder(Party.Host));
        var outcome = await Actor.AsksForAsync(new TheOutcome());
        await Assert.That(outcome.Succeeded).IsTrue().Because($"the pre-order could not be sent: {outcome.Status} {outcome.Body}");
    }

    [When("the {party} tries to send the pre-order to the restaurant")]
    public Task WhenSomeoneTriesToSendThePreOrder(Party who) => Actor.AttemptsToAsync(new SubmitTheGroupOrder(who));

    [When("the host tries to send the pre-order of a group that does not exist")]
    public Task WhenTheHostSendsTheOrderOfAGroupThatDoesNotExist() =>
        Actor.AttemptsToAsync(new SubmitTheGroupOrder(Party.Host, SubmitTarget.AGroupThatDoesNotExist));

    [When("the host tries to send the pre-order for a booking that does not exist")]
    public Task WhenTheHostSendsTheOrderOfABookingThatDoesNotExist() =>
        Actor.AttemptsToAsync(new SubmitTheGroupOrder(Party.Host, SubmitTarget.ABookingThatDoesNotExist));

    [When("the host removes {string} from the group")]
    [Scope(Tag = "api")]
    public Task WhenTheHostRemovesAGuest(string guest) => Actor.AttemptsToAsync(new RemoveFromTheGroup(Party.Host, guest));

    [When("the host tries to remove {string} from the first group")]
    public Task WhenTheHostRemovesAGuestFromTheFirstGroup(string guest) =>
        Actor.AttemptsToAsync(new RemoveFromTheGroup(Party.Host, guest, RemoveFromTheGroup.TheFirstDinner));

    [When("the host tries to remove someone who never joined the group")]
    public Task WhenTheHostRemovesSomeoneWhoNeverJoined() => Actor.AttemptsToAsync(new RemoveFromTheGroup(Party.Host, guestName: null));

    [When("the {party} tries to remove {string} from the group")]
    public Task WhenSomeoneTriesToRemoveAGuest(Party who, string guest) => Actor.AttemptsToAsync(new RemoveFromTheGroup(who, guest));

    // ---- what the host sees: on the storefront ----

    [Then("the host sees these participants:")]
    [Scope(Tag = "ui")]
    public async Task ThenTheHostSeesTheParticipantsOnTheStorefront(Table expected)
    {
        var view = await Actor.AsksForAsync(new TheGroupOrderOnThePage());
        var shown = view.Participants.Select(p => $"{p.Title} | {p.Status} | {p.Subtotal}").ToList();
        var wanted = expected.Rows.Select(r => $"Guest: {r["participant"]} | {r["cart"]} | {r["subtotal"]}").ToList();

        await Assert.That(shown).IsEquivalentTo(wanted).Because($"the screen shows: {string.Join("; ", shown)}");
    }

    [Then("the grand total is {string}")]
    [Scope(Tag = "ui")]
    public async Task ThenTheGrandTotalIsShown(string total) =>
        await Assert.That((await Actor.AsksForAsync(new TheGroupOrderOnThePage())).GrandTotal).IsEqualTo(total);

    [Then("the consolidated pre-order is:")]
    [Scope(Tag = "ui")]
    public async Task ThenTheConsolidatedPreOrderIsShown(Table expected)
    {
        var view = await Actor.AsksForAsync(new TheGroupOrderOnThePage());
        var wanted = expected.Rows.Select(r => $"{Recall<DishResponse>(r["dish"]).Name} × {r["quantity"]}").ToList();
        var shown = view.ConsolidatedOrder.Select(WithoutPrice).ToList();

        await Assert.That(shown).IsEquivalentTo(wanted).Because($"the screen shows: {string.Join("; ", view.ConsolidatedOrder)}");
    }

    [Then("the host is told that nobody has added any dishes yet")]
    [Scope(Tag = "ui")]
    public async Task ThenTheHostIsToldNobodyHasAddedDishes()
    {
        var view = await Actor.AsksForAsync(new TheGroupOrderOnThePage());
        await Assert.That(view.Notice).Contains("Participants haven't added any dishes yet");
        await Assert.That(view.Participants).IsEmpty();
        await Assert.That(view.CanSubmit).IsFalse();
    }

    [Then("the pre-order is marked as sent to the restaurant")]
    [Scope(Tag = "ui")]
    public async Task ThenThePreOrderIsMarkedAsSent()
    {
        var view = await Actor.AsksForAsync(new TheGroupOrderOnThePage());
        await Assert.That(view.Status).IsEqualTo("Sent to Restaurant");
        await Assert.That(view.CanSubmit).IsFalse().Because("a sent pre-order can no longer be submitted");
    }

    // ---- what the host gets: through the service ----

    [Then("the host sees these participants:")]
    [Scope(Tag = "api")]
    public async Task ThenTheHostSeesTheParticipants(Table expected)
    {
        var group = await Actor.AsksForAsync(new TheGroupOrderInTheService());
        var shown = group.Participants
            .Select(p => $"{p.Name} | {p.Items.Sum(i => i.Quantity)} | {p.Total.ToString("0.00", CultureInfo.InvariantCulture)} | {(p.IsSubmitted ? "yes" : "no")}")
            .ToList();
        var wanted = expected.Rows
            .Select(r => $"{r["participant"]} | {r["dishes"]} | {r["total"]} | {r["confirmed"]}")
            .ToList();

        await Assert.That(shown).IsEquivalentTo(wanted).Because($"the service returned: {string.Join("; ", shown)}");
    }

    [Then("the group total is {decimal}")]
    public async Task ThenTheGroupTotalIs(decimal total)
    {
        var group = await Actor.AsksForAsync(new TheGroupOrderInTheService());
        await Assert.That(group.Total).IsEqualTo(total);
        await Assert.That(group.Total).IsEqualTo(group.Participants.Sum(p => p.Total))
            .Because("the group total is the sum of every participant's total");
    }

    [Then("nobody is in the group")]
    public async Task ThenNobodyIsInTheGroup()
    {
        var group = await Actor.AsksForAsync(new TheGroupOrderInTheService());
        await Assert.That(group.Participants).IsEmpty();
        await Assert.That(group.Total).IsEqualTo(0m);
    }

    [Then("the restaurant receives:")]
    public async Task ThenTheRestaurantReceives(Table expected)
    {
        var lines = await Actor.AsksForAsync(new TheOrderSentToTheRestaurant());
        var received = lines.Select(l => $"{l.DishId} × {l.Quantity}").ToList();
        var wanted = expected.Rows.Select(r => $"{Recall<DishResponse>(r["dish"]).Id} × {r["quantity"]}").ToList();

        await Assert.That(received).IsEquivalentTo(wanted).Because($"the restaurant received: {string.Join("; ", received)}");
    }

    // ---- the waiter ----

    [Then("the waiter sees the order, sent by the customer, with:")]
    public async Task ThenTheWaiterSeesTheOrder(Table expected)
    {
        var order = await Actor.AsksForAsync(new TheOrderTheWaiterSees());
        var shown = order.OrderItems.Select(i => $"{i.Dish.Name} × {i.Quantity}").ToList();
        var wanted = expected.Rows.Select(r => $"{Recall<DishResponse>(r["dish"]).Name} × {r["quantity"]}").ToList();

        await Assert.That(order.Status).IsEqualTo(OrderStatus.SubmittedByCustomer);
        await Assert.That(shown).IsEquivalentTo(wanted).Because($"the waiter sees: {string.Join("; ", shown)}");
    }

    // ---- refusals ----

    [Then("the {party} is turned away")]
    public async Task ThenSomeoneIsTurnedAway(Party who)
    {
        // Someone signed in but not the host is forbidden; someone without a customer's sign-in (a guest's token is
        // not one) is not let in at all.
        var expected = who is Party.AnotherCustomer or Party.Waiter ? 403 : 401;
        var outcome = await Actor.AsksForAsync(new TheOutcome());

        await Assert.That(outcome.Status).IsEqualTo(expected).Because($"response body was: {outcome.Body}");
    }

    [Then("the host is told {string}")]
    public async Task ThenTheHostIsTold(string message)
    {
        var outcome = await Actor.AsksForAsync(new TheOutcome());

        await Assert.That(outcome.Succeeded).IsFalse().Because($"the request unexpectedly succeeded: {outcome.Body}");
        await Assert.That(outcome.Status).IsEqualTo(400).Because($"response body was: {outcome.Body}");
        await Assert.That(outcome.Body).Contains(message);
    }

    [Then("the host is told that the {word} could not be found")]
    public async Task ThenTheHostIsToldNotFound(string what)
    {
        var outcome = await Actor.AsksForAsync(new TheOutcome());
        await Assert.That(outcome.Status).IsEqualTo(404).Because($"looking for the {what}, the service answered: {outcome.Body}");
    }

    /// <summary>"Spring Salad × 3 $36.00" → "Spring Salad × 3": the price is checked by the grand total.</summary>
    private static string WithoutPrice(string line)
    {
        var price = line.LastIndexOf(" $", StringComparison.Ordinal);
        return price > 0 ? line[..price] : line;
    }
}
