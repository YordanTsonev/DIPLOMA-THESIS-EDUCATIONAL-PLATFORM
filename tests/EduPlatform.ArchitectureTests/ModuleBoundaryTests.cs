using System.Reflection;
using EduPlatform.BuildingBlocks.Application.Messaging;
using EduPlatform.BuildingBlocks.Domain;
using NetArchTest.Rules;
using Shouldly;

namespace EduPlatform.ArchitectureTests;

/// <summary>
/// These tests are what turns "modular monolith" from an intention into a rule.
/// Without them the module boundaries erode within weeks: someone adds a using directive,
/// it compiles, and the architecture quietly becomes a big ball of mud.
/// </summary>
public sealed class ModuleBoundaryTests
{
    private const string ModulesNamespacePrefix = "EduPlatform.Modules";

    private static readonly Assembly[] ModuleAssemblies = DiscoverAssemblies(ModulesNamespacePrefix);

    [Fact]
    public void Domain_layer_must_not_depend_on_infrastructure_concerns()
    {
        foreach (var assembly in DiscoverAssemblies(".Domain"))
        {
            var result = Types.InAssembly(assembly)
                .ShouldNot()
                .HaveDependencyOnAny(
                    "Microsoft.EntityFrameworkCore",
                    "Npgsql",
                    "Microsoft.AspNetCore",
                    "StackExchange.Redis",
                    "Serilog")
                .GetResult();

            result.IsSuccessful.ShouldBeTrue(
                $"{assembly.GetName().Name} is a domain assembly and must stay free of infrastructure. " +
                $"Offenders: {Describe(result)}");
        }
    }

    [Fact]
    public void Modules_must_not_reference_another_modules_internals()
    {
        foreach (var assembly in ModuleAssemblies)
        {
            var ownModule = ExtractModuleName(assembly.GetName().Name!);

            // A module may only see another module through its Contracts assembly.
            var forbidden = ModuleAssemblies
                .Select(other => other.GetName().Name!)
                .Where(name => !name.EndsWith(".Contracts", StringComparison.Ordinal))
                .Where(name => !ExtractModuleName(name).Equals(ownModule, StringComparison.Ordinal))
                .ToArray();

            if (forbidden.Length == 0)
            {
                continue;
            }

            var result = Types.InAssembly(assembly)
                .ShouldNot()
                .HaveDependencyOnAny(forbidden)
                .GetResult();

            result.IsSuccessful.ShouldBeTrue(
                $"Module '{ownModule}' reaches into another module's internals. " +
                $"Go through that module's Contracts assembly instead. Offenders: {Describe(result)}");
        }
    }

    [Fact]
    public void Application_layer_must_not_depend_on_its_own_infrastructure()
    {
        foreach (var assembly in DiscoverAssemblies(".Application"))
        {
            var result = Types.InAssembly(assembly)
                .ShouldNot()
                .HaveDependencyOn("Microsoft.EntityFrameworkCore")
                .GetResult();

            result.IsSuccessful.ShouldBeTrue(
                $"{assembly.GetName().Name} must describe what it needs through interfaces, " +
                $"not reach for EF Core directly. Offenders: {Describe(result)}");
        }
    }

    [Fact]
    public void Command_and_query_handlers_must_be_sealed()
    {
        foreach (var assembly in DiscoverAssemblies(".Application"))
        {
            HandlersMustBeSealed(assembly, typeof(ICommandHandler<,>));
            HandlersMustBeSealed(assembly, typeof(IQueryHandler<,>));
        }
    }

    /// <remarks>
    /// Only concrete classes are checked. The abstractions in BuildingBlocks —
    /// <see cref="ICommandHandler{TCommand}"/> and <see cref="CommandHandler{TCommand}"/> —
    /// exist precisely to be implemented and inherited from.
    /// </remarks>
    private static void HandlersMustBeSealed(Assembly assembly, Type handlerInterface)
    {
        var result = Types.InAssembly(assembly)
            .That()
            .AreClasses()
            .And()
            .AreNotAbstract()
            .And()
            .ImplementInterface(handlerInterface)
            .Should()
            .BeSealed()
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(
            $"Handlers are use cases, not extension points — seal them. Offenders: {Describe(result)}");
    }

    [Fact]
    public void Domain_events_must_be_immutable_records()
    {
        foreach (var assembly in DiscoverAssemblies(".Domain"))
        {
            var offenders = Types.InAssembly(assembly)
                .That()
                .ImplementInterface(typeof(IDomainEvent))
                .GetTypes()
                .Where(type => type is { IsClass: true, IsAbstract: false })
                .Where(type => !IsRecord(type))
                .Select(type => type.Name)
                .ToArray();

            offenders.ShouldBeEmpty(
                "A domain event describes something that already happened, so it must be an " +
                "immutable record. Offenders: " + string.Join(", ", offenders));
        }
    }

    private static bool IsRecord(Type type) =>
        type.GetMethod("<Clone>$", BindingFlags.Public | BindingFlags.Instance) is not null;

    private static string ExtractModuleName(string assemblyName)
    {
        // EduPlatform.Modules.Identity.Application -> Identity
        var parts = assemblyName.Split('.');
        return parts.Length >= 3 ? parts[2] : assemblyName;
    }

    private static string Describe(TestResult result) =>
        result.FailingTypeNames is { Count: > 0 }
            ? string.Join(", ", result.FailingTypeNames)
            : "(none reported)";

    /// <summary>
    /// Loads the solution's own assemblies from the test output directory. Assemblies are
    /// only in memory once something has touched them, so scanning the directory is what
    /// makes these tests catch a module that nothing references yet.
    /// </summary>
    private static Assembly[] DiscoverAssemblies(string nameFragment)
    {
        var directory = Path.GetDirectoryName(typeof(ModuleBoundaryTests).Assembly.Location)!;

        return [.. Directory.EnumerateFiles(directory, "EduPlatform.*.dll")
            .Where(path => Path.GetFileNameWithoutExtension(path).Contains(nameFragment, StringComparison.Ordinal))
            .Select(TryLoad)
            .Where(assembly => assembly is not null)
            .Select(assembly => assembly!)];
    }

    private static Assembly? TryLoad(string path)
    {
        try
        {
            return Assembly.LoadFrom(path);
        }
        catch (BadImageFormatException)
        {
            return null;
        }
    }
}
