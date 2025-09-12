// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: GlobalSettings.cs
//
// Author: Mikael Danielsson
// Date Created: 23-04-2023
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using SplineArchitect.Objects;
using UnityEditor;
using UnityEngine;
using static SplineArchitect.GlobalSettings;

namespace SplineArchitect
{
    public static class GlobalSettings
    {
        //Containers
        private static Vector2 generalWindowPosition;

        //Cached data
        private static Color gridColor;
        private static bool gridOccluded;
        private static bool drawGridDistanceLabels;
        private static bool showNormals;
        private static SplineHideMode splineHideMode;
        private static bool controlPanelWindowMinimized;
        private static bool extendedWindowMinimized;
        private static bool windowHorizontalOrder;
        private static float gridSize;
        private static bool gridVisibility;

        //General
        private static bool gridColorInit;
        private static bool gridOccludedInit;
        private static bool drawGridDistanceLabelsInit;
        private static bool showNormalsInit;
        private static bool splineHideModeInit;
        private static bool controlPanelWindowMinimizedInit;
        private static bool extendedWindowMinimizedInit;
        private static bool windowHorizontalOrderInit;
        private static bool gridSizeInit;
        private static bool gridVisibilityInit;

        public static HandleType GetHandleType()
        {
            return (HandleType)EditorPrefs.GetInt("SplineArchitect_handleType", 0);
        }

        public static void SetHandleType(HandleType handleType)
        {
            EditorPrefs.SetInt("SplineArchitect_handleType", (int)handleType);
        }

        public static float GetNormalsSpacing()
        {
            return EditorPrefs.GetFloat("SplineArchitect_normalsSpacing", 50);
        }

        public static float GetSplineViewDistance()
        {
            return EditorPrefs.GetFloat("SplineArchitect_splineViewDistance", 500);
        }

        public static float GetNormalsLength()
        {
            return EditorPrefs.GetFloat("SplineArchitect_normalsLength", 1);
        }

        public static float GetSplineLineResolution()
        {
            return EditorPrefs.GetFloat("SplineArchitect_splineLineResolution", 100);
        }

        public static bool GetControlPanelWindowMinimized()
        {
            if (!controlPanelWindowMinimizedInit)
            {
                controlPanelWindowMinimizedInit = true;
                controlPanelWindowMinimized = EditorPrefs.GetBool("SplineArchitect_controlPanelWindowMinimized", false);
            }

            return controlPanelWindowMinimized;
        }

        public static void SetControlPanelWindowMinimized(bool value)
        {
            EditorPrefs.SetBool("SplineArchitect_controlPanelWindowMinimized", value);
            controlPanelWindowMinimized = value;
        }

        public static void SetWindowHorizontalOrder(bool value)
        {
            EditorPrefs.SetBool("SplineArchitect_windowHorizontalOrder", value);
            windowHorizontalOrder = value;
        }

        public static bool GetWindowHorizontalOrder()
        {
            if (!windowHorizontalOrderInit)
            {
                windowHorizontalOrderInit = true;
                windowHorizontalOrder = EditorPrefs.GetBool("SplineArchitect_windowHorizontalOrder", true);
            }

            return windowHorizontalOrder;
        }

        public static bool GetExtendedWindowMinimized()
        {
            if (!extendedWindowMinimizedInit)
            {
                extendedWindowMinimizedInit = true;
                extendedWindowMinimized = EditorPrefs.GetBool("SplineArchitect_extendedWindowMinimized", false);
            }

            return extendedWindowMinimized;
        }

        public static void SetExtendedWindowMinimized(bool value)
        {
            EditorPrefs.SetBool("SplineArchitect_extendedWindowMinimized", value);
            extendedWindowMinimized = value;
        }

