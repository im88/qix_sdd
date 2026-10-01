# Qix SDD Constitution

## Core Principles

### I. Simplicity First (YAGNI)

- Every feature MUST be implemented in the smallest form that satisfies its spec's acceptance
  scenarios. Do not build speculative features, settings, or extension points.
- Do not add abstractions (interfaces, layers, patterns, generic frameworks) until at least two
  concrete uses exist, unless the spec requires them.
- Any deviation (an extra project, an extra dependency, or a non-obvious design) MUST be listed with
  a justification in the Complexity Tracking table of the feature's `plan.md`.

**Rationale**: A small arcade game gains nothing from enterprise architecture. Keeping the code
small keeps it readable, easy to change, and quick to verify.

### II. Lightweight, Targeted Testing

- Automated tests are REQUIRED for core and risky game logic: territory claiming and fill (area
  calculation), collision detection between the player, the Qix, and the Sparx, scoring, and
  win/lose conditions.
- Automated tests are OPTIONAL for console rendering, input handling, and other glue code. For
  these, a documented manual playtest is acceptable verification.
- TDD is permitted but not mandated. Tests MUST pass before a feature is considered done.
- Test suites MUST stay fast and deterministic: no real-time waits and no dependence on the real
  console. Inject randomness and time where tests need to control them.

**Rationale**: Tests go where bugs are costly and hard to spot by playing. Code you can check by
playing it for a minute does not need a test harness.

### III. Always Playable

- The game MUST build and launch from the command line after every completed user story.
- Each user story MUST deliver a change the player can observe when running the game, so that it
  can be demoed and verified on its own.
- A feature MUST NOT leave the game unplayable or crashing on the main branch.

**Rationale**: A runnable game after every increment keeps feedback loops short. It also matches
Spec Kit's model of user stories that are independently testable.

## Technology & Platform Constraints

- **Language/runtime**: C# on the current .NET LTS release. Nullable reference types MUST be
  enabled.
- **Application type**: Console (CLI) application. The game renders in the terminal with
  characters and colors and reads keyboard input. There is no GUI framework and no web front end.
- **Platform**: The game MUST run in Windows Terminal. Cross-platform terminal support
  (Linux/macOS) is desirable but not required unless a spec asks for it.
- **Dependencies**: Prefer the .NET base class library. Each third-party NuGet package MUST be
  justified in the feature's `plan.md` (see Principle I).
- **Testing**: Use one test project with a single standard .NET test framework (xUnit by default).
- **Build**: `dotnet build` and `dotnet test` from the repository root MUST be the only commands
  needed to build and verify the solution.

## Development Workflow

- Work follows the Spec Kit flow: `/speckit-specify` → `/speckit-clarify` (as needed) →
  `/speckit-plan` → `/speckit-tasks` → `/speckit-implement`.
- Every `plan.md` MUST pass the Constitution Check gate against this document before design work
  starts, and again after design.
- Definition of done for a feature:
  1. `dotnet build` succeeds with no errors.
  2. `dotnet test` passes.
  3. The game launches, and a manual playtest of the feature's acceptance scenarios succeeds.
- Gameplay changes MUST state in their spec which Qix behaviors they affect (player movement,
  drawing, the Qix, the Sparx, claiming, scoring, levels).

## Governance

- This constitution overrides conflicting practices or guidance elsewhere in the repository.
- **Amendments** are made with `/speckit-constitution`. Each amendment MUST update the version and
  the Last Amended date, and MUST state its rationale in the commit message.
- **Versioning** follows semantic versioning:
  - MAJOR: a principle is removed or redefined in a backward-incompatible way.
  - MINOR: a principle or section is added, or guidance is materially expanded.
  - PATCH: clarifications, wording changes, typo fixes.
- **Compliance**: `/speckit-plan` (Constitution Check) and `/speckit-analyze` verify that features
  comply. A violation MUST be either fixed or justified in Complexity Tracking. An unjustified
  violation blocks implementation.

**Version**: 1.0.0 | **Ratified**: 2026-10-01 | **Last Amended**: 2026-10-01
