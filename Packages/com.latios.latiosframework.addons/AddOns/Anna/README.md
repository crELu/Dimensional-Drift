# Add-On README Template

Anna is a simple out-of-the-box rigid body physics engine built on Psyshock’s
UnitySim APIs. It is intended to be general-purpose, striking a balance between
performance, flexibility, and most importantly, ease-of-use.

## Features

Currently, Anna provides basic rigid bodies which can collide with each other
and the environment. Additionally, rigid bodies can have particular position and
rotation axes locked.

## Getting Started

**Scripting Define:** LATIOS_ADDON_ANNA

**Requirements:**

-   Requires Latios Framework 0.11.1 or newer

**Main Author(s):** Dreaming I’m Latios

**Additional Contributors:**

**Support:** Please make feature requests for features you would like to see
added! You can use any of the Latios Framework support channels to make
requests.

### Installing

Add the following to `LatiosBootstrap` (only the runtime is necessary):

```csharp
Latios.Psyshock.Anna.AnnaBootstrap.InstallAnna(world);
```

### Usage

Use the `CollisionTagAuthoring` component to specify static environment and
kinematic colliders in your scene. And use the `AnnaRigidBodyAuthoring`
component to set up rigid bodies. Use the `AnnaSettingsAuthoring` to configure
scene properties.

At runtime, you can either directly modify the `RigidBody` values, or you can
use the `AddImpulse` dynamic buffer.
