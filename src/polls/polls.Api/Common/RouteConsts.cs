namespace Polls.Api.Common;

internal static class RouteConsts
{
    public const string Api = "/api";
    public const string Health = Api + "/health";
    public const string HealthLive = Health + "/live";
    public const string HealthReady = Health + "/ready";
    public const string Docs = Api + "/docs";
    public const string OpenApi = Api + "/openapi/{documentName}.json";

    public const string ReadyTag = "ready";
}
