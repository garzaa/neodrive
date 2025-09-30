// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: EHandleSceneView.cs
//
// Author: Mikael Danielsson
// Date Created: 18-10-2023
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using UnityEngine;
using UnityEditor;

namespace SplineArchitect
{
    public class EHandleSceneView
    {
        public static bool mouseInsideSceneView { get; private set; }
        public static bool mouseDragEnabled { get; private set; }

        public static void BeforeSceneGUIGlobal(SceneView sceneView, Event e)
        {
            if(e.type == EventType.Repaint)
            {
                float toolbarHeight = EditorStyles.toolbar.fixedHeight;
            }

            if (e.type == EventType.MouseEnterWindow)
            {
                mouseInsideSceneView = true;
                EHandleTool.UpdateOrientationForPositionTool();
            }

            if (e.type == EventType.MouseLeaveWindow)
            {
                mouseInsideSceneView = false;
            }

            if (e.type == EventType.MouseDrag)
            {
                mouseDragEnabled = true;
            }

            if (e.type == EventType.MouseUp)
            {
                mouseDragEnabled = false;
            }
        }

        public static bool IsValid(SceneView sceneView)
        {
            if (SceneView.lastActiveSceneView == sceneView)
                return true;

            return false;
        }

        public static void RepaintCurrent()
        {
            GetCurrent().Repaint();
        }

        public static Camera GetCamera()
        {
            return GetCurrent().camera;
        }

        public static SceneView GetCurrent()
        {
            return SceneView.lastActiveSceneView;
        }
    }
}
