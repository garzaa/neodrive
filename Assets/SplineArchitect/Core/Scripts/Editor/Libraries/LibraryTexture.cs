// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: LibraryColor.cs
//
// Author: Mikael Danielsson
// Date Created: 09-12-2023
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using UnityEditor;
using UnityEngine;

namespace SplineArchitect.Libraries
{
    public class LibraryTexture
    {
        const string path = "/Core/Textures/";

        //Others
        public static Texture2D slider { get; private set; }
        public static Texture2D minimize { get; private set; }
        public static Texture2D maximize { get; private set; }
        public static Texture2D inputField { get; private set; }
        public static Texture2D inputFieldActive { get; private set; }
        public static Texture2D popUpField { get; private set; }

        //Buttons
        public static Texture2D buttonDefault { get; private set; }
        public static Texture2D buttonDefaultActive { get; private set; }
        public static Texture2D buttonDefaultPressed { get; private set; }
        public static Texture2D buttonSubMenu { get; private set; }
        public static Texture2D buttonSubMenuActive { get; private set; }
        public static Texture2D buttonSubMenuPressed { get; private set; }
        public static Texture2D buttonDefaultRed { get; private set; }
        public static Texture2D buttonDefaultRedPressed { get; private set; }
        public static Texture2D buttonDefaultGreen { get; private set; }
        public static Texture2D buttonDefaultGreenPressed { get; private set; }

        //Icons
        public static Texture2D iconConstrined { get; private set; }
        public static Texture2D iconNotConstrined { get; private set; }
        public static Texture2D iconJoin { get; private set; }
        public static Texture2D iconGrid { get; private set; }
        public static Texture2D iconGridActive { get; private set; }
        public static Texture2D iconGeneral { get; private set; }
        public static Texture2D iconGeneralActive { get; private set; }
        public static Texture2D iconMirror { get; private set; }
        public static Texture2D iconMirrorActive { get; private set; }
        public static Texture2D iconRuntime { get; private set; }
        public static Texture2D iconRuntimeActive { get; private set; }
        public static Texture2D iconSpline { get; private set; }
        public static Texture2D iconSplineActive { get; private set; }
        public static Texture2D iconLoop { get; private set; }
        public static Texture2D iconLoopActive { get; private set; }
        public static Texture2D iconNormals { get; private set; }
        public static Texture2D iconNormalsActive { get; private set; }
        public static Texture2D iconHide { get; private set; }
        public static Texture2D iconHideActive { get; private set; }
        public static Texture2D iconHideActive2 { get; private set; }
        public static Texture2D iconAdd { get; private set; }
        public static Texture2D iconAddActive { get; private set; }
        public static Texture2D iconTerrain { get; private set; }
        public static Texture2D iconTerrainActive { get; private set; }
        public static Texture2D iconInfo { get; private set; }
        public static Texture2D iconInfoActive { get; private set; }
        public static Texture2D iconSettings { get; private set; }
        public static Texture2D iconSettingsActive { get; private set; }
        public static Texture2D iconReverse { get; private set; }
        public static Texture2D iconFlatten { get; private set; }
        public static Texture2D iconToCenter { get; private set; }
        public static Texture2D iconSelectSpline { get; private set; }
        public static Texture2D iconSelectAll { get; private set; }
        public static Texture2D iconNextControlPoint { get; private set; }
        public static Texture2D iconPrevControlPoint { get; private set; }
        public static Texture2D iconMove { get; private set; }
        public static Texture2D iconSplit { get; private set; }
        public static Texture2D iconLink { get; private set; }
        public static Texture2D iconUnlink { get; private set; }
        public static Texture2D iconAlignGrid { get; private set; }
        public static Texture2D iconAlign { get; private set; }
        public static Texture2D iconNoise { get; private set; }
        public static Texture2D iconNoiseActive { get; private set; }
        public static Texture2D iconDefault { get; private set; }
        public static Texture2D iconX { get; private set; }
        public static Texture2D iconExternalLink { get; private set; }
        public static Texture2D iconCenterGrid { get; private set; }
        public static Texture2D iconUpArrow { get; private set; }
        public static Texture2D iconDownArrow { get; private set; }
        public static Texture2D iconExport { get; private set; }
        public static Texture2D iconMinimize { get; private set; }
        public static Texture2D iconCopy { get; private set; }
        public static Texture2D iconPaste { get; private set; }
        public static Texture2D iconAlignTangents { get; private set; }
        public static Texture2D iconMagnet { get; private set; }
        public static Texture2D iconMagnetActive { get; private set; }
        public static Texture2D iconMagnetActive2 { get; private set; }

