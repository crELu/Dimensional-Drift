# Latios Framework Add-Ons [0.2.2]

This is an extra Unity package for the Latios Framework containing various
add-on modules and functionality.

Add-ons are not held to the same quality standard as the Latios Framework. Many
of the add-ons only support a single transform system or target platform. Some
may be very niche to specific types of projects. Some may be experimental, or
may suffer from fundamental design issues that may never be fixed. Some may
require prerelease versions of the Latios Framework. Some may be abandoned by
their primary author. And some may be absolutely perfect for your project.

Each add-on is disabled by default and must be enabled by a scripting define. If
an add-on requires any assets, it must be imported through the add-on’s
associated package samples. Add-ons are allowed to depend on other packages not
part of the Latios Framework dependencies.

## Usage

First make sure you have installed the Latios Framework into the project.

Next, install this package.

Consult the README for each add-on you wish to enable to determine which
scripting define you need to add to your project. Once you have added it,
consult the add-on’s documentation for further usage.

## Contributing

This package is designed to be more friendly to contributors than the Latios
Framework itself. Consult the *\~Contributors Documentation\~* folder to learn
how to contribute your own add-ons or improve existing add-ons.

## Add-Ons Directory

### Physics

-   Anna – A rigid body physics engine focused on ease-of-use

### Animation

-   Mecanim V1 – The original Mecanim runtime implementation that used to be in
    the Mimic module
-   Mecanim V2 – A new Mecanim runtime implementation that aims to fix
    deep-rooted issues in V1 (still under construction)
-   KAG50 – An animation state machine and graph implementation that was
    originally written for Entities 0.50

### Rendering and Visual Effects

-   Cyline – A simple 3D Line Renderer
-   Shuriken – A recreation of Unity’s particle system in pure ECS (still under
    construction)

## Special Thanks To These Awesome Contributors

-   Sovogal – Primary author of Mecanim V1
-   Alemnunez – Fix for broken inert rule in KAG50
