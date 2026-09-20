# AGENTS.md

## Project

This repository contains **Panda Facial**, a Unity Editor tool for authoring facial animation with BlendShapes.

Repository:

```text
SILL-BILL/panda-facial-unity
```

Project name:

```text
Panda Facial
```

Maintainer:

```text
Gonsaku
```

Panda Facial is an independent Unity project.

It is inspired by the custom facial controller workflow developed for Mixamo Rig Kai in Blender, but it must not depend on Mixamo Rig Kai at runtime or require Mixamo-specific skeleton data.

---

# Core Goal

Panda Facial exists to make facial animation authoring easier inside Unity while leaving behind normal Unity animation data.

The intended workflow is:

```text
Custom Inspector
      ↓
Facial Mapping
      ↓
Target SkinnedMeshRenderer
      ↓
Timeline current time
      ↓
AnimationClip
      ↓
Actual BlendShape curves
```

The final animation must remain usable without Panda Facial authoring components.

Panda Facial is an authoring tool, not a required runtime facial system.

---

# Supported Unity Versions

## Minimum Supported Version

```text
Unity 2022.3.22f1
```

This version is the primary compatibility baseline because Panda Facial is expected to be used in VRChat-related production environments.

## Additional Target

Panda Facial must also remain compatible with:

```text
Unity 6.x
```

Unity 6 compatibility is required for newer MR / XR projects.

## Compatibility Priority

Use this priority order:

```text
1. Unity 2022.3.22f1 compatibility
2. Unity 2022.3 LTS stability
3. Unity 6.x compatibility
```

Do not introduce a Unity 6-only API when the same feature can reasonably be implemented using APIs available in Unity 2022.3.22f1.

If version-dependent code is unavoidable, isolate it behind Unity version compile defines or a small compatibility layer.

Do not silently raise the minimum Unity version.

---

# Primary Verification Versions

Important implementation changes should be tested with:

```text
Unity 2022.3.22f1
Unity 6.x
```

Unity 2022.3.22f1 is the primary development baseline.

Unity 6.x is the secondary compatibility target.

Do not report a Unity version as verified unless it was actually tested.

---

# Current Scope

Panda Facial currently focuses on facial animation driven by **BlendShapes**.

## In Scope

Examples include:

```text
Brow expressions
Eyelid / eye-expression BlendShapes
Blink
Squint
Eye smile
Mouth position
Mouth corner up / down
Mouth spread
Mouth narrow
AIUEO / vowel BlendShapes
Custom BlendShape channels
```

---

# Explicitly Out of Scope

The following are not part of the current Panda Facial scope:

```text
Eye Look / Eye Aim
Eye bone rotation
Head Aim
Bone-based facial deformation
Jaw bone control
Runtime eye tracking
Runtime facial IK
Full facial bone rigs
```

Eye movement / gaze control must remain a separate system.

Do not add eye-look behavior to Panda Facial unless explicitly approved.

---

# Mouth Controller Rules

The mouth controller must follow the same conceptual separation as the Blender custom facial controller.

## Mouth Position

The mouth position control is only for:

```text
Mouth Left / Right
Mouth Up / Down
```

It is not a mouth-open controller.

## Mouth Corners

Left and right mouth-corner controls are used for:

```text
Corner Up
Corner Down
```

## Mouth Width

Mouth width controls are used for:

```text
Spread / Wide
Narrow
```

## Mouth Opening

Mouth opening must not be implicitly driven by the mouth-position control.

Mouth opening should be authored through:

```text
A
I
U
E
O
```

or through explicitly mapped custom BlendShapes such as:

```text
MouthOpen
MouthClose
JawOpen
```

depending on the character.

---

# Editor Tool First

Panda Facial is primarily a **Unity Editor authoring tool**.

Prefer implementations inside:

```text
Editor/
```

unless Runtime code is genuinely required.

Avoid introducing Runtime dependencies only to simplify Editor implementation.

The preferred architecture is:

```text
Panda Facial Editor UI
        ↓
Mapping
        ↓
Target SkinnedMeshRenderer
        ↓
AnimationClip BlendShape curves
```

