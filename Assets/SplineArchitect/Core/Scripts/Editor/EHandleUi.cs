// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: EHandleUi.cs
//
// Author: Mikael Danielsson
// Date Created: 16-02-2023
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System;

using UnityEditor;
using UnityEngine;

using SplineArchitect.Objects;
using SplineArchitect.Ui;
using SplineArchitect.Libraries;
using SplineArchitect.Utility;
using SplineArchitect.CustomTools;

namespace SplineArchitect
{
    public class EHandleUi
    {
        public enum ButtonType
        {
            DEFAULT,
            DEFAULT_ACTIVE,
            DEFAULT_GREEN,
            DEFAULT_RED,
            DEFAULT_MIDDLE_LEFT,
            SUB_MENU
        }

        public struct DropDownButtonData
        {
            public GUIContent gUIContent;
            public bool active;
        }

        private static bool initalized = false;
        private static int backgroundStyleCounter = 0;
        private static bool blockNextMouseUp;

        //Options
        public static string[] optionsEasing;
        public static string[] optionsNoiseType;
        public static string[] optionsNoiseGroups = new string[] { "A", "B", "C", "D", "E", "F", "G", "H" };
        public static string[] optionsNoiseGroupsAndNone = new string[] { "None", "A", "B", "C", "D", "E", "F", "G", "H" };
        public static string[] optionsSpace = new string[] { "World", "Local" };

        private static void Init(SceneView sceneView)
        {
            if (!initalized)
            {
                initalized = true;

                //LibraryTexture needs to run first
                LibraryTexture.Initalize();
                LibraryGUIStyle.Initalize();
                LibraryGUIContent.Initalize();
                MenuGeneral.Start(sceneView);
                CreateEasingList();
                CreateNoiseTypeList();
            }
        }

        public static void BeforeSceneGUIGlobal(Event e)
        {
            if (e.type == EventType.MouseDown)
                GUI.FocusControl("");

            if(e.control && e.keyCode != KeyCode.None && e.keyCode != KeyCode.LeftControl && e.keyCode != KeyCode.RightControl)
                GUI.FocusControl("");

            if (e.command && e.keyCode != KeyCode.None && e.keyCode != KeyCode.LeftCommand && e.keyCode != KeyCode.RightCommand)
                GUI.FocusControl("");
        }

        public static void OnSelectionChange()
        {
            EActionToLateSceneGUI.Add(() => { GUI.FocusControl(""); }, EventType.Layout);
        }

        public static void OnSceneGUIGlobal(SceneView sceneView)
        {
            Init(sceneView);

            if (GlobalSettings.IsUiHidden())
                return;

            Handles.BeginGUI();

            MenuGeneral.OnSceneGUI(sceneView);

            if (!GlobalSettings.IsUiMinimized())
            {
                Spline spline = EHandleSelection.selectedSpline;

                if (spline != null)
                {
                    MenuSpline.OnSceneGUI(spline);

                    SplineObject selected = EHandleSelection.selectedSplineObject;

                    if (selected != null)
                    {
                        MenuSplineObject.OnSceneGUI(spline, selected);
                    }
                    else if (spline.selectedControlPoint > 0)
                    {
                        int index = SplineUtility.ControlPointIdToSegmentIndex(spline.selectedControlPoint);

                        if (index >= spline.segments.Count)
                            index = spline.segments.Count - 1;

                        Segment segement = spline.segments[index];
                        Segment.ControlHandle segementType = SplineUtility.GetControlPointType(spline.selectedControlPoint);

                        if (segementType == Segment.ControlHandle.ANCHOR)
                        {
                            MenuAnchor.OnSceneGUI(spline, index);
                        }
                        else if (segementType == Segment.ControlHandle.TANGENT_A || segementType == Segment.ControlHandle.TANGENT_B)
                        {
                            MenuTangent.OnSceneGUI(spline, segement, segementType);
                        }
                    }
                }
            }

            HandleSceneGUIBlockInput();

            Handles.EndGUI();
        }

