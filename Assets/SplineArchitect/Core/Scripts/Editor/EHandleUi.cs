// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: EHandleUi.cs
//
// Author: Mikael Danielsson
// Date Created: 16-02-2023
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System;

using UnityEditor.ShortcutManagement;
using UnityEditor;
using UnityEngine;

using SplineArchitect.CustomTools;
using SplineArchitect.Libraries;
using SplineArchitect.Objects;
using SplineArchitect.Ui;
using SplineArchitect.Utility;

namespace SplineArchitect
{
    public class EHandleUi
    {
        public static bool initialized { get; private set; }
        private static int frameCounter;

        //Options
        public static string[] optionsEasing;
        public static string[] optionsNoiseType;
        public static string[] optionsNoiseGroups = new string[] { "A", "B", "C", "D", "E", "F", "G", "H" };
        public static string[] optionsNoiseGroupsAndNone = new string[] { "None", "A", "B", "C", "D", "E", "F", "G", "H" };
        public static string[] optionsSpace = new string[] { "World", "Local" };

        public static void Init()
        {
            if (!initialized)
            {
                initialized = true;

                LibraryTexture.Init();
                LibraryGUIContent.Init();

                EActionToSceneGUI.Add(() => 
                {
                    LibraryGUIStyle.Init();
                    CreateEasingList();
                    CreateNoiseTypeList();
                }, EActionToSceneGUI.Type.LATE, EventType.Layout);

                int preloadedTextures = LibraryTexture.GetPreloadedTextureCount();
                if (preloadedTextures > 0)
                    Debug.LogWarning($"{preloadedTextures} textures have been preloaded. This should only happen during the first time you import Spline Architect into a project.");
            }
        }

        public static void UpdateGlobal()
        {
            for (int i = WindowBase.instances.Count - 1; i >= 0; i--)
            {
                WindowBase item = WindowBase.instances[i];

                if (item.toolbarToggleBase != null)
                    item.toolbarToggleBase.SyncWindowPosition();
            }

            UpdateInfoWindow();
        }

        public static void OnSceneGUIGlobal(SceneView sceneView)
        {
            Event e = Event.current;

            //Update when using position tool
            if (PositionTool.activePart != PositionTool.Part.NONE)
                WindowBase.RepaintAll();

            if (EHandleSpline.controlPointCreationActive && e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
            {
                Tools.current = Tool.Move;
                e.Use();
            }
        }

        public static void BeforeAssemblyReload()
        {
            LibraryTexture.DestroyPreloadedTextures();
        }

        public static void OnEditorWantsToQuit()
        {
            CloseAllWindows();
            LibraryTexture.DestroyPreloadedTextures();
        }

        public static void OnDisposeDeformJob()
        {
            WindowBase.RepaintAll();
        }

        public static void OnShortcutBindingChanged()
        {
            foreach (ToolbarToggleBase tcp in ToolbarToggleBase.instances)
            {
                ToolbarToggleControlPanel ttcp = tcp as ToolbarToggleControlPanel;

                if (ttcp == null)
                    continue;

                tcp.tooltip = $"Control panel ({ShortcutManager.instance.GetShortcutBinding(EHandleShortcuts.hideUiId)})";
            }
        }

        public static void OnWindowFocusChanged()
        {
            if (EditorWindow.focusedWindow == null)
                return;

            for (int i = WindowBase.instances.Count - 1; i >= 0; i--)
            {
                WindowBase item = WindowBase.instances[i];

                if (item == null)
                    continue;

                if (EditorWindow.focusedWindow == item)
                    continue;

                if (EditorWindow.focusedWindow.titleContent.text == "Color")
                    continue;

                if (item.toolbarDropdownToggleGrid != null)
                {
                    if (item.toolbarDropdownToggleGrid.pointerHovering)
                        continue;

                    EActionDelayed.Add(() =>
                    {
                        item.Close();
                    }, 0, 5, EActionDelayed.Type.FRAMES);
                }
            }
        }

        public static void CloseAllWindows()
        {
            EditorWindow[] windows = Resources.FindObjectsOfTypeAll<EditorWindow>();
            if (windows.Length > 0)
            {
                for (int i = 0; i < windows.Length; i++)
                {
                    if (windows[i] == null)
                        continue;

                    WindowBase windowBase = windows[i] as WindowBase;
                    if (windowBase != null)
                    {
                        windowBase.Close();
                    }
                }
            }
        }

        public static void UpdateInfoWindow()
        {
            frameCounter++;

            if (frameCounter < 8)
                return;

            frameCounter = 0;

            if (WindowBase.instances == null || WindowBase.instances.Count != 1)
                return;

            WindowInfo windowInfo = WindowBase.instances[0] as WindowInfo;

            if (windowInfo == null)
                return;

            windowInfo.Repaint();
        }

        public static void CreateEasingList()
        {
            optionsEasing = Enum.GetNames(typeof(Easing));

            for (int y = 0; y < optionsEasing.Length; y++)
            {
                optionsEasing[y] = EConversionUtility.CapitalizeString(optionsEasing[y]);
            }
        }

        public static void CreateNoiseTypeList()
        {
            optionsNoiseType = Enum.GetNames(typeof(NoiseLayer.Type));

            for (int y = 0; y < optionsNoiseType.Length; y++)
            {
                optionsNoiseType[y] = EConversionUtility.CapitalizeString(optionsNoiseType[y]);
            }
        }
    }
}
