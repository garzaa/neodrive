// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: ToolbarButtonGrid.cs
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
using SplineArchitect.Utility;

namespace SplineArchitect.Ui
{
    [EditorToolbarElement(ID, typeof(SceneView))]
    public class ToolbarDropdownToggleGrid : EditorToolbarDropdownToggle
    {
        public const string ID = "SplineArchitect_toolbarButtonGrid";
        public static List<ToolbarDropdownToggleGrid> instances = new List<ToolbarDropdownToggleGrid>();

        private WindowBase windowBase;
        private Vector2 oldPosition;
        public bool pointerHovering;

        public ToolbarDropdownToggleGrid()
        {
            tooltip = "Toggle grid";
            icon = EditorGUIUtility.isProSkin ? LibraryTexture.iconGrid : LibraryTexture.iconGridLight;

            dropdownClicked += ToggleMenu;
            this.RegisterValueChangedCallback(OnValueChanged);
            RegisterCallback<DetachFromPanelEvent>(OnDetach);
            RegisterCallback<AttachToPanelEvent>(OnAttach);
            SetValueWithoutNotify(GlobalSettings.GetGridVisibility());
            RegisterCallback<PointerEnterEvent>(OnPointerEntered);
            RegisterCallback<PointerLeaveEvent>(OnPointerLeave);

            Image img = this.Q<UnityEngine.UIElements.Image>();
            if (img != null)
            {
                img.style.width = 13;
                img.style.height = 13;
                img.scaleMode = ScaleMode.StretchToFill;
            }
        }

        private void OnPointerEntered(PointerEnterEvent evt)
        {
            pointerHovering = true;
        }

        private void OnPointerLeave(PointerLeaveEvent evt)
        {
            pointerHovering = false;
        }

        private void OnAttach(AttachToPanelEvent evt)
        {
            if (!instances.Contains(this))
                instances.Add(this);
        }

        private void OnDetach(DetachFromPanelEvent evt)
        {
            CloseOrOpenWindow(false);
            instances.Remove(this);
        }

        private void OnValueChanged(ChangeEvent<bool> evt)
        {
            GlobalSettings.SetGridVisibility(evt.newValue);

            foreach(ToolbarDropdownToggleGrid tdtg in instances)
            {
                tdtg.SetValueWithoutNotify(GlobalSettings.GetGridVisibility());
            }

            CloseWindow();
            WindowBase.RepaintAll();
        }

        private void ToggleMenu()
        {
            CloseOrOpenWindow(windowBase == null);
        }

        private void CloseOrOpenWindow(bool open)
        {
            if (open) ShowWindow<WindowGrid>();
            else CloseWindow();
        }

        private void ShowWindow<T>() where T : WindowBase
        {

            if (!EHandleUi.initialized)
            {
                SetValueWithoutNotify(false);
                return;
            }

            Vector2 windowPos = EUiUtility.GetWindowAnchorPosition(SceneView.lastActiveSceneView, worldBound, parent.resolvedStyle.flexDirection == FlexDirection.Row);

            windowBase = ScriptableObject.CreateInstance<T>();
            windowBase.OpenWindow(windowPos, true);
            windowBase.toolbarDropdownToggleGrid = this;

            oldPosition = worldBound.position;
        }

        private void CloseWindow()
        {
            if (windowBase != null)
            {
                windowBase.toolbarDropdownToggleGrid = null;
                windowBase.Close();
                windowBase = null;
            }
        }

        public bool DidMove()
        {
            if (oldPosition != worldBound.position)
            {
                oldPosition = worldBound.position;
                return true;
            }

            return false;
        }
    }
}
