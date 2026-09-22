using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Timeline;
using UnityEngine;

namespace SillBill.PandaFacial.Editor
{
    [CustomEditor(typeof(PandaFacialAuthoringTarget))]
    internal sealed class PandaFacialAuthoringTargetEditor : UnityEditor.Editor
    {
        private SerializedProperty defaultFaceRendererProperty;
        private SerializedProperty targetRendererProperty;
        private SerializedProperty blendShapeNameProperty;
        private SerializedProperty selectedSemanticIdProperty;
        private SerializedProperty semanticMappingsProperty;

        private bool mappingFoldout;
        private bool mouthControllerFoldout = true;
        private bool mirrorMouthCorners = true;
        private bool debugFoldout;
        private bool controllerFoldout;
        private bool semanticControlFoldout;
        private bool directModeFoldout;
        private Vector2 eyelidDragStartMouse;
        private PandaFacialEyelidState eyelidDragStartValue;
        private PandaFacialPadAxis eyelidDragAxis;
        private float mouthPositionX;
        private float mouthPositionY;
        private float mouthWidth;
        private float mouthCornerLeftY;
        private float mouthCornerRightY;
        private float vowelA;
        private float vowelI;
        private float vowelU;
        private float vowelE;
        private float vowelO;
        private float blinkLeft;
        private float blinkRight;
        private string controllerStatus;
        private PandaFacialDetectionReport autoDetectReport;
        private bool autoDetectDetailsFoldout;
        private readonly Dictionary<PandaFacialSemanticGroup, bool> groupFoldouts =
            new Dictionary<PandaFacialSemanticGroup, bool>();
        private readonly Dictionary<string, bool> mappingOverrideFoldouts =
            new Dictionary<string, bool>();

