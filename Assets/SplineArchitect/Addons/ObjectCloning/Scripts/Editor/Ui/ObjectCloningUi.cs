// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: ObjectCloningUi.cs
//
// Author: Mikael Danielsson
// Date Created: 20-04-2025
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using UnityEngine;

using SplineArchitect.Libraries;
using SplineArchitect.Objects;
using SplineArchitect.Utility;

namespace SplineArchitect.Ui
{
    public class ObjectCloningUi
    {
        private static string[] cloneDirectionOptions = new string[] { "Forward", "Backward" };

        public static Rect CalcSplineObjectWindowSize(Spline spline, SplineObject so)
        {
            Rect rect = new Rect();
            rect.height = WindowBase.menuItemHeight * 8 + 11;

            rect.width = 255;

            return rect;
        }

        public static void DrawSplineObjectWindow(Spline spline, SplineObject so)
        {
            GUILayout.BeginHorizontal(LibraryGUIStyle.backgroundSubHeader);
            EUiUtility.CreateLabelField("<b>OBJECT CLONING</b>", LibraryGUIStyle.textSubHeader, true);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal(EUiUtility.GetBackgroundStyle());
            EUiUtility.CreateXYZInputFields("Offset", so.cloneOffset, (offset, dif) =>
            {
                EHandleSelection.UpdatedSelectedSplineObjectsRecordUndo((selected) =>
                {
                    selected.cloneOffset = offset;
                }, "Updated clone offset");

                EHandleSelection.UpdatedSelectedSplineObjects((selected) =>
                {
                    EHandleObjectCloning.UpdateCloneAmount(selected);
                });
            }, 55, 10, 62);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal(EUiUtility.GetBackgroundStyle());
            EUiUtility.CreateToggleField("", so.cloneUseFixedAmount, (value) =>
            {
                EHandleSelection.UpdatedSelectedSplineObjectsRecordUndo((selected) =>
                {
                    selected.cloneUseFixedAmount = value;
                    if (!value && selected.cloningEnabled) EHandleObjectCloning.UpdateCloneAmount(so);
                }, "Change fixed amount");

                EHandleSelection.UpdatedSelectedSplineObjects((selected) =>
                {
                    EHandleObjectCloning.UpdateCloneEndSnapping(selected);
                });
            }, true, true);

            if (!so.cloneUseFixedAmount)
            {
                EUiUtility.CreateSliderAndInputField("Amount:", so.cloneAmount, (newValue) => { }, 0, 100, 95, 38, 0, true, false);
            }
            else
            {
                EUiUtility.CreateSliderAndInputField("Amount:", so.cloneAmount, (newValue) =>
                {
                    EHandleSelection.UpdatedSelectedSplineObjectsRecordUndo((selected) =>
                    {
                        if (newValue < 0) newValue = 0;
                        selected.cloneAmount = (int)newValue;
                    }, "Changed clone amount");
                }, 0, 100, 95, 38, 0, true, true);
            }

            EUiUtility.CreateButton(ButtonType.DEFAULT, LibraryGUIContent.textSet, 30, 18, () =>
            {
                EHandleSelection.UpdatedSelectedSplineObjects((selected) =>
                {
                    EHandleObjectCloning.UpdateCloneAmount(selected);
                });
            }, so.type == SplineObject.Type.DEFORMATION && so.cloningEnabled);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal(EUiUtility.GetBackgroundStyle());
            EUiUtility.CreateToggleField("Snap end:", so.cloneSnapEnd, (value) =>
            {
                EHandleSelection.UpdatedSelectedSplineObjectsRecordUndo((selected) =>
                {
                    selected.cloneSnapEnd = value;
                }, "Change snap end");

                EHandleSelection.UpdatedSelectedSplineObjects((selected) =>
                {
                    EHandleObjectCloning.UpdateCloneAmount(selected);
                    EHandleObjectCloning.UpdateCloneEndSnapping(selected);
                });
            }, !so.cloneUseFixedAmount, true);

            EUiUtility.CreateFloatFieldWithLabel("Snap offset:", so.cloneSnapEndOffset, (value) =>
            {
                EHandleSelection.UpdatedSelectedSplineObjectsRecordUndo((selected) =>
                {
                    selected.cloneSnapEndOffset = value;
                }, "Set clone snap offset");

                EHandleSelection.UpdatedSelectedSplineObjects((selected) =>
                {
                    EHandleObjectCloning.UpdateCloneEndSnapping(selected);
                });
            }, 70, 78, true, so.cloneSnapEnd);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal(EUiUtility.GetBackgroundStyle());
            GUILayout.FlexibleSpace();
            EUiUtility.CreatePopupField("Direction:", 69, (int)so.cloneDirection, cloneDirectionOptions, (int newValue) =>
            {
                SplineObject.CloneDirection direction = (SplineObject.CloneDirection)newValue;

                EHandleSelection.UpdatedSelectedSplineObjectsRecordUndo((selected) =>
                {
                    selected.cloneDirection = direction;
                }, "Set clone direction", false, EHandleUndo.RecordType.REGISTER_COMPLETE_OBJECT);

                EHandleSelection.UpdatedSelectedSplineObjects((selected) =>
                {
                    EHandleObjectCloning.ToggleCloneDirection(selected);
                });
            }, -1, true);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal(LibraryGUIStyle.backgroundMainLayerButtons);

            GUILayout.FlexibleSpace();
            if (so.cloningEnabled || (so.clones != null && so.clones.Count > 0))
            { 
                EUiUtility.CreateButton(ButtonType.DEFAULT, LibraryGUIContent.textDeleteClones, 92, 18, () =>
                {
                    EHandleSelection.UpdatedSelectedSplineObjects((selected) =>
                    {
                        EHandleObjectCloning.Disable(selected);
                    });
                });
            }
            else
            {
                if(so.transform.childCount == 0)
                    EUiUtility.CreateInfoMessageIcon(LibraryGUIContent.warningMsgCantCloneWithNoChildren);
                else if(so.type != SplineObject.Type.DEFORMATION)
                    EUiUtility.CreateInfoMessageIcon(LibraryGUIContent.warningMsgCanOnlyCloneWithTypeDeformation);

                EUiUtility.CreateButton(ButtonType.DEFAULT, LibraryGUIContent.textCloneChildren, 92, 18, () =>
                {
                    EHandleSelection.UpdatedSelectedSplineObjects((selected) =>
                    {
                        EHandleObjectCloning.Enable(selected.splineParent, selected);
                    });
                }, so.transform.childCount > 0 && so.type == SplineObject.Type.DEFORMATION);
            }

            GUILayout.EndHorizontal();
        }
    }
}
