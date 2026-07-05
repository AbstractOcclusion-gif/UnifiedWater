# Changelog

All notable changes to this package are documented here.

## [1.0.0] - 2026-07-03

First Asset Store release.

### Added
- Interactive ripple simulation (compute-based heightfield) with frame-rate-independent
  stepping: identical wave speed at 30 fps on a tablet and 144 fps in the editor.
- Two-way object coupling: multi-point buoyancy with righting torque and wave drift
  (`WaterBuoyancy`), analytic drop / footprint-delta disturbance (`WaterInteractable`),
  entry splashes with drifting droplets and flipbook crown (`WaterSplash` + emitter).
- Ambient wind-wave layer (sum of sines) that floating objects ride, with wind speed,
  heading, and spread controls.
- Turbulence-driven surface foam (generation/decay/advection) plus fully GPU-resident
  foam/spray particles (compute spawn + procedural quads, no readback, WebGPU-safe).
- Caustics, hybrid god rays with real shadow shafts, and per-body reflections:
  SSR, planar, or sky, over a procedural sky or the scene's URP probe.
- Water fog (Beer-Lambert, HDR extinction), opacity di