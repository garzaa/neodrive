// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: ToolbarDropdownHandleType.cs
//
// Author: Mikael Danielsson
// Date Created: 17-08-2025
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System.Collections.Generic;

using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;

using SplineArchitect.Libraries;
using SplineArchitect.Objects;

namespace SplineArchitect.Ui
{
    [EditorToolbarElement(ID, typeof(SceneView))]
    public class ToolbarDropdownHandleType : EditorToolbarDropdown
    {
        public const string ID = "SplineArchitect_toolbarDropdownHandleType";
        public static List<ToolbarDropdownHandleType> instances = new List<ToolbarDropdownHandleType>();

        public ToolbarDropdownHandleType()
        {
            HandleType handleType = GlobalSettings.GetHandleType();
            SyncVisualData(handleType);
            clicked += ShowMenu;

            RegisterCallback<DetachFromPanelEvent>(OnDetach);
            RegisterCallback<AttachToPanelEvent>(OnAttach);
            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        private void OnAttach(AttachToPanelEvent evt)
        {
            if (!instances.Contains(this))
                instances.Add(this);
        }

        private void OnDetach(DetachFromPanelEvent evt)
        {
            instances.Remove(this);
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            if (parent.resolvedStyle.flexDirection == FlexDirection.Row)
                style.width = 48;
            else
                style.width = 36;
        }

        void ShowMenu()
        {
            HandleType handleType = GlobalSettings.GetHandleType();

            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Mirrored"), handleType == HandleType.MIRRORED, () =>
            {
                GlobalSettings.SetHandleType(HandleType.MIRRORED);
                foreach (ToolbarDropdownHandleType tdht in instances) tdht.SyncVisualData(HandleType.MIRRORED);
            });
            menu.AddItem(new GUIContent("Continuous"), handleType == HandleType.CONTINUOUS, () =>
            {
                GlobalSettings.SetHandleType(HandleType.CONTINUOUS);
                foreach (ToolbarDropdownHandleType tdht in instances) tdht.SyncVisualData(HandleType.CONTINUOUS);
            });
            menu.AddItem(new GUIContent("Broken"), handleType == HandleType.BROKEN, () =>
            {
                GlobalSettings.SetHandleType(HandleType.BROKEN);
                foreach (ToolbarDropdownHandleType tdht in instances) tdht.SyncVisualData(HandleType.BROKEN);
            });

            Rect r = worldBound;
            Vector2 screenPos = GUIUtility.GUIToScreenPoint(new Vector2(worldBound.x, worldBound.y));
            menu.DropDown(new Rect(worldBound.position, worldBound.size));
        }

        private void SyncVisualData(HandleType handleType)
        {
            bool isDark = EditorGUIUtility.isProSkin;

            //Tooltip
            if (handleType == HandleType.MIRRORED)          tooltip = "Handle type - Mirrored";
            else if (handleType == HandleType.CONTINUOUS)   tooltip = "Handle type - Continuous";
            else if (handleType == HandleType.BROKEN)       tooltip = "Handle type - Broken";

            //Icon
            if (handleType == HandleType.MIRRORED && isDark)         icon = LibraryTexture.iconHandleMirrored;
            else if (handleType == HandleType.MIRRORED && !isDark)   icon = LibraryTexture.iconHandleMirroredLight;
            else if (handleType == HandleType.CONTINUOUS && isDark)  icon = LibraryTexture.iconHandleContinuous;
            else if (handleType == HandleType.CONTINUOUS && !isDark) icon = LibraryTexture.iconHandleContinuousLight;
            else if (handleType == HandleType.BROKEN && isDark)      icon = LibraryTexture.iconHandleBroken;
            else if (handleType == HandleType.BROKEN && !isDark)     icon = LibraryTexture.iconHandleBrokenLight;


            Image img = this.Q<UnityEngine.UIElements.Image>();

            if (img != null)
            {
                img.style.width = 28;
                img.style.height = 14;
                img.scaleMode = ScaleMode.StretchToFill;
            }
        }
    }
}