        private static void HandleSceneGUIBlockInput()
        {
            Event e = Event.current;

            if (e.type == EventType.MouseDown)
            {
                if (MousePointerAboveAnyMenu(e))
                {
                    blockNextMouseUp = true;
                    e.Use();
                }
            }

            if(e.type == EventType.MouseUp && blockNextMouseUp)
            {
                blockNextMouseUp = false;
                e.Use();
            }
        }

        public static bool MousePointerAboveAnyMenu(Event e)
        {
            if (GlobalSettings.IsUiHidden())
                return false;

            if (GlobalSettings.IsUiMinimized())
                return false;

            if (EHandleSelection.selectedSplineObject != null && MenuSplineObject.GetRect().Contains(e.mousePosition) ||
                EHandleSelection.selectedSpline != null && MenuSpline.GetRect().Contains(e.mousePosition) ||
                EHandleSelection.selectedSpline != null && MenuAnchor.GetRect().Contains(e.mousePosition) ||
                EHandleSelection.selectedSpline != null && MenuTangent.GetRect().Contains(e.mousePosition))
            {
                return true;
            }

            return false;
        }

        public static void CreateEasingList()
        {
            optionsEasing = Enum.GetNames(typeof(Easing));

            for (int y = 0; y < optionsEasing.Length; y++)
            {
                optionsEasing[y] = EConversionUtility.CapitalizeString(optionsEasing[y]);
            }
        }

        public static void CreateNoiseTypeList()
        {
            optionsNoiseType = Enum.GetNames(typeof(NoiseLayer.Type));

            for (int y = 0; y < optionsNoiseType.Length; y++)
            {
                optionsNoiseType[y] = EConversionUtility.CapitalizeString(optionsNoiseType[y]);
            }
        }

        public static void CreateToggle(bool value, Action<bool> onChange, float width = 0)
        {
            bool oldValue = value;
            if(width == 0)
                value = EditorGUILayout.Toggle(value);
            else
                value = EditorGUILayout.Toggle(value, GUILayout.Width(width));

            if (oldValue != value)
                onChange.Invoke(value);
        }

        public static void CreateButtonToggle(ButtonType buttonType, GUIContent icon, GUIContent iconActive, float width, float height, Action onPress, bool active, bool enable = true)
        {
            GUIStyle buttonStyle = null;
            GUIStyle buttonActiveStyle = null;

            if(buttonType == ButtonType.SUB_MENU)
            {
                buttonStyle = LibraryGUIStyle.buttonSubMenu;
                buttonActiveStyle = LibraryGUIStyle.buttonSubMenuActive;
            }
            else if (buttonType == ButtonType.DEFAULT_GREEN)
            {
                buttonStyle = LibraryGUIStyle.buttonDefaultGreen;
                buttonActiveStyle = LibraryGUIStyle.buttonDefaultActive;
            }
            else if (buttonType == ButtonType.DEFAULT_RED)
            {
                buttonStyle = LibraryGUIStyle.buttonDefaultRed;
                buttonActiveStyle = LibraryGUIStyle.buttonDefaultActive;
            }
            else if(buttonType == ButtonType.DEFAULT_MIDDLE_LEFT)
            {
                buttonStyle = LibraryGUIStyle.buttonDefaultMiddleLeft;
                buttonActiveStyle = LibraryGUIStyle.buttonDefaultMiddleLeftActive;
            }
            else
            {
                buttonStyle = LibraryGUIStyle.buttonDefault;
                buttonActiveStyle = LibraryGUIStyle.buttonDefaultActive;
            }

            GUI.enabled = enable;
            if (GUILayout.Button(active ? iconActive : icon, active ? buttonActiveStyle : buttonStyle, GUILayout.Width(width), GUILayout.Height(height)))
                onPress.Invoke();
            GUI.enabled = true;
        }

