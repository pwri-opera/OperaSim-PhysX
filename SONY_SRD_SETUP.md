# Sony Spatial Reality Display setup

This branch integrates the ZX200 operator view with Sony Spatial Reality Display (SRD).
The Sony SDK itself is not stored in this repository because it is distributed under
Sony's own EULA.

## Required software

- Unity 2022.3 (this project currently uses 2022.3.16f1)
- Spatial Reality Display Settings 2.6.0
- Spatial Reality Display Unity Plugin 2.6.0
- Windows Intel 64-bit build target

Download the SDK from Sony and import every item in the Unity package:

https://xyn.sony.net/en/developer/setup/spatial-reality-display/download-info

## Scene

Open `Assets/Scenes/SampleScene.unity`. The scene contains `ZX200 Spatial Display Rig`,
which follows `zx200/body_link` using the same operator offset as the regular camera:

- Position: `(-1.0, 1.5, 0.6)`
- Rotation: `(0, 0, 0)`
- SRD view-space scale: `1.0`

When an SRD is connected, the SRDisplayManager cameras render the scene. When no SRD
is connected, SRDisplayManager is disabled and the regular ZX200 operator camera is
enabled automatically.

## Build settings

- Scene: `Assets/Scenes/SampleScene.unity`
- Platform: Windows
- Architecture: Intel 64-bit
- Scripting backend: Mono
- Active Input Handling: Input Manager (Old) or Both
- VSync Count: Don't Sync for every quality level

The SRD view volume can be adjusted with `SRD View Space Scale` on the
`SRDisplayManager` component. Keep the left-eye and right-eye camera settings equal.
