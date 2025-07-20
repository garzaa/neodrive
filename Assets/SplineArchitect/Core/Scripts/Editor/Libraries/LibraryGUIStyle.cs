// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: GUIStyleLibrary.cs
//
// Author: Mikael Danielsson
// Date Created: 18-06-2023
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using UnityEngine;
using UnityEditor;

namespace SplineArchitect.Libraries
{
    public static class LibraryGUIStyle
    {
        //Settings
        public const float menuItemHeight = 22;

        //General
        public static GUIStyle specificX { get; private set; }
        public static GUIStyle specificYZ { get; private set; }
        public static GUIStyle specificFromToX { get; private set; }
        public static GUIStyle specificFromToYZ { get; private set; }
        public static GUIStyle sliderDefault { get; private set; }
        public static GUIStyle sliderThumbDefault { get; private set; }
        public static GUIStyle popUpFieldSmallText { get; private set; }
        public static GUIStyle separatorWhite { get; private set; }
        public static GUIStyle infoIcon { get; private set; }
        public static GUIStyle empty { get; private set; }
        public static GUIStyle scrollbarStyle { get; private set; }

        //Buttons
        public static GUIStyle buttonDefault { get; private set; }
        public static GUIStyle buttonDefaultGreen { get; private set; }
        public static GUIStyle buttonDefaultRed { get; private set; }
        public static GUIStyle buttonDefaultActive { get; private set; }
        public static GUIStyle buttonDefaultMiddleLeft { get; private set; }
        public static GUIStyle buttonDefaultMiddleLeftActive { get; private set; }
        public static GUIStyle buttonSubMenu { get; private set; }
        public static GUIStyle buttonSubMenuActive { get; private set; }

        //Texts
        public static GUIStyle textHeader { get; private set; }
        public static GUIStyle textHeaderBlack { get; private set; }
        public static GUIStyle textSubHeader { get; private set; }
        public static GUIStyle textDefault { get; private set; }
        public static GUIStyle textDefaultBlack { get; private set; }
        public static GUIStyle textSmall { get; private set; }
        public static GUIStyle textNoWdith { get; private set; }
        public static GUIStyle textFieldNoWidth { get; private set; }
        public static GUIStyle textField { get; private set; }
        public static GUIStyle textFieldSmall { get; private set; }
        public static GUIStyle textFieldVerySmall { get; private set; }
        public static GUIStyle textSceneView { get; private set; }

        //Lines
        public static GUIStyle lineGrey { get; private set; }
        public static GUIStyle lineWhite { get; private set; }
        public static GUIStyle lineYellowThick { get; private set; }
        public static GUIStyle lineBlackThick { get; private set; }
        public static GUIStyle lineYellow { get; private set; }
        public static GUIStyle lineBlack { get; private set; }

        //Backgrounds
        public static GUIStyle backgroundHeader { get; private set; }
        public static GUIStyle backgroundBottomMenu { get; private set; }
        public static GUIStyle backgroundToolbar { get; private set; }
        public static GUIStyle backgroundSubHeader { get; private set; }
        public static GUIStyle backgroundItem2 { get; private set; }
        public static GUIStyle backgroundItem1 { get; private set; }
        public static GUIStyle backgroundSelectedLayerHeader { get; private set; }
        public static GUIStyle backgroundSelectedLayer { get; private set; }
        public static GUIStyle backgroundMainLayerButtons { get; private set; }

