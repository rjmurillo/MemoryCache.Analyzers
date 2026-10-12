using System.Reflection;

namespace MemoryCache.Analyzers.Tests;

/// <summary>
/// Checks that test classes and test methods in this assembly follow the
/// naming conventions in <c>CONTRIBUTING.md</c>.
/// </summary>
public class TestConventionTests
{
    /// <summary>
    /// Verifies that every type with a test method has a name that ends with <c>Tests</c>.
    /// </summary>
    [Fact]
    public void TestClassesEndWithTests()
    {
        string[] violations = FindTestMethods()
            .Select(method => method.DeclaringType!)
            .Distinct()
            .Where(type => !type.Name.EndsWith("Tests", StringComparison.Ordinal))
            .Select(type => type.FullName!)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"Test classes must end with 'Tests': {string.Join(", ", violations)}");
    }

    /// <summary>
    /// Verifies that no test method name contains an underscore.
    /// </summary>
    [Fact]
    public void TestMethodsHaveNoUnderscores()
    {
        string[] violations = FindTestMethods()
            .Where(method => method.Name.Contains('_', StringComparison.Ordinal))
            .Select(method => $"{method.DeclaringType!.Name}.{method.Name}")
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"Test method names must not contain '_': {string.Join(", ", violations)}");
    }

    private static IEnumerable<MethodInfo> FindTestMethods()
    {
        const BindingFlags publicMethods =
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

        return typeof(TestConventionTests).Assembly
            .GetTypes()
            .SelectMany(type => type.GetMethods(publicMethods))
            .Where(method => method.GetCustomAttributes<FactAttribute>(inherit: true).Any());
    }
}