        private void OnEnable()
        {
            defaultFaceRendererProperty = serializedObject.FindProperty("defaultFaceRenderer");
            targetRendererProperty = serializedObject.FindProperty("targetRenderer");
            blendShapeNameProperty = serializedObject.FindProperty("blendShapeName");
            selectedSemanticIdProperty = serializedObject.FindProperty("selectedSemanticId");
            semanticMappingsProperty = serializedObject.FindProperty("semanticMappings");

            foreach (PandaFacialSemanticGroup group in Enum.GetValues(typeof(PandaFacialSemanticGroup)))
                groupFoldouts[group] = group == PandaFacialSemanticGroup.LipSync;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var authoringTarget = (PandaFacialAuthoringTarget)target;

            EditorGUILayout.LabelField("Panda Facial Authoring Target", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Animate the character's actual facial BlendShapes. Panda Facial authoring values are not stored as animation curves.",
                MessageType.Info);

            EditorGUILayout.PropertyField(
                defaultFaceRendererProperty,
                new GUIContent(
                    "Default Face Renderer",
                    "Used by facial mappings unless a channel has an advanced renderer override."));

            mouthControllerFoldout = EditorGUILayout.Foldout(
                mouthControllerFoldout,
                "Mouth Controller",
                true);
            if (mouthControllerFoldout)
            {
                EditorGUI.indentLevel++;
                DrawMouthController(authoringTarget);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();
            DrawEyeExpressionController(authoringTarget);

            EditorGUILayout.Space();
            DrawBrowController(authoringTarget);

            mappingFoldout = EditorGUILayout.Foldout(mappingFoldout, "Facial Mapping", true);
            if (mappingFoldout)
            {
                EditorGUI.indentLevel++;
                DrawMappingSettings(authoringTarget);
                EditorGUI.indentLevel--;
            }

            debugFoldout = EditorGUILayout.Foldout(debugFoldout, "Debug / Verification", true);
            if (debugFoldout)
            {
                EditorGUI.indentLevel++;
                controllerFoldout = EditorGUILayout.Foldout(
                    controllerFoldout,
                    "Controller Logic Verification",
                    true);
                if (controllerFoldout)
                {
                    EditorGUI.indentLevel++;
                    DrawControllerVerification(authoringTarget);
                    EditorGUI.indentLevel--;
                }

                semanticControlFoldout = EditorGUILayout.Foldout(
                    semanticControlFoldout,
                    "Internal Channel Control",
                    true);
                if (semanticControlFoldout)
                {
                    EditorGUI.indentLevel++;
                    DrawSemanticSelector();
                    EditorGUI.indentLevel--;
                }

                directModeFoldout = EditorGUILayout.Foldout(
                    directModeFoldout,
                    "Direct BlendShape Verification",
                    true);
                if (directModeFoldout)
                {
                    EditorGUI.indentLevel++;
                    DrawDirectSettings();
                    EditorGUI.indentLevel--;
                }
                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();

            if (debugFoldout && semanticControlFoldout)
            {
                EditorGUILayout.Space();
                PandaFacialResolvedMapping resolved = PandaFacialMappingResolver.Resolve(
                    authoringTarget,
                    selectedSemanticIdProperty.stringValue);
                DrawSemanticAuthoring(resolved);
            }

            if (debugFoldout && directModeFoldout)
            {
                EditorGUILayout.Space();
                DrawDirectAuthoring();
            }
        }

        private void DrawMouthController(PandaFacialAuthoringTarget authoringTarget)
        {
            PandaFacialControllerVector2State position =
                PandaFacialControllerValueReader.ReadMouthPosition(authoringTarget);
            PandaFacialControllerVector2State cornerLeft =
                PandaFacialControllerValueReader.ReadMouthCornerLeftXY(authoringTarget);
            PandaFacialControllerVector2State cornerRight =
                PandaFacialControllerValueReader.ReadMouthCornerRightXY(authoringTarget);
            PandaFacialControllerValueState a = ReadVowel(authoringTarget, "A", PandaFacialSemanticChannels.MouthA);
            PandaFacialControllerValueState i = ReadVowel(authoringTarget, "I", PandaFacialSemanticChannels.MouthI);
            PandaFacialControllerValueState u = ReadVowel(authoringTarget, "U", PandaFacialSemanticChannels.MouthU);
            PandaFacialControllerValueState e = ReadVowel(authoringTarget, "E", PandaFacialSemanticChannels.MouthE);
            PandaFacialControllerValueState o = ReadVowel(authoringTarget, "O", PandaFacialSemanticChannels.MouthO);

            bool positionOpen = DrawMouthSectionFoldout(
                authoringTarget,
                PandaFacialMouthSection.Position,
                "Mouth Position");
            if (positionOpen)
            {
                EditorGUI.indentLevel++;
                EditorGUI.BeginChangeCheck();
                Vector2 newPosition = DrawVector2Pad("Mouth Position XY", position.Value);
                if (EditorGUI.EndChangeCheck())
                {
                    PreviewController(
                        authoringTarget,
                        PandaFacialControllerLogic.MouthPosition(newPosition.x, newPosition.y));
                }
                EditorGUILayout.LabelField(
                    "X " + position.X.Value.ToString("+0.00;-0.00;0.00") +
                    "    Y " + position.Y.Value.ToString("+0.00;-0.00;0.00"),
                    EditorStyles.centeredGreyMiniLabel);
                DrawResetAndKeyButtons(
                    authoringTarget,
                    "Reset Mouth Position",
                    "Set Mouth Position Key",
                    "Write the current Mouth Position controller state to the active animation clip at the current time.",
                    PandaFacialControllerLogic.MouthPosition(0f, 0f),
                    PandaFacialControllerLogic.MouthPosition(position.X.Value, position.Y.Value));
                DrawStateWarning(position.X);
                DrawStateWarning(position.Y);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();
            bool cornersOpen = DrawMouthSectionFoldout(
                authoringTarget,
                PandaFacialMouthSection.Corners,
                "Mouth Corners");
            if (cornersOpen)
            {
                EditorGUI.indentLevel++;
                mirrorMouthCorners = EditorGUILayout.ToggleLeft(
                    new GUIContent(
                        "Mirror",
                        "When enabled, editing either corner applies the same Outer/Inner and Up/Down meaning to both sides."),
                    mirrorMouthCorners);
                DrawMouthCornerPad(authoringTarget, true, cornerLeft);
                DrawMouthCornerPad(authoringTarget, false, cornerRight);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();
            IReadOnlyList<PandaFacialSemanticWeight> currentVowels = PandaFacialControllerLogic.Vowels(
                a.Value, i.Value, u.Value, e.Value, o.Value);
            bool vowelsOpen = DrawMouthSectionFoldout(
                authoringTarget,
                PandaFacialMouthSection.Vowels,
                "AIUEO");
            if (vowelsOpen)
            {
                EditorGUI.indentLevel++;
                DrawVowelSlider(authoringTarget, "A", PandaFacialSemanticChannels.MouthA, a);
                DrawVowelSlider(authoringTarget, "I", PandaFacialSemanticChannels.MouthI, i);
                DrawVowelSlider(authoringTarget, "U", PandaFacialSemanticChannels.MouthU, u);
                DrawVowelSlider(authoringTarget, "E", PandaFacialSemanticChannels.MouthE, e);
                DrawVowelSlider(authoringTarget, "O", PandaFacialSemanticChannels.MouthO, o);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Reset Vowels"))
                    PreviewController(authoringTarget, PandaFacialControllerLogic.Vowels(0f, 0f, 0f, 0f, 0f));
                if (GUILayout.Button(new GUIContent(
                        "Set Vowel Keys",
                        "Write the current A, I, U, E, and O values to the active animation clip at the current time.")))
                    WriteControllerKeys(authoringTarget, currentVowels);
                EditorGUILayout.EndHorizontal();
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();
            IReadOnlyList<PandaFacialSemanticWeight> currentMouth = PandaFacialControllerLogic.Mouth(
                position.X.Value,
                position.Y.Value,
                cornerLeft.X.Value,
                cornerLeft.Y.Value,
                cornerRight.X.Value,
                cornerRight.Y.Value,
                a.Value,
                i.Value,
                u.Value,
                e.Value,
                o.Value);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent(
                    "Reset Mouth",
                    "Reset Mouth Position, both corners, and all vowel controls.")))
                PreviewController(authoringTarget, PandaFacialControllerLogic.ResetMouth());
            if (GUILayout.Button(new GUIContent(
                    "Set All Mouth Keys",
                    "Write all current Mouth controller states to the active animation clip at the current time.")))
                WriteControllerKeys(authoringTarget, currentMouth);
            EditorGUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(controllerStatus))
                EditorGUILayout.HelpBox(controllerStatus, MessageType.Info);
        }

        private static bool DrawMouthSectionFoldout(
            PandaFacialAuthoringTarget authoringTarget,
            PandaFacialMouthSection section,
            string label)
        {
            int mapped = PandaFacialMouthFoldoutState.CountMapped(authoringTarget, section);
            int total = PandaFacialMouthFoldoutState.GetChannelCount(section);
            string status = mapped == 0 ? "Unmapped" : mapped + "/" + total;
            bool isOpen = PandaFacialMouthFoldoutState.Get(authoringTarget, section);

            EditorGUI.BeginChangeCheck();
            bool newValue = EditorGUILayout.Foldout(
                isOpen,
                label + "  (" + status + ")",
                true);
            if (EditorGUI.EndChangeCheck())
                PandaFacialMouthFoldoutState.Set(authoringTarget, section, newValue);
            return newValue;
        }

        private void DrawEyeExpressionController(PandaFacialAuthoringTarget authoringTarget)
        {
            PandaFacialEyelidValueState eyelidLeft = PandaFacialControllerValueReader.ReadEyelid(
                authoringTarget, true, PandaFacialUpperFaceSessionState.GetOpenExpression(authoringTarget, true));
            PandaFacialEyelidValueState eyelidRight = PandaFacialControllerValueReader.ReadEyelid(
                authoringTarget, false, PandaFacialUpperFaceSessionState.GetOpenExpression(authoringTarget, false));
            RememberEyelidExpression(authoringTarget, true, eyelidLeft.Value);
            RememberEyelidExpression(authoringTarget, false, eyelidRight.Value);
            PandaFacialControllerValueState surpriseLeft = ReadControl(
                authoringTarget, "Surprise Left", PandaFacialSemanticChannels.EyeSurpriseL);
            PandaFacialControllerValueState surpriseRight = ReadControl(
                authoringTarget, "Surprise Right", PandaFacialSemanticChannels.EyeSurpriseR);
            PandaFacialControllerValueState angryLeft = ReadControl(
                authoringTarget, "Angry Left", PandaFacialSemanticChannels.EyeAngryL);
            PandaFacialControllerValueState angryRight = ReadControl(
                authoringTarget, "Angry Right", PandaFacialSemanticChannels.EyeAngryR);
            PandaFacialControllerValueState sadLeft = ReadControl(
                authoringTarget, "Sad Left", PandaFacialSemanticChannels.EyeSadL);
            PandaFacialControllerValueState sadRight = ReadControl(
                authoringTarget, "Sad Right", PandaFacialSemanticChannels.EyeSadR);
            PandaFacialControllerValueState jitoLeft = ReadControl(
                authoringTarget, "Eyelid Jito Left", PandaFacialSemanticChannels.EyeJitoL);
            PandaFacialControllerValueState jitoRight = ReadControl(
                authoringTarget, "Eyelid Jito Right", PandaFacialSemanticChannels.EyeJitoR);

            IReadOnlyList<PandaFacialSemanticWeight> current =
                PandaFacialControllerLogic.EyeExpressions(
                    EyelidClose(eyelidLeft.Value), EyelidClose(eyelidRight.Value),
                    EyelidSmile(eyelidLeft.Value), EyelidSmile(eyelidRight.Value),
                    surpriseLeft.Value, surpriseRight.Value,
                    angryLeft.Value, angryRight.Value,
                    sadLeft.Value, sadRight.Value,
                    jitoLeft.Value, jitoRight.Value);

            bool isOpen = DrawUpperFaceFoldout(
                authoringTarget,
                PandaFacialUpperFaceSection.EyeExpression,
                "Eyelid / Eye Expression");
            if (isOpen)
            {
                EditorGUI.indentLevel++;
                bool sync = DrawSessionToggle(
                    "Eyelid Sync L/R",
                    PandaFacialUpperFaceSessionState.GetEyelidSync(authoringTarget),
                    value => PandaFacialUpperFaceSessionState.SetEyelidSync(authoringTarget, value));
                DrawEyelidController(authoringTarget, true, eyelidLeft, sync);
                DrawEyelidController(authoringTarget, false, eyelidRight, sync);
                DrawPairedController(authoringTarget, "Surprise",
                    PandaFacialSemanticChannels.EyeSurpriseL, PandaFacialSemanticChannels.EyeSurpriseR,
                    surpriseLeft, surpriseRight, sync);
                DrawPairedController(authoringTarget, "Angry",
                    PandaFacialSemanticChannels.EyeAngryL, PandaFacialSemanticChannels.EyeAngryR,
                    angryLeft, angryRight, sync);
                DrawPairedController(authoringTarget, "Sad",
                    PandaFacialSemanticChannels.EyeSadL, PandaFacialSemanticChannels.EyeSadR,
                    sadLeft, sadRight, sync);
                DrawPairedController(authoringTarget, "Eyelid Jito",
                    PandaFacialSemanticChannels.EyeJitoL, PandaFacialSemanticChannels.EyeJitoR,
                    jitoLeft, jitoRight, sync);
                EditorGUI.indentLevel--;
            }

            DrawEyelidGroupResetAndKeyButtons(
                authoringTarget,
                "Reset Eyelid / Eye Expression",
                "Set All Eyelid Keys",
                PandaFacialControllerLogic.ResetEyeExpressions(),
                current);
        }

        private void DrawBrowController(PandaFacialAuthoringTarget authoringTarget)
        {
            PandaFacialControllerValueState upLeft = ReadControl(
                authoringTarget, "Brow Up Left", PandaFacialSemanticChannels.BrowUpL);
            PandaFacialControllerValueState upRight = ReadControl(
                authoringTarget, "Brow Up Right", PandaFacialSemanticChannels.BrowUpR);
            PandaFacialControllerValueState downLeft = ReadControl(
                authoringTarget, "Brow Down Left", PandaFacialSemanticChannels.BrowDownL);
            PandaFacialControllerValueState downRight = ReadControl(
                authoringTarget, "Brow Down Right", PandaFacialSemanticChannels.BrowDownR);
            PandaFacialControllerValueState angryLeft = ReadControl(
                authoringTarget, "Brow Angry Left", PandaFacialSemanticChannels.BrowAngryL);
            PandaFacialControllerValueState angryRight = ReadControl(
                authoringTarget, "Brow Angry Right", PandaFacialSemanticChannels.BrowAngryR);
            PandaFacialControllerValueState sadLeft = ReadControl(
                authoringTarget, "Brow Sad Left", PandaFacialSemanticChannels.BrowSadL);
            PandaFacialControllerValueState sadRight = ReadControl(
                authoringTarget, "Brow Sad Right", PandaFacialSemanticChannels.BrowSadR);
            PandaFacialControllerValueState smileLeft = ReadControl(
                authoringTarget, "Brow Smile Left", PandaFacialSemanticChannels.BrowSmileL);
            PandaFacialControllerValueState smileRight = ReadControl(
                authoringTarget, "Brow Smile Right", PandaFacialSemanticChannels.BrowSmileR);
            PandaFacialControllerValueState seriousLeft = ReadControl(
                authoringTarget, "Brow Serious Left", PandaFacialSemanticChannels.BrowSeriousL);
            PandaFacialControllerValueState seriousRight = ReadControl(
                authoringTarget, "Brow Serious Right", PandaFacialSemanticChannels.BrowSeriousR);

            IReadOnlyList<PandaFacialSemanticWeight> current = PandaFacialControllerLogic.Brows(
                upLeft.Value, upRight.Value,
                downLeft.Value, downRight.Value,
                angryLeft.Value, angryRight.Value,
                sadLeft.Value, sadRight.Value,
                smileLeft.Value, smileRight.Value,
                seriousLeft.Value, seriousRight.Value);

            bool isOpen = DrawUpperFaceFoldout(
                authoringTarget,
                PandaFacialUpperFaceSection.Brow,
                "Brow");
            if (isOpen)
            {
                EditorGUI.indentLevel++;
                bool sync = DrawSessionToggle(
                    "Brow Sync L/R",
                    PandaFacialUpperFaceSessionState.GetBrowSync(authoringTarget),
                    value => PandaFacialUpperFaceSessionState.SetBrowSync(authoringTarget, value));
                DrawPairedController(authoringTarget, "Brow Up",
                    PandaFacialSemanticChannels.BrowUpL, PandaFacialSemanticChannels.BrowUpR,
                    upLeft, upRight, sync);
                DrawPairedController(authoringTarget, "Brow Down",
                    PandaFacialSemanticChannels.BrowDownL, PandaFacialSemanticChannels.BrowDownR,
                    downLeft, downRight, sync);
                DrawPairedController(authoringTarget, "Brow Angry",
                    PandaFacialSemanticChannels.BrowAngryL, PandaFacialSemanticChannels.BrowAngryR,
                    angryLeft, angryRight, sync);
                DrawPairedController(authoringTarget, "Brow Sad",
                    PandaFacialSemanticChannels.BrowSadL, PandaFacialSemanticChannels.BrowSadR,
                    sadLeft, sadRight, sync);
                DrawPairedController(authoringTarget, "Brow Smile",
                    PandaFacialSemanticChannels.BrowSmileL, PandaFacialSemanticChannels.BrowSmileR,
                    smileLeft, smileRight, sync);
                DrawPairedController(authoringTarget, "Brow Serious",
                    PandaFacialSemanticChannels.BrowSeriousL, PandaFacialSemanticChannels.BrowSeriousR,
                    seriousLeft, seriousRight, sync);
                EditorGUI.indentLevel--;
            }

            DrawGroupResetAndKeyButtons(
                authoringTarget,
                "Reset Brow",
                "Set All Brow Keys",
                PandaFacialControllerLogic.ResetBrows(),
                current);
        }

        private static bool DrawUpperFaceFoldout(
            PandaFacialAuthoringTarget authoringTarget,
            PandaFacialUpperFaceSection section,
            string label)
        {
            int mapped = PandaFacialUpperFaceFoldoutState.CountMapped(authoringTarget, section);
            int total = PandaFacialUpperFaceFoldoutState.GetChannelCount(section);
            string status = mapped == 0 ? "Unmapped" : mapped + "/" + total;
            bool isOpen = PandaFacialUpperFaceFoldoutState.Get(authoringTarget, section);
            EditorGUI.BeginChangeCheck();
            bool newValue = EditorGUILayout.Foldout(isOpen, label + "  (" + status + ")", true);
            if (EditorGUI.EndChangeCheck())
                PandaFacialUpperFaceFoldoutState.Set(authoringTarget, section, newValue);
            return newValue;
        }

        private static bool DrawSessionToggle(string label, bool value, Action<bool> setter)
        {
            EditorGUI.BeginChangeCheck();
            bool result = EditorGUILayout.ToggleLeft(label, value);
            if (EditorGUI.EndChangeCheck())
                setter(result);
            return result;
        }

        private void DrawEyelidController(
            PandaFacialAuthoringTarget authoringTarget,
            bool isLeft,
            PandaFacialEyelidValueState state,
            bool sync)
        {
            string side = isLeft ? "Left" : "Right";
            EditorGUILayout.Space(2f);
            EditorGUILayout.LabelField(side + " Eyelid Close / Smile", EditorStyles.miniBoldLabel);
            EditorGUILayout.LabelField(
                "Smile  ←  Expression  →  Close     Open ↑ / Closed ↓",
                EditorStyles.centeredGreyMiniLabel);

            PandaFacialEyelidState value = DrawEyelidPad(side + " Eyelid", state.Value, out bool changed);
            if (changed)
            {
                RememberEyelidExpression(authoringTarget, isLeft, value);
                if (sync)
                {
                    PandaFacialUpperFaceSessionState.SetOpenExpression(authoringTarget, !isLeft, value.Expression);
                    PreviewController(
                        authoringTarget,
                        PandaFacialControllerLogic.EyelidCloseSmilePair(value, value));
                }
                else
                {
                    PreviewController(
                        authoringTarget,
                        PandaFacialControllerLogic.EyelidCloseSmile(isLeft, value));
                }
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Reset " + side))
            {
                var neutral = new PandaFacialEyelidState(1f, 1f);
                PandaFacialUpperFaceSessionState.SetOpenExpression(authoringTarget, isLeft, 1f);
                if (sync)
                    PandaFacialUpperFaceSessionState.SetOpenExpression(authoringTarget, !isLeft, 1f);
                PreviewController(
                    authoringTarget,
                    sync
                        ? PandaFacialControllerLogic.EyelidCloseSmilePair(neutral, neutral)
                        : PandaFacialControllerLogic.EyelidCloseSmile(isLeft, neutral));
            }
            if (GUILayout.Button(new GUIContent(
                    "Set " + side + " Eyelid Key",
                    "Write both Eyelid Close and Eye Smile at the current Timeline or Animator clip time.")))
            {
                WriteControllerKeys(
                    authoringTarget,
                    sync
                        ? PandaFacialControllerLogic.EyelidCloseSmilePair(value, value)
                        : PandaFacialControllerLogic.EyelidCloseSmile(isLeft, value));
            }
            EditorGUILayout.EndHorizontal();
            DrawStateWarning(state.Close);
            DrawStateWarning(state.Smile);
        }

        private PandaFacialEyelidState DrawEyelidPad(
            string controlName,
            PandaFacialEyelidState value,
            out bool changed)
        {
            float availableWidth = Mathf.Max(100f, EditorGUIUtility.currentViewWidth - 90f);
            float size = Mathf.Clamp(availableWidth, 110f, 180f);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            Rect rect = GUILayoutUtility.GetRect(size, size, GUILayout.Width(size), GUILayout.Height(size));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            GUI.Box(rect, GUIContent.none);
            int controlId = GUIUtility.GetControlID(controlName.GetHashCode(), FocusType.Passive, rect);
            bool isActive = GUIUtility.hotControl == controlId;
            Color quiet = EditorGUIUtility.isProSkin
                ? new Color(1f, 1f, 1f, 0.18f)
                : new Color(0f, 0f, 0f, 0.18f);
            Color active = new Color(0.2f, 0.65f, 1f, 0.85f);
            Color verticalColor = isActive && eyelidDragAxis == PandaFacialPadAxis.Vertical ? active : quiet;
            Color horizontalColor = isActive && eyelidDragAxis == PandaFacialPadAxis.Horizontal ? active : quiet;
            EditorGUI.DrawRect(new Rect(rect.center.x, rect.y + 1f, 1f, rect.height - 2f), verticalColor);
            EditorGUI.DrawRect(new Rect(rect.x + 1f, rect.center.y, rect.width - 2f, 1f), horizontalColor);

            changed = false;
            Event current = Event.current;
            if (current.button == 0)
            {
                if (current.type == EventType.MouseDown && rect.Contains(current.mousePosition))
                {
                    GUIUtility.hotControl = controlId;
                    eyelidDragStartMouse = current.mousePosition;
                    eyelidDragStartValue = value;
                    eyelidDragAxis = PandaFacialPadAxis.None;
                    current.Use();
                }
                else if (current.type == EventType.MouseDrag && isActive)
                {
                    Vector2 delta = current.mousePosition - eyelidDragStartMouse;
                    eyelidDragAxis = PandaFacialAxisLock.Resolve(eyelidDragAxis, delta);
                    if (eyelidDragAxis != PandaFacialPadAxis.None)
                    {
                        value = PandaFacialAxisLock.Apply(
                            eyelidDragStartValue,
                            delta,
                            new Vector2(rect.width, rect.height),
                            eyelidDragAxis);
                        changed = true;
                        GUI.changed = true;
                    }
                    current.Use();
                }
                else if (current.type == EventType.MouseUp && isActive)
                {
                    GUIUtility.hotControl = 0;
                    eyelidDragAxis = PandaFacialPadAxis.None;
                    current.Use();
                }
            }

            Vector2 handle = new Vector2(
                Mathf.Lerp(rect.xMin, rect.xMax, value.Expression),
                Mathf.Lerp(rect.yMax, rect.yMin, value.Openness));
            GUI.Box(new Rect(handle.x - 6f, handle.y - 6f, 12f, 12f), GUIContent.none, EditorStyles.miniButton);
            EditorGUIUtility.AddCursorRect(rect, MouseCursor.MoveArrow);
            return value;
        }

        private static void RememberEyelidExpression(
            PandaFacialAuthoringTarget target,
            bool isLeft,
            PandaFacialEyelidState state)
        {
            if (state.Openness < 0.9999f)
                PandaFacialUpperFaceSessionState.SetOpenExpression(target, isLeft, state.Expression);
        }

        private static float EyelidClose(PandaFacialEyelidState state)
        {
            return (1f - state.Openness) * state.Expression;
        }

        private static float EyelidSmile(PandaFacialEyelidState state)
        {
            return (1f - state.Openness) * (1f - state.Expression);
        }

        private void DrawPairedController(
            PandaFacialAuthoringTarget authoringTarget,
            string label,
            string leftSemanticId,
            string rightSemanticId,
            PandaFacialControllerValueState left,
            PandaFacialControllerValueState right,
            bool sync)
        {
            EditorGUILayout.LabelField(label, EditorStyles.miniBoldLabel);
            EditorGUI.BeginChangeCheck();
            float newLeft = EditorGUILayout.Slider("L", left.Value, 0f, 1f);
            if (EditorGUI.EndChangeCheck())
                PreviewController(authoringTarget, sync
                    ? PandaFacialControllerLogic.Pair(leftSemanticId, rightSemanticId, newLeft, newLeft)
                    : PandaFacialControllerLogic.Single(leftSemanticId, newLeft));

            EditorGUI.BeginChangeCheck();
            float newRight = EditorGUILayout.Slider("R", right.Value, 0f, 1f);
            if (EditorGUI.EndChangeCheck())
                PreviewController(authoringTarget, sync
                    ? PandaFacialControllerLogic.Pair(leftSemanticId, rightSemanticId, newRight, newRight)
                    : PandaFacialControllerLogic.Single(rightSemanticId, newRight));

            DrawResetAndKeyButtons(
                authoringTarget,
                "Reset " + label,
                "Set " + label + " Keys",
                "Write the current independent Left and Right values to the active animation clip.",
                PandaFacialControllerLogic.Pair(leftSemanticId, rightSemanticId, 0f, 0f),
                PandaFacialControllerLogic.Pair(
                    leftSemanticId, rightSemanticId, left.Value, sync ? left.Value : right.Value));
            DrawStateWarning(left);
            DrawStateWarning(right);
        }

        private void DrawEyelidGroupResetAndKeyButtons(
            PandaFacialAuthoringTarget authoringTarget,
            string resetLabel,
            string keyLabel,
            IReadOnlyList<PandaFacialSemanticWeight> resetOutputs,
            IReadOnlyList<PandaFacialSemanticWeight> currentOutputs)
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(resetLabel))
            {
                PandaFacialUpperFaceSessionState.SetOpenExpression(authoringTarget, true, 1f);
                PandaFacialUpperFaceSessionState.SetOpenExpression(authoringTarget, false, 1f);
                PreviewController(authoringTarget, resetOutputs);
            }
            if (GUILayout.Button(new GUIContent(
                    keyLabel,
                    "Write all current Eyelid and Eye Expression BlendShape values at the current Timeline or Animator clip time.")))
            {
                WriteControllerKeys(authoringTarget, currentOutputs);
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawGroupResetAndKeyButtons(
            PandaFacialAuthoringTarget authoringTarget,
            string resetLabel,
            string keyLabel,
            IReadOnlyList<PandaFacialSemanticWeight> resetOutputs,
            IReadOnlyList<PandaFacialSemanticWeight> currentOutputs)
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(resetLabel))
                PreviewController(authoringTarget, resetOutputs);
            if (GUILayout.Button(keyLabel))
                WriteControllerKeys(authoringTarget, currentOutputs);
            EditorGUILayout.EndHorizontal();
        }

        private static PandaFacialControllerValueState ReadControl(
            PandaFacialAuthoringTarget authoringTarget,
            string label,
            string semanticId)
        {
            return PandaFacialControllerValueReader.ReadSingle(authoringTarget, label, semanticId);
        }

        private void DrawMouthCornerPad(
            PandaFacialAuthoringTarget authoringTarget,
            bool isLeft,
            PandaFacialControllerVector2State state)
        {
            string side = isLeft ? "Left" : "Right";
            EditorGUILayout.LabelField(side + " Corner XY", EditorStyles.miniBoldLabel);
            EditorGUILayout.LabelField(
                isLeft ? "Inner  ←    →  Outer" : "Outer  ←    →  Inner",
                EditorStyles.centeredGreyMiniLabel);
            EditorGUI.BeginChangeCheck();
            Vector2 value = DrawVector2Pad(side + " Mouth Corner XY", state.Value);
            if (EditorGUI.EndChangeCheck())
            {
                PreviewController(
                    authoringTarget,
                    CreateCornerOutputs(isLeft, value));
            }

            IReadOnlyList<PandaFacialSemanticWeight> resetOutputs = mirrorMouthCorners
                ? (isLeft
                    ? PandaFacialControllerLogic.MirrorMouthCornersFromLeft(0f, 0f)
                    : PandaFacialControllerLogic.MirrorMouthCornersFromRight(0f, 0f))
                : (isLeft
                    ? PandaFacialControllerLogic.MouthCornerLeft(0f, 0f)
                    : PandaFacialControllerLogic.MouthCornerRight(0f, 0f));
            DrawResetAndKeyButtons(
                authoringTarget,
                "Reset " + side + " Corner",
                "Set " + side + " Corner Key",
                mirrorMouthCorners
                    ? "Write both mirrored Mouth Corner states to the active animation clip at the current time."
                    : "Write the current " + side + " Mouth Corner state to the active animation clip at the current time.",
                resetOutputs,
                CreateCornerOutputs(isLeft, state.Value));
            DrawStateWarning(state.X);
            DrawStateWarning(state.Y);
        }

        private IReadOnlyList<PandaFacialSemanticWeight> CreateCornerOutputs(bool isLeft, Vector2 value)
        {
            if (mirrorMouthCorners)
            {
                return isLeft
                    ? PandaFacialControllerLogic.MirrorMouthCornersFromLeft(value.x, value.y)
                    : PandaFacialControllerLogic.MirrorMouthCornersFromRight(value.x, value.y);
            }

            return isLeft
                ? PandaFacialControllerLogic.MouthCornerLeft(value.x, value.y)
                : PandaFacialControllerLogic.MouthCornerRight(value.x, value.y);
        }

        private void DrawVowelSlider(
            PandaFacialAuthoringTarget authoringTarget,
            string label,
            string semanticId,
            PandaFacialControllerValueState state)
        {
            EditorGUI.BeginChangeCheck();
            float value = EditorGUILayout.Slider(label, state.Value, 0f, 1f);
            if (EditorGUI.EndChangeCheck())
                PreviewController(authoringTarget, PandaFacialControllerLogic.Single(semanticId, value));
            DrawResetAndKeyButtons(
                authoringTarget,
                "Reset " + label,
                "Set " + label + " Key",
                "Write the current " + label + " value to the active animation clip at the current time.",
                PandaFacialControllerLogic.Single(semanticId, 0f),
                PandaFacialControllerLogic.Single(semanticId, state.Value));
            DrawStateWarning(state);
        }

        private void DrawResetAndKeyButtons(
            PandaFacialAuthoringTarget authoringTarget,
            string resetLabel,
            string keyLabel,
            string keyTooltip,
            IReadOnlyList<PandaFacialSemanticWeight> resetOutputs,
            IReadOnlyList<PandaFacialSemanticWeight> currentOutputs)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(resetLabel, GUILayout.MinWidth(96f)))
                PreviewController(authoringTarget, resetOutputs);
            if (GUILayout.Button(new GUIContent(keyLabel, keyTooltip), GUILayout.MinWidth(112f)))
                WriteControllerKeys(authoringTarget, currentOutputs);
            EditorGUILayout.EndHorizontal();
        }

