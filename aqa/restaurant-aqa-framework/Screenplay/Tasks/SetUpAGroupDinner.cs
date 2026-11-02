using Microsoft.Playwright;
using Restaurant_Aqa_Framework.Api;
using Restaurant_Aqa_Framework.Api.Dtos.Dishes;
using Restaurant_Aqa_Framework.Api.Dtos.GroupPreOrders;
using Restaurant_Aqa_Framework.Api.Dtos.Locations;
using Restaurant_Aqa_Framework.Api.Dtos.Orders;
using Restaurant_Aqa_Framework.Api.Dtos.Reservations;
using Restaurant_Aqa_Framework.Api.Dtos.Tables;
using Restaurant_Aqa_Framework.Api.Seeding;
using Restaurant_Aqa_Framework.Configuration;
using Restaurant_Aqa_Framework.Screenplay.Questions;

namespace Restaurant_Aqa_Framework.Screenplay.Tasks;

/// <summary>
/// A host books a table for a group and gets the invite to share — everything the group pre-order needs, set up
/// through the service:
/// <list type="bullet">
/// <item>the configured location (<c>TESTLOCATION_ID</c>, which has the standing waiter), a fresh table there, and
/// three dishes with round prices: <see cref="FirstDish"/>, <see cref="SecondDish"/>, <see cref="HostsDish"/>;</item>
/// <item>a freshly registered host (remembered as <see cref="Host"/>), the booking (on a random future day, so runs
/// never collide), optionally the host's own pre-order — the group's order that the guests' dishes are added
/// to;</item>
/// <item>the invite token and the group it opens, remembered as a <see cref="GroupDinner"/> (also under
/// <paramref name="name"/> when given).</item>
/// </list>
/// Everything is removed afterwards: the booking is cancelled, the dishes and the table deleted. A second dinner in
/// the same scenario reuses the host and the dishes and only books another table.
/// </summary>
public sealed class SetUpAGroupDinner(int guests = 6, bool withTheHostsOwnPreOrder = true, string? name = null) : ITask
{
    public const string Host = "host";
    public const string FirstDish = "first dish";
    public const string SecondDish = "second dish";
    public const string HostsDish = "host's dish";

    public const decimal FirstDishPrice = 12.50m;
    public const decimal SecondDishPrice = 8.00m;
    public const decimal HostsDishPrice = 6.00m;

    public async Task PerformAsAsync(Actor actor)
    {
        var cleanup = actor.Recall<CleanupScope>();
        var waiter = await actor.AsksForAsync(new TheWaiterSession());

        var location = await TheLocationAsync(actor);
        var table = await ATableAsync(actor, location, guests, waiter, cleanup);

        if (actor.RecallAll<DishResponse>().Count == 0)
        {
            await ADishAsync(actor, location, FirstDish, DishType.MainCourse, FirstDishPrice, waiter, cleanup);
            await ADishAsync(actor, location, SecondDish, DishType.Appetizer, SecondDishPrice, waiter, cleanup);
            await ADishAsync(actor, location, HostsDish, DishType.Dessert, HostsDishPrice, waiter, cleanup);
        }

        var host = await TheHostAsync(actor);
        var reservation = await ABookingAsync(actor, table, guests, host, cleanup);

        if (withTheHostsOwnPreOrder)
            await TheHostsPreOrderAsync(actor, reservation, host);

        var dinner = await TheInviteAsync(actor, reservation, host);
        if (name is null)
            actor.Remember(dinner);
        else
            actor.Remember(name, dinner);
    }

    private static async Task<LocationResponse> TheLocationAsync(Actor actor)
    {
        if (actor.TryRecall<LocationResponse>(out var known))
            return known;

        var response = await new LocationsApi(actor.UsingTheApi()).GetLocationAsync(Settings.Current.RequireTestLocation());
        var location = await RequireAsync<LocationResponse>(response, "Looking up the test location");
        actor.Remember(location);
        return location;
    }

