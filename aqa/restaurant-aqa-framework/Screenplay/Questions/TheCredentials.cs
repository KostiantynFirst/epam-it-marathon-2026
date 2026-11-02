using Restaurant_Aqa_Framework.Api;
using Restaurant_Aqa_Framework.Api.Dtos.Auth;
using Restaurant_Aqa_Framework.Api.Seeding;
using Restaurant_Aqa_Framework.Screenplay.Tasks;

namespace Restaurant_Aqa_Framework.Screenplay.Questions;

/// <summary>The bearer token a <see cref="Party"/> calls the service with (null for someone not signed in). Another
/// customer is registered the first time they are needed; a guest is whoever joined the dinner first, or a new guest
/// if nobody has.</summary>
public sealed class TheCredentials(Party party) : IQuestion<string?>
{
    public const string AnotherCustomer = "another customer";

    public async Task<string?> AnsweredByAsync(Actor actor) => party switch
    {
        Party.Host => actor.Recall<CustomerAccount>(SetUpAGroupDinner.Host).IdToken,
        Party.AnotherCustomer => (await AnotherCustomerAsync(actor)).IdToken,
        Party.Guest => (await AGuestAsync(actor)).Token,
        Party.Waiter => (await actor.AsksForAsync(new TheWaiterSession())).IdToken,
        Party.Nobody => null,
        _ => throw new InvalidOperationException($"Unknown party: {party}"),
    };

    private static async Task<CustomerAccount> AnotherCustomerAsync(Actor actor)
    {
        if (actor.RecallAll<CustomerAccount>().FirstOrDefault(a => a != actor.Recall<CustomerAccount>(SetUpAGroupDinner.Host)) is { } known)
            return known;

        var account = await RegisterAndSignInAsync(actor);
        actor.Remember(AnotherCustomer, account);
        return account;
    }

    private static async Task<GroupGuest> AGuestAsync(Actor actor)
    {
        var guests = actor.RecallAll<GroupGuest>();
        if (guests.Count > 0)
            return guests[0];

        await actor.AttemptsToAsync(new JoinTheGroup("Curious guest"));
        return actor.Recall<GroupGuest>("Curious guest");
    }

    /// <summary>Registers a fresh customer and signs them in; the service always makes a newcomer a customer.</summary>
    public static async Task<CustomerAccount> RegisterAndSignInAsync(Actor actor)
    {
        var auth = new AuthApi(actor.UsingTheApi());
        var visitor = SampleDataFactory.RegisterRequest();

        var registered = await auth.RegisterAsync(visitor);
        if (!registered.Ok)
            throw new InvalidOperationException(
                $"Registering a customer failed with {registered.Status}: {await registered.ReadErrorMessageAsync()}");

        var signedIn = await auth.SignInAsync(new SignInRequest(visitor.Email, visitor.Password));
        if (!signedIn.Ok)
            throw new InvalidOperationException(
                $"Signing the customer in failed with {signedIn.Status}: {await signedIn.ReadErrorMessageAsync()}");

        var token = (await signedIn.ReadAsAsync<SignInResponse>()).IdToken;
        return new CustomerAccount(visitor.Email, visitor.Password, token);
    }
}
