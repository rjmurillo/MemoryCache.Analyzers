using System.Diagnostics;

namespace MemoryCache.Analyzers;

/// <summary>
/// Flags usage of System.Runtime.Caching.MemoryCache in .NET Core+ projects. Use Microsoft.Extensions.Caching.Memory instead.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SystemRuntimeCachingAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// Diagnostic ID for MemoryCache usage in .NET Core+.
    /// </summary>
#pragma warning disable ECS0200 // Roslyn analyzers require const for DiagnosticId
    public const string DiagnosticId = "SRC1000";
#pragma warning restore ECS0200

#pragma warning disable ECS1300 // Roslyn analyzers require inline initialization for DiagnosticDescriptor
    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        title: "Avoid System.Runtime.Caching.MemoryCache in .NET Core+",
        messageFormat: "Do not use System.Runtime.Caching.MemoryCache in .NET Core or .NET 5+. Use Microsoft.Extensions.Caching.Memory instead.",
        category: "Compatibility",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "System.Runtime.Caching.MemoryCache is a compatibility bridge for porting from .NET Framework. Prefer Microsoft.Extensions.Caching.Memory in .NET Core+.",
        helpLinkUri: $"https://github.com/rjmurillo/MemoryCache.Analyzers/blob/{ThisAssembly.GitCommitId}/docs/rules/{DiagnosticId}.md");
#pragma warning restore ECS1300

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(RegisterCompilationStartAction);
    }

    private static void RegisterCompilationStartAction(CompilationStartAnalysisContext compilationContext)
    {
        KnownSymbols knownSymbols = new(compilationContext.Compilation);

        // Only analyze if System.Runtime.Caching is referenced
        INamedTypeSymbol? memoryCacheType = knownSymbols.SystemRuntimeCachingMemoryCache;
        if (memoryCacheType is null)
        {
            return;
        }

        compilationContext.RegisterOperationAction(ctx => AnalyzeObjectCreation(ctx, memoryCacheType), OperationKind.ObjectCreation);
    }

    private static void AnalyzeObjectCreation(OperationAnalysisContext context, INamedTypeSymbol memoryCacheType)
    {
        Debug.Assert(context.Operation is IObjectCreationOperation, "The action is registered only for OperationKind.ObjectCreation.");
        IObjectCreationOperation creation = (IObjectCreationOperation)context.Operation;

        if (!ITypeSymbolExtensions.IsOrDerivedFrom(creation.Type, memoryCacheType))
        {
            return;
        }

        // Only analyze if targeting .NET Core+ (not .NET Framework)
        if (context.Compilation.IsTargetingNetFramework() == true)
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Rule, creation.Syntax.GetLocation()));
    }
}
