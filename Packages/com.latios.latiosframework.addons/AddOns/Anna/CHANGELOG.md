# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic
Versioning](http://semver.org/spec/v2.0.0.html).

## [0.1.1] – 2025-2-8

### Fixed

-   Fixed system order in projects using injection-based system ordering
-   Fixed rigid bodies and kinematic colliders with the environment tag showing
    up in multiple collision layers by explicitly excluding them from the
    environment layer
-   Fixed position axis locking forcing the center of mass world-position to
    zero along the axis, when it is supposed to lock to the initial position in
    the frame
-   Fixed single-axis rotation locking accidentally using dual-axis locking

## [0.1.0] - 2025-1-18

### This is the first release of *Anna* as an add-on.
