# Unified Water — Architecture Draft 01

**Status:** living design doc. Data spine + descriptor factory are implemented and compiling green; passes and domains are not built yet.
**Engine:** Unity 6000.3 / URP 17.3, RenderGraph-native.
**Standalone:** Unified Water depends on nothing outside itself. The Evan Wallace port, KWS, Crest, RAM, and Stylized Water are **inspiration references only** — we take ideas, never their code (see `Docs/RULES.md`).
**Scope of this doc:** the load-bearing decisions — the Water Field, the domain abstraction, the RenderGraph pass graph, and the module decomposition — plus the constant/enum catalog that keeps the code free of magic numbers and god classes.

This doc covers **Phase 1 (ponds)** concretely and sketches how Phases 2–4 (ocean, shore, rivers) slot in without a rewrite. Every Unity/URP API detail is verified against the URP 17 docs — cross-checked against how the references solved the same problem — before code is written.

---

## 1. Design principles (binding)

These come from the project coding standards and the lessons of the asset survey. They constrain every later decision.

- **No god class.** KWS's single hidden `WaterSystem` MonoBehaviour split over four partial classes is the anti-pattern. Responsibilities here are split across small, single-purpose types (Section 6).
- **Composition over inheritance.** A domain is not a subclass; it is a `WaterDomain` composed with a cascade-layout strategy. Providers and passes are pluggable components, not an inheritance tree.
- **No magic numbers, no hardcoded strings.** Every literal lives in a named constant, a tier asset field, or an enum (Section 7). Shaders read the same values via uniforms fed from those constants.
- **Fail fast at boundaries.** Provider registration, domain configuration, and query calls validate inputs and throw with a clear message rather than silently producing black water.
- **Minimal public surface.** Only `WaterVolume` (authoring) and `WaterQuery` (gameplay) are public. Providers, passes, and the field are `internal`.
- **Prefer immutability.** Field descriptors, tier data, and layer metadata are read-only after construction; only the GPU textures mutate.
- **DRY — one implementation.** Any calculation or method needed in two places has exactly one implementation that both call (a shared helper), never a second copy. This is internal to our own codebase; we do not import code from the references.
- **Everything optional, tier-gated.** Every effect (reflection, caustics, god rays, underwater, foam, spectrum, flow) is individually toggleable per domain and gated by the quality tier. A disabled effect contributes no provider, no pass, no allocated layer, and no shader keyword — cost is zero when off.

---

## 2. The three core abstractions

Everything hangs off three contracts. Get these right and every feature is a plug-in.

**The Water Field** — the single source of truth. A stack of camera- or body-centred GPU textures, one per data *layer* (Section 4). Nothing in the system samples "the water" directly; it samples the field.

**Providers** (`IWaterFieldProvider`) — anything that *writes* a layer: FFT spectrum, analytic wave bank, ripple sim, obstacle footprints, foam accumulation, depth/SDF, flow. Each declares which layer(s) and which cascade(s) it targets, and contributes one or more RenderGraph passes.

**Consumers + one Query API** (`WaterQuery`) — the surface shaders, buoyancy, and gameplay all *read* the field. Gameplay reads through a single async-readback query with a CPU analytic fallback — the pattern every reference (Crest, KWS, Stylized, the port) independently converged on.

```
Providers ──write──▶  Water Field (layer textures)  ──read──▶ Surface shader
                          ▲                                    Consumers
                          └──────── WaterQuery (async + CPU fallback) ◀── gameplay
```

---

## 3. The domain abstraction (the decision that prevents a Phase-2 rewrite)

A **Water Domain** is a region of water backed by one field. It comes in two configurations that share the *same* field interface, differing only by their **cascade layout strategy** (composition, not subclassing):

- **Bounded domain** → `SingleCascadeLayout`. One fixed-extent cascade. This is a pond/pool/lake. It is the degenerate, depth-1 case of the ocean system — *not* a separate pool-space path.
- **Cascaded domain** → `ClipmapCascadeLayout`. A camera-following stack of N concentric cascades at increasing extent. This is the ocean.