        public static void CreateButton(ButtonType buttonType, GUIContent icon, float width, float height, Action onPress, bool enable = true)
        {
            GUIStyle buttonStyle = null;

            if (buttonType == ButtonType.SUB_MENU)
                buttonStyle = LibraryGUIStyle.buttonSubMenu;
            else if (buttonType == ButtonType.DEFAULT_RED)
                buttonStyle = LibraryGUIStyle.buttonDefaultRed;
            else if (buttonType == ButtonType.DEFAULT_GREEN)
                buttonStyle = LibraryGUIStyle.buttonDefaultGreen;
            else if (buttonType == ButtonType.DEFAULT_MIDDLE_LEFT)
                buttonStyle = LibraryGUIStyle.buttonDefaultMiddleLeft;
            else if (buttonType == ButtonType.DEFAULT_ACTIVE)
                buttonStyle = LibraryGUIStyle.buttonDefaultActive;
            else
                buttonStyle = LibraryGUIStyle.buttonDefault;

            GUI.enabled = enable;
            if (GUILayout.Button(icon, buttonStyle, GUILayout.Width(width), GUILayout.Height(height)))
                onPress.Invoke();
            GUI.enabled = true;
        }

        public static void CreateObjectField(string label, UnityEngine.Object o, Type typeOf, Action<UnityEngine.Object> actionOnChange, float width = 0, float labelWidth = -1, bool skipGroup = false, bool blackText = false)
        {
            if(!skipGroup) GUILayout.BeginHorizontal(GetBackgroundStyle());
            if (labelWidth == -1)
                GUILayout.Label(label, blackText ? LibraryGUIStyle.textDefaultBlack : LibraryGUIStyle.textDefault);
            else
                GUILayout.Label(label, blackText ? LibraryGUIStyle.textDefaultBlack : LibraryGUIStyle.textDefault, GUILayout.Width(labelWidth));
            GUI.SetNextControlName(label);
            UnityEngine.Object newO;

            if (width == 0)
                newO = EditorGUILayout.ObjectField(o, typeOf, true);
            else
                newO = EditorGUILayout.ObjectField(o, typeOf, true, GUILayout.Width(width));

            if (!skipGroup) GUILayout.EndHorizontal();

            if (o != newO) actionOnChange.Invoke(newO);

            if (GUI.GetNameOfFocusedControl() == label && Event.current.type == EventType.KeyUp && Event.current.keyCode == KeyCode.Delete)
                Event.current.Use();
        }