        public static Texture2D iconInfoMsg { get; private set; }
        public static Texture2D iconWarningMsg { get; private set; }
        public static Texture2D iconErrorMsg { get; private set; }

        public static Texture2D gScale95_100 { get; private set; }
        public static Texture2D gScale90_100 { get; private set; }
        public static Texture2D gScale80_100 { get; private set; }
        public static Texture2D gScale60_100 { get; private set; }
        public static Texture2D gScale40_100 { get; private set; }
        public static Texture2D gScale40_90 { get; private set; }
        public static Texture2D gScale30_100 { get; private set; }
        public static Texture2D gScale20_100 { get; private set; }
        public static Texture2D gScale20_80 { get; private set; }
        public static Texture2D gScale15_100 { get; private set; }
        public static Texture2D gScale10_100 { get; private set; }
        public static Texture2D gScale10_80 { get; private set; }
        public static Texture2D gScale7_100 { get; private set; }
        public static Texture2D gScale5_100 { get; private set; }
        public static Texture2D gScale3_100 { get; private set; }
        public static Texture2D gScale0_100 { get; private set; }
        public static Texture2D gScale0_80 { get; private set; }
        public static Texture2D gScale0_50 { get; private set; }
        public static Texture2D yellow100 { get; private set; }

        public static Texture2D empty { get; private set; }