        private static PandaFacialControllerValueState ReadVowel(
            PandaFacialAuthoringTarget authoringTarget,
            string label,
            string semanticId)
        {
            return PandaFacialControllerValueReader.ReadSingle(authoringTarget, label, semanticId);
        }

        private static void DrawStateWarning(PandaFacialControllerValueState state)
        {
            if (state.HasWarning)
                EditorGUILayout.HelpBox(state.Warning, MessageType.Warning);
        }

        private static Vector2 DrawVector2Pad(string controlName, Vector2 value)
        {
            float availableWidth = Mathf.Max(100f, EditorGUIUtility.currentViewWidth - 70f);
            float size = Mathf.Clamp(availableWidth, 110f, 220f);

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            Rect rect = GUILayoutUtility.GetRect(size, size, GUILayout.Width(size), GUILayout.Height(size));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            GUI.Box(rect, GUIContent.none);
            Color lineColor = EditorGUIUtility.isProSkin
                ? new Color(1f, 1f, 1f, 0.18f)
                : new Color(0f, 0f, 0f, 0.18f);
            EditorGUI.DrawRect(
                new Rect(rect.x + rect.width * 0.5f, rect.y + 1f, 1f, rect.height - 2f),
                lineColor);
            EditorGUI.DrawRect(
                new Rect(rect.x + 1f, rect.y + rect.height * 0.5f, rect.width - 2f, 1f),
                lineColor);

            int controlId = GUIUtility.GetControlID(controlName.GetHashCode(), FocusType.Passive, rect);
            Event current = Event.current;
            if (current.button == 0)
            {
                if (current.type == EventType.MouseDown && rect.Contains(current.mousePosition))
                {
                    GUIUtility.hotControl = controlId;
                    value = PadMouseToValue(rect, current.mousePosition);
                    GUI.changed = true;
                    current.Use();
                }
                else if (current.type == EventType.MouseDrag && GUIUtility.hotControl == controlId)
                {
                    value = PadMouseToValue(rect, current.mousePosition);
                    GUI.changed = true;
                    current.Use();
                }
                else if (current.type == EventType.MouseUp && GUIUtility.hotControl == controlId)
                {
                    GUIUtility.hotControl = 0;
                    current.Use();
                }
            }

            value.x = Mathf.Clamp(value.x, -1f, 1f);
            value.y = Mathf.Clamp(value.y, -1f, 1f);
            Vector2 handle = new Vector2(
                Mathf.Lerp(rect.xMin, rect.xMax, (value.x + 1f) * 0.5f),
                Mathf.Lerp(rect.yMax, rect.yMin, (value.y + 1f) * 0.5f));
            Rect handleRect = new Rect(handle.x - 6f, handle.y - 6f, 12f, 12f);
            GUI.Box(handleRect, GUIContent.none, EditorStyles.miniButton);
            EditorGUIUtility.AddCursorRect(rect, MouseCursor.MoveArrow);
            return value;
        }