        public static void CreateXYZInputFields(string label, Vector3 currentValue, ref string containerX, ref string containerY, ref string containerZ, Action<Vector3, Vector3> actionOnValueChange, float inputFieldWidth, float labelWidth = -1, float spaceAfterXYFields = -1, bool disableX = false, bool disableY = false, bool disableZ = false, bool skipGroup = false, bool boldText = true)
        {
            if (!skipGroup) GUILayout.BeginHorizontal(GetBackgroundStyle());
            GUI.enabled = !disableX;
            if (boldText) GUILayout.Label($"<b>{label}</b>", LibraryGUIStyle.textDefault);
            else
            {
                if(labelWidth == -1)
                    GUILayout.Label(label, LibraryGUIStyle.textDefault);
                else
                    GUILayout.Label(label, LibraryGUIStyle.textDefault, GUILayout.Width(labelWidth));
            }
            GUILayout.Label("X", LibraryGUIStyle.specificX);
            GUI.SetNextControlName("field_x");
            containerX = GUILayout.TextField(containerX, LibraryGUIStyle.textFieldNoWidth, GUILayout.Width(inputFieldWidth));
            if(spaceAfterXYFields != -1) CreateEmpty(spaceAfterXYFields);

            GUI.enabled = !disableY;
            GUILayout.Label("Y", LibraryGUIStyle.specificYZ);
            GUI.SetNextControlName("field_y");
            containerY = GUILayout.TextField(containerY, LibraryGUIStyle.textFieldNoWidth, GUILayout.Width(inputFieldWidth));
            if (spaceAfterXYFields != -1) CreateEmpty(spaceAfterXYFields);

            GUI.enabled = !disableZ;
            GUILayout.Label("Z", LibraryGUIStyle.specificYZ);
            GUI.SetNextControlName("field_z");
            containerZ = GUILayout.TextField(containerZ, LibraryGUIStyle.textFieldNoWidth, GUILayout.Width(inputFieldWidth));
            if (!skipGroup) GUILayout.EndHorizontal();

            GUI.enabled = true;

            Vector3 dif = new Vector3(0, 0, 0);
            Vector3 newValue = currentValue;
            newValue.x = Mathf.Round(newValue.x * 100) / 100;
            newValue.y = Mathf.Round(newValue.y * 100) / 100;
            newValue.z = Mathf.Round(newValue.z * 100) / 100;

            if (Event.current.type != EventType.Layout)
                return;

            if (PositionTool.activePart == PositionTool.Part.NONE && (GUI.GetNameOfFocusedControl() == "field_x" ||
                                                                      GUI.GetNameOfFocusedControl() == "field_y" ||
                                                                      GUI.GetNameOfFocusedControl() == "field_z"))
            {
                if (float.TryParse(containerX, out float newXValue))
                {
                    dif.x = newValue.x - newXValue;
                    newValue.x = newXValue;
                }

                if (float.TryParse(containerY, out float newYValue))
                {
                    dif.y = newValue.y - newYValue;
                    newValue.y = newYValue;
                }

                if (float.TryParse(containerZ, out float newZValue))
                {
                    dif.z = newValue.z - newZValue;
                    newValue.z = newZValue;
                }

                if (GeneralUtility.IsEqual(newXValue, currentValue.x, 0.01f) &&
                    GeneralUtility.IsEqual(newYValue, currentValue.y, 0.01f) &&
                    GeneralUtility.IsEqual(newZValue, currentValue.z, 0.01f))
                    return;

                actionOnValueChange.Invoke(newValue, dif);
            }
            else
            {
                containerX = newValue.x.ToString();
                containerY = newValue.y.ToString();
                containerZ = newValue.z.ToString();
            }
        }