    private static async Task<TableResponse> ATableAsync(
        Actor actor, LocationResponse location, int guests, WaiterSession waiter, CleanupScope cleanup)
    {
        var api = new TablesApi(actor.UsingTheApi());
        var request = SampleDataFactory.CreateTableRequest(location.Id, capacity: guests);
        var table = await RequireAsync<TableResponse>(await api.CreateTableAsync(request, waiter.IdToken), "Adding a table");

        actor.Remember(table);
        cleanup.TrackDelete($"table:{table.Id}", () => api.DeleteTableAsync(table.Id, bearerToken: waiter.IdToken));
        return table;
    }

    private static async Task ADishAsync(
        Actor actor, LocationResponse location, string key, DishType type, decimal price, WaiterSession waiter, CleanupScope cleanup)
    {
        var api = new DishesApi(actor.UsingTheApi());
        var request = SampleDataFactory.CreateDishRequest([location.Id], type) with { Price = price };
        var dish = await RequireAsync<DishResponse>(await api.CreateDishAsync(request, waiter.IdToken), $"Adding the {key}");

        actor.Remember(key, dish);
        cleanup.TrackDelete($"dish:{dish.Id}", () => api.DeleteDishAsync(dish.Id, force: true, bearerToken: waiter.IdToken));
    }

    private static async Task<CustomerAccount> TheHostAsync(Actor actor)
    {
        if (actor.RecallAll<CustomerAccount>().Count > 0)
            return actor.Recall<CustomerAccount>(Host);

        var host = await TheCredentials.RegisterAndSignInAsync(actor);
        actor.Remember(Host, host);
        return host;
    }

    private static async Task<ReservationResponse> ABookingAsync(
        Actor actor, TableResponse table, int guests, CustomerAccount host, CleanupScope cleanup)
    {
        // A random day weeks ahead at lunch time: far from the 15-minute notice and the 30-minute pre-order cut-off,
        // and on a table of its own, so parallel scenarios and repeated runs never overlap.
        var daysAhead = 14 + (BitConverter.ToUInt16(Guid.NewGuid().ToByteArray(), 0) % 280);
        var start = DateTime.UtcNow.Date.AddDays(daysAhead).AddHours(11);
        var api = new ReservationsApi(actor.UsingTheApi());
        var request = SampleDataFactory.CreateReservationRequest(table.Id, start, guests);
        var reservation = await RequireAsync<ReservationResponse>(
            await api.CreateReservationAsync(request, host.IdToken), "Booking the table");

        actor.Remember(reservation);
        cleanup.TrackDelete($"reservation:{reservation.Id}", () => api.CancelReservationAsync(reservation.Id, host.IdToken));
        return reservation;
    }

    private static async Task TheHostsPreOrderAsync(Actor actor, ReservationResponse reservation, CustomerAccount host)
    {
        var dish = actor.Recall<DishResponse>(HostsDish);
        var request = new CreateOrderRequest(reservation.Id, [new OrderItemRequest(dish.Id, 1)]);
        var response = await new OrdersApi(actor.UsingTheApi()).CreateOrderAsync(request, host.IdToken);
        if (!response.Ok)
            throw new InvalidOperationException(
                $"The host's own pre-order failed with {response.Status}: {await response.ReadErrorMessageAsync()}");
    }

    private static async Task<GroupDinner> TheInviteAsync(Actor actor, ReservationResponse reservation, CustomerAccount host)
    {
        var api = new GroupPreOrdersApi(actor.UsingTheApi());
        var token = await RequireAsync<InviteTokenResponse>(
            await api.GetInviteTokenAsync(reservation.Id, host.IdToken), "Getting the invite");
        var invite = await RequireAsync<GuestInviteResponse>(await api.GetInviteAsync(token.Token), "Opening the invite");

        return new GroupDinner(reservation.Id, invite.GroupReservationId, token.Token);
    }

    private static async Task<T> RequireAsync<T>(IAPIResponse response, string what)
    {
        if (!response.Ok)
            throw new InvalidOperationException($"{what} failed with {response.Status}: {await response.ReadErrorMessageAsync()}");

        return await response.ReadAsAsync<T>();
    }
}