        //Need to instalize like this. LibraryGUIStyle is dependent on different textures that needs to be created first.
        public static void Initalize()
        {
            //General
            specificX = CreateTextStyle(Color.white, new RectOffset(6, 4, 2, 2), new RectOffset(0, 0, 0, 0), 13, 15);
            specificYZ = CreateTextStyle(Color.white, new RectOffset(6, 4, 2, 2), new RectOffset(10, 0, 0, 0), 13, 15);
            specificFromToX = CreateTextStyle(Color.white, new RectOffset(6, 4, 2, 2), new RectOffset(0, 0, 0, 0), 13, 15);
            specificFromToYZ = CreateTextStyle(Color.white, new RectOffset(6, 4, 2, 2), new RectOffset(10, 0, 0, 0), 13, 15);
            empty = new GUIStyle();
            scrollbarStyle = CreateScrollBar();
            separatorWhite = CreateBoxStyle(LibraryTexture.gScale80_100, 16, 4, new RectOffset(0, 0, 3, 0));

            //Backgrounds
            backgroundHeader = CreateBackgroundStyle(LibraryTexture.gScale95_100, menuItemHeight);
            backgroundSubHeader = CreateBackgroundStyle(LibraryTexture.gScale95_100, 12);
            backgroundToolbar = CreateBackgroundStyle(LibraryTexture.gScale40_100, 23);
            backgroundBottomMenu = CreateBackgroundStyle(LibraryTexture.gScale15_100, 18);
            backgroundItem1 = CreateBackgroundStyle(LibraryTexture.gScale10_100, menuItemHeight);
            backgroundItem2 = CreateBackgroundStyle(LibraryTexture.gScale7_100, menuItemHeight);
            backgroundSelectedLayerHeader = CreateBackgroundStyle(LibraryTexture.yellow100, menuItemHeight);
            backgroundSelectedLayer = CreateBackgroundStyle(LibraryTexture.gScale15_100, menuItemHeight);
            backgroundMainLayerButtons = CreateBackgroundStyle(LibraryTexture.gScale30_100, menuItemHeight);

            //Lines
            lineGrey = CreateBoxStyle(LibraryTexture.gScale0_100, 1, 0, new RectOffset(0, 0, 0, 0));
            lineWhite = CreateBoxStyle(LibraryTexture.gScale95_100, 1, 0, new RectOffset(0, 0, 0, 0));
            lineYellowThick = CreateBackgroundStyle(LibraryTexture.yellow100, 4);
            lineBlackThick = CreateBackgroundStyle(LibraryTexture.gScale5_100, 4);
            lineYellow = CreateBoxStyle(LibraryTexture.yellow100, 1, 0, new RectOffset(0, 0, 0, 0));
            lineBlack = CreateBoxStyle(LibraryTexture.gScale0_100, 1, 0, new RectOffset(0, 0, 0, 0));

            //Texts
            textHeader = CreateTextStyle(Color.white, new RectOffset(6, 4, 2, 2), new RectOffset(0, 0, 0, 0), 13);
            textHeaderBlack = CreateTextStyle(Color.black, new RectOffset(6, 4, 2, 2), new RectOffset(0, 0, 0, 0), 12);
            textSubHeader = CreateTextStyle(new Color(0, 0, 0, 1), new RectOffset(6, 4, -10, 0), new RectOffset(0, 0, 0, 0), 9);
            textDefault = CreateTextStyle(Color.white, new RectOffset(6, 4, 0, 2), new RectOffset(0, 0, 0, 0), 12);
            textDefaultBlack = CreateTextStyle(Color.black, new RectOffset(6, 4, 0, 2), new RectOffset(0, 0, 0, 0), 12);
            textNoWdith = CreateTextStyle(Color.white, new RectOffset(0, 0, 0, 2), new RectOffset(0, 0, 0, 0), 12);
            textSmall = CreateTextStyle(Color.white, new RectOffset(6, 4, 0, 2), new RectOffset(0, 0, 0, 0), 10);
            textFieldNoWidth = CreateTextField(0, 18, new RectOffset(2, 2, 2, 2));
            textField = CreateTextField(50, 18, new RectOffset(2, 2, 2, 2));
            textFieldSmall = CreateTextField(38, 18, new RectOffset(2, 2, 2, 2));
            textFieldVerySmall = CreateTextField(26, 18, new RectOffset(2, 2, 2, 2));
            textSceneView = CreateSceneViewText(18, new RectOffset(2, 1, 2, 3));

            //Sliders
            sliderDefault = CreateSlider();
            sliderThumbDefault = CreateSliderThumb();
            popUpFieldSmallText = CreatePopUpFiled();
            infoIcon = CreateInfoMsgIcon();

            //Buttons
            buttonDefault = CreateButton(LibraryTexture.buttonDefault, LibraryTexture.buttonDefaultPressed, new RectOffset(2, 2, 2, 2), TextAnchor.MiddleCenter);
            buttonDefaultGreen = CreateButton(LibraryTexture.buttonDefaultGreen, LibraryTexture.buttonDefaultGreenPressed, new RectOffset(2, 2, 2, 2), TextAnchor.MiddleCenter);
            buttonDefaultRed = CreateButton(LibraryTexture.buttonDefaultRed, LibraryTexture.buttonDefaultRedPressed, new RectOffset(2, 2, 2, 2), TextAnchor.MiddleCenter);
            buttonDefaultActive = CreateButton(LibraryTexture.buttonDefaultActive, LibraryTexture.buttonDefaultPressed, new RectOffset(2, 2, 2, 2), TextAnchor.MiddleCenter);
            buttonDefaultMiddleLeft = CreateButton(LibraryTexture.buttonDefault, LibraryTexture.buttonDefaultPressed, new RectOffset(2, 2, 2, 2), TextAnchor.MiddleLeft);
            buttonDefaultMiddleLeftActive = CreateButton(LibraryTexture.buttonDefaultActive, LibraryTexture.buttonDefaultPressed, new RectOffset(2, 2, 2, 2), TextAnchor.MiddleLeft);
            buttonSubMenu = CreateButton(LibraryTexture.gScale15_100, LibraryTexture.gScale40_100, new RectOffset(0, 0, 0, 0), TextAnchor.MiddleCenter);
            buttonSubMenuActive = CreateButton(LibraryTexture.gScale7_100, LibraryTexture.gScale7_100, new RectOffset(0, 0, 0, 0), TextAnchor.MiddleCenter);
        }

