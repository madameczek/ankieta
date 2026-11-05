using System.Reflection;

namespace Polls.Api.Common.Handlers;

internal static class HandlerRegistration
{
    /// <summary>Registers every concrete <see cref="IHandler"/> (internal types included) as scoped, by its own type.</summary>
    public static IServiceCollection RegisterHandlersFromAssemblyContaining<TMarker>(this IServiceCollection services)
    {
        foreach (var type in ConcreteTypesOf<IHandler>(typeof(TMarker).Assembly))
        {
            services.AddScoped(type);
        }

        return services;
    }

    internal static IEnumerable<Type> ConcreteTypesOf<TInterface>(Assembly assembly) =>
        assembly.DefinedTypes
            .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(TInterface).IsAssignableFrom(t))
            .Select(t => t.AsType());
}
