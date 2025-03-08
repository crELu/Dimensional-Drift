# Add-On README Template

Cyline is a 3D line renderer add-on using Kinemation’s Unique Mesh APIs. The
generated mesh is a tubular or cylindrical shape. The implementation is based on
the original work created for Game Objects here:
<https://github.com/survivorr9049/LineRenderer3D>

## Getting Started

**Scripting Define:** LATIOS_ADDON_CYLINE

**Requirements:**

-   Requires Latios Framework 0.12.0-alpha.6 or newer

**Main Author(s):** Dreaming I’m Latios

**Support:** Feel free to reach me through any of the same channels you would
use for the Latios Framework!

### Installing

Add the following installer line to your bootstrap after Kinemation for both the
`LatiosBootstrap` and the `LatiosEditorBootstrap`:

```csharp
Latios.Cyline.CylineBootstrap.InstallCyline(world);
```

### Authoring and Baking

Add the *Line Renderer 3D (Cyline)* component to any subscene or referenced
prefab GameObject with a *Mesh Renderer*.

To preview your mesh, go to your *Preferences* window and in the *Entities* tab,
set the *Scene View Mode* to *Runtime Data*. You can also preview in the *Game*
tab.

Use the list of points to pre-populate the line points. You can control the
thickness for each point individually. The *resolution* parameter specifies how
many vertices are used to form a “ring” around each point. Larger values will
result in a more rounded cylindrical shape.

### Runtime

At runtime, you can modify the points using the
`DynamicBuffer<LineRenderer3DPoint>`. For your changes to be processed, you must
also enable the `LineRenderer3DConfig` component. This component is also where
you can modify the resolution if required.

The system responsible for building the mesh updates right before Kinemation
inside of `PresentationSystemGroup`. You can make modifications before that
point, or during `SimulationSystemGroup`. However, there is no real bad place to
modify the line renderer if you are willing to tolerate a frame delay.
