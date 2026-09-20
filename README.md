# Panda Facial

Panda Facial is a Unity Editor authoring tool for writing facial BlendShape keys directly into standard `AnimationClip` curves used by Timeline.

## Requirements

- Unity 2022.3.22f1 or later in the 2022.3 LTS line
- Unity 6.x is also supported
- Timeline 1.7.6 or later

## MVP workflow

1. Add **Panda Facial > Authoring Target** to the character root.
2. Assign one facial `SkinnedMeshRenderer`.
3. Place the Timeline playhead inside one facial Animation clip. If clips overlap, select the intended clip explicitly (a locked Inspector can keep the authoring UI visible).
4. Choose a BlendShape and set its 0–100 value in the Custom Inspector.
5. Click **Write Key at Timeline Playhead**.

The tool writes `blendShape.<ActualBlendShapeName>` on the selected clip. The authoring component and package are not required for playback after authoring.

## Semantic mapping

Built-in semantic channels such as `mouth_a`, `eye_close_l`, and `brow_up_r` can be mapped to one or more character-specific `SkinnedMeshRenderer` and BlendShape targets. The primary mapping stays simple; optional targets, per-target renderer overrides, enabled states, and 0–1 weight multipliers are available under **Advanced / Additional Targets**. Semantic authoring resolves every target and still writes only actual `blendShape.<name>` curves.

**Auto Detect Unmapped Channels** can fill empty primary mappings from confirmed aliases on the Default Face Renderer. Detection prefers exact, then case-insensitive, then normalized names; ambiguous matches remain unmapped and existing mappings are never overwritten. Unmapped channels are optional. A disabled target, missing renderer, missing Mesh, or removed BlendShape is reported per target without disabling valid targets. Eye-look and bone-based gaze controls are outside Panda Facial's semantic channel contract.

## Controller logic

UI-independent controller logic converts normalized animator inputs into semantic weights. It supports mouth position X/Y, bilateral mouth width, left/right mouth-corner Y, independent AIUEO weights, independent blinking, and a generic single-channel weight.

Controller outputs always include zero values for opposing channels. The Editor resolves every output through semantic mapping, skips unmapped or invalid channels independently, and can write all valid outputs to the same Timeline time as actual BlendShape curves. The Inspector exposes a minimal verification UI; it is not the final facial-controller layout and its controller values are not serialized.

## Eyelid and brow controllers

The Custom Inspector provides independent Left and Right sliders for Eyelid Close, Eye Smile, Surprise, Angry, Sad, and Eyelid Jito, plus Brow Up, Brow Down, Brow Angry, Brow Sad, Brow Smile, and Brow Serious. Each pair and each complete section can be reset or keyed. The controls read their current values from mapped BlendShape weights and write only actual `blendShape.<name>` curves.

Eyelid and Brow are separate mapped-aware foldouts. A section initially opens when at least one channel is validly mapped and otherwise starts closed. Manual choices are remembered per Authoring Target for the current Editor session. Section reset and key actions remain accessible while a foldout is closed. Eye Look, eye-bone control, and Mirror/Sync are intentionally outside this foundation release.

## Mouth controller

The Custom Inspector includes production mouth authoring controls: a draggable Mouth Position XY pad, independent left/right Mouth Corner XY pads, optional bidirectional mirroring, and independent AIUEO sliders. Corner X controls Inner/Outer width while Corner Y controls Up/Down. Each control can be reset or explicitly keyed, and **Reset Mouth** clears the complete mouth controller through one controller operation.

Set **Default Face Renderer** once, then choose BlendShapes in Facial Mapping. Individual channels can use an Advanced renderer override for multi-mesh characters. Changing the default renderer never rewrites saved BlendShape names.

Displayed values are reconstructed from mapped BlendShape weights rather than stored on `PandaFacialAuthoringTarget`. If opposing channels are both active, the displayed value is positive minus negative and the Inspector shows a warning. Partial or invalid mappings do not disable unrelated controls. Development-only controls are grouped under the default-closed **Debug / Verification** foldout.

Mouth Position, Mouth Corners, and AIUEO have independent foldouts. A section initially opens when at least one channel in it is validly mapped; otherwise it starts closed. Manual foldout choices are remembered per Authoring Target for the current Editor session and never modify animation or runtime data. **Reset Mouth** and **Set All Mouth Keys** remain available when every section is closed.