        public static void CreateXYZFromToInputFields(string label, Vector3 currentValueFrom, Vector3 currentValueTo, ref string containerXFrom, ref string containerYFrom, ref string containerZFrom, ref string containerXTo, ref string containerYTo, ref string containerZTo, Action<Vector3, Vector3> actionOnValueChange, float paddingLeft = 6, float labelWidth = 58, bool skipGroup = false)
        {
            if (!skipGroup) GUILayout.BeginHorizontal(GetBackgroundStyle());
            GUILayout.Space(paddingLeft);
            GUILayout.Label($"<b>{label}</b>", LibraryGUIStyle.textNoWdith, GUILayout.Width(labelWidth));

            GUILayout.BeginHorizontal(GUILayout.Width(325));
            GUILayout.Label("X", LibraryGUIStyle.specificFromToX);
            GUI.SetNextControlName("field_from_x");
            containerXFrom = GUILayout.TextField(containerXFrom, LibraryGUIStyle.textFieldSmall);
            GUILayout.Label("-", LibraryGUIStyle.textNoWdith);
            GUI.SetNextControlName("field_to_x");
            containerXTo = GUILayout.TextField(containerXTo, LibraryGUIStyle.textFieldSmall);

            GUILayout.Label("Y", LibraryGUIStyle.specificFromToYZ);
            GUI.SetNextControlName("field_from_y");
            containerYFrom = GUILayout.TextField(containerYFrom, LibraryGUIStyle.textFieldSmall);
            GUILayout.Label("-", LibraryGUIStyle.textNoWdith);
            GUI.SetNextControlName("field_to_y");
            containerYTo = GUILayout.TextField(containerYTo, LibraryGUIStyle.textFieldSmall);
            GUILayout.Label("Z", LibraryGUIStyle.specificFromToYZ);
            GUI.SetNextControlName("field_from_z");
            containerZFrom = GUILayout.TextField(containerZFrom, LibraryGUIStyle.textFieldSmall);
            GUILayout.Label("-", LibraryGUIStyle.textNoWdith);
            GUI.SetNextControlName("field_to_z");
            containerZTo = GUILayout.TextField(containerZTo, LibraryGUIStyle.textFieldSmall);
            GUILayout.EndHorizontal();

            if (!skipGroup) GUILayout.EndHorizontal();

            Vector3 newValueFrom = currentValueFrom;
            Vector3 newValueTo = currentValueTo;

            if (Event.current.type != EventType.Layout)
                return;

            if (PositionTool.activePart == PositionTool.Part.NONE && (GUI.GetNameOfFocusedControl() == "field_from_x" ||
                                                                      GUI.GetNameOfFocusedControl() == "field_from_y" ||
                                                                      GUI.GetNameOfFocusedControl() == "field_from_z" ||
                                                                      GUI.GetNameOfFocusedControl() == "field_to_x" ||
                                                                      GUI.GetNameOfFocusedControl() == "field_to_y" ||
                                                                      GUI.GetNameOfFocusedControl() == "field_to_z"))
            {
                if (float.TryParse(containerXFrom, out float newValueFromX)) newValueFrom.x = newValueFromX;
                if (float.TryParse(containerYFrom, out float newValueFromY)) newValueFrom.y = newValueFromY;
                if (float.TryParse(containerZFrom, out float newValueFromZ)) newValueFrom.z = newValueFromZ;

                if (float.TryParse(containerXTo, out float newValueToX)) newValueTo.x = newValueToX;
                if (float.TryParse(containerYTo, out float newValueToY)) newValueTo.y = newValueToY;
                if (float.TryParse(containerZTo, out float newValueToZ)) newValueTo.z = newValueToZ;


                if (GeneralUtility.IsEqual(newValueFrom, currentValueFrom) &&
                    GeneralUtility.IsEqual(newValueTo, currentValueTo))
                    return;

                actionOnValueChange.Invoke(newValueFrom, newValueTo);
            }
            else
            {
                containerXFrom = newValueFrom.x.ToString();
                containerYFrom = newValueFrom.y.ToString();
                containerZFrom = newValueFrom.z.ToString();
                containerXTo = newValueTo.x.ToString();
                containerYTo = newValueTo.y.ToString();
                containerZTo = newValueTo.z.ToString();
            }
        }

        public static void CreateFromToInputField(string label, float currentValueFrom, float currentValueTo, ref string containerFrom, ref string containerTo, Action<float, float> actionOnValueChange,  float paddingLeft = 6, float labelWidth = 58, bool boldText = true, bool skipGroup = false)
        {
            if(!skipGroup) GUILayout.BeginHorizontal(GetBackgroundStyle());
            GUILayout.Space(paddingLeft);
            if(boldText) GUILayout.Label($"<b>{label}</b>", LibraryGUIStyle.textNoWdith, GUILayout.Width(labelWidth));
            else GUILayout.Label(label, LibraryGUIStyle.textNoWdith, GUILayout.Width(labelWidth));
            GUI.SetNextControlName("field_from");
            containerFrom = GUILayout.TextField(containerFrom, LibraryGUIStyle.textFieldSmall);
            GUILayout.Label("-", LibraryGUIStyle.textNoWdith, GUILayout.Width(6));
            GUI.SetNextControlName("field_to");
            containerTo = GUILayout.TextField(containerTo, LibraryGUIStyle.textFieldSmall);
            if (!skipGroup) GUILayout.EndHorizontal();

            float newValueFrom = currentValueFrom;
            float newValueTo = currentValueTo;

            if (Event.current.type != EventType.Layout)
                return;

            if (PositionTool.activePart == PositionTool.Part.NONE && (GUI.GetNameOfFocusedControl() == "field_from" ||
                                                                      GUI.GetNameOfFocusedControl() == "field_to"))
            {
                if (float.TryParse(containerFrom, out float valueFrom)) newValueFrom = valueFrom;
                if (float.TryParse(containerTo, out float valueTo)) newValueTo = valueTo;

                if (GeneralUtility.IsEqual(newValueFrom, currentValueFrom) &&
                    GeneralUtility.IsEqual(newValueTo, currentValueTo))
                    return;

                actionOnValueChange.Invoke(newValueFrom, newValueTo);
            }
            else
            {
                containerFrom = newValueFrom.ToString();
                containerTo = newValueTo.ToString();
            }
        }