Because a pond is literally "a cascade stack of length 1," Phase 2 adds cascades; it does not rewrite Phase 1. The layout strategy owns: cascade count, per-cascade world extent, and recentre/scroll behaviour. The rest of the engine is layout-agnostic.

> This directly avoids the port experiment's core flaw: rather than a fixed pool-space sim window, a pond is simply the finest cascade of a general system.

---

## 4. Water Field layout (first-draft layers & formats)

Each layer is a `Texture2DArray` with one slice per cascade (Crest/KWS pattern). A bounded pond has array length `1`. Formats below are proposals, justified against the surveyed systems; they are locked before implementation, not during.

| Layer (enum `WaterLayer`) | Format (proposed) | Channels | Written by | Justification |
|---|---|---|---|---|
| `Displacement` | `RGBAHalf` | xyz world displacement, w = fold/Jacobian | FFT + analytic providers | Crest `AnimatedWaves` packs displacement + derived whitecap term |
| `Dynamic` | `ARGBFloat` | height, velocity, nx, nz | Ripple sim provider | Ping-pong height-field state; full float is required for stable mean/volume conservation on WebGPU (a lesson from the experiment) |
| `SurfaceNormalFoam` | `RGBAHalf` | nx, nz, foamCoverage, wetness | Derived (compose pass) | Combined normal+foam sample keeps the surface shader to one fetch |
| `Depth` | `RGHalf` | bedDepth, shoreSdf | Depth provider | Crest `DepthLod` + jump-flood SDF for shore |
| `Flow` | `RGHalf` | flowX, flowZ | Flow provider (Phase 4) | KWS flowmap semantics |
| `Absorption` | per-body uniform for Phase 1; optional `RGBA32` later | tint + extinction | — | Spatially-uniform first (per-channel Beer-Lambert is enough for ponds); spatial variation deferred |

Notes:
- `Dynamic` is double-buffered (ping-pong) — it carries state across frames. `Displacement` and `SurfaceNormalFoam` are recomputed each frame. `Depth`/`Flow` are semi-static.
- The compose pass reads `Displacement` + `Dynamic`, writes `SurfaceNormalFoam`, so the surface shader samples derived data, never raw sim state.
- Layer *presence* is per-domain and tier-driven: a pond with no wind may disable `Displacement`; a calm ocean may disable `Dynamic` far from shore.

---

## 5. RenderGraph pass graph — Phase 1 (pond)

Passes are RenderGraph nodes; RenderGraph owns resource lifetime, aliasing, and async-compute scheduling (this is the concrete payoff of Unity 6 over the old hand-scheduled list). Each pass is one class, one responsibility. Phase-1 node set and dependencies:

```
[RippleSimPass]      (compute, ping-pong)   writes Dynamic
       │
[ObstacleInjectPass] (compute)              writes Dynamic   (object footprints)
       │
[WaveComposePass]    (compute)  reads Dynamic (+Displacement if present)
       │                                    writes SurfaceNormalFoam
       ├────────────▶ [CausticsPass]        reads SurfaceNormalFoam → writes Caustics RT
       │
[SurfacePass]        (raster)   reads SurfaceNormalFoam, Caustics, Depth → color
       │
[UnderwaterPass]     (raster/fullscreen)    reads Depth, camera color → fog/absorption
       │
[GodRayPass]         (raster, additive)     reads Caustics, shadows, depth
```

Later phases add nodes without reordering the spine: `FftWavePass` and `AnalyticWavePass` feed `Displacement` upstream of compose (Phase 2); `DepthBakePass`/`ShoreSdfPass` feed `Depth` (Phase 3); `FlowPass` feeds `Flow` (Phase 4). A single `WaterRenderGraphController` builds the graph from the set of enabled providers — it wires nodes, it contains no water logic itself.

---

## 6. Module decomposition (no god class)

Each type below is single-responsibility. Contrast the KWS monolith. Namespaces are illustrative.

**Data**
- `WaterField` — owns the layer textures; allocation/resize/lifetime only. No simulation logic.
- `WaterFieldDescriptor` — immutable: which layers, which formats, cascade count, resolution. Built from the tier + domain.
- `WaterQualityTier` (ScriptableObject) — a tier asset written fresh in our namespace; the single home for all budget numbers (resolutions, cascade count, god-ray steps, foam caps, reflection mode).

