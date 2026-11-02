namespace Restaurant_Aqa_Framework.Screenplay;

/// <summary>A guest who joined a group dinner through the invite: their session and the token that proves it is theirs.</summary>
public sealed record GroupGuest(string Name, string SessionId, string Token);
