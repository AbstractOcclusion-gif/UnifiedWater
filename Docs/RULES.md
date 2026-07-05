# Coding Standards

## Constants & Naming

- **No magic numbers** — every literal value lives in a named constant in a dedicated constants file, never inline
- **No hardcoded strings** — use enums or consts
- **Descriptive names** — no abbreviations except well-known ones (i, ctx, cfg)

## Structure & Flow

- **Composition over inheritance**
- **Short, single-responsibility functions** — if it does two things, split it
- **Early returns over deep nesting** — guard clauses first
- **Top-down readable code** — a reader shouldn't have to jump around
- **Cohesive modules** — group related logic together

## DRY — one implementation, always

- **Never duplicate logic.** If a calculation, method, or piece of logic is needed in more than one place, there is exactly ONE implementation and every caller uses it.
- **No redundant math or copy-pasted functions.** When something is needed twice, extract it into a shared helper and call it — never write a second copy.
- This is an *internal* rule: it is about our own codebase having a single source of truth, not about importing code from anywhere.

## Error Handling

- **Fail fast with clear error messages** — don't silently swallow failures
- **Validate inputs at boundaries** — trust nothing from the outside

## Code Hygiene

- **Comments explain WHY, not WHAT** — the code says what, the comment says why
- **No dead code** — no commented-out blocks, no unused functions
- **Consistent formatting** — follow the language's conventions (no debates)
- **Prefer immutability** — mutate only when you have a reason
- **Minimize public API surface** — expose only what's needed

## Reference assets are inspiration only

- The Evan Wallace port, KWS Water, Crest, RAM, and Stylized Water are **references to learn from**, in their own projects. Take ideas and approaches; **never import, embed, or depend on their code.** Unified Water is standalone and self-contained.
- Everything in this project is written fresh in our own namespace.

## Always follow rules

- **Never guess, never invent, always check** — verify your claims
- **Ask before touching code** — never write code without my express authorisation
