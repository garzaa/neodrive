// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: ObjectCloningUi.cs
//
// Author: Mikael Danielsson
// Date Created: 20-04-2025
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using SplineArchitect.Libraries;
using SplineArchitect.Objects;
using UnityEngine;

namespace SplineArchitect.Ui
{
    public class ObjectCloningUi
    {
        private static string[] cloneDirectionOptions = new string[] { "Forward", "Backward" };
        private static string cloneAmount = "";
        private static string containerXClone = "0";
        private static string containerYClone = "0";
        private static string containerZClone = "0";


        public static Rect CalcSplineObjectWindowSize(Spline spline, SplineObject so)
        {
            Rect rect = new Rect();
            rect.height = LibraryGUIStyle.menuItemHeight * 6 + 20;
            return rect;
        }

        public static void DrawSplineObjectWindow(Spline spline, SplineObject so)
        {
            GUILayout.BeginHorizontal(LibraryGUIStyle.backgroundSubHeader);
            EHandleUi.CreateLabelField("<b>OBJECT CLONING</b>", LibraryGUIStyle.textSubHeader, true);
            GUILayout.EndHorizontal();

            EHandleUi.CreateXYZInputFields("Offset", so.cloneOffset, ref containerXClone, ref containerYClone, ref containerZClone, (offset, dif) =>
            {
                EHandleSelection.UpdatedSelectedSplineObjectsRecordUndo((selected) =>
                {
                    selected.cloneOffset = offset;
                }, "Updated rotation");
            }, 50);

            GUILayout.BeginHorizontal(EHandleUi.GetBackgroundStyle());
            EHandleUi.CreateToggleField("", so.cloneUseFixedAmount, (value) =>
            {
                EHandleSelection.UpdatedSelectedSplineObjectsRecordUndo((selected) =>
                {
                    selected.cloneUseFixedAmount = value;
                }, "Change fixed amount");
            }, true, true);

            EHandleUi.CreateSliderAndInputField("Amount: ", so.cloneMenuAmount, ref cloneAmount, (newValue) =>
            {
                EHandleSelection.UpdatedSelectedSplineObjectsRecordUndo((selected) =>
                {
                    if (newValue < 0) newValue = 0;
                    selected.cloneMenuAmount = (int)newValue;
                }, "Changed clone amount");
            }, 0, 100, 100, 38, 0, true, so.cloneUseFixedAmount);

            //Set
            EHandleUi.CreateButton(EHandleUi.ButtonType.DEFAULT, LibraryGUIContent.textSet, 38, 18, () =>
            {
                EHandleSelection.UpdatedSelectedSplineObjectsRecordUndo((selected) =>
                {
                    selected.cloneAmount = selected.cloneMenuAmount;
                }, "Set clone amount");
            }, so.cloneUseFixedAmount);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal(EHandleUi.GetBackgroundStyle());
            EHandleUi.CreatePopupField("Direction: ", 100, (int)so.cloneDirection, cloneDirectionOptions, (int newValue) =>
            {
                SplineObject.CloneDirection direction = (SplineObject.CloneDirection)newValue;

                EHandleSelection.UpdatedSelectedSplineObjectsRecordUndo((selected) =>
                {
                    selected.cloneDirection = direction;
                }, "Set clone direction");

            }, -1, true);

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal(LibraryGUIStyle.backgroundMainLayerButtons);
            if (so.clones == null)
            {
                GUILayout.FlexibleSpace();
                EHandleUi.CreateButton(EHandleUi.ButtonType.DEFAULT, LibraryGUIContent.textClone, 42, 18, () =>
                {
                    EHandleObjectCloning.CloneSelection(spline, so);
                });

                if(so.transform.childCount > 0 && so.type == SplineObject.Type.DEFORMATION)
                {
                    EHandleUi.CreateButton(EHandleUi.ButtonType.DEFAULT, LibraryGUIContent.textCloneChildren, 90, 18, () =>
                    {
                        EHandleSelection.UpdatedSelectedSplineObjectsRecordUndo((selected) =>
                        {
                            selected.isClone = false;
                            EHandleObjectCloning.CloneChildren(spline, selected);
                        }, "Created clones");
                    });
                }
            }
            else
            {
                GUILayout.FlexibleSpace();
                EHandleUi.CreateButton(EHandleUi.ButtonType.DEFAULT, LibraryGUIContent.textDeleteClones, 90, 18, () =>
                {
                    EHandleSelection.UpdatedSelectedSplineObjectsRecordUndo((selected) =>
                    {
                        selected.isCloneHead = false;
                    }, "Deleted clones");
                });
            }
            GUILayout.EndHorizontal();
        }
    }
}
