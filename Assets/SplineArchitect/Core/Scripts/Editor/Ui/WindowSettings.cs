// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: WindowSettings.cs
//
// Author: Mikael Danielsson
// Date Created: 05-08-2025
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using SplineArchitect.Libraries;
using SplineArchitect.Objects;
using SplineArchitect.Utility;
using UnityEditor;
using UnityEngine;

namespace SplineArchitect.Ui
{
    public class WindowSettings : WindowBase
    {
        protected override void OnGUIExtended()
        {
            EUiUtility.ResetGetBackgroundStyleId();

            GUILayout.BeginHorizontal(LibraryGUIStyle.backgroundHeader);
            EUiUtility.CreateLabelField("<b>Settings</b>", LibraryGUIStyle.textHeaderBlack, true);

            EUiUtility.CreateButton(ButtonType.DEFAULT, LibraryGUIContent.iconClose, 19, 14, () =>
            {
                EActionToSceneGUI.Add(() =>
                {
                    toolbarToggleBase.SetValueWithoutNotify(false);
                    Close();
                }, EActionToSceneGUI.Type.LATE, EventType.Repaint);
                EHandleSceneView.RepaintCurrent();
            });
            GUILayout.EndHorizontal();

            //Spline resolution
            EUiUtility.CreateSliderAndInputField("Spline line resolution:", GlobalSettings.GetSplineLineResolution(), (float newValue) =>
            {
                GlobalSettings.SetSplineLineResolution(newValue);
                EHandleSceneView.RepaintCurrent();
            }, 1, 200, 125, 50);

            //Spline view distance
            EUiUtility.CreateSliderAndInputField("Spline view distance:", GlobalSettings.GetSplineViewDistance(), (float newValue) =>
            {
                GlobalSettings.SetSplineViewDistance(newValue);
                EHandleSceneView.RepaintCurrent();
            }, 100, 2500, 125, 50);

            //Normals spacing
            EUiUtility.CreateSliderAndInputField("Normals spacing:", GlobalSettings.GetNormalsSpacing(), (float newValue) =>
            {
                GlobalSettings.SetNormalsSpacing(newValue);
                EHandleSceneView.RepaintCurrent();
            }, 1, 200, 125, 50);

            //Normals length
            EUiUtility.CreateSliderAndInputField("Normals length:", GlobalSettings.GetNormalsLength(), (float newValue) =>
            {
                GlobalSettings.SetNormalsLength(newValue);
                EHandleSceneView.RepaintCurrent();
            }, 0.001f, 2, 125, 50);

            //Control point Size
            EUiUtility.CreateSliderAndInputField("Control point size:", GlobalSettings.GetControlPointSize(), (float newValue) =>
            {
                GlobalSettings.SetControlPointSize(newValue);
                EHandleSceneView.RepaintCurrent();
            }, 0.05f, 2.5f, 125, 50);

            GUILayout.BeginHorizontal(EUiUtility.GetBackgroundStyle());
            EUiUtility.CreateToggleField("Enable info icons:", GlobalSettings.GetInfoIconsVisibility(), (newValue) =>
            {
                GlobalSettings.SetInfoIconsVisibility(newValue);
            }, true, true);

            EUiUtility.CreateToggleField("Horizontal layout:", GlobalSettings.GetWindowHorizontalOrder(), (newValue) =>
            {
                GlobalSettings.SetWindowHorizontalOrder(newValue);
            }, true, true);
            GUILayout.EndHorizontal();
        }

        protected override void UpdateWindowSize()
        {
            cachedRect.size = new Vector2(327, 22 * 6 + 20);
        }
    }
}
