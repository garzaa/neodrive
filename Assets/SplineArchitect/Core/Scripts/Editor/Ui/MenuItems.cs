// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: MenuItems.cs
//
// Author: Mikael Danielsson
// Date Created: 31-01-2023
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using UnityEngine;
using UnityEditor;

using SplineArchitect.Objects;

namespace SplineArchitect.Ui
{
    public static class MenuItems
    {
        [MenuItem("Tools/Spline Architect/Toggle menus", false, 0)]
        public static void ToggleMenus()
        {
            GlobalSettings.ToggleUiHidden();
            SceneView.RepaintAll();
        }

        [MenuItem("GameObject/Spline Architect/Spline", false, 100)]
        public static void CreateSpline()
        {
            GameObject go = new GameObject("Spline");
            EHandleUndo.RegisterCreatedObject(go, "Created spline");

            EHandleUndo.AddComponent<Spline>(go);
            Transform selected = Selection.activeTransform;

            if (selected != null)
                EHandleUndo.SetTransformParent(go.transform, selected, "Created Spline");

            Selection.activeTransform = go.transform;
        }

        [MenuItem("GameObject/Spline Architect/Spline Connector", false, 101)]
        public static void CreateSplineConnector()
        {
            GameObject go = new GameObject("SplineConnector");
            EHandleUndo.RegisterCreatedObject(go, "Created spline connector");

            EHandleUndo.AddComponent<SplineConnector>(go);
            Transform selected = Selection.activeTransform;

            if (selected != null)
                EHandleUndo.SetTransformParent(go.transform, selected, "Created spline connector");

            Selection.activeTransform = go.transform;
        }
    }
}