Avoid this as the core architecture:

```text
Panda Facial Runtime Controller
        ↓
Custom animated parameters
        ↓
Runtime conversion
        ↓
BlendShapes
```

The core authoring workflow should not require a custom MonoBehaviour to evaluate facial animation during delivery.

---

# Direct BlendShape Animation Rule

This is one of the most important Panda Facial design rules:

**Animation keys should be written directly to the target model's actual BlendShape properties.**

Example:

```text
SkinnedMeshRenderer
└ blendShape.Mouth_CornerUp_L
```

Panda Facial should act as an authoring interface, not as an intermediate animation dependency.

A Panda Facial control may manipulate multiple BlendShapes while authoring, but the resulting AnimationClip should contain the actual BlendShape curves.

Example:

```text
Panda Facial Mouth Width
        ↓
Wide
        ↓
blendShape.Mouth_Spread_L
blendShape.Mouth_Spread_R
```

and:

```text
Panda Facial Mouth Width
        ↓
Narrow
        ↓
blendShape.Mouth_Narrow_L
blendShape.Mouth_Narrow_R
```

The authoring controller itself does not need to remain in the delivered project.

---

# Timeline Workflow

Panda Facial is designed around facial animation authored on **Unity Timeline**.

Expected structure:

```text
Timeline
├ Body Animation Track
└ Facial Animation Track
     └ Facial AnimationClip
```

The animator should be able to:

```text
1. Move the Timeline playhead to a frame.
2. Manipulate Panda Facial controls.
3. Insert / record keys at the current Timeline time.
4. Continue editing the resulting BlendShape animation using normal Unity tools.
```

Facial animation should remain separable from body animation whenever practical.

Do not force body and facial animation into the same AnimationClip.

---

# Delivery Independence

The desired workflow is:

```text
Author with Panda Facial
        ↓
BlendShape curves are stored in AnimationClip
        ↓
Remove Panda Facial authoring component / package if necessary
        ↓
Facial animation still works
```

Avoid designs that require Panda Facial runtime components for normal playback.

A future optional runtime module may be added separately, but it must not become a requirement for the core authoring workflow.

---

# UI Direction

The initial authoring UI should use a **Custom Inspector**.

Do not use uGUI for the primary authoring interface unless explicitly requested.

The UI should reproduce the practical feel of the Blender custom facial controller without reproducing its 3D controller presentation.

Useful UI elements include:

```text
2D drag pads
1D sliders
Buttons
Toggles
Left / Right paired controls
Reset controls
Foldouts
Tabs
Custom channel lists
```

Prioritize animator usability.

Avoid exposing raw BlendShape names during normal animation work when a mapped Panda Facial control can represent them more clearly.

---

# Inspector Technology

For the initial implementation, prefer stable Editor APIs available in Unity 2022.3.22f1.

IMGUI-based Custom Inspectors are acceptable and may be preferred during early development because of their compatibility and simplicity.

UI Toolkit may be introduced later when it clearly improves usability or maintainability without breaking Unity 2022.3 compatibility.

Do not rewrite a working UI only to adopt a newer framework.

---

# Mapping

Panda Facial must not assume fixed BlendShape names.

Different characters may use different names.

Example:

```text
Panda Facial semantic channel:
Mouth Corner Up L

Character A:
Mouth_CornerUp_L

Character B:
mouthSmileLeft

Character C:
Fcl_MTH_Smile_L
```

Mapping must isolate character-specific BlendShape names from the Panda Facial control UI.

The mapping system should support:

```text
Built-in Panda Facial channels
Custom user-defined channels
Missing / unmapped channels
Different target meshes
```

A missing optional mapping must not break unrelated controls.

---

# Custom Channels

Custom BlendShape channels are an important feature.

Users should be able to register facial BlendShapes that are not represented by built-in Panda Facial controls.

Custom channels must remain separate from built-in semantic channels.

Do not hard-code project-specific BlendShape names into the core tool.

---

# Multi-Mesh Direction

The initial MVP may support a single facial `SkinnedMeshRenderer`.

