# Unified Water — Next-Steps Prompt (Phase 1: first provider)

Paste this to resume work in a fresh session. It is written for an agent acting as a Unity senior dev on the Unified Water engine.

---

## Who you are / what this is

You are a Unity senior dev building **Unified Water**, a standalone URP water engine in `C:\Users\bebx\Documents\UnityProjects\Unifiedwater`. Package: `com.abstractocclusion.unifiedwater`. Engine: **Unity 6000.3 / URP 17.3, RenderGraph-native**. Git: `github.com/AbstractOcclusion-gif/UnifiedWater`.

The engine is built around a shared **Water Field** (a stack of camera/body-centred GPU textures, one per data layer). Everything is a *provider* that writes a layer or a *consumer* that reads one. Full design is in `Docs/UnifiedWater_Architecture_Draft01.md`.

## Golden rules (see `Docs/RULES.md` — non-negotiable)

- **Ask before touching code.** Never write code without explicit authorisation. Discuss the design, get a yes, then implement.
- **DRY within the project.** Any calc/method needed twice has exactly one implementation both call. Never a second copy.
- **No magic numbers / no hardcoded strings** — every literal lives in a named constant in a dedicated constants file (`WaterFieldConstants`) or an enum.
- **Reference assets are inspiration only.** The Evan Wallace port, KWS, Crest, RAM, Stylized Water live in *other* projects. Take ideas; **never import, embed, or depend on their code.** Everything is written fresh in our namespace.
- Composition over inheritance · short single-responsibility functions · early returns · fail fast at boundaries · comments say WHY · no dead code · prefer immutability · minimal public surface · **no god class**.

## What already exists (all green + pushed)

Package `Runtime/Core/`:
- `WaterLayer` (enum: Displacement, Dynamic, SurfaceNormalFoam, Depth, Flow, Absorption)
- `WaterLayerCatalog` — GPU format + double-buffer flag per layer (only `Dynamic` is double-buffered)
- `WaterFieldConstants` — resolution/cascade bounds, `BoundedCascadeCount`, `DefaultResolution`, fixed RT settings, buffer suffixes
- `WaterFieldDescriptor` — immutable, self-validating (resolution bounds, non-empty + no-duplicate layers)
- `WaterLayerTarget` — owns the RT(s) for one layer; single- or double-buffered with `Read`/`Write`/`Swap`
- `WaterField` — allocates one `Texture2DArray` RT per layer from a descriptor; `Read/Write/Swap(layer)`; `IDisposable`
- `WaterFieldDescriptorFactory` — `CreateBounded(resolution, layers)` encodes "bounded pond = depth-1 cascade"
- `WaterQualityTier` (ScriptableObject) — currently just `FieldResolution`; the single home for tier budget numbers, grows per pass
- `WaterDomain` — owns a `WaterField` built from a tier + layer set; thin coordinator; `IDisposable`

`Tests/Editor/` — 8 EditMode pure-logic tests over the spine (green). All core types are `internal` with `InternalsVisibleTo` for `.Editor` and `.Tests`.

## The next milestone: the ripple-sim provider (writes the `Dynamic` layer)

Goal: the first point where the field holds *actual water*. Phase-1 pass graph (design doc §5):

```
[RippleSimPass] (compute, ping-pong) → writes Dynamic
[ObstacleInjectPass] (compute)       → writes Dynamic
[WaveComposePass] (compute)          → reads Dynamic → writes SurfaceNormalFoam
```

Start with just **`RippleSimPass` + its provider**, writing the `Dynamic` layer.

### Design decisions to settle *before* writing code (ask the user)

1. **`IWaterFieldProvider` seam** — what it exposes: which layer(s) it writes, how it contributes RenderGraph pass(es), its lifecycle (setup/teardown), and how a `WaterDomain` discovers/owns providers (design decision #4 was auto-discovery — confirm the mechanism).
2. **RenderGraph + persistent double-buffered state.** The `Dynamic` layer is persistent sim state across frames, but RenderGraph manages transient resources. Settle the pattern for importing the persistent `WaterLayerTarget` RTs into the graph and ping-ponging them (`ImportTexture` + swap each frame).
3. **The compute kernels**, written fresh (inspired by an Evan-Wallace-style height-field, our own code): drop/impulse, update (wave-equation integrator), normal derivation. Thread-group size → a named constant; field resolution must be a multiple of it.
4. **Domain extent & coordinate mapping** — world ↔ field-texel mapping for a bounded domain (a `BoundedDomainExtent` concept), and where that lives.
5. **Update tick & URP hook** — who drives the fixed-timestep sim, and the `ScriptableRendererFeature` that registers the RenderGraph passes.
6. **`WaterVolume`** (public authoring MonoBehaviour) — likely needed here to place a domain in a scene and feed the tier; keep it thin (config + delegate).

### Suggested order

Provider seam → import/ping-pong pattern proven with a trivial clear pass → the sim compute + `RippleSimPass` → a minimal `WaterVolume` to drive it → EditMode/PlayMode test or a debug view of the `Dynamic` RT. Commit after each green step.

## Working notes (this workspace)

- Confirm compiles via Unity (the agent can't run the Unity compiler). Trust the editor Console / the Read tool over quick shell reads — **the shell's file cache lags a few seconds behind writes**.
- The agent's shell **cannot delete files** on the project mount — ask the user to delete folders/files in Explorer or Unity.
- Reference GUID/name asmdef refs by GUID if a name reference ever fails to resolve.
- After each increment: present files, then give `git add -A && commit && push` lines for the user to run.
