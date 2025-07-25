// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: MenuGeneral.cs
//
// Author: Mikael Danielsson
// Date Created: 15-10-2023
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

using UnityEditor;
using UnityEngine;

using SplineArchitect.Libraries;
using SplineArchitect.Objects;

namespace SplineArchitect.Ui
{
    public class MenuGeneral
    {
        public enum GeneralMenu : byte
        {
            GENERAL,
            SETTINGS,
            INFO
        }

        //Settings
        public const string toolName = "Spline Architect";
        public const string versionNumber = "1.0.4";

        //General
        public static GeneralMenu generalMenu { get; private set; }
        private const float windowSpace = 3;
        private const float toolbarHeight = 25;
        private static Rect generalWindowRect = new Rect(0, 127, 137, 111);
        private static Vector2 oldGeneralWindowPos = new Vector2(-999, -999);
        private static string containerGridSize = "";
        private static string containerSnapValue = "";
        private static string containerNormalSpacing = "";
        private static string containerNormalLength = "";
        private static string containerSplineViewDistance = "";
        private static string containerControlPointSize = "";
        private static string containerSplineLineResolution = "";

        //Addons
        private static List<string> activeAddons = new List<string>{"None"};

        private static string[] segementMovementType = new string[] 
        {
            "Continuous", 
            "Mirrored",
            "Broken"
        };

        private static string[] gridColorType = new string[]
        {
            "White",
            "Grey",
            "Black"
        };

        public static void Start(SceneView sceneView)
        {
            containerSnapValue = GlobalSettings.GetSnapIncrement().ToString();
            Vector2 pos = GlobalSettings.GetGeneralWindowPosition();
            generalWindowRect.x = pos.x;
            generalWindowRect.y = pos.y;
        }

        public static void OnSceneGUI(SceneView sceneView)
        {
            //Draw window and update drag position
            UpdateRect();
            generalWindowRect = GUILayout.Window(0, generalWindowRect, windowID => DrawWindow(windowID), "", LibraryGUIStyle.empty);
            UpdatePosition(sceneView);
        }