        public static void Initalize()
        {
            string mainFolderPath = EHandleMainFolder.GetFolderPath();

            gScale95_100 = AssetDatabase.LoadAssetAtPath<Texture2D>($"{mainFolderPath}{path}gScale95_100.png");
            gScale90_100 = AssetDatabase.LoadAssetAtPath<Texture2D>($"{mainFolderPath}{path}gScale90_100.png");
            gScale80_100 = AssetDatabase.LoadAssetAtPath<Texture2D>($"{mainFolderPath}{path}gScale80_100.png");
            gScale60_100 = AssetDatabase.LoadAssetAtPath<Texture2D>($"{mainFolderPath}{path}gScale60_100.png");
            gScale40_100 = AssetDatabase.LoadAssetAtPath<Texture2D>($"{mainFolderPath}{path}gScale40_100.png");
            gScale40_90 = AssetDatabase.LoadAssetAtPath<Texture2D>($"{mainFolderPath}{path}gScale40_90.png");
            gScale30_100 = AssetDatabase.LoadAssetAtPath<Texture2D>($"{mainFolderPath}{path}gScale30_100.png");
            gScale20_100 = AssetDatabase.LoadAssetAtPath<Texture2D>($"{mainFolderPath}{path}gScale20_100.png");
            gScale20_80 = AssetDatabase.LoadAssetAtPath<Texture2D>($"{mainFolderPath}{path}gScale20_80.png");
            gScale15_100 = AssetDatabase.LoadAssetAtPath<Texture2D>($"{mainFolderPath}{path}gScale15_100.png");
            gScale10_100 = AssetDatabase.LoadAssetAtPath<Texture2D>($"{mainFolderPath}{path}gScale10_100.png");
            gScale10_80 = AssetDatabase.LoadAssetAtPath<Texture2D>($"{mainFolderPath}{path}gScale10_80.png");
            gScale7_100 = AssetDatabase.LoadAssetAtPath<Texture2D>($"{mainFolderPath}{path}gScale7_100.png");
            gScale5_100 = AssetDatabase.LoadAssetAtPath<Texture2D>($"{mainFolderPath}{path}gScale5_100.png");
            gScale3_100 = AssetDatabase.LoadAssetAtPath<Texture2D>($"{mainFolderPath}{path}gScale3_100.png");
            gScale0_100 = AssetDatabase.LoadAssetAtPath<Texture2D>($"{mainFolderPath}{path}gScale0_100.png");
            gScale0_80 = AssetDatabase.LoadAssetAtPath<Texture2D>($"{mainFolderPath}{path}gScale0_80.png");
            gScale0_50 = AssetDatabase.LoadAssetAtPath<Texture2D>($"{mainFolderPath}{path}gScale0_50.png");
            yellow100 = AssetDatabase.LoadAssetAtPath<Texture2D>($"{mainFolderPath}{path}yellow100.png");

            empty = AssetDatabase.LoadAssetAtPath<Texture2D>($"{mainFolderPath}{path}empty.png");

            minimize = AssetDatabase.LoadAssetAtPath<Texture2D>($"{mainFolderPath}{path}minimize.png");
            maximize = AssetDatabase.LoadAssetAtPath<Texture2D>($"{mainFolderPath}{path}maximize.png");
            iconConstrined = AssetDatabase.LoadAssetAtPath<Texture2D>($"{mainFolderPath}{path}constrainedIcon.png");
            iconNotConstrined = AssetDatabase.LoadAssetAtPath<Texture2D>($"{mainFolderPath}{path}notConstrainedIcon.png");
            iconErrorMsg = AssetDatabase.LoadAssetAtPath<Texture2D>($"{mainFolderPath}{path}errorMsgIcon.png");
            iconWarningMsg = AssetDatabase.LoadAssetAtPath<Texture2D>($"{mainFolderPath}{path}warningMsgIcon.png");
            iconInfoMsg = AssetDatabase.LoadAssetAtPath<Texture2D>($"{mainFolderPath}{path}infoMsgIcon.png");
            iconMove = AssetDatabase.LoadAssetAtPath<Texture2D>($"{mainFolderPath}{path}moveIcon.png");
            iconNextControlPoint = AssetDatabase.LoadAssetAtPath<Texture2D>($"{mainFolderPath}{path}nextControlPointIcon.png");
            iconPrevControlPoint = AssetDatabase.LoadAssetAtPath<Texture2D>($"{mainFolderPath}{path}prevControlPointIcon.png");
            iconSelectSpline = AssetDatabase.LoadAssetAtPath<Texture2D>($"{mainFolderPath}{path}selectCurveIcon.png");
            iconSelectAll = AssetDatabase.LoadAssetAtPath<Texture2D>($"{mainFolderPath}{path}selectAllIcon.png");
            iconToCenter = AssetDatabase.LoadAssetAtPath<Texture2D>($"{mainFolderPath}{path}toCenterIcon.png");
            iconJoin = AssetDatabase.LoadAssetAtPath<Texture2D>($"{mainFolderPath}{path}joinIcon.png");
            iconGrid = AssetDatabase.LoadAssetAtPath<Texture2D>($"{mainFolderPath}{path}gridIcon.png");
            iconGridActive = AssetDatabase.LoadAssetAtPath<Texture2D>($"{mainFolderPath}{path}gridIcon_active.png");
            iconGeneral = AssetDatabase.LoadAssetAtPath<Texture2D>($"{mainFolderPath}{path}generalIcon.png");
            iconGeneralActive = AssetDatabase.LoadAssetAtPath<Texture2D>($"{mainFolderPath}{path}generalIcon_active.png");
            iconMirror = AssetDatabase.LoadAssetAtPath<Texture2D>($"{mainFolderPath}{path}mirrorIcon.png");
            iconMirrorActive = AssetDatabase.LoadAssetAtPath<Texture2D>($"{mainFolderPath}{path}mirrorIcon_active.png");
            iconRuntime = AssetDatabase.LoadAssetAtPath<Texture2D>($"{mainFolderPath}{path}runtimeIcon.png");
            iconRuntimeActive = AssetDatabase.LoadAssetAtPath<Texture2D>($"{mainFolderPath}{path}runtimeIcon_active.png");
            iconSpline = AssetDatabase.LoadAssetAtPath<Texture2D>($"{mainFolderPath}{path}curveIcon.png");
            iconSplineActive = AssetDatabase.LoadAssetAtPath<Texture2D>($"{mainFolderPath}{path}curveIcon_active.png");
            iconNoise = AssetDatabase.LoadAssetAtPath<Texture2D>($"{mainFolderPath}{path}noiseIcon.png");
            iconNoiseActive = AssetDatabase.LoadAssetAtPath<Texture2D>($"{mainFolderPath}{path}noiseIcon_active.png");
            iconLoop = AssetDatabase.LoadAssetAtPath<Texture2D>($"{mainFolderPath}{path}loopIcon.png");
            iconLoopActive = AssetDatabase.LoadAssetAtPath<Texture2D>($"{mainFolderPath}{path}loopIcon_active.png");
            iconNormals = AssetDatabase.LoadAssetAtPath<Texture2D>($"{mainFolderPath}{path}normalsIcon.png");
            iconNormalsActive = AssetDatabase.LoadAssetAtPath<Texture2D>($"{mainFolderPath}{path}normalsIcon_active.png");
            iconHide = AssetDatabase.LoadAssetAtPath<Texture2D>($"{mainFolderPath}{path}hideIcon.png");
            iconHideActive = AssetDatabase.LoadAssetAtPath<Texture2D>($"{mainFolderPath}{path}hideIcon_active.png");
            iconHideActive2 = AssetDatabase.LoadAssetAtPath<Texture2D>($"{mainFolderPath}{path}hideIcon_active2.png");
            iconAdd = AssetDatabase.LoadAssetAtPath<Texture2D>($"{mainFolderPath}{path}addIcon.png");
            iconAddActive = AssetDatabase.LoadAssetAtPath<Texture2D>($"{mainFolderPath}{path}addIcon_active.png");
            iconTerrain = AssetDatabase.LoadAssetAtPath<Texture2D>($"{mainFolderPath}{path}terrainIcon.png");
            iconInfo = AssetDatabase.LoadAssetAtPath<Texture2D>($"{mainFolderPath}{path}infoIcon.png");
            iconSettings = AssetDatabase.LoadAssetAtPath<Texture2D>($"{mainFolderPath}{path}settingsIcon.png");
            iconTerrainActive = AssetDatabase.LoadAssetAtPath<Texture2D>($"{mainFolderPath}{path}terrainIcon_active.png");
            iconInfoActive = AssetDatabase.LoadAssetAtPath<Texture2D>($"{mainFolderPath}{path}infoIcon_active.png");
            iconSettingsActive = AssetDatabase.LoadAssetAtPath<Texture2D>($"{mainFolderPath}{path}settingsIcon_active.png");
            iconReverse = AssetDatabase.LoadAssetAtPath<Texture2D>($"{mainFolderPath}{path}reverseIcon.png");
            iconFlatten = AssetDatabase.LoadAssetAtPath<Texture2D>($"{mainFolderPath}{path}flattenIcon.png");
            iconSplit = AssetDatabase.LoadAssetAtPath<Texture2D>($"{mainFolderPath}{path}splitIcon.png");
            iconLink = AssetDatabase.LoadAssetAtPath<Texture2D>($"{mainFolderPath}{path}linkIcon.png");
            iconUnlink = AssetDatabase.LoadAssetAtPath<Texture2D>($"{mainFolderPath}{path}unlinkIcon.png");
            iconAlignGrid = AssetDatabase.LoadAssetAtPath<Texture2D>($"{mainFolderPath}{path}alignGridIcon.png");
            iconAlign = AssetDatabase.LoadAssetAtPath<Texture2D>($"{mainFolderPath}{path}alignIcon.png");
            iconX = AssetDatabase.LoadAssetAtPath<Texture2D>($"{mainFolderPath}{path}xIcon.png");
            iconExternalLink = AssetDatabase.LoadAssetAtPath<Texture2D>($"{mainFolderPath}{path}externalLinkIcon.png");
            iconCenterGrid = AssetDatabase.LoadAssetAtPath<Texture2D>($"{mainFolderPath}{path}centerGridIcon.png");
            iconDefault = AssetDatabase.LoadAssetAtPath<Texture2D>($"{mainFolderPath}{path}defaultIcon.png");
            slider = AssetDatabase.LoadAssetAtPath<Texture2D>($"{mainFolderPath}{path}slider.png");
            iconUpArrow = AssetDatabase.LoadAssetAtPath<Texture2D>($"{mainFolderPath}{path}upArrowIcon.png");
            iconDownArrow = AssetDatabase.LoadAssetAtPath<Texture2D>($"{mainFolderPath}{path}downArrowIcon.png");
            iconExport = AssetDatabase.LoadAssetAtPath<Texture2D>($"{mainFolderPath}{path}exportIcon.png");
            iconMinimize = AssetDatabase.LoadAssetAtPath<Texture2D>($"{mainFolderPath}{path}minimizeIcon.png");
            iconCopy = AssetDatabase.LoadAssetAtPath<Texture2D>($"{mainFolderPath}{path}copyIcon.png");
            iconPaste = AssetDatabase.LoadAssetAtPath<Texture2D>($"{mainFolderPath}{path}pasteIcon.png");
            buttonDefault = AssetDatabase.LoadAssetAtPath<Texture2D>($"{mainFolderPath}{path}defaultButton9s.png");
            buttonDefaultActive = AssetDatabase.LoadAssetAtPath<Texture2D>($"{mainFolderPath}{path}defaultButton_active9s.png");
            buttonDefaultPressed = AssetDatabase.LoadAssetAtPath<Texture2D>($"{mainFolderPath}{path}defaultButton_pressed9s.png");
            buttonSubMenu = AssetDatabase.LoadAssetAtPath<Texture2D>($"{mainFolderPath}{path}subMenuButton9s.png");
            buttonSubMenuActive = AssetDatabase.LoadAssetAtPath<Texture2D>($"{mainFolderPath}{path}subMenuButton_active9s.png");
            buttonSubMenuPressed = AssetDatabase.LoadAssetAtPath<Texture2D>($"{mainFolderPath}{path}subMenuButton_pressed9s.png");
            inputField = AssetDatabase.LoadAssetAtPath<Texture2D>($"{mainFolderPath}{path}inputField9s.png");
            inputFieldActive = AssetDatabase.LoadAssetAtPath<Texture2D>($"{mainFolderPath}{path}inputField_active9s.png");
            popUpField = AssetDatabase.LoadAssetAtPath<Texture2D>($"{mainFolderPath}{path}popUpField9s.png");
            buttonDefaultRed = AssetDatabase.LoadAssetAtPath<Texture2D>($"{mainFolderPath}{path}defaultButtonRed9s.png");
            buttonDefaultRedPressed = AssetDatabase.LoadAssetAtPath<Texture2D>($"{mainFolderPath}{path}defaultButtonRed_pressed9s.png");
            buttonDefaultGreen = AssetDatabase.LoadAssetAtPath<Texture2D>($"{mainFolderPath}{path}defaultButtonGreen9s.png");
            buttonDefaultGreenPressed = AssetDatabase.LoadAssetAtPath<Texture2D>($"{mainFolderPath}{path}defaultButtonGreen_pressed9s.png");
            iconAlignTangents = AssetDatabase.LoadAssetAtPath<Texture2D>($"{mainFolderPath}{path}alignTangentsIcon.png");
            iconMagnet = AssetDatabase.LoadAssetAtPath<Texture2D>($"{mainFolderPath}{path}magnetIcon.png");
            iconMagnetActive = AssetDatabase.LoadAssetAtPath<Texture2D>($"{mainFolderPath}{path}magnetIcon_active.png");
            iconMagnetActive2 = AssetDatabase.LoadAssetAtPath<Texture2D>($"{mainFolderPath}{path}magnetIcon_active2.png");
        }
    }
}
