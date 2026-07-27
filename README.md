# Heimdall

> Heimdall — the watchman who decides who crosses Bifröst.

![Heimdall — the ever-vigilant watchman of the gods, keeper of the Bifröst, whose sight and hearing know no limit](https://github.com/user-attachments/assets/22732b59-696b-4ab6-9d8a-872ebf531d96 "Heimdall — the watchman who decides who crosses Bifröst")

*Image credit: [@norsemythologyclips](https://www.instagram.com/norsemythologyclips/) — go follow them.*

The authn story for the Norse Architecture — **`Norse.AuthN`**: login, register, forgot-password, 2FA setup, recovery, and reset, enforced identically across Blazor Server, WASM, and MAUI, with the backing gRPC service. Code-wise it still depends on nothing above Asgard, but since Himinbjörg's `feature/identity-web-server` merge (`v0.0.5`) the relationship between the two realms runs both ways: Himinbjörg's `Identity.Web.Server` NorseRefs this repo's `AuthN.Services` and `AuthN.Components.FluentUI` to implement the gRPC contract and host its pages, so Heimdall isn't unambiguously the topmost realm in the dependency chain anymore.

## Status

`AuthN.Components`, `AuthN.Components.FluentUI`, and `AuthN.Services` are live — the Himinbjörg→Heimdall component migration moved the injection-clean subset (Login, Register, Logout, and their validators/requests) over mechanically; components with real backend injections (`UserManager`/`SignInManager`/`HttpContext`) still live on Himinbjörg's side, in `Identity.Web.Server`'s `Components/Pages/**` tree — that branch merged to Himinbjörg's `master` (tagged `v0.0.5`) and the gRPC wireup it was pending on is done, so what remains is only migrating that page tree over here. `AuthN.Services` was carved out of `AuthN.Components` to isolate `IAuthenticationService` (the gRPC contract) and its wire records (`LoginRequest`, `RegisterRequest`, `LogoutRequest`, `LoginResult`, `LogoutResult`) from Razor/FluentValidation/Blazilla — a consumer building their own UI on the contract references only this thin assembly and wires it to whatever backend they choose. The hand-written `IAuthenticationGateway`/`AuthenticationResult` are gone — a follow-up slice retired both; `IAuthenticationService` carries Asgard's `[GenerateGateway]` and `AuthN.Services` still emits the generated gateway interface, but components no longer consume it. Instead, Login/Register/Logout.razor inject `IAuthenticationService` directly — each host (Himinbjörg's `Identity.Web.Server` for Blazor Server, Yggdrasil for WASM) registers its own implementation via DI, and components stay transport-dumb because the substitution is the seam. Components consume `Task<Outcome<T>>` and pattern-match the result; there is no more `AuthenticationResult` wrapper anywhere. Himinbjörg's and Yggdrasil's consuming code has already been updated to the split `Norse.AuthN.Components`/`Norse.AuthN.Services` namespaces. Pages still carry the ASP.NET Identity scaffold's `/Account/*` routes deliberately — renaming them is a separate, deferred curation pass. Design for what's next happens first: brainstorm → spec → plan, recorded in Glitnir's `docs/Heimdall/`, before any further project is scaffolded here.

## The cosmos

Heimdall is one realm of the [Norse Architecture](https://github.com/NorseArchitecture). The whole platform composes at [Bifröst](https://github.com/NorseArchitecture/Bifrost) — clone once, cross the bridge, and every session starts there so decisions get brainstormed across the entire landscape, not in isolation. Every design is tried in [Glitnir](https://github.com/NorseArchitecture/Glitnir), the design court, before code is forged here; this realm's specs and plans will live in the court's [docs/Heimdall/](https://github.com/NorseArchitecture/Glitnir/tree/master/docs/Heimdall) once they converge.

## Soundtrack: Heimdallr Vakir | Heimdall Awakens
[![Soundtrack: Heimdallr Vakir | Heimdall Awakens](https://img.youtube.com/vi/eg6fcDpvtkA/maxresdefault.jpg)](https://www.youtube.com/watch?v=eg6fcDpvtkA)
