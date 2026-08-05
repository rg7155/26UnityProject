# Rewind Survivors — Claude Code Adapter

모든 프로젝트 규칙·기술 결정·코딩 스타일·커밋 규칙은 루트의 `AGENTS.md`가 단일 원본이다. 아래 import로 자동 로드된다.

@AGENTS.md

- 공통 스킬의 원본은 `.agents/skills/`이다.
- Claude가 인식하는 `.claude/skills/`는 동기화 사본이다. 공통 스킬을 수정한 뒤에는 프로젝트 루트에서 `powershell -ExecutionPolicy Bypass -File .\.agents\Sync-ClaudeSkills.ps1`를 실행한다.
- Knowledge Base의 단일 원본은 `.claude/knowledge/`이다. 기능 구현·버그 수정 전 검색하고, 재발 위험이 있는 문제를 해결하면 해당 워크플로에 따라 기록한다.
- `.claude/agents/`는 Claude 형식의 에이전트 어댑터이고, `.codex/agents/`는 Codex TOML 어댑터다. 공통 프로젝트 규칙을 이 파일들에 중복하지 않는다.

Claude 전용 권한 설정은 `.claude/settings.local.json`에만 둔다. 이 파일은 공통 프로젝트 규칙이나 동기화 대상이 아니다.