        private static void DrawWindow(int windowID)
        {
            if (!GlobalSettings.IsUiMinimized())
            {
                EHandleUi.ResetGetBackgroundStyleId();

                GUILayout.BeginHorizontal();
                EHandleUi.CreateButton(EHandleUi.ButtonType.SUB_MENU, LibraryGUIContent.minimize, 30, 14, () => 
                {
                    ToggleUi();
                });
                GUILayout.EndHorizontal();

                //Selected border
                if (EHandleSelection.selectedSpline != null) GUILayout.Box("", LibraryGUIStyle.lineBlackThick, GUILayout.ExpandWidth(true));
                else GUILayout.Box("", LibraryGUIStyle.lineYellowThick, GUILayout.ExpandWidth(true));

                //Header
                GUILayout.BeginHorizontal(LibraryGUIStyle.backgroundHeader);
                EHandleUi.CreateLabelField($"<b>{toolName}</b>", LibraryGUIStyle.textHeaderBlack, true);
                GUILayout.Label(LibraryGUIContent.iconMove, GUILayout.Height(20), GUILayout.Width(20));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal(LibraryGUIStyle.backgroundToolbar);

                //Create Spline/Segements
                EHandleUi.CreateButtonToggle(EHandleUi.ButtonType.DEFAULT, LibraryGUIContent.iconAdd, LibraryGUIContent.iconAddActive, 35, 19, () =>
                {
                    EHandleSpline.controlPointCreationActive = !EHandleSpline.controlPointCreationActive;
                }, EHandleSpline.controlPointCreationActive);

                //Toggle hide none selected
                GlobalSettings.SplineHideMode hideMode = GlobalSettings.GetSplineHideMode();
                EHandleUi.CreateButtonToggle(EHandleUi.ButtonType.DEFAULT, LibraryGUIContent.iconHide, 
                                             hideMode == GlobalSettings.SplineHideMode.SELECTED_OCCLUDED ? LibraryGUIContent.iconHideActive2 : 
                                             LibraryGUIContent.iconHideActive, 35, 19, () =>
                {
                    int value = (int)GlobalSettings.GetSplineHideMode() + 1;
                    if (value > 2) value = 0;
                    GlobalSettings.SetSplineHideMode((GlobalSettings.SplineHideMode)value);
                }, hideMode > 0);

                //Toggle help box visibility
                EHandleUi.CreateButtonToggle(EHandleUi.ButtonType.DEFAULT, LibraryGUIContent.iconGrid, LibraryGUIContent.iconGridActive, 35, 19, () =>
                {
                    GlobalSettings.SetGridVisibility(!GlobalSettings.GetGridVisibility());
                }, GlobalSettings.GetGridVisibility());

                //Toggle normals visibility
                EHandleUi.CreateButtonToggle(EHandleUi.ButtonType.DEFAULT, LibraryGUIContent.iconNormals, LibraryGUIContent.iconNormalsActive, 35, 19, () =>
                {
                    GlobalSettings.SetShowNormals(!GlobalSettings.ShowNormals());
                }, GlobalSettings.ShowNormals());

                GUILayout.EndHorizontal();

                if(generalMenu == GeneralMenu.GENERAL)
                {
                    GUILayout.BeginHorizontal(LibraryGUIStyle.backgroundSubHeader);
                    EHandleUi.CreateLabelField("<b>GENERAL</b>", LibraryGUIStyle.textSubHeader, true);
                    GUILayout.EndHorizontal();

                    if (GlobalSettings.GetGridVisibility())
                    {
                        EHandleUi.CreatePopupField("Grid color: ", 80, GlobalSettings.GetGridColorType(), gridColorType, (int newValue) =>
                        {
                            GlobalSettings.SetGridColorType(newValue);
                        });

                        EHandleUi.CreateInputField("Grid size: ", GlobalSettings.GetGridSize(), ref containerGridSize, (newValue) =>
                        {
                            GlobalSettings.SetGridSize(newValue);
                        }, 80);
                    }

                    EHandleUi.CreateInputField("Snap length: ", GlobalSettings.GetSnapIncrement(), ref containerSnapValue, (newValue) =>
                    {
                        GlobalSettings.SetSnapIncrement(newValue);
                    }, 80);

                    EHandleUi.CreatePopupField("Handle type: ", 80, GlobalSettings.GetSegementMovementType(), segementMovementType, (int newValue) =>
                    {
                        GlobalSettings.SetSgementMovementType((EHandleSegment.SegmentMovementType)newValue);
                    });
                }
                else if (generalMenu == GeneralMenu.SETTINGS)
                {
                    GUILayout.BeginHorizontal(LibraryGUIStyle.backgroundSubHeader);
                    EHandleUi.CreateLabelField("<b>SETTINGS</b>", LibraryGUIStyle.textSubHeader, true);
                    GUILayout.EndHorizontal();

                    //Spline resolution
                    EHandleUi.CreateSliderAndInputField("Spline line resolution: ", GlobalSettings.GetSplineLineResolution(), ref containerSplineLineResolution, (float newValue) =>
                    {
                        GlobalSettings.SetSplineLineResolution(newValue);
                    }, 1, 200, 125, 50);

                    //Spline view distance
                    EHandleUi.CreateSliderAndInputField("Spline view distance: ", GlobalSettings.GetSplineViewDistance(), ref containerSplineViewDistance, (float newValue) =>
                    {
                        GlobalSettings.SetSplineViewDistance(newValue);
                    }, 100, 2500, 125, 50);

                    //Normals spacing
                    EHandleUi.CreateSliderAndInputField("Normals spacing: ", GlobalSettings.GetNormalsSpacing(), ref containerNormalSpacing, (float newValue) =>
                    {
                        GlobalSettings.SetNormalsSpacing(newValue);
                    }, 1, 200, 125, 50);

                    //Normals length
                    EHandleUi.CreateSliderAndInputField("Normals length: ", GlobalSettings.GetNormalsLength(), ref containerNormalLength, (float newValue) =>
                    {
                        GlobalSettings.SetNormalsLength(newValue);
                    }, 0.001f, 2, 125, 50);

                    //Control point size
                    EHandleUi.CreateSliderAndInputField("Control point size: ", GlobalSettings.GetControlPointSize(), ref containerControlPointSize, (float newValue) =>
                    {
                        GlobalSettings.SetControlPointSize(newValue);
                    }, 0.05f, 2.5f, 125, 50);

                    EHandleUi.CreateToggleField("Enable info icons: ", GlobalSettings.GetInfoIconsVisibility(), (newValue) =>
                    {
                        GlobalSettings.SetInfoIconsVisibility(newValue);
                    }, true);
                }
                else if (generalMenu == GeneralMenu.INFO)
                {
                    GUILayout.BeginHorizontal(LibraryGUIStyle.backgroundSubHeader);
                    EHandleUi.CreateLabelField("<b>INFO</b>", LibraryGUIStyle.textSubHeader, true);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal(EHandleUi.GetBackgroundStyle());
                    EHandleUi.CreateButton(EHandleUi.ButtonType.DEFAULT_MIDDLE_LEFT, LibraryGUIContent.textUserManual, 96, 18, () =>
                    {
                        Application.OpenURL("https://splinearchitect.com/#/user_manual");
                    });
                    EHandleUi.CreateButton(EHandleUi.ButtonType.DEFAULT_MIDDLE_LEFT, LibraryGUIContent.textDocumenation, 111, 18, () =>
                    {
                        Application.OpenURL("https://splinearchitect.com/");
                    });
                    EHandleUi.CreateButton(EHandleUi.ButtonType.DEFAULT_MIDDLE_LEFT, LibraryGUIContent.textDiscord, 69, 18, () =>
                    {
                        Application.OpenURL("https://discord.gg/uDyCeGKff7");
                    });
                    GUILayout.EndHorizontal();

                    EHandleUi.CreateLabelField($"Version: {versionNumber}", LibraryGUIStyle.textDefault);

                    GUILayout.BeginHorizontal(EHandleUi.GetBackgroundStyle());
                    EHandleUi.CreateLabelField($"Splines: {HandleRegistry.GetSplines().Count}", LibraryGUIStyle.textDefault, true);
                    EHandleUi.CreateLabelField($"Length: {Mathf.Round(EHandleSpline.lengthAllSplines)}", LibraryGUIStyle.textDefault, true);
                    EHandleUi.CreateLabelField($"Lines drawn: {EHandleSpline.totalLinesDrawn}", LibraryGUIStyle.textDefault, true);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal(LibraryGUIStyle.backgroundSubHeader);
                    EHandleUi.CreateLabelField("<b>ADDONS INSTALLED</b>", LibraryGUIStyle.textSubHeader, true);
                    GUILayout.EndHorizontal();

                    foreach (string s in activeAddons)
                    {
                        EHandleUi.CreateLabelField($"{s}", LibraryGUIStyle.textDefault);
                    }
                }
            }

            //Bottom
            if(!GlobalSettings.IsUiMinimized())
            {
                EHandleUi.CreateLine();
                GUILayout.BeginHorizontal(LibraryGUIStyle.backgroundBottomMenu);
                EHandleUi.CreateButtonToggle(EHandleUi.ButtonType.SUB_MENU, LibraryGUIContent.iconGeneral, LibraryGUIContent.iconGeneralActive, 35, 18, () =>
                {
                    generalMenu = GeneralMenu.GENERAL;
                }, generalMenu == GeneralMenu.GENERAL);

                EHandleUi.CreateButtonToggle(EHandleUi.ButtonType.SUB_MENU, LibraryGUIContent.iconSettings, LibraryGUIContent.iconSettingsActive, 35, 18, () =>
                {
                    generalMenu = GeneralMenu.SETTINGS;
                }, generalMenu == GeneralMenu.SETTINGS);

                EHandleUi.CreateButtonToggle(EHandleUi.ButtonType.SUB_MENU, LibraryGUIContent.iconInfo, LibraryGUIContent.iconInfoActive, 35, 18, () =>
                {
                    generalMenu = GeneralMenu.INFO;
                }, generalMenu == GeneralMenu.INFO);
                GUILayout.EndHorizontal();
            }
            else
            {
                EHandleUi.CreateLine();
                GUILayout.BeginHorizontal(LibraryGUIStyle.backgroundBottomMenu);
                EHandleUi.CreateButton(EHandleUi.ButtonType.SUB_MENU, LibraryGUIContent.maximize, 30, 18, () =>
                {
                    ToggleUi();
                });
                GUILayout.FlexibleSpace();
                EHandleUi.CreateLabelField($"<b>{toolName} {versionNumber}</b>", LibraryGUIStyle.textSmall, true);
                GUILayout.EndHorizontal();
            }

            EHandleUi.CreateLine();
            GUI.DragWindow();
        }

