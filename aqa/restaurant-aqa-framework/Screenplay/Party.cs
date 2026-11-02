namespace Restaurant_Aqa_Framework.Screenplay;

/// <summary>Who is asking to see or change the group pre-order.</summary>
public enum Party
{
    /// <summary>The customer who booked the table and invited the guests.</summary>
    Host,

    /// <summary>A different signed-in customer.</summary>
    AnotherCustomer,

    /// <summary>One of the invited guests, with their guest-session token.</summary>
    Guest,

    /// <summary>The restaurant's waiter.</summary>
    Waiter,

    /// <summary>Someone who is not signed in at all.</summary>
    Nobody,
}
