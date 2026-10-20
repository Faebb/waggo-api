namespace Waggo.Api.Endpoints.Walkers.Requests;

/// <summary>Body of <c>POST /api/v1/admin/walkers/{id}/reject</c>. The reason is checked by the domain.</summary>
public sealed record RejectWalkerRequest(string? Reason);