        private static void UpdatePosition(SceneView sceneView)
        {
            float th = toolbarHeight;
            if (EHandlePrefab.IsPrefabStageActive()) th = toolbarHeight * 2;

            //User changed position on general window
            if (oldGeneralWindowPos.x != generalWindowRect.x || oldGeneralWindowPos.y != generalWindowRect.y)
            {
                //Dont set current position first time
                if (generalWindowRect.x != -999 && generalWindowRect.y != -999)
                {
                    GlobalSettings.SetGeneralWindowPosition(generalWindowRect.x, generalWindowRect.y);
                    GlobalSettings.SetGeneralAtWindowBottom(generalWindowRect.y + generalWindowRect.height + th + windowSpace >= sceneView.position.height);
                }
            }

            generalWindowRect.x = GlobalSettings.GetGeneralWindowPosition().x;
            generalWindowRect.y = GlobalSettings.GetGeneralWindowPosition().y;

            //Stop on top
            if (generalWindowRect.y < windowSpace)
                generalWindowRect.y = windowSpace;
            //Stop on bottom
            else if (generalWindowRect.y + generalWindowRect.height + th + windowSpace > sceneView.position.height)
                generalWindowRect.y = sceneView.position.height - generalWindowRect.height - th - windowSpace;
            //Stop on left
            if (generalWindowRect.x < windowSpace)
                generalWindowRect.x = windowSpace;
            //Stop on right
            else if (generalWindowRect.x + generalWindowRect.width + windowSpace > sceneView.position.width)
                generalWindowRect.x = sceneView.position.width - generalWindowRect.width - windowSpace;

            oldGeneralWindowPos.Set(generalWindowRect.x, generalWindowRect.y);

            //Keep window at bottom.
            if (GlobalSettings.IsGeneralWindowAtBottom())
                generalWindowRect.y = sceneView.position.height - generalWindowRect.height - th - windowSpace;
        }

