# Changelog

All notable changes to Panda Facial are documented in this file.

## [0.1.9] - 2026-09-22

### Added

- Added confirmed eyelid aliases for Eyelid Close, Eye Smile, Surprise, Angry, Sad, and Eyelid Jito detection.

### Changed

- Replaced the built-in `eye_squint_l` / `eye_squint_r` semantic IDs with `eye_jito_l` / `eye_jito_r`.
- Added automatic legacy mapping migration that preserves renderer overrides, BlendShape names, multipliers, enabled states, additional targets, and non-duplicate data when old and new IDs coexist.

## [0.1.8] - 2026-09-20

### Added

- Added one-to-many semantic mappings with independent renderer overrides, BlendShape names, enabled states, and weight multipliers.
- Added conservative auto detection for unmapped channels using confirmed aliases and exact, case-insensitive, then normalized matching.
- Added per-target preview and AnimationClip key writing with invalid-target isolation, curve preservation, and Undo support.

### Changed

- Renamed the Inspector-facing eye labels to Eyelid Close and Eyelid Jito without changing their semantic IDs.
- Kept the existing v0.1.7 mapping fields as the primary target and added serialized migration defaults for existing mappings.

## [0.1.7] - 2026-09-20

### Added

- Added production-ready foundation controls for Blink, Eye Smile, Surprise, Angry, Sad, and Squint with independent Left and Right values.
- Added Brow Up, Brow Down, Brow Angry, Brow Sad, Brow Smile, and Brow Serious controls with independent Left and Right values.
- Added mapped-aware Editor-session foldouts, pair reset/key actions, and section-wide reset/key actions for Eyelid and Brow controls.

### Changed

- Updated Eyelid and Brow mapping labels to human-readable Left and Right names.
- Extended controller logic without adding serialized authoring values, eye-look behavior, or multi-target mappings.

## [0.1.6] - 2026-09-20

### Changed

- Added independent Mouth Position, Mouth Corners, and AIUEO section foldouts.
- Initialize each section open only when it has at least one valid mapped channel.
- Preserve user foldout choices per Authoring Target for the current Editor session.

## [0.1.5] - 2026-09-18

### Changed

- Added a default face renderer with per-channel renderer overrides.
- Combined mouth-corner vertical and width controls into mirrored-capable XY pads.
- Renamed explicit key and reset actions for clarity and grouped development UI under Debug / Verification.
- Replaced internal terminology in the normal Inspector with human-readable mapping labels.

## [0.1.4] - 2026-09-17

### Added

- Added the first production Mouth Controller UI to the Custom Inspector.
- Added a draggable Mouth Position XY pad, mouth-corner and width controls, AIUEO controls, and resets.
- Added controller-value reconstruction from mapped BlendShape weights with partial and invalid mapping diagnostics.

## [0.1.3] - 2026-09-17

### Added

- Added UI-independent controller logic for mouth position, width, corners, vowels, and blinking.
- Added multi-semantic preview and same-time Timeline key writing through semantic mappings.
- Added partial and invalid mapping isolation for controller operations.

## [0.1.2] - 2026-09-17

### Added

- Added the built-in Panda Facial semantic channel contract.
- Added per-channel renderer and BlendShape mapping storage.
- Added centralized mapping resolution with unmapped and invalid states.
- Added a minimal mapping and semantic authoring UI.

## [0.1.1] - 2026-09-17

### Fixed

- Removed the serialized authoring slider value so Timeline recording cannot create Panda Facial authoring-value curves.
- Read and edit the slider value directly through the target renderer's actual BlendShape weight.
- Recognize Timeline Infinite Clips and embedded Recorded AnimationClips as writable recording contexts.

## [0.1.0] - 2026-09-17

### Added

- Initial UPM package structure.
- Minimal BlendShape authoring component and Custom Inspector.
- Direct key insertion into the selected Timeline AnimationClip at the current playhead time.
- Safe hierarchy binding validation, curve preservation, and Undo support.
