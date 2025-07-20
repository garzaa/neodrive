// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: GlobalSettings.cs
//
// Author: Mikael Danielsson
// Date Created: 23-04-2023
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using UnityEditor;
using UnityEngine;

namespace SplineArchitect
{
    public static class GlobalSettings
    {
        public enum SplineHideMode
        {
            NONE,
            SELECTED,
            SELECTED_OCCLUDED
        }

        //Containers
        private static Vector2 generalWindowPosition;

        public static float GetSnapIncrement(bool forceNoShift = false)
        {
            float snapIncrement = EditorPrefs.GetFloat("SplineArchitect_snapIncrement", 0.25f);
            if (!forceNoShift && Event.current != null && Event.current.shift && Event.current.alt)
                return snapIncrement * 10;

            return snapIncrement;
        }

        public static int GetSegementMovementType()
        {
            return EditorPrefs.GetInt("SplineArchitect_segementMovementType", 0);
        }

        public static float GetNormalsSpacing()
        {
            return EditorPrefs.GetFloat("SplineArchitect_normalsSpacing", 50);
        }

        public static float GetNormalsLength()
        {
            return EditorPrefs.GetFloat("SplineArchitect_normalsLength", 1);
        }

        public static float GetSplineLineResolution()
        {
            return EditorPrefs.GetFloat("SplineArchitect_splineLineResolution", 100);
        }

        public static float GetGridSize()
        {
            return EditorPrefs.GetFloat("SplineArchitect_gridSize", 1);
        }

        public static int GetGridColorType()
        {
            return EditorPrefs.GetInt("SplineArchitect_gridColorType", 1);
        }

        public static float GetControlPointSize()
        {
            return EditorPrefs.GetFloat("SplineArchitect_controlPointSize", 0.75f);
        }

        public static bool GetInfoIconsVisibility()
        {
            return EditorPrefs.GetBool("SplineArchitect_infoMessages", true);
        }

        public static bool GetGridVisibility()
        {
            return EditorPrefs.GetBool("SplineArchitect_gridVisibility", false);
        }

        public static Vector2 GetGeneralWindowPosition()
        {
            generalWindowPosition.x = EditorPrefs.GetFloat("SplineArchitect_generalWindowPosX", -999);
            generalWindowPosition.y = EditorPrefs.GetFloat("SplineArchitect_generalWindowPosY", -999);

            return generalWindowPosition;
        }

        public static SplineHideMode GetSplineHideMode()
        {
            return (SplineHideMode)EditorPrefs.GetInt("SplineArchitect_splineHiddenMode", 0);
        }

        public static void SetSplineHideMode(SplineHideMode value)
        {
            EditorPrefs.SetInt("SplineArchitect_splineHiddenMode", (int)value);
        }

        public static void SetSnapIncrement(float value)
        {
            EditorPrefs.SetFloat("SplineArchitect_snapIncrement", value);
        }

        public static void SetSnapIncrement(string value)
        {
            if (float.TryParse(value, out float result))
            {
                SetSnapIncrement(result);
            }
        }

        public static void SetGeneralWindowPosition(float x, float y)
        {
            EditorPrefs.SetFloat("SplineArchitect_generalWindowPosX", x);
            EditorPrefs.SetFloat("SplineArchitect_generalWindowPosY", y);
        }

        public static void SetGeneralAtWindowBottom(bool value)
        {
            EditorPrefs.SetBool("SplineArchitect_generalAtWindowBottom", value);
        }

        public static void SetGridVisibility(bool value)
        {
            EditorPrefs.SetBool("SplineArchitect_gridVisibility", value);
        }

        public static void SetShowNormals(bool value)
        {
            EditorPrefs.SetBool("SplineArchitect_showNormals", value);
        }

        public static void SetNormalsSpacing(float value)
        {
            value = Mathf.Round(value);
            EditorPrefs.SetFloat("SplineArchitect_normalsSpacing", value);
        }

        public static void SetNormalsLength(float value)
        {
            value = Mathf.Round(value * 100) / 100;
            if(value < 0.001f) value = 0.001f;

            EditorPrefs.SetFloat("SplineArchitect_normalsLength", value);
        }

        public static void SetSplineLineResolution(float value)
        {
            if (value < 1)
                value = 1;

            value = Mathf.Round(value);

            EditorPrefs.SetFloat("SplineArchitect_splineLineResolution", value);
        }

        public static void SetSgementMovementType(EHandleSegment.SegmentMovementType movementType)
        {
            EditorPrefs.SetInt("SplineArchitect_segementMovementType", (int)movementType);
        }

        public static void SetInfoIconsVisibility(bool value)
        {
            EditorPrefs.SetBool("SplineArchitect_infoMessages", value);
        }

        public static void SetControlPointSize(float value)
        {
            if (value < 0.05f)
                value = 0.05f;

            EditorPrefs.SetFloat("SplineArchitect_controlPointSize", value);
        }

        public static void SetGridSize(float value)
        {
            if (value < 0.05f)
                value = 0.05f;

            EditorPrefs.SetFloat("SplineArchitect_gridSize", value);
        }

        public static void SetGridColorType(int value)
        {
            EditorPrefs.SetInt("SplineArchitect_gridColorType", value);
        }


        public static bool IsGeneralWindowAtBottom()
        {
            return EditorPrefs.GetBool("SplineArchitect_generalAtWindowBottom", true);
        }

        public static bool IsUiHidden()
        {
            return EditorPrefs.GetBool("SplineArchitect_uiHidden", false);
        }


        public static void ToggleUiHidden()
        {
            if (EditorPrefs.GetBool("SplineArchitect_uiHidden", false))
                EditorPrefs.SetBool("SplineArchitect_uiHidden", false);
            else
                EditorPrefs.SetBool("SplineArchitect_uiHidden", true);
        }

        public static bool ShowNormals()
        {
            return EditorPrefs.GetBool("SplineArchitect_showNormals", false);
        }
    }
}