However, the architecture must not permanently assume that all facial BlendShapes live on one mesh.

Future characters may split facial data across:

```text
Face
Eyes
Teeth
Tongue
Accessories
```

Avoid early architectural choices that would make multi-mesh mapping difficult later.

---

# Undo / Redo

Editor operations that modify:

```text
Mappings
Animation data
Authoring values
Custom channels
```

should support Unity Undo / Redo whenever practical.

Expected behavior:

```text
Modify facial control
Ctrl + Z
Previous state is restored
```

Use Unity's Undo system for Editor-side state changes.

Do not create destructive Editor operations without Undo unless technically unavoidable and clearly documented.

---

# Animation Data Safety

Animation authoring must be conservative.

Before writing animation data:

```text
1. Confirm the intended character.
2. Confirm the intended SkinnedMeshRenderer.
3. Confirm the intended BlendShape property.
4. Confirm the intended AnimationClip.
5. Confirm the intended Timeline time.
6. Preserve unrelated curves.
7. Register Undo when practical.
```

Do not silently write animation data into an unintended clip.

Do not recreate or overwrite an entire AnimationClip when only one BlendShape curve needs to change.

Do not delete unrelated animation curves.

---

# BlendShape Binding Safety

BlendShape animation curves must target the correct hierarchy path and `SkinnedMeshRenderer`.

Do not assume the face mesh is directly below the character root.

Example hierarchies may be:

```text
CharacterRoot
├ Body
├ Head
│  └ FaceMesh
└ Accessories
```

or something completely different.

Generate bindings from the actual object hierarchy.

Do not hard-code transform paths.

---

# Error Handling

Panda Facial should fail safely when required data is missing.

Examples:

```text
No SkinnedMeshRenderer
No BlendShapes
Missing mapping
No selected facial target
No writable AnimationClip
No valid Timeline context
Unsupported clip state
Invalid hierarchy binding
```

Display clear Editor warnings or errors.

Do not silently guess the target AnimationClip when multiple plausible targets exist.

---

# Package Direction

Panda Facial should be structured so that it can be distributed through Unity Package Manager.

Expected package identity:

```text
name: com.sillbill.panda-facial
displayName: Panda Facial
```

Minimum Unity version:

```text
2022.3.22f1
```

Recommended high-level layout:

```text
panda-facial-unity/
├ package.json
├ README.md
├ LICENSE
├ AGENTS.md
├ CHANGELOG.md
├ Editor/
├ Runtime/
├ Tests/
├ Samples~/
├ Documentation~/
└ Development~/
```

Keep development-only project files separate from distributable package content.

---

# Development Project

A development Unity project may be stored under:

```text
Development~/
```

Example:

```text
Development~/
└ PandaFacialDev/
   ├ Assets/
   ├ Packages/
   └ ProjectSettings/
```

Generated Unity directories must not be committed:

```text
Library/
Temp/
Obj/
Logs/
UserSettings/
Build/
Builds/
```

Do not commit Unity-generated caches.

---

# Test Assets

Use dedicated test meshes / characters when validating Panda Facial.

Representative test BlendShapes may include:

```text
MouthLeft
MouthRight
MouthUp
MouthDown

Mouth_CornerUp_L
Mouth_CornerUp_R
Mouth_CornerDown_L
Mouth_CornerDown_R

Mouth_Spread_L
Mouth_Spread_R
Mouth_Narrow_L
Mouth_Narrow_R

A
I
U
E
O

Blink_L
Blink_R
```

These names are examples for test assets only.

Do not turn test-asset names into required production naming conventions.

---

# First Technical Milestone

The first implementation milestone should remain intentionally small.

## MVP

Prove this data path first:

```text
1. Reference one target SkinnedMeshRenderer.
2. Select one BlendShape.
3. Display one authoring control in a Custom Inspector.
4. Identify the active Timeline / AnimationClip context.
5. Write a key directly to the actual BlendShape curve.
6. Verify the resulting animation still works without Panda Facial authoring data.
```

Success condition:

```text
Custom Inspector
        ↓
Current Timeline Time
        ↓
AnimationClip
        ↓
Actual BlendShape Curve
```

