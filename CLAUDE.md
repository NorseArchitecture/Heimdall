# CLAUDE.md — Heimdall (`Norse.AuthN`)

## 0. Wrong Root — Halt

Session root must be **Bifröst**, not this repo directly — org-wide settings (`superpowers`, permission rules) only apply from the actual root, and Claude Code never merges a submodule's own `.claude/settings.json` into a parent-launched session. If `claude` was run from inside **Heimdall**, stop: don't read further, don't propose changes, don't run anything — tell the user to `cd ../Bifrost` and start there. (This repo's `.claude/settings.json` carries a `SessionStart` hook meant to block this before you ever see this file; if you're reading this anyway, the hook was bypassed, disabled, or failed — halt regardless.)

> **Do not commit, push, or rewrite git history** — stage (`git add`), show the diff, stop; the human reviews and commits. This applies even when a skill's flow includes a commit step. **US English spelling** everywhere — code, comments, docs, commits.

## 1. What This Repository Is

Heimdall is **the gate** — `Norse.AuthN`: the authn story built on Himinbjörg, presenting login, register, forgot-password, 2FA setup, recovery, and reset uniformly across Blazor Server, WASM, and MAUI, plus the backing gRPC service. It is the topmost realm in the dependency chain among the current submodules — nothing else rides above it.

`AuthN.Components` and `AuthN.Components.FluentUI` are live — the Himinbjörg→Heimdall component migration moved the injection-clean subset (Login, Register, Logout, and their validators/requests) over mechanically; components with real backend injections (`UserManager`/`SignInManager`/`HttpContext`) exist only on Himinbjörg's unmerged `feature/identity-web-server` branch, pending the gRPC wireup slice. Pages still carry the ASP.NET Identity scaffold's `/Account/*` routes deliberately — route renaming is a separate, deferred curation pass, not bundled into relocation. Before writing anything beyond this slice: brainstorm → spec → plan, recorded in `../Glitnir/docs/Heimdall/`, per the org's spec-first discipline. Do not scaffold ahead of a converged spec. Every plan's REQUIRED SUB-SKILL line names `superpowers:subagent-driven-development` as the default (not a recommendation among equals — `executing-plans` is the narrow fallback for separate-session review checkpoints) paired with `superpowers:test-driven-development` — implementation here is subagent-orchestrated and test-driven, never one without the other (`../Glitnir/CLAUDE.md` §2.8).

See `../Bifrost/CLAUDE.md` (§2 The Naming Model) and `../Glitnir/CLAUDE.md` (§3 Bounded Context Map) for the full realm table and how Heimdall fits the rest of the cosmos.