        private static GUIStyle CreateButton(Texture2D texture, Texture2D textureActive, RectOffset margin, TextAnchor textAnchor)
        {
            GUIStyle button = new GUIStyle
            {
                normal = { background = texture },
                hover = { background = texture },
                active = { background = textureActive },
                border = new RectOffset(3, 3, 3, 3),
                padding = new RectOffset(6, 6, 3, 3),
                margin = margin,
                alignment = textAnchor,
            };

            button.normal.textColor = Color.white;
            button.focused.textColor = Color.white;
            button.active.textColor = Color.white;
            button.hover.textColor = Color.white;

            return button;
        }

        private static GUIStyle CreateScrollBar()
        {
            GUIStyle style = new GUIStyle(GUI.skin.verticalScrollbar);
            style.normal.background = LibraryTexture.gScale10_100;

            return style;
        }

        private static GUIStyle CreatePopUpFiled()
        {
            GUIStyle style = new GUIStyle()
            {
                normal = {
                    background = LibraryTexture.popUpField,
                    textColor = Color.white
                },
                fontSize = 11,
                alignment = TextAnchor.MiddleLeft,
                fixedHeight = 18f,
                padding = new RectOffset(6, 20, 3, 3),
                margin = new RectOffset(2, 2, 2, 2),
                border = new RectOffset(3, 16, 3, 3),
                clipping = TextClipping.Clip
            };

            return style;
        }

        private static GUIStyle CreateSliderThumb()
        {
            GUIStyle style = new GUIStyle(GUI.skin.horizontalSliderThumb);
            style.margin = new RectOffset(0, 0, 0, 0);

            return style;
        }

        private static GUIStyle CreateSlider()
        {
            GUIStyle style = new GUIStyle(GUI.skin.horizontalSlider);
            style.normal.background = LibraryTexture.slider;
            style.margin = new RectOffset(0, 0, 6, 0);

            return style;
        }

        private static GUIStyle CreateBackgroundStyle(Texture2D texture, float size)
        {
            GUIStyle style = new GUIStyle();
            style.normal.background = texture;
            style.fixedHeight = size;

            return style;
        }

        private static GUIStyle CreateBoxStyle(Texture2D texture, float height, float width, RectOffset margin)
        {
            GUIStyle style = new GUIStyle();
            style.normal.background = texture;
            style.fixedHeight = height;
            style.fixedWidth = width;
            style.margin = margin;

            return style;
        }

        private static GUIStyle CreateTextStyle(Color color, RectOffset padding, RectOffset margin, int size, float elementWidth = 0)
        {
            GUIStyle textStyle = new GUIStyle();

            if(elementWidth != 0)
                textStyle.fixedWidth = elementWidth;

            textStyle.fixedHeight = menuItemHeight;
            textStyle.richText = true;
            textStyle.padding = padding;
            textStyle.margin = margin;
            textStyle.normal.textColor = color;
            textStyle.fontSize = size;
            textStyle.alignment = TextAnchor.MiddleLeft;

            return textStyle;
        }

        private static GUIStyle CreateTextField(float width, float height, RectOffset margin)
        {
            GUIStyle textField = new GUIStyle
            {
                normal = {
                    background = LibraryTexture.inputField,
                    textColor = Color.white,
                },
                focused = {
                    background = LibraryTexture.inputFieldActive,
                    textColor = Color.white
                },
                active = {
                    background = LibraryTexture.inputFieldActive,
                    textColor = Color.white
                },
                border = new RectOffset(4, 4, 4, 4),
                padding = new RectOffset(4, 4, 2, 2),
                margin = margin,
                alignment = TextAnchor.MiddleLeft,
                fontSize = 12,
                fixedHeight = height,
                fixedWidth = width,
                clipping = TextClipping.Clip,
                wordWrap = false
            };

            return textField;
        }

        private static GUIStyle CreateSceneViewText(float height, RectOffset padding)
        {
            GUIStyle textField = new GUIStyle
            {
                normal = {
                    background = LibraryTexture.inputField,
                    textColor = Color.white,
                },
                focused = {
                    background = LibraryTexture.inputFieldActive,
                    textColor = Color.white
                },
                active = {
                    background = LibraryTexture.inputFieldActive,
                    textColor = Color.white
                },
                border = new RectOffset(4, 4, 4, 4),
                padding = padding,
                alignment = TextAnchor.MiddleCenter,
                fontSize = 12,
                fixedHeight = height,
            };

            return textField;
        }

        private static GUIStyle CreateInfoMsgIcon()
        {
            GUIStyle textStyle = new GUIStyle();
            textStyle.fixedWidth = 13;
            textStyle.fixedHeight = 13;
            textStyle.padding = new RectOffset(0, 0, 0, 0);
            textStyle.margin = new RectOffset(5, 3, 4, 0);
            textStyle.alignment = TextAnchor.MiddleCenter;

            return textStyle;
        }
    }
}