        public static void CreateSliderAndInputField(string label, float currentValue, ref string textFieldValue, Action<float> actionOnValueChange, float sliderLeftValue, float sliderRightValue, float sliderWidth, float textFieldWidth, float labelWidth = 0, bool skipGroup = false, bool enable = true)
        {
            if(!skipGroup) GUILayout.BeginHorizontal(GetBackgroundStyle());
            GUI.enabled = enable;
            if(labelWidth == 0)
                GUILayout.Label(label, LibraryGUIStyle.textDefault);
            else
                GUILayout.Label(label, LibraryGUIStyle.textDefault, GUILayout.Width(labelWidth));
            float oldValue = currentValue;
#if UNITY_EDITOR_OSX
            float newValue = GUILayout.HorizontalSlider(currentValue, sliderLeftValue, sliderRightValue, GUILayout.Width(sliderWidth));
#else
            float newValue = GUILayout.HorizontalSlider(currentValue, sliderLeftValue, sliderRightValue, LibraryGUIStyle.sliderDefault, LibraryGUIStyle.sliderThumbDefault, GUILayout.Width(sliderWidth));
#endif
            GUI.SetNextControlName(label);
            textFieldValue = GUILayout.TextField(textFieldValue, LibraryGUIStyle.textFieldNoWidth, GUILayout.Width(textFieldWidth));
            GUI.enabled = true;
            if (!skipGroup) GUILayout.EndHorizontal();

            newValue = Mathf.Round(newValue * 100) / 100;
            if (GUI.GetNameOfFocusedControl() == label)
            {
                if (float.TryParse(textFieldValue, out float newValueContainer))
                    newValue = newValueContainer;
            }
            else
                textFieldValue = newValue.ToString();

            if (GeneralUtility.IsEqual(newValue, oldValue))
                return;

            actionOnValueChange.Invoke(newValue);
        }

        public static void CreateLabelField(string label, GUIStyle guiStyle, bool skipGroup = false, float labelWidth = -1)
        {
            if(!skipGroup) GUILayout.BeginHorizontal(GetBackgroundStyle());
            if(labelWidth == -1) GUILayout.Label(label, guiStyle);
            else GUILayout.Label(label, guiStyle, GUILayout.Width(labelWidth));
            if (!skipGroup) GUILayout.EndHorizontal();
        }

        public static void CreateToggleField(string text, bool currentValue, Action<bool> actionOnValueChange, bool enable = true, bool skipGroup = false, float labelWidth = 0, float width = 0)
        {
            bool oldValue = currentValue;
            if (!skipGroup) GUILayout.BeginHorizontal(GetBackgroundStyle());
            GUI.enabled = enable;
            if(text.Length > 0)
            {
                if (labelWidth == 0) GUILayout.Label(text, LibraryGUIStyle.textDefault);
                else GUILayout.Label(text, LibraryGUIStyle.textDefault, GUILayout.Width(labelWidth));
            }
            if(width == 0) currentValue = EditorGUILayout.Toggle(currentValue);
            else currentValue = EditorGUILayout.Toggle(currentValue, GUILayout.Width(width));

            GUI.enabled = true;
            if (!skipGroup) GUILayout.EndHorizontal();

            if (currentValue == oldValue)
                return;

            actionOnValueChange.Invoke(currentValue);
        }

