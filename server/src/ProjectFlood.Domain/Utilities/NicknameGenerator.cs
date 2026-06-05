namespace ProjectFlood.Domain.Utilities;

public sealed class NicknameGenerator
{
    private static readonly string[] Adjectives =
    [
        "Bright",
        "Calm",
        "Swift",
        "Lucky",
        "Brave",
        "Fresh"
    ];

    private static readonly string[] Nouns =
    [
        "Wave",
        "Drop",
        "Bloom",
        "Spark",
        "Stone",
        "Leaf"
    ];

    public string Generate()
    {
        var rng = Random.Shared;
        return Adjectives[rng.Next(Adjectives.Length)]
             + Nouns[rng.Next(Nouns.Length)]
             + rng.Next(100, 1000);
    }
}