        public static bool GetDrawGridDistanceLabels()
        {
            if (!drawGridDistanceLabelsInit)
            {
                drawGridDistanceLabelsInit = true;
                drawGridDistanceLabels = EditorPrefs.GetBool("SplineArchitect_drawGridDistanceLabels", false);
            }

            return drawGridDistanceLabels;
        }

        public static void SetDrawGridDistanceLabels(bool value)
        {
            EditorPrefs.SetBool("SplineArchitect_drawGridDistanceLabels", value);
            drawGridDistanceLabels = value;
        }

        public static bool GetGridOccluded()
        {
            if(!gridOccludedInit)
            {
                gridOccludedInit = true;
                gridOccluded = EditorPrefs.GetBool("SplineArchitect_gridOccluded", true);
            }

            return gridOccluded;
        }

        public static void SetGridOccluded(bool value)
        {
            EditorPrefs.SetBool("SplineArchitect_gridOccluded", value);
            gridOccluded = value;
        }

        public static SplineHideMode GetSplineHideMode()
        {
            if(!splineHideModeInit)
            {
                splineHideModeInit = true;
                splineHideMode = (SplineHideMode)EditorPrefs.GetInt("SplineArchitect_splineHiddenMode", 0);
            }

            return splineHideMode;
        }

        public static void SetSplineHideMode(SplineHideMode value)
        {
            EditorPrefs.SetInt("SplineArchitect_splineHiddenMode", (int)value);
            splineHideMode = value;
        }

        public static float GetGridSize()
        {
            if (!gridSizeInit)
            {
                gridSizeInit = true;
                gridSize = EditorPrefs.GetFloat("SplineArchitect_gridSize", 1);
            }

            return gridSize;
        }

        public static void SetGridSize(float value)
        {
            if (value < 0.05f)
                value = 0.05f;

            EditorPrefs.SetFloat("SplineArchitect_gridSize", value);
            gridSize = value;
        }

        public static Color GetGridColor()
        {
            if(!gridColorInit)
            {
                gridColorInit = true;
                gridColor = new Color(EditorPrefs.GetFloat("SplineArchitect_gridColorR", 1),
                                      EditorPrefs.GetFloat("SplineArchitect_gridColorG", 1),
                                      EditorPrefs.GetFloat("SplineArchitect_gridColorB", 1),
                                      1);
            }
    
            return gridColor;
        }

        public static void SetGridColor(Color value)
        {
            EditorPrefs.SetFloat("SplineArchitect_gridColorR", value.r);
            EditorPrefs.SetFloat("SplineArchitect_gridColorG", value.g);
            EditorPrefs.SetFloat("SplineArchitect_gridColorB", value.b);
            gridColor = new Color(value.r, value.g, value.b, 1);
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
            if (!gridVisibilityInit)
            {
                gridVisibilityInit = true;
                gridVisibility = EditorPrefs.GetBool("SplineArchitect_gridVisibility", false);
            }

            return gridVisibility;
        }

        public static void SetGridVisibility(bool value)
        {
            EditorPrefs.SetBool("SplineArchitect_gridVisibility", value);
            gridVisibility = value;
        }

        public static Vector2 GetGeneralWindowPosition()
        {
            generalWindowPosition.x = EditorPrefs.GetFloat("SplineArchitect_generalWindowPosX", -999);
            generalWindowPosition.y = EditorPrefs.GetFloat("SplineArchitect_generalWindowPosY", -999);

            return generalWindowPosition;
        }

        public static void SetSplineViewDistance(float value)
        {
            EditorPrefs.SetFloat("SplineArchitect_splineViewDistance", value);
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

        public static bool GetShowNormals()
        {
            if (!showNormalsInit)
            {
                showNormalsInit = true;
                showNormals = EditorPrefs.GetBool("SplineArchitect_showNormals", false);
            }

            return showNormals;
        }

        public static void SetShowNormals(bool value)
        {
            showNormals = value;
            EditorPrefs.SetBool("SplineArchitect_showNormals", value);
        }
    }
}
