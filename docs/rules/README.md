# Diagnostics / rules

| ID | Category | Severity | Code fix | Title | Implementation file |
| --- | --- | --- | --- | --- | --- |
| [SRC1000](SRC1000.md) | Compatibility | Warning | No | Avoid System.Runtime.Caching.MemoryCache in .NET Core+ | [SystemRuntimeCachingAnalyzer](../../src/MemoryCache.Analyzers/SystemRuntimeCachingAnalyzer.cs) |

## Guidance for future rules

### Categories

- **Compatibility**: Rules that guide users to compatible solutions for their target framework

### Diagnostic ID ranges

| Range | Category | Description |
| --- | --- | --- |
| SRC1000-1099 | Compatibility | Ensures compatible solutions for the target framework |

- When adding new rules, assign the next available ID in the appropriate category range.
- Document new rules in this table, including their category, severity, code fix availability, title, and links to both documentation and implementation.
- For more, see the root [README.md](../../README.md).