**Spatial**
- `WaterDomain` — a region + its field + its cascade strategy. Thin coordinator.
- `ICascadeLayout` → `SingleCascadeLayout`, `ClipmapCascadeLayout` — cascade count/extent/recentre.

**Providers** (`IWaterFieldProvider`, each contributes passes)
- `RippleSimProvider`, `ObstacleProvider` (Phase 1)
- `FftWaveProvider`, `AnalyticWaveProvider` (Phase 2)
- `DepthProvider` (Phase 3), `FlowProvider` (Phase 4)

**Rendering**
- `WaterRenderGraphController` — assembles the pass graph from enabled providers.
- One class per pass: `RippleSimPass`, `ObstacleInjectPass`, `WaveComposePass`, `CausticsPass`, `SurfacePass`, `UnderwaterPass`, `GodRayPass`.

**API surface (public)**
- `WaterVolume` (MonoBehaviour) — authoring/config only; registers a domain and its providers, then delegates. Deliberately thin.
- `WaterQuery` — async height/normal/flow sampling with CPU analytic fallback.

Everything else is `internal`.

---

## 7. Constant & enum catalog (no magic numbers)

First-draft home for every literal. Values are placeholders to be tuned; the point is they are *named*, not inline.

**Enums**
- `WaterLayer { Displacement, Dynamic, SurfaceNormalFoam, Depth, Flow, Absorption }`
- `DomainKind { Bounded, Cascaded }`
- `QualityTier { Low, Medium, High, Ultra }`
- `ReflectionMode { SkyOnly, Planar, ScreenSpace }`
- `CausticsMode { RealProjected, TexturedFocal }`

**`WaterLayerCatalog` (static, implemented)** — one named `GraphicsFormat` per layer plus each layer's double-buffer flag. No format literal or buffering assumption appears anywhere else.

**`WaterFieldConstants` (static, implemented)** — allocation limits and fixed texture settings: min/max resolution, cascade bounds, `BoundedCascadeCount`, no-MSAA, no-depth, and buffer name suffixes.

**Per-tier budget numbers** live on the `WaterQualityTier` asset (resolutions, cascade count, god-ray steps, foam caps, reflection mode, and tuned sim values such as ripple damping and obstacle deadband). Tuned fresh here — a reference's published number may be a starting point, but the value lives in our tier asset.

**Shader side** — the same names are surfaced as uniforms via a single publisher, so no shader hardcodes a constant either. Shader keyword names live in a `WaterKeywords` static class (no hardcoded strings).

---

## 8. Decisions (resolved 2026-07-05)

1. **Field texture formats** — RESOLVED. `Dynamic` stays full `ARGBFloat` (needed for the WebGPU mean-conservation fix). Rest of Section 4 table stands.
2. **Compose vs sample-on-the-fly** — RESOLVED: **precompute** `SurfaceNormalFoam` in a compose pass. Caveat per the optionality principle — each contribution to the composed layer (normals, foam, wetness) is tier-gated, and the compose pass itself is skipped if a domain uses neither dynamic nor displacement waves.
3. **Assembly layout** — RESOLVED: single `com.abstractocclusion.unifiedwater` UPM package with internal folders (runtime/editor split by folder + asmdef, one package), consistent with existing `abstractocclusion` packaging.
4. **Provider registration** — RESOLVED: **auto-discovery**. Providers self-register; `WaterVolume` enumerates enabled ones per tier. Registration still validates fail-fast (duplicate/unknown layer targets throw).

---

## 9. Status & next step

**Done (green):** the data spine — `WaterLayer`, `WaterLayerCatalog`, `WaterFieldConstants`, `WaterFieldDescriptor`, `WaterLayerTarget`, `WaterField` — plus `WaterFieldDescriptorFactory` (encodes bounded pond = depth-1 cascade). All internal, immutable, fail-fast, zero external coupling.

**Next:** make the spine live — a `WaterDomain` that owns a `WaterField`, and a small `WaterQualityTier` written fresh in our namespace to feed the factory. Still no rendering; this yields an allocatable, configurable domain to build the first pass against.
