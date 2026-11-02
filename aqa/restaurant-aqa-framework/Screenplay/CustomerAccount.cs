namespace Restaurant_Aqa_Framework.Screenplay;

/// <summary>A customer registered by the scenario (the host, or someone else) and signed in to the service.</summary>
public sealed record CustomerAccount(string Email, string Password, string IdToken);
