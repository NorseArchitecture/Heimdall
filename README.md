# Heimdall

> Heimdall — the watchman who decides who crosses Bifröst.

![Heimdall — the ever-vigilant watchman of the gods, keeper of the Bifröst, whose sight and hearing know no limit](https://github.com/user-attachments/assets/22732b59-696b-4ab6-9d8a-872ebf531d96 "Heimdall — the watchman who decides who crosses Bifröst")

*Image credit: [@norsemythologyclips](https://www.instagram.com/norsemythologyclips/) — go follow them.*

The authn story for the Norse Architecture — **`Norse.AuthN`**: login, register, forgot-password, 2FA setup, recovery, and reset, enforced identically across Blazor Server, WASM, and MAUI, with the backing gRPC service. It is the topmost realm in the dependency chain among the current submodules — nothing else rides above it.

## Status

`AuthN.Components`, `AuthN.Components.FluentUI`, and `AuthN.Services` are live — the Himinbjörg→Heimdall component migration moved the injection-clean subset (Login, Register, Logout, and their validators/requests) over mechanically; components with real backend injections (`UserManager`/`SignInManager`/`HttpContext`) exist only on Himinbjörg's unmerged `feature/identity-web-server` branch, pending the gRPC wireup slice. `AuthN.Services` was carved out of `AuthN.Components` to isolate `IAuthenticationService` (the gRPC contract) and its wire records (`LoginRequest`, `RegisterRequest`, `LogoutRequest`, `LoginResult`) from Razor/FluentValidation/Blazilla — a consumer building their own UI on the contract references only this thin assembly and wires it to whatever backend they choose; `IAuthenticationGateway`/`AuthenticationResult` (the Blazor-facing abstraction) stayed behind in `AuthN.Components`. Himinbjörg's and Yggdrasil's consuming code still points at the old `Norse.AuthN.Components` namespace for these types and will need a follow-up pass before the whole platform builds again. Pages still carry the ASP.NET Identity scaffold's `/Account/*` routes deliberately — renaming them is a separate, deferred curation pass. Design for what's next happens first: brainstorm → spec → plan, recorded in Glitnir's `docs/Heimdall/`, before any further project is scaffolded here.

## The cosmos

Heimdall is one realm of the [Norse Architecture](https://github.com/NorseArchitecture). The whole platform composes at [Bifröst](https://github.com/NorseArchitecture/Bifrost) — clone once, cross the bridge, and every session starts there so decisions get brainstormed across the entire landscape, not in isolation. Every design is tried in [Glitnir](https://github.com/NorseArchitecture/Glitnir), the design court, before code is forged here; this realm's specs and plans will live in the court's [docs/Heimdall/](https://github.com/NorseArchitecture/Glitnir/tree/master/docs/Heimdall) once they converge.

## Soundtrack: Heimdallr Vakir | Heimdall Awakens
[![Soundtrack: Heimdallr Vakir | Heimdall Awakens](https://img.youtube.com/vi/eg6fcDpvtkA/maxresdefault.jpg)](https://www.youtube.com/watch?v=eg6fcDpvtkA)