Do not build the full facial controller UI before this path is proven.

---

# Versioning Policy

## Critical Rule

**Increment the Panda Facial version whenever an implementation change modifies code, behavior, UI, serialized data, package contents, animation behavior, mapping behavior, or compatibility behavior.**

Do not create multiple materially different test builds using the same version.

The purpose is simple:

**The version number must be enough to identify whether the user is testing the old build or the new build.**

---

# Initial Version

If no version has been established yet, initialize Panda Facial at:

```text
0.1.0
```

Use Semantic Versioning format:

```text
MAJOR.MINOR.PATCH
```

During early development, most implementation changes should increment `PATCH`.

Example:

```text
0.1.0
↓ First direct BlendShape key prototype
0.1.1

0.1.1
↓ Timeline write fix
0.1.2

0.1.2
↓ Inspector UI change
0.1.3
```

---

# When to Increment PATCH

Increment the Patch Version for changes including:

```text
Bug fixes
Editor UI changes
Mapping changes
Timeline behavior changes
AnimationClip writing changes
BlendShape binding changes
Undo / Redo behavior changes
Serialization changes
New controls
Controller behavior changes
Compatibility fixes
Unity 2022 / Unity 6 compatibility changes
Package changes affecting distributed behavior
Test-build changes that alter actual implementation behavior
```

If the implementation or distributed package meaningfully changes, increment the version.

---

# Minor Version Changes

Use a Minor Version increment when a larger feature milestone is intentionally promoted.

Example:

```text
0.1.x
Direct BlendShape key authoring prototype

0.2.0
Built-in facial controller set

0.3.0
Custom channels and improved mapping
```

Do not automatically increment Minor for every feature during rapid development.

The maintainer decides when a milestone deserves a Minor increment.

---

# Changes That Usually Do Not Require a Version Increment

A version increment is generally not required for changes limited to:

```text
AGENTS.md
Internal development notes
Analysis documents
Comments only
Formatting only
README typo fixes
Non-distributed development scripts
```

If documentation changes accompany an implementation change, the implementation change still requires a version increment.

---

# Version Source of Truth

The primary package version is:

```text
package.json
```

Example:

```json
{
  "name": "com.sillbill.panda-facial",
  "displayName": "Panda Facial",
  "version": "0.1.0"
}
```

Keep duplicated version information synchronized when present.

Potential locations include:

```text
package.json
CHANGELOG.md
README.md
Git tag
GitHub Release
```

Do not leave conflicting version values in the repository.

---

# Version Report Rule

Every implementation / modification report must include:

```text
Version Before:
Version After:
Version Bumped: Yes / No
Reason:
```

Example:

```text
Version Before: 0.1.2
Version After: 0.1.3
Version Bumped: Yes
Reason: Direct Timeline BlendShape key insertion behavior was changed.
```

Documentation-only example:

```text
Version Before: 0.1.3
Version After: 0.1.3
Version Bumped: No
Reason: AGENTS.md documentation-only update.
```

This reporting rule also applies to development builds.

---

# Build Identity Rule

Never create two different implementation builds with the same Panda Facial version.

Bad:

```text
0.1.3 build A
0.1.3 build B with changed code
```

Good:

```text
0.1.3
0.1.4
```

The version must identify the tested implementation.

---

# Commit / Push / Tag / Release Safety

Version changes and Git publishing actions are separate.

Changing:

```text
package.json version
```

does not grant permission to:

```text
git commit
git push
git tag
create GitHub Release
publish package
```

Do not perform publishing operations unless the user explicitly requests them.

Local implementation and testing may proceed without publishing.

---

# Git Safety

Do not rewrite repository history without explicit approval.

Do not:

```text
force push
delete branches
rewrite shared tags
reset shared history
remove releases
```

unless explicitly requested.

Before release work, verify:

```text
Current branch
Current commit
package.json version
Tag target
Release target
```

---

# Unity Project Safety

Do not upgrade the project Unity version unless explicitly requested.

Do not modify unrelated project-wide settings.

Avoid:

