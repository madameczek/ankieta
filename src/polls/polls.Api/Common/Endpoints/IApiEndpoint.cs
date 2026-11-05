namespace Polls.Api.Common.Endpoints;

/// <summary>A group of REST endpoints; implementations only translate formats and call handlers.</summary>
internal interface IApiEndpoint
{
    void Map(IEndpointRouteBuilder app);
}
