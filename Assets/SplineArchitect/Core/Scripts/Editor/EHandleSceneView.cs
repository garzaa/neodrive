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
        public static bool mouseInsideSceneView { get; private set; } = false;
        private static SceneView activeSceneView = null;
        private static Rect sceneDrawingArea = new Rect();

        public static void BeforeSceneGUIGlobal(Event e)
        {
            if(e.type == EventType.Repaint)
            {
                bool mouseInSceneView = sceneDrawingArea.Contains(e.mousePosition);
                mouseInsideSceneView = mouseInSceneView;
            }

            if (e.type == EventType.MouseEnterWindow)
            {
                EHandleTool.UpdateOrientationForPositionTool();
            }
        }

        public static void TryUpdate(SceneView sceneView)
        {
            Vector2 mousePos = Event.current.mousePosition;
            float toolbarHeight = EditorStyles.toolbar.fixedHeight;
            sceneDrawingArea.Set(0, toolbarHeight, sceneView.position.width, sceneView.position.height - toolbarHeight);
            mousePos.y = sceneView.position.height - mousePos.y;

            if (sceneDrawingArea.Contains(mousePos))
                activeSceneView = sceneView;
        }

        public static bool IsValid(SceneView sceneView)
        {
            if (activeSceneView == sceneView)
                return true;

            return false;
        }

        public static SceneView GetSceneView()
        {
            if (activeSceneView)
                return activeSceneView;
            else if (SceneView.currentDrawingSceneView)
                return SceneView.currentDrawingSceneView;
            else
                return SceneView.lastActiveSceneView;
        }
    }
}
