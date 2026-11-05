namespace Polls.Api.Common;

internal static class RouteConsts
{
    public const string Api = "/api";
    public const string HealthLive = Api + "/health/live";
    public const string HealthReady = Api + "/health/ready";
    public const string Docs = Api + "/docs";
    public const string OpenApi = Api + "/openapi/{documentName}.json";

    public const string ReadyTag = "ready";
}
