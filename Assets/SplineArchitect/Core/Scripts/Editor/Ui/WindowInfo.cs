// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: WindowInfo.cs
//
// Author: Mikael Danielsson
// Date Created: 05-08-2025
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using SplineArchitect.Libraries;
using SplineArchitect.Objects;
using SplineArchitect.Utility;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SplineArchitect.Ui
{
    public class WindowInfo : WindowBase
    {
        public const string toolName = "Spline Architect";
        public const string versionNumber = "1.2.5";

        private static List<string> activeAddons = new List<string> { "None" };

        protected override void OnGUIExtended()
        {
            GUILayout.BeginHorizontal(LibraryGUIStyle.backgroundHeader);
            EUiUtility.CreateLabelField("<b>Info</b>", LibraryGUIStyle.textHeaderBlack, true);

            //Close
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

            GUILayout.BeginHorizontal(EUiUtility.GetBackgroundStyle());
            EUiUtility.CreateButton(ButtonType.DEFAULT_MIDDLE_LEFT, LibraryGUIContent.textUserManual, 96, 18, () =>
            {
                Application.OpenURL("https://splinearchitect.com/#/user_manual");
            });
            EUiUtility.CreateButton(ButtonType.DEFAULT_MIDDLE_LEFT, LibraryGUIContent.textDocumenation, 111, 18, () =>
            {
                Application.OpenURL("https://splinearchitect.com/");
            });
            EUiUtility.CreateButton(ButtonType.DEFAULT_MIDDLE_LEFT, LibraryGUIContent.textDiscord, 69, 18, () =>
            {
                Application.OpenURL("https://discord.gg/uDyCeGKff7");
            });
            GUILayout.EndHorizontal();

            EUiUtility.CreateLabelField($"Version: {versionNumber}", LibraryGUIStyle.textDefault);

            GUILayout.BeginHorizontal(EUiUtility.GetBackgroundStyle());
            EUiUtility.CreateLabelField($"Splines: {HandleRegistry.GetSplines().Count}", LibraryGUIStyle.textDefault, true);
            EUiUtility.CreateLabelField($"Length: {Mathf.Round(EHandleSpline.lengthAllSplines)}", LibraryGUIStyle.textDefault, true);
            EUiUtility.CreateLabelField($"Lines drawn: {EHandleSpline.totalLinesDrawn}", LibraryGUIStyle.textDefault, true);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal(LibraryGUIStyle.backgroundSubHeader);
            EUiUtility.CreateLabelField("<b>ADDONS INSTALLED</b>", LibraryGUIStyle.textSubHeader, true);
            GUILayout.EndHorizontal();

            foreach (string s in activeAddons)
            {
                EUiUtility.CreateLabelField($"{s}", LibraryGUIStyle.textDefault);
            }
        }

        protected override void UpdateWindowSize()
        {
            cachedRect.size = new Vector2(327, menuItemHeight * 4 + 10);
            cachedRect.height += menuItemHeight * activeAddons.Count;
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
