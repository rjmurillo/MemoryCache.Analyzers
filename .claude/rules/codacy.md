# Codacy

These rules apply only when Codacy MCP server tools are available.
If they are not, skip this file.

## Tool arguments

For any tool that takes `provider`, `organization`, or `repository`, use:

- provider: `gh`
- organization: `rjmurillo`
- repository: `MemoryCache.Analyzers`

Avoid `git remote -v` unless you need it.

## After you edit a file

- Run `codacy_cli_analyze` for each edited file.
  - `rootPath`: the workspace path.
  - `file`: the path of the edited file.
  - `tool`: leave unset.
- If it finds issues in your edits, fix them.

## After you change dependencies

- After any package change, run `codacy_cli_analyze` with:
  - `rootPath`: the workspace path.
  - `tool`: `trivy`.
  - `file`: leave unset.
- If the new packages bring in vulnerabilities, stop.
  Fix the security issues first, then resume the task.

## General

- Repeat the steps for each changed file.
- "Fix" means suggest the fix and apply it when you can.
- Do not wait for the user to ask for analysis.
- Do not use `codacy_cli_analyze` for duplication, complexity, or coverage.
- Do not install the Codacy CLI by hand with brew, npm, npx, or any package manager.
- Only send provider, organization, and repository when the project is a git repository.

## When a Codacy tool returns 404

- Offer to run `codacy_setup_repository`. Never run it on your own.
- If the user agrees, run it, then retry the failed call once.