        private static Vector2 PadMouseToValue(Rect rect, Vector2 mousePosition)
        {
            float x = ((mousePosition.x - rect.xMin) / rect.width) * 2f - 1f;
            float y = 1f - ((mousePosition.y - rect.yMin) / rect.height) * 2f;
            return new Vector2(Mathf.Clamp(x, -1f, 1f), Mathf.Clamp(y, -1f, 1f));
        }

        private void DrawControllerVerification(PandaFacialAuthoringTarget authoringTarget)
        {
            EditorGUILayout.HelpBox(
                "Debug controls only. Values are not serialized; outputs are resolved through Semantic Mapping.",
                MessageType.Info);

            DrawSignedController(
                authoringTarget,
                "Mouth Position X",
                ref mouthPositionX,
                PandaFacialControllerLogic.MouthPositionX);
            DrawSignedController(
                authoringTarget,
                "Mouth Position Y",
                ref mouthPositionY,
                PandaFacialControllerLogic.MouthPositionY);
            DrawSignedController(
                authoringTarget,
                "Mouth Width",
                ref mouthWidth,
                PandaFacialControllerLogic.MouthWidth);
            DrawSignedController(
                authoringTarget,
                "Mouth Corner L Y",
                ref mouthCornerLeftY,
                PandaFacialControllerLogic.MouthCornerLeft);
            DrawSignedController(
                authoringTarget,
                "Mouth Corner R Y",
                ref mouthCornerRightY,
                PandaFacialControllerLogic.MouthCornerRight);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("AIUEO", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            vowelA = EditorGUILayout.Slider("A", vowelA, 0f, 1f);
            vowelI = EditorGUILayout.Slider("I", vowelI, 0f, 1f);
            vowelU = EditorGUILayout.Slider("U", vowelU, 0f, 1f);
            vowelE = EditorGUILayout.Slider("E", vowelE, 0f, 1f);
            vowelO = EditorGUILayout.Slider("O", vowelO, 0f, 1f);
            IReadOnlyList<PandaFacialSemanticWeight> vowels = PandaFacialControllerLogic.Vowels(
                vowelA, vowelI, vowelU, vowelE, vowelO);
            if (EditorGUI.EndChangeCheck())
                PreviewController(authoringTarget, vowels);
            if (GUILayout.Button("Write AIUEO Keys at Timeline Playhead"))
                WriteControllerKeys(authoringTarget, vowels);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Eyelid Close", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            blinkLeft = EditorGUILayout.Slider("Eyelid Close L", blinkLeft, 0f, 1f);
            blinkRight = EditorGUILayout.Slider("Eyelid Close R", blinkRight, 0f, 1f);
            IReadOnlyList<PandaFacialSemanticWeight> blink = PandaFacialControllerLogic.Blink(
                blinkLeft, blinkRight);
            if (EditorGUI.EndChangeCheck())
                PreviewController(authoringTarget, blink);
            if (GUILayout.Button("Write Eyelid Close Keys at Timeline Playhead"))
                WriteControllerKeys(authoringTarget, blink);

            if (!string.IsNullOrEmpty(controllerStatus))
                EditorGUILayout.HelpBox(controllerStatus, MessageType.Info);
        }

        private void DrawSignedController(
            PandaFacialAuthoringTarget authoringTarget,
            string label,
            ref float value,
            Func<float, IReadOnlyList<PandaFacialSemanticWeight>> createOutputs)
        {
            EditorGUI.BeginChangeCheck();
            value = EditorGUILayout.Slider(label, value, -1f, 1f);
            IReadOnlyList<PandaFacialSemanticWeight> outputs = createOutputs(value);
            if (EditorGUI.EndChangeCheck())
                PreviewController(authoringTarget, outputs);
            if (GUILayout.Button("Write " + label + " Keys at Timeline Playhead"))
                WriteControllerKeys(authoringTarget, outputs);
        }

        private void PreviewController(
            PandaFacialAuthoringTarget authoringTarget,
            IReadOnlyList<PandaFacialSemanticWeight> outputs)
        {
            PandaFacialControllerOperationReport report =
                PandaFacialControllerAnimationUtility.ApplyPreview(authoringTarget, outputs);
            controllerStatus = FormatControllerReport("Preview", report);
            SceneView.RepaintAll();
        }

        private void WriteControllerKeys(
            PandaFacialAuthoringTarget authoringTarget,
            IReadOnlyList<PandaFacialSemanticWeight> outputs)
        {
            SkinnedMeshRenderer contextRenderer = null;
            for (int i = 0; i < outputs.Count; i++)
            {
                PandaFacialResolvedMapping resolved = PandaFacialMappingResolver.Resolve(
                    authoringTarget,
                    outputs[i].SemanticId);
                if (resolved.IsMapped)
                {
                    contextRenderer = resolved.Renderer;
                    break;
                }
            }

            PandaFacialTimelineContext context = default;
            string error = null;
            if (contextRenderer == null ||
                !PandaFacialTimelineContextProvider.TryGetContext(
                    contextRenderer,
                    out context,
                    out error))
            {
                controllerStatus = string.IsNullOrEmpty(error)
                    ? "No mapped controller output has a writable Timeline context."
                    : error;
                return;
            }

            try
            {
                PandaFacialControllerOperationReport report =
                    PandaFacialControllerAnimationUtility.WriteKeys(
                        authoringTarget,
                        outputs,
                        context.AnimationClip,
                        context.AnimationRoot,
                        (float)context.AnimationTime);
                controllerStatus = FormatControllerReport("Key write", report);
                TimelineEditor.Refresh(RefreshReason.ContentsModified | RefreshReason.SceneNeedsUpdate);
                context.Director.Evaluate();
                SceneView.RepaintAll();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                controllerStatus = exception.Message;
            }
        }

        private static string FormatControllerReport(
            string operation,
            PandaFacialControllerOperationReport report)
        {
            string summary = operation + ": " + report.AppliedCount +
                             " targets applied from " + report.OutputCount + " semantic outputs; " +
                             report.UnmappedCount + " unmapped; " +
                             report.InvalidCount + " invalid; " +
                             report.DisabledCount + " disabled.";
            if (report.Warnings.Count == 0)
                return summary;
            return summary + "\n" + string.Join("\n", report.Warnings);
        }

        private void DrawSemanticSelector()
        {
            IReadOnlyList<PandaFacialSemanticChannel> channels = PandaFacialSemanticChannels.BuiltIn;
            string[] options = new string[channels.Count];
            int selectedIndex = 0;
            bool found = false;

            for (int i = 0; i < channels.Count; i++)
            {
                PandaFacialSemanticChannel channel = channels[i];
                options[i] = GetGroupDisplayName(channel.Group) + " / " + channel.DisplayName;
                if (channel.Id == selectedSemanticIdProperty.stringValue)
                {
                    selectedIndex = i;
                    found = true;
                }
            }

            EditorGUI.BeginChangeCheck();
            selectedIndex = EditorGUILayout.Popup("Semantic Channel", selectedIndex, options);
            if (EditorGUI.EndChangeCheck() || !found)
                selectedSemanticIdProperty.stringValue = channels[selectedIndex].Id;
        }

        private void DrawMappingSettings(PandaFacialAuthoringTarget authoringTarget)
        {
            using (new EditorGUI.DisabledScope(authoringTarget.DefaultFaceRenderer == null))
            {
                if (GUILayout.Button(new GUIContent(
                        "Auto Detect Unmapped Channels",
                        "Detect confirmed aliases on Default Face Renderer. Existing mappings are preserved.")))
                {
                    serializedObject.ApplyModifiedProperties();
                    autoDetectReport = PandaFacialAutoDetectUtility.DetectAndApply(authoringTarget);
                    serializedObject.Update();
                }
            }

            if (authoringTarget.DefaultFaceRenderer == null)
                EditorGUILayout.HelpBox("Assign Default Face Renderer to use Auto Detect.", MessageType.Info);
            DrawAutoDetectReport(autoDetectReport);
            EditorGUILayout.Space();

            foreach (PandaFacialSemanticGroup group in Enum.GetValues(typeof(PandaFacialSemanticGroup)))
            {
                groupFoldouts[group] = EditorGUILayout.Foldout(
                    groupFoldouts[group],
                    GetGroupDisplayName(group),
                    true);
                if (!groupFoldouts[group])
                    continue;

                EditorGUI.indentLevel++;
                foreach (PandaFacialSemanticChannel channel in PandaFacialSemanticChannels.BuiltIn)
                {
                    if (channel.Group != group)
                        continue;
                    DrawMappingRow(authoringTarget, channel);
                }
                EditorGUI.indentLevel--;
            }
        }

        private void DrawAutoDetectReport(PandaFacialDetectionReport report)
        {
            if (report == null)
                return;
            if (!string.IsNullOrEmpty(report.Error))
            {
                EditorGUILayout.HelpBox(report.Error, MessageType.Warning);
                return;
            }

            EditorGUILayout.HelpBox(
                "Auto Detect Result — Mapped: " + report.MappedCount +
                ", Unmapped: " + report.UnmappedCount +
                ", Ambiguous: " + report.AmbiguousCount +
                ", Skipped Existing: " + report.SkippedExistingCount,
                report.AmbiguousCount > 0 ? MessageType.Warning : MessageType.Info);
            if (report.AmbiguousCount == 0)
                return;

            autoDetectDetailsFoldout = EditorGUILayout.Foldout(
                autoDetectDetailsFoldout,
                "Ambiguous Candidates",
                true);
            if (!autoDetectDetailsFoldout)
                return;
            EditorGUI.indentLevel++;
            for (int i = 0; i < report.Entries.Count; i++)
            {
                PandaFacialDetectionEntry entry = report.Entries[i];
                if (entry.Status != PandaFacialDetectionStatus.Ambiguous)
                    continue;
                string displayName = PandaFacialSemanticChannels.TryGet(
                    entry.SemanticId,
                    out PandaFacialSemanticChannel channel)
                    ? channel.DisplayName
                    : entry.SemanticId;
                EditorGUILayout.LabelField(
                    displayName + ": " + string.Join(", ", entry.Candidates));
            }
            EditorGUI.indentLevel--;
        }

        private void DrawMappingRow(
            PandaFacialAuthoringTarget authoringTarget,
            PandaFacialSemanticChannel channel)
        {
            SerializedProperty mapping = FindMappingProperty(channel.Id);
            if (mapping != null)
                PandaFacialAutoDetectUtility.Migrate(mapping);
            SkinnedMeshRenderer overrideRenderer = mapping != null
                ? mapping.FindPropertyRelative("targetRenderer").objectReferenceValue as SkinnedMeshRenderer
                : null;
            SkinnedMeshRenderer defaultRenderer =
                defaultFaceRendererProperty.objectReferenceValue as SkinnedMeshRenderer;
            SkinnedMeshRenderer effectiveRenderer = overrideRenderer != null
                ? overrideRenderer
                : defaultRenderer;
            string currentBlendShape = mapping != null
                ? mapping.FindPropertyRelative("blendShapeName").stringValue
                : string.Empty;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(channel.DisplayName, GUILayout.Width(125f));
            EditorGUI.BeginChangeCheck();
            string newBlendShape = DrawBlendShapeMappingField(effectiveRenderer, currentBlendShape);
            bool changed = EditorGUI.EndChangeCheck();

            PandaFacialResolvedMapping status = PandaFacialMappingResolver.Resolve(authoringTarget, channel.Id);
            if (status.Status == PandaFacialMappingStatus.Unmapped)
                GUILayout.Label("—", GUILayout.Width(16f));
            else if (!status.IsMapped)
                GUILayout.Label(EditorGUIUtility.IconContent("console.warnicon.sml"), GUILayout.Width(20f));
            else
                GUILayout.Label("✓", GUILayout.Width(16f));
            EditorGUILayout.EndHorizontal();

            if (changed)
            {
                mapping = mapping ?? CreateMappingProperty(channel.Id);
                mapping.FindPropertyRelative("blendShapeName").stringValue = newBlendShape;
            }

            bool advanced = mappingOverrideFoldouts.TryGetValue(channel.Id, out bool isOpen) && isOpen;
            advanced = EditorGUILayout.Foldout(advanced, "Advanced / Additional Targets", true);
            mappingOverrideFoldouts[channel.Id] = advanced;
            if (advanced)
            {
                mapping = mapping ?? CreateMappingProperty(channel.Id);
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField("Primary Target", EditorStyles.miniBoldLabel);
                EditorGUILayout.PropertyField(
                    mapping.FindPropertyRelative("primaryEnabled"),
                    new GUIContent("Enabled"));
                SerializedProperty primaryMultiplier =
                    mapping.FindPropertyRelative("primaryWeightMultiplier");
                primaryMultiplier.floatValue = EditorGUILayout.Slider(
                    "Weight Multiplier",
                    primaryMultiplier.floatValue,
                    0f,
                    1f);
                bool useOverride = overrideRenderer != null;
                EditorGUI.BeginChangeCheck();
                useOverride = EditorGUILayout.Toggle("Override Renderer", useOverride);
                if (EditorGUI.EndChangeCheck())
                {
                    mapping = mapping ?? CreateMappingProperty(channel.Id);
                    mapping.FindPropertyRelative("targetRenderer").objectReferenceValue =
                        useOverride ? defaultRenderer : null;
                    overrideRenderer = useOverride ? defaultRenderer : null;
                }

                if (useOverride)
                {
                    EditorGUI.BeginChangeCheck();
                    SkinnedMeshRenderer selectedOverride =
                        (SkinnedMeshRenderer)EditorGUILayout.ObjectField(
                            "Renderer",
                            overrideRenderer,
                            typeof(SkinnedMeshRenderer),
                            true);
                    if (EditorGUI.EndChangeCheck())
                    {
                        mapping = mapping ?? CreateMappingProperty(channel.Id);
                        mapping.FindPropertyRelative("targetRenderer").objectReferenceValue = selectedOverride;
                    }
                }

                EditorGUILayout.Space();
                SerializedProperty additionalTargets =
                    mapping.FindPropertyRelative("additionalTargets");
                bool removed = false;
                for (int i = 0; i < additionalTargets.arraySize; i++)
                {
                    SerializedProperty additional = additionalTargets.GetArrayElementAtIndex(i);
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Additional Target " + (i + 2), EditorStyles.miniBoldLabel);
                    if (GUILayout.Button("Remove", GUILayout.Width(64f)))
                    {
                        additionalTargets.DeleteArrayElementAtIndex(i);
                        removed = true;
                    }
                    EditorGUILayout.EndHorizontal();
                    if (!removed)
                    {
                        EditorGUILayout.PropertyField(
                            additional.FindPropertyRelative("enabled"),
                            new GUIContent("Enabled"));
                        SerializedProperty multiplier =
                            additional.FindPropertyRelative("weightMultiplier");
                        multiplier.floatValue = EditorGUILayout.Slider(
                            "Weight Multiplier",
                            multiplier.floatValue,
                            0f,
                            1f);

                        SerializedProperty additionalRenderer =
                            additional.FindPropertyRelative("targetRenderer");
                        bool additionalOverride = additionalRenderer.objectReferenceValue != null;
                        EditorGUI.BeginChangeCheck();
                        additionalOverride = EditorGUILayout.Toggle(
                            "Override Renderer",
                            additionalOverride);
                        if (EditorGUI.EndChangeCheck())
                        {
                            additionalRenderer.objectReferenceValue =
                                additionalOverride ? defaultRenderer : null;
                        }
                        if (additionalOverride)
                        {
                            additionalRenderer.objectReferenceValue = EditorGUILayout.ObjectField(
                                "Renderer",
                                additionalRenderer.objectReferenceValue,
                                typeof(SkinnedMeshRenderer),
                                true);
                        }

                        SkinnedMeshRenderer additionalEffectiveRenderer =
                            additionalRenderer.objectReferenceValue as SkinnedMeshRenderer;
                        if (additionalEffectiveRenderer == null)
                            additionalEffectiveRenderer = defaultRenderer;
                        SerializedProperty additionalName =
                            additional.FindPropertyRelative("blendShapeName");
                        additionalName.stringValue = DrawBlendShapeMappingField(
                            additionalEffectiveRenderer,
                            additionalName.stringValue);
                    }
                    EditorGUILayout.EndVertical();
                    if (removed)
                        break;
                }

                if (GUILayout.Button("+ Add Target"))
                {
                    int index = additionalTargets.arraySize;
                    additionalTargets.InsertArrayElementAtIndex(index);
                    SerializedProperty added = additionalTargets.GetArrayElementAtIndex(index);
                    added.FindPropertyRelative("targetRenderer").objectReferenceValue = null;
                    added.FindPropertyRelative("blendShapeName").stringValue = string.Empty;
                    added.FindPropertyRelative("weightMultiplier").floatValue = 1f;
                    added.FindPropertyRelative("enabled").boolValue = true;
                }
                EditorGUI.indentLevel--;
            }

            if (status.Status != PandaFacialMappingStatus.Unmapped && !status.IsMapped)
                EditorGUILayout.HelpBox(PandaFacialMappingResolver.GetStatusMessage(status), MessageType.Warning);
        }

        private static string DrawBlendShapeMappingField(
            SkinnedMeshRenderer renderer,
            string currentBlendShape)
        {
            if (renderer == null || renderer.sharedMesh == null || renderer.sharedMesh.blendShapeCount == 0)
                return EditorGUILayout.TextField(currentBlendShape, GUILayout.MinWidth(140f));

            int count = renderer.sharedMesh.blendShapeCount;
            bool currentExists = string.IsNullOrEmpty(currentBlendShape);
            for (int i = 0; i < count && !currentExists; i++)
                currentExists = renderer.sharedMesh.GetBlendShapeName(i) == currentBlendShape;

            int missingOffset = !currentExists ? 1 : 0;
            string[] options = new string[count + 1 + missingOffset];
            options[0] = "<Unmapped>";
            int selected = 0;
            if (!currentExists)
            {
                options[1] = "<Missing: " + currentBlendShape + ">";
                selected = 1;
            }

            for (int i = 0; i < count; i++)
            {
                int optionIndex = i + 1 + missingOffset;
                options[optionIndex] = renderer.sharedMesh.GetBlendShapeName(i);
                if (options[optionIndex] == currentBlendShape)
                    selected = optionIndex;
            }

            int newSelected = EditorGUILayout.Popup(selected, options, GUILayout.MinWidth(140f));
            if (newSelected == 0)
                return string.Empty;
            if (!currentExists && newSelected == 1)
                return currentBlendShape;
            return options[newSelected];
        }

        private void DrawSemanticAuthoring(PandaFacialResolvedMapping resolved)
        {
            EditorGUILayout.LabelField("Semantic Control", EditorStyles.boldLabel);
            if (!resolved.IsMapped)
            {
                MessageType type = resolved.Status == PandaFacialMappingStatus.Unmapped
                    ? MessageType.Info
                    : MessageType.Warning;
                EditorGUILayout.HelpBox(PandaFacialMappingResolver.GetStatusMessage(resolved), type);
                return;
            }

            EditorGUILayout.LabelField("Actual BlendShape", resolved.BlendShapeName);
            DrawActualBlendShapeSlider(resolved.Renderer, resolved.BlendShapeIndex);
            DrawTimelineContextAndKeyButton(
                resolved.Renderer,
                resolved.BlendShapeName,
                "Write Semantic Key at Timeline Playhead");
        }

        private void DrawDirectSettings()
        {
            EditorGUILayout.PropertyField(targetRendererProperty, new GUIContent("Target Renderer"));
            SkinnedMeshRenderer renderer = targetRendererProperty.objectReferenceValue as SkinnedMeshRenderer;
            if (renderer == null || renderer.sharedMesh == null || renderer.sharedMesh.blendShapeCount == 0)
                return;

            int count = renderer.sharedMesh.blendShapeCount;
            string[] names = new string[count];
            int selectedIndex = 0;
            bool found = false;
            for (int i = 0; i < count; i++)
            {
                names[i] = renderer.sharedMesh.GetBlendShapeName(i);
                if (names[i] == blendShapeNameProperty.stringValue)
                {
                    selectedIndex = i;
                    found = true;
                }
            }

            EditorGUI.BeginChangeCheck();
            selectedIndex = EditorGUILayout.Popup("BlendShape", selectedIndex, names);
            if (EditorGUI.EndChangeCheck() || !found)
                blendShapeNameProperty.stringValue = names[selectedIndex];
        }

        private void DrawDirectAuthoring()
        {
            SkinnedMeshRenderer renderer = targetRendererProperty.objectReferenceValue as SkinnedMeshRenderer;
            if (renderer == null || renderer.sharedMesh == null)
            {
                EditorGUILayout.HelpBox("Assign a direct-mode target renderer.", MessageType.Info);
                return;
            }

            int index = renderer.sharedMesh.GetBlendShapeIndex(blendShapeNameProperty.stringValue);
            if (index < 0)
            {
                EditorGUILayout.HelpBox("The direct-mode BlendShape is invalid.", MessageType.Warning);
                return;
            }

            EditorGUILayout.LabelField("Direct Verification", EditorStyles.boldLabel);
            DrawActualBlendShapeSlider(renderer, index);
            DrawTimelineContextAndKeyButton(renderer, blendShapeNameProperty.stringValue, "Write Direct Key");
        }

        private static void DrawActualBlendShapeSlider(SkinnedMeshRenderer renderer, int blendShapeIndex)
        {
            float currentValue = renderer.GetBlendShapeWeight(blendShapeIndex);
            EditorGUI.BeginChangeCheck();
            float newValue = EditorGUILayout.Slider("Value", currentValue, 0f, 100f);
            if (!EditorGUI.EndChangeCheck())
                return;

            Undo.RecordObject(renderer, "Set Panda Facial BlendShape");
            renderer.SetBlendShapeWeight(blendShapeIndex, newValue);
            PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            EditorUtility.SetDirty(renderer);
        }

        private static void DrawTimelineContextAndKeyButton(
            SkinnedMeshRenderer renderer,
            string blendShapeName,
            string buttonLabel)
        {
            bool hasContext = PandaFacialTimelineContextProvider.TryGetContext(
                renderer,
                out PandaFacialTimelineContext context,
                out string error);

            if (hasContext)
            {
                DrawContext(context);
                if (!IsRendererUnderRoot(renderer, context.AnimationRoot))
                    error = "The target renderer is not under the selected track binding root.";
            }

            if (!string.IsNullOrEmpty(error))
                EditorGUILayout.HelpBox(error, MessageType.Warning);

            using (new EditorGUI.DisabledScope(!string.IsNullOrEmpty(error) || !hasContext))
            {
                if (GUILayout.Button(buttonLabel))
                    WriteKey(renderer, blendShapeName, context);
            }
        }

        private static void DrawContext(PandaFacialTimelineContext context)
        {
            EditorGUILayout.LabelField("Timeline Context", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField("Director", context.Director, typeof(UnityEngine.Playables.PlayableDirector), true);
                EditorGUILayout.ObjectField("Animation Clip", context.AnimationClip, typeof(AnimationClip), false);
                EditorGUILayout.ObjectField("Binding Root", context.AnimationRoot, typeof(Transform), true);
                EditorGUILayout.DoubleField("Timeline Time", context.SequenceTime);
                EditorGUILayout.DoubleField("Animation Time", context.AnimationTime);
            }
        }

        private static void WriteKey(
            SkinnedMeshRenderer renderer,
            string blendShapeName,
            PandaFacialTimelineContext context)
        {
            try
            {
                int blendShapeIndex = renderer.sharedMesh.GetBlendShapeIndex(blendShapeName);
                EditorCurveBinding binding = PandaFacialAnimationUtility.CreateBlendShapeBinding(
                    context.AnimationRoot,
                    renderer,
                    blendShapeName);
                PandaFacialAnimationUtility.WriteBlendShapeKey(
                    context.AnimationClip,
                    binding,
                    (float)context.AnimationTime,
                    renderer.GetBlendShapeWeight(blendShapeIndex));

                TimelineEditor.Refresh(RefreshReason.ContentsModified | RefreshReason.SceneNeedsUpdate);
                context.Director.Evaluate();
                SceneView.RepaintAll();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("Panda Facial", exception.Message, "OK");
            }
        }

        private SerializedProperty FindMappingProperty(string semanticId)
        {
            for (int i = 0; i < semanticMappingsProperty.arraySize; i++)
            {
                SerializedProperty mapping = semanticMappingsProperty.GetArrayElementAtIndex(i);
                if (mapping.FindPropertyRelative("semanticId").stringValue == semanticId)
                    return mapping;
            }
            return null;
        }

        private SerializedProperty CreateMappingProperty(string semanticId)
        {
            int index = semanticMappingsProperty.arraySize;
            semanticMappingsProperty.InsertArrayElementAtIndex(index);
            SerializedProperty mapping = semanticMappingsProperty.GetArrayElementAtIndex(index);
            mapping.FindPropertyRelative("semanticId").stringValue = semanticId;
            mapping.FindPropertyRelative("targetRenderer").objectReferenceValue = null;
            mapping.FindPropertyRelative("blendShapeName").stringValue = string.Empty;
            mapping.FindPropertyRelative("schemaVersion").intValue = 2;
            mapping.FindPropertyRelative("primaryWeightMultiplier").floatValue = 1f;
            mapping.FindPropertyRelative("primaryEnabled").boolValue = true;
            mapping.FindPropertyRelative("additionalTargets").arraySize = 0;
            return mapping;
        }

        private static bool IsRendererUnderRoot(SkinnedMeshRenderer renderer, Transform root)
        {
            return renderer != null && root != null &&
                   (renderer.transform == root || renderer.transform.IsChildOf(root));
        }

        private static string GetGroupDisplayName(PandaFacialSemanticGroup group)
        {
            switch (group)
            {
                case PandaFacialSemanticGroup.LipSync:
                    return "Mouth Vowels";
                case PandaFacialSemanticGroup.MouthPosition:
                    return "Mouth Position";
                case PandaFacialSemanticGroup.MouthCorner:
                    return "Mouth Corner";
                case PandaFacialSemanticGroup.MouthWidth:
                    return "Mouth Corner Width";
                case PandaFacialSemanticGroup.EyeExpression:
                    return "Eyelid / Eye Expression";
                case PandaFacialSemanticGroup.Brow:
                    return "Brow";
                default:
                    return group.ToString();
            }
        }
    }
}
