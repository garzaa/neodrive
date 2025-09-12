// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: WindowGrid.cs
//
// Author: Mikael Danielsson
// Date Created: 15-08-2025
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System.Collections.Generic;

using UnityEditor;
using UnityEngine;

using SplineArchitect.Libraries;
using SplineArchitect.Objects;
using SplineArchitect.Utility;

namespace SplineArchitect.Ui
{
    public class WindowGrid : WindowBase
    {
        protected override void OnGUIExtended()
        {
            GUILayout.BeginHorizontal(LibraryGUIStyle.backgroundHeader);
            EUiUtility.CreateLabelField("<b>Grid settings</b>", LibraryGUIStyle.textHeaderBlack, true);

            //Close
            EUiUtility.CreateButton(ButtonType.DEFAULT, LibraryGUIContent.iconClose, 19, 14, () =>
            {
                EActionToSceneGUI.Add(() =>
                {
                    Close();
                }, EActionToSceneGUI.Type.LATE, EventType.Repaint);
                EHandleSceneView.RepaintCurrent();
            });
            GUILayout.EndHorizontal();

            EUiUtility.CreateColorField("Color:", GlobalSettings.GetGridColor(), (Color newColor) =>
            {
                GlobalSettings.SetGridColor(newColor);
                EHandleSceneView.RepaintCurrent();
            }, 70);

            EUiUtility.CreateFloatFieldWithLabel("Size:", GlobalSettings.GetGridSize(), (newValue) =>
            {
                GlobalSettings.SetGridSize(newValue);
                EHandleSceneView.RepaintCurrent();
            }, 70, 44);

            EUiUtility.CreateToggleField("Occluded:", GlobalSettings.GetGridOccluded(), (newValue) =>
            {
                GlobalSettings.SetGridOccluded(newValue);
                EHandleSceneView.RepaintCurrent();
            });

            EUiUtility.CreateToggleField("Distance labels:", GlobalSettings.GetDrawGridDistanceLabels(), (newValue) =>
            {
                GlobalSettings.SetDrawGridDistanceLabels(newValue);
                EHandleSceneView.RepaintCurrent();
            });
        }

        protected override void UpdateWindowSize()
        {
            cachedRect.size = new Vector2(125, menuItemHeight * 4 + 20);
        }
    }
}
