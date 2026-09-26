namespace Goetia.Modules;

/// <summary>DSR (Territory 968) shared action/status ids.</summary>
internal static class Dsr
{
    public const uint Territory = 968;

    public const uint CastWrathOfTheHeavens = 27529;
    public const uint CastWrothFlames = 27973;

    public const uint StatusThunderstruck = 2833;
    public const uint StatusSpreadingFlames = 2758;
    public const uint StatusEntangledFlames = 2759;

    public static readonly IReadOnlySet<uint> Territories = new HashSet<uint> { Territory };

    public static bool AnyWrothFlames(ModuleContext ctx) =>
        ctx.AnyPartyHasStatus(StatusSpreadingFlames) || ctx.AnyPartyHasStatus(StatusEntangledFlames);
}
