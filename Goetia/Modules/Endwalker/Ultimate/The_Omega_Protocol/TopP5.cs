namespace Goetia.Modules;

/// <summary>TOP (Territory 1122) P5 Dynamis shared action/status ids.</summary>
internal static class TopP5
{
    public const uint Territory = 1122;

    public const uint CastDynamisDelta = 31624;
    public const uint CastDynamisSigma = 32788;
    public const uint CastDynamisOmega = 32789;

    public const uint StatusFirstInLine = 3004;
    public const uint StatusSecondInLine = 3005;
    public const uint StatusHelloNear = 3442;
    public const uint StatusHelloFar = 3443;
    public const uint StatusDynamis = 3444;

    public static readonly IReadOnlySet<uint> Territories = new HashSet<uint> { Territory };

    public static bool HasNearOrFar(ModuleContext ctx, int seat) =>
        ctx.HasStatus(seat, StatusHelloNear) || ctx.HasStatus(seat, StatusHelloFar);

    public static bool AnyNearOrFar(ModuleContext ctx) =>
        ctx.AnyPartyHasStatus(StatusHelloNear) || ctx.AnyPartyHasStatus(StatusHelloFar);
}
