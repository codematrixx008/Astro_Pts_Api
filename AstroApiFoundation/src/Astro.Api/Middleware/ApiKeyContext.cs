namespace Astro.Api.Middleware;

public sealed record ApiKeyContext(
    long ApiKeyId,
    long OrgId,
    IReadOnlyList<string> Scopes,
    string Prefix,
    int? DailyQuota,
    string? PlanCode
);




//public sealed class ApiKeyContext
//{
//    public long ApiKeyId { get; init; }
//    public long OrgId { get; init; }
//    public IReadOnlySet<string> Scopes { get; init; } = new HashSet<string>();

//    public int? DailyQuota { get; init; }   // NEW
//    public string? PlanCode { get; init; }  // NEW
//}