```text
Changing render pipeline
Replacing project packages unnecessarily
Deleting unrelated assets
Changing unrelated Player Settings
Modifying unrelated Timeline assets
```

Panda Facial development should minimize its impact on the surrounding Unity project.

---

# Dependency Policy

Prefer Unity built-in APIs for:

```text
Editor
Animation
Timeline
Serialization
Undo
```

Avoid adding third-party dependencies unless explicitly approved.

Panda Facial should remain lightweight.

Any new dependency must be checked for:

```text
Unity 2022.3.22f1 compatibility
Unity 6 compatibility
License compatibility
UPM compatibility
Editor-only vs Runtime impact
```

---

# Runtime Dependency Policy

The core Panda Facial workflow should not require Runtime code.

If Runtime code is added, clearly justify why the functionality cannot remain Editor-only.

A future optional Runtime module must not silently become mandatory for existing authoring workflows.

---

# Code Organization

Keep responsibilities separated.

Recommended conceptual structure:

```text
Editor/
├ UI
├ Mapping
├ Timeline
├ Animation
└ Utilities

Runtime/
└ Only code genuinely required at runtime
```

Avoid one large class that simultaneously handles:

```text
Inspector drawing
Mapping storage
Timeline discovery
AnimationClip modification
BlendShape evaluation
```

Split responsibilities when practical.

---

# Relationship to Mixamo Rig Kai

Panda Facial was inspired by the custom facial controller developed inside Mixamo Rig Kai.

However:

```text
Mixamo Rig Kai
≠
Panda Facial
```

Panda Facial must not require:

```text
Mixamo Rig Kai
Mixamo skeleton naming
Mixamo bones
Kai Blender files
Kai-specific Runtime data
```

The design language may be shared, but the implementations are independent.

---

# Relationship to PandaLip

PandaLip may later provide AIUEO or lip-sync animation data to Panda Facial workflows.

This integration is not required for the Panda Facial MVP.

Do not couple Panda Facial core architecture to PandaLip unless an integration feature is explicitly approved.

Both projects must remain independently usable.

---

# Development Philosophy

Prefer:

```text
Small verified steps
Direct animation data
Clear mappings
Low runtime coupling
Backward compatibility
Animator-friendly UI
Conservative data modification
```

Avoid:

```text
Large speculative systems
Hidden Runtime dependencies
Hard-coded character names
Unity 6-only implementation paths
Automatic destructive edits
Features outside the agreed scope
```

Prove direct BlendShape key authoring first.

Then expand.

---

# Completion Report

When completing an implementation task, report at minimum:

```text
Version Before:
Version After:
Version Bumped:
Reason:

Unity Versions Tested:
Files Changed:
Behavior Added / Changed:
Known Issues:
Git Status:
```

Example:

```text
Version Before: 0.1.0
Version After: 0.1.1
Version Bumped: Yes
Reason: Added first direct BlendShape key authoring prototype.

Unity Versions Tested:
- Unity 2022.3.22f1

Files Changed:
- package.json
- Editor/PandaFacialInspector.cs
- Editor/PandaFacialAnimationUtility.cs

Behavior Added / Changed:
- One mapped BlendShape can be keyed directly into an AnimationClip.

Known Issues:
- Unity 6 verification not yet performed.
- Multi-mesh mapping not implemented.

Git Status:
- Not committed
- Not pushed
```

Do not claim a test passed unless it was actually executed.

---

# Current Development Priority

Current priority order:

```text
1. Establish package structure.
2. Establish the development Unity project.
3. Create minimal target BlendShape mapping.
4. Create minimal Custom Inspector.
5. Prove direct BlendShape key writing.
6. Prove Timeline-time authoring.
7. Verify AnimationClip independence from Panda Facial.
8. Expand to the Blender-inspired facial controller UI.
```

Do not skip directly to the complete controller UI before validating the animation-data path.

---

# Final Principle

Panda Facial exists to make facial animation easier for animators while leaving behind standard Unity animation data.

When choosing between:

```text
clever internal architecture
```

and:

```text
simple, reliable animation data that survives delivery
```

prefer the second.
