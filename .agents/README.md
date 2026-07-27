# Shared AI Configuration

- `AGENTS.md` is the single source of truth for project rules.
- `.agents/skills/` is the single source of truth for shared skills.
- `.agents/roles/` is the single source of truth for shared agent responsibilities.
- `.claude/knowledge/` is the single source of truth for the shared Knowledge Base.
- `.claude/agents/` and `.codex/agents/` only contain tool-format adapters.

After changing a shared skill, run the following command from the project root before using Claude Code:

```powershell
powershell -ExecutionPolicy Bypass -File .\.agents\Sync-ClaudeSkills.ps1
```

The script overwrites only the matching skill folders in `.claude/skills/`. Claude-only skills, such as `interview-prep`, are left untouched.