        public static void CreateToggleXYZField(string label, Vector3Int currentValue, Action<Vector3Int> actionOnValueChange, float paddingLeft = 6, bool skipGroup = false)
        {
            Vector3Int oldValue = currentValue;

            //Follow axels
            if(!skipGroup) GUILayout.BeginHorizontal(GetBackgroundStyle());
            GUILayout.Label("", LibraryGUIStyle.textNoWdith, GUILayout.Width(paddingLeft));
            GUILayout.Label(label, LibraryGUIStyle.textNoWdith);
            GUILayout.Label("X", LibraryGUIStyle.specificX);
            currentValue.x = EditorGUILayout.Toggle(currentValue.x != 0) ? 1 : 0;

            GUILayout.Label("Y", LibraryGUIStyle.specificYZ);
            currentValue.y = EditorGUILayout.Toggle(currentValue.y != 0) ? 1 : 0;

            GUILayout.Label("Z", LibraryGUIStyle.specificYZ);
            currentValue.z = EditorGUILayout.Toggle(currentValue.z != 0) ? 1 : 0;
            if (!skipGroup) GUILayout.EndHorizontal();

            if (currentValue == oldValue)
                return;

            actionOnValueChange.Invoke(currentValue);
        }

        public static void CreateInputField(string labelAndControllerName, float currentValue, ref string textFieldValue, Action<float> actionOnValueChange, float fieldWidth, float labelWidth = -1, bool skipGroup = false, bool emptyLabel = false)
        {
            if (!skipGroup) GUILayout.BeginHorizontal(GetBackgroundStyle());
            if (labelWidth == -1) GUILayout.Label(emptyLabel ? "" : labelAndControllerName, LibraryGUIStyle.textDefault);
            else GUILayout.Label(emptyLabel ? "" : labelAndControllerName, LibraryGUIStyle.textDefault, GUILayout.Width(labelWidth));
            GUI.SetNextControlName(labelAndControllerName);
            textFieldValue = GUILayout.TextField(textFieldValue, LibraryGUIStyle.textFieldNoWidth, GUILayout.Width(fieldWidth));
            if (!skipGroup) GUILayout.EndHorizontal();

            float newValue = currentValue;

            if (GUI.GetNameOfFocusedControl() == labelAndControllerName)
            {
                if (float.TryParse(textFieldValue, out float value))
                    newValue = value;
            }
            else
                textFieldValue = currentValue.ToString();

            if (GeneralUtility.IsEqual(newValue, currentValue))
                return;

            actionOnValueChange.Invoke(newValue);
        }

        public static void CreateInputFieldString(string text, string currentValue, float width, Action<string> actionOnValueChange, float labelWidth = 0, bool skipGroup = false, bool enable = false, bool blackText = false)
        {
            string oldValue = currentValue;
            if (!skipGroup) GUILayout.BeginHorizontal(GetBackgroundStyle());
            GUI.enabled = enable;
            if(labelWidth == 0) GUILayout.Label(text, blackText ? LibraryGUIStyle.textDefaultBlack : LibraryGUIStyle.textDefault);
            else GUILayout.Label(text, blackText ? LibraryGUIStyle.textDefaultBlack : LibraryGUIStyle.textDefault, GUILayout.Width(labelWidth));
            GUI.SetNextControlName(text);
            currentValue = GUILayout.TextField(currentValue, LibraryGUIStyle.textFieldNoWidth, GUILayout.Width(width));
            GUI.enabled = true;
            if (!skipGroup) GUILayout.EndHorizontal();

            if (currentValue == oldValue)
                return;

            actionOnValueChange.Invoke(currentValue);
        }

