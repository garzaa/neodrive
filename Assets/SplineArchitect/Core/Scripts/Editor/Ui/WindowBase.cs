// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: WindowBase.cs
//
// Author: Mikael Danielsson
// Date Created: 05-08-2025
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.Reflection;

using UnityEditor;
using UnityEngine;

using SplineArchitect.Utility;
using SplineArchitect.Objects;

namespace SplineArchitect.Ui
{
    public abstract class WindowBase : EditorWindow
    {
        public const float menuItemHeight = 22;

        public static List<WindowBase> instances = new List<WindowBase>();

        public ToolbarToggleBase toolbarToggleBase;
        public ToolbarDropdownToggleGrid toolbarDropdownToggleGrid;
        public WindowBase extendedWindow;
        public Rect cachedRect = new Rect(0, 0, 0, 0);

        private bool skipFirstOnGUI = true;

        public static void CloseAll()
        {
            for (int i = instances.Count - 1; i >= 0; i--)
            {
                WindowBase item = instances[i];

                if (item == null)
                    continue;

                item.SetToolbarButtonValue(false);
                item.Close();
            }
        }

        public void SetToolbarButtonValue(bool value)
        {
            if (toolbarToggleBase != null)
                toolbarToggleBase.SetValueWithoutNotify(value);
        }

        public static void RepaintAll()
        {
            foreach (WindowBase item in instances)
            {
                if (item != null)
                {
                    item.Repaint();
                }

                EActionDelayed.Add(() =>
                {
                    foreach (WindowBase wb in WindowBase.instances)
                    {
                        WindowSpline sp = wb as WindowSpline;

                        if (sp != null)
                            sp.Repaint();
                    }
                }, 0, 0, EActionDelayed.Type.FRAMES);
            }
        }

        private void OnEnable()
        {
            instances.Add(this);
            minSize = new Vector2(22, 22);

            System.Type hostViewType = typeof(Editor).Assembly.GetType("UnityEditor.HostView");
            if (hostViewType == null)
            {
                Debug.LogWarning("[Spline Architect] UnityEditor.HostView not found. Internal Unity API may have changed.");
                return;
            }

            FieldInfo fieldInfo = hostViewType.GetField("k_DockedMinSize", BindingFlags.Static | BindingFlags.NonPublic);
            if (fieldInfo == null)
            {
                Debug.LogWarning("[Spline Architect] UnityEditor.HostView.k_DockedMinSize not found. Unity may have changed internal APIs.");
                return;
            }
            fieldInfo.SetValue(null, new Vector2(22, 22));
        }

        private void OnDestroy()
        {
            instances.Remove(this);
        }

        private void OnGUI()
        {
            if (!EHandleUi.initialized)
                return;

            Event e = Event.current;
            if (e.type == EventType.Repaint)
            {
                //Need to skip first SceneGUI and set the size of the window else during the first frame the window will look weird.
                if (skipFirstOnGUI)
                {
                    skipFirstOnGUI = false;

                    UpdateWindowSize();
                    position = cachedRect;
                    HandleExtendedWindow();
                    return;
                }
            }

            GUILayout.BeginHorizontal();
            
            EUiUtility.CreateVerticalGreyLine();

            GUILayout.BeginVertical();
            EUiUtility.CreateHorizontalGreyLine();
            OnGUIExtended();
            EUiUtility.CreateHorizontalGreyLine();
            GUILayout.EndVertical();

            EUiUtility.CreateVerticalGreyLine();

            GUILayout.EndHorizontal();

            UpdateWindowSize();
            HandleExtendedWindow();
            if (!GeneralUtility.IsEqual(position, cachedRect))
            {
                position = cachedRect;
                Repaint();

                if (extendedWindow != null)
                {
#if UNITY_EDITOR_WIN
                    //Doing this in mac os will result in the extended window not showing up. Seems you can only set the windows position inside its own OnGUI.
                    extendedWindow.position = extendedWindow.cachedRect;
#endif
                    extendedWindow.Repaint();
                }
            }
        }

        protected abstract void OnGUIExtended();

        public void UpdateChacedPosition(Vector2 screenPos)
        {
            cachedRect.x = screenPos.x;
            cachedRect.y = screenPos.y;
        }

        public void OpenWindow(Vector2 position, bool focused)
        {
            OpenWindow(focused);
            UpdateChacedPosition(position);
        }

        public void OpenWindow(bool focused)
        {
            EditorWindow lastFocus = EditorWindow.focusedWindow;
            ShowPopup();
            if(lastFocus != null && !focused)
            {
                lastFocus.Focus();
            }
            else if(focused)
            {
                Focus();
            }

        }

        protected abstract void UpdateWindowSize();

        protected virtual void HandleExtendedWindow(){}
    }
}