        public static void ToggleUi()
        {
            GlobalSettings.ToggleUiMinimized();

            if (GlobalSettings.IsUiMinimized())
            {
                generalWindowRect.y += generalWindowRect.height - LibraryGUIStyle.menuItemHeight + 1;
                UpdateRect();
            }
            else
            {
                UpdateRect();
                generalWindowRect.y -= generalWindowRect.height - LibraryGUIStyle.menuItemHeight + 1;
            }
        }

        private static void UpdateRect()
        {
            //If the generalWindowRect.width value is below what the ui element takes in minimum space. All other menus will look bad and the hide ui function will not work correctly.
            if (generalMenu == GeneralMenu.GENERAL)
            {
                if(GlobalSettings.GetGridVisibility())
                {
                    generalWindowRect.height = LibraryGUIStyle.menuItemHeight * 7 + 30;
                }
                else
                {
                    generalWindowRect.height = LibraryGUIStyle.menuItemHeight * 5 + 30;
                }

                generalWindowRect.width = 180;
            }
            else if (generalMenu == GeneralMenu.SETTINGS)
            {
                generalWindowRect.height = LibraryGUIStyle.menuItemHeight * 9 + 30;
                generalWindowRect.width = 332;
            }
            else if (generalMenu == GeneralMenu.INFO)
            {
                generalWindowRect.height = LibraryGUIStyle.menuItemHeight * 7 + 30;
                generalWindowRect.height += LibraryGUIStyle.menuItemHeight * activeAddons.Count - 10;

                generalWindowRect.width = 308;
            }

            if (GlobalSettings.IsUiMinimized())
            {
                generalWindowRect.height = LibraryGUIStyle.menuItemHeight * 1 - 2;
                generalWindowRect.width = 180;
            }
        }

        public static Rect GetRect()
        {
            return generalWindowRect;
        }

        public static void DisplayAddonName(string name)
        {
            if (activeAddons[0] == "None")
                activeAddons[0] = $"{name}";
            else
            {
                foreach (string s in activeAddons)
                {
                    if (s == name)
                        return;
                }

                activeAddons.Add(name);
            }
        }
    }
}