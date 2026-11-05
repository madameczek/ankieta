using System.Runtime.CompilerServices;
using AwesomeAssertions;
using NetArchTest.Rules;
using Polls.Api.Common.Results;

namespace Polls.Architecture.Tests;

public sealed class LayerTests
{
    private static Types ApiTypes => Types.InAssembly(typeof(Error).Assembly);

    [Fact]
    public void Domain_DependsOnlyOnItself_HasNoInfrastructureOrWebDependencies()
    {
        var result = ApiTypes
            .That().ResideInNamespaceMatching(@"^Polls\.Api\.\w+\.Domain")
            .ShouldNot().HaveDependencyOnAny(
                "Microsoft.EntityFrameworkCore",
                "Microsoft.AspNetCore",
                "Polls.Api.Infrastructure",
                "System.Net.Http")
            .GetResult();

        result.FailingTypeNames.Should().BeNullOrEmpty();
    }

    [Fact]
    public void Core_IsHttpAgnostic_HasNoAspNetCoreDependency()
    {
        var result = ApiTypes
            .That().ResideInNamespaceMatching(@"^Polls\.Api\.\w+\.Core")
            .ShouldNot().HaveDependencyOn("Microsoft.AspNetCore")
            .GetResult();

        result.FailingTypeNames.Should().BeNullOrEmpty();
    }

    [Fact]
    public void ConcreteClasses_AreSealed()
    {
        var result = ApiTypes
            .That().AreClasses().And().AreNotAbstract()
            .And().DoNotHaveCustomAttribute(typeof(CompilerGeneratedAttribute))
            .And().DoNotHaveName("Program")
            .Should().BeSealed()
            .GetResult();

        result.FailingTypeNames.Should().BeNullOrEmpty();
    }

    [Fact]
    public void Types_AreInternal_ExceptProgram()
    {
        var result = ApiTypes
            .That().AreNotNested()
            .And().DoNotHaveCustomAttribute(typeof(CompilerGeneratedAttribute))
            .And().DoNotHaveName("Program")
            .ShouldNot().BePublic()
            .GetResult();

        result.FailingTypeNames.Should().BeNullOrEmpty();
    }
}