        public static void CreatePopupField(string label, float width, int currentType, string[] options, Action<int> actionOnValueChange, float labelWidth = -1, bool skipGroup = false, bool enable = true, bool skipLabel = false, bool blackText = false)
        {
            int oldType = currentType;

            if (!skipGroup) GUILayout.BeginHorizontal(GetBackgroundStyle());
            GUI.enabled = enable;
            if(!skipLabel)
            {
                if(labelWidth == -1)
                    GUILayout.Label(label, blackText ? LibraryGUIStyle.textDefaultBlack : LibraryGUIStyle.textDefault);
                else
                    GUILayout.Label(label, blackText ? LibraryGUIStyle.textDefaultBlack : LibraryGUIStyle.textDefault, GUILayout.Width(labelWidth));
            }
            currentType = EditorGUILayout.Popup(currentType, options, LibraryGUIStyle.popUpFieldSmallText, GUILayout.Width(width));
            GUI.enabled = true;
            if (!skipGroup) GUILayout.EndHorizontal();

            if (currentType == oldType)
                return;

            actionOnValueChange.Invoke(currentType);
        }

        public static void CreateMinMaxSlider(string label, ref float minValue, ref float maxValue, float minLimit, float maxLimit, float labelWidth, float sliderWidth = -1, bool skipGroup = false)
        {
            if (!skipGroup) GUILayout.BeginHorizontal(GetBackgroundStyle());
            string startValue = Mathf.Round(minValue * 100).ToString();
            string endValue = Mathf.Round(maxValue * 100).ToString();
            GUILayout.Label($"{label} {startValue}", LibraryGUIStyle.textDefault, GUILayout.Width(labelWidth));
            if(sliderWidth == -1)
                EditorGUILayout.MinMaxSlider(ref minValue, ref maxValue, minLimit, maxLimit);
            else
                EditorGUILayout.MinMaxSlider(ref minValue, ref maxValue, minLimit, maxLimit, GUILayout.Width(sliderWidth));
            GUILayout.Label(endValue, LibraryGUIStyle.textDefault, GUILayout.Width(30));
            if (!skipGroup) GUILayout.EndHorizontal();
        }

        public static void CreateErrorWarningMessageIcon(GUIContent guiContent)
        {
            GUILayout.Label(guiContent, LibraryGUIStyle.infoIcon);
        }

        public static void CreateInfoMessageIcon(GUIContent guiContent)
        {
            if(GlobalSettings.GetInfoIconsVisibility())
                GUILayout.Label(guiContent, LibraryGUIStyle.infoIcon);
        }

        public static void CreateLine()
        {
            GUILayout.Box("", LibraryGUIStyle.lineGrey, GUILayout.ExpandWidth(true));
        }

        public static void CreateYellowLine()
        {
            GUILayout.Box("", LibraryGUIStyle.lineYellow, GUILayout.ExpandWidth(true));
        }

        public static void CreateBlackLine()
        {
            GUILayout.Box("", LibraryGUIStyle.lineBlack, GUILayout.ExpandWidth(true));
        }

        public static void CreateWhiteLine()
        {
            GUILayout.Box("", LibraryGUIStyle.lineWhite, GUILayout.ExpandWidth(true));
        }

        public static void CreateSeparator()
        {
            GUILayout.Box("", LibraryGUIStyle.separatorWhite);
        }

        public static void CreateEmpty(float width)
        {
            GUILayout.Label(LibraryTexture.empty, GUILayout.Width(width));
        }

        public static GUIStyle GetBackgroundStyle(bool keepOld = false)
        {
            if(!keepOld)
                backgroundStyleCounter++;

            if (backgroundStyleCounter % 2 == 0)
                return LibraryGUIStyle.backgroundItem1;
            else
                return LibraryGUIStyle.backgroundItem2;
        }

        public static void ResetGetBackgroundStyleId()
        {
            backgroundStyleCounter = 1;
        }
    }
}
