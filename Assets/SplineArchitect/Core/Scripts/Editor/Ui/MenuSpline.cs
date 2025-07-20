// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: MenuSpline.cs
//
// Author: Mikael Danielsson
// Date Created: 15-10-2023
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

using UnityEditor;
using UnityEngine;

using SplineArchitect.Objects;
using SplineArchitect.Libraries;
using SplineArchitect.Utility;

namespace SplineArchitect.Ui
{
    public class MenuSpline
    {
        //Settings
        const int maxNoisesLayers = 16;

        private static GUIContent guiContentContainer = new GUIContent("Spline");
        private static Rect splineWindowRect = new Rect(150, 100, 150, 0);

        private static string containerNormalResolution = "";
        private static string splineResolution = "";
        private static string containerRed = "";
        private static string containerGreen = "";
        private static string containerBlue = "";

        private static string[] containerNoiseScaleX = new string[maxNoisesLayers];
        private static string[] containerNoiseScaleY = new string[maxNoisesLayers];
        private static string[] containerNoiseScaleZ = new string[maxNoisesLayers];
        private static string[] containerNoiseSeed = new string[maxNoisesLayers];
        private static string[] containerOctaves = new string[maxNoisesLayers];
        private static string[] containerFrequency = new string[maxNoisesLayers];
        private static string[] containerAmplitude = new string[maxNoisesLayers];

        private static string[] optionsNormalType = new string[] { "Static", "Dynamic" };
        private static string[] optionsComponentMode = new string[] { "Remove from build", "Inactive", "Active" };
        private static string[] optionsDeformedMeshMode = new string[]{ "Save in build", "Save in scene", "Generate", "Do nothing" };
        private static string[] optionsDeformedMeshModePrefab = new string[] { "Generate" };

        //Addons
        private static List<(string, Action<Spline>)> addonsDrawWindow = new();
        private static List<(string, GUIContent, GUIContent)> addonsButtons = new();
        private static List<(string, Func<Spline, Rect>)> addonsCalcWindowSize = new();

        public static void OnSceneGUI(Spline spline)
        {
            //Note: This should not be needed. Need to look in to it more later.
            if (spline == null)
                return;

            UpdateRect(spline);
            GUILayout.Window(1, splineWindowRect, windowID => DrawWindow(windowID, spline), "", LibraryGUIStyle.empty);
        }

        private static void DrawWindow(int windowID, Spline spline)
        {

            #region Top
            if (spline == null)
                return;

            EHandleUi.ResetGetBackgroundStyleId();

            //Selected border
            if (EHandleSelection.selectedSplineObject != null || spline.selectedControlPoint != 0) GUILayout.Box("", LibraryGUIStyle.lineBlackThick, GUILayout.ExpandWidth(true));
            else GUILayout.Box("", LibraryGUIStyle.lineYellowThick, GUILayout.ExpandWidth(true));

            //Header
            GUILayout.BeginHorizontal(LibraryGUIStyle.backgroundHeader);
            if(EHandleSelection.selectedSplines.Count == 0)
                EHandleUi.CreateLabelField($"<b>{spline.name}</b> ", LibraryGUIStyle.textHeaderBlack, true);
            else
                EHandleUi.CreateLabelField($"<b>{spline.name} + ({EHandleSelection.selectedSplines.Count})</b> ", LibraryGUIStyle.textHeaderBlack, true);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal(LibraryGUIStyle.backgroundToolbar);

            //Toggle loop
            EHandleUi.CreateButtonToggle(EHandleUi.ButtonType.DEFAULT, LibraryGUIContent.iconLoop, LibraryGUIContent.iconLoopActive, 35, 19, () =>
            {
                EHandleSelection.UpdatedSelectedSplinesRecordUndo((selected) =>
                {
                    selected.loop = !selected.loop;
                    EHandleSpline.EnableDisableLoop(selected, selected.loop);
                }, "Toggle loop");
            }, spline.loop);

            EHandleUi.CreateSeparator();

            //Reverse Control points
            EHandleUi.CreateButton(EHandleUi.ButtonType.DEFAULT, LibraryGUIContent.iconReverse, 35, 19, () =>
            {
                EHandleSelection.UpdatedSelectedSplinesRecordUndo((selected) =>
                {
                    selected.ReverseSegments();
                    selected.selectedAnchors.Clear();
                    selected.selectedControlPoint = 0;

                    foreach (Segment s in selected.segments)
                    {
                        s.linkTarget = Segment.LinkTarget.NONE;
                        if(s.splineConnector != null)
                        {
                            s.splineConnector.RemoveConnection(s);
                            s.splineConnector = null;
                        }
                    }

                }, "Reverse control points");

                EHandleTool.ActivatePositionToolForControlPoint(spline);
            });

            //Flatten Control points
            EHandleUi.CreateButton(EHandleUi.ButtonType.DEFAULT, LibraryGUIContent.iconFlatten, 35, 19, () =>
            {
                EHandleSelection.UpdatedSelectedSplinesRecordUndo((selected) =>
                {
                    EHandleSpline.FlattenControlPoints(selected);
                    EHandleSegment.LinkMovementAll(selected);
                }, "Flatten control points");

                EHandleTool.ActivatePositionToolForControlPoint(spline);
            });

            //Align Control points
            EHandleUi.CreateButton(EHandleUi.ButtonType.DEFAULT, LibraryGUIContent.iconAlign, 35, 19, () =>
            {
                EHandleUndo.RecordNow(spline, "Aligned control points");
                EHandleSpline.AlignSelectedSegments(spline);
            }, spline.selectedAnchors.Count > 0);

            //Select spline
            EHandleUi.CreateButton(EHandleUi.ButtonType.DEFAULT, LibraryGUIContent.iconSelectSpline, 35, 19, () =>
            {
                EHandleUndo.RecordNow(spline, "Selected Spline");
                Selection.objects = null;
                Selection.activeTransform = spline.transform;
                spline.selectedControlPoint = 0;
                spline.selectedAnchors.Clear();

                //Also need to record the transform becouse of transform.position change
                EHandleUndo.RecordNow(spline.transform, "Selected Spline");
                spline.TransformToCenter(out Vector3 dif);
                if (!GeneralUtility.IsZero(dif))
                    spline.monitor.MarkEditorDirty();

                EHandleSelection.ForceUpdate();
            });

            //Select all control points
            EHandleUi.CreateButton(EHandleUi.ButtonType.DEFAULT, LibraryGUIContent.iconSelectAll, 35, 19, () =>
            {
                if (EHandleSelection.selectedSplineObject)
                {
                    Selection.activeTransform = spline.transform;
                    EHandleSelection.selectedSplineObject = null;
                    EHandleSelection.selectedSplineObjects.Clear();

                    //This can only happen when a so is selected. Else we cant select a new spline.
                    EHandleSelection.stopNextUpdateSelection = true;
                }

                EHandleUndo.RecordNow(spline, "Selected all anchors");
                int totalAnchors = spline.segments.Count;
                int[] anchors = new int[totalAnchors - 1];

                for (int i = 0; i < totalAnchors - 1; i++)
                    anchors[i] = i * 3 + 1003;

                EHandleSelection.SelectSecondaryAnchors(spline, anchors);
                EHandleSelection.SelectPrimaryControlPoint(spline, 1000);

                EHandleSelection.ForceUpdate();
            });

            //Join selected splines
            EHandleUi.CreateButton(EHandleUi.ButtonType.DEFAULT, LibraryGUIContent.iconJoin, 35, 19, () => 
            {
                EHandleSpline.JoinSelection();
            }, EHandleSelection.selectedSplines.Count > 0);

            if (GlobalSettings.GetGridVisibility())
            {
                if (EHandleSelection.selectedSplines.Count > 0)
                {
                    //Align grids
                    EHandleUi.CreateButton(EHandleUi.ButtonType.DEFAULT, LibraryGUIContent.iconAlignGrid, 35, 19, () =>
                    {
                        EHandleSelection.UpdatedSelectedSplinesRecordUndo((spline2) =>
                        {
                            EHandleGrid.AlignGrid(spline, spline2);
                        }, "Aligned grids");
                    });
                }
                else
                {
                    //Center grids
                    EHandleUi.CreateButton(EHandleUi.ButtonType.DEFAULT, LibraryGUIContent.iconCenterGrid, 35, 19, () =>
                    {
                        EHandleSelection.UpdatedSelectedSplinesRecordUndo((ac2) =>
                        {
                            EHandleGrid.GridToCenter(spline);
                        }, "Centered grid");
                    });
                }
            }

            GUILayout.EndHorizontal();
            #endregion

            #region Deformation
            // SPLINE MENU DEFORMATION
            if (spline.selectedSplineMenu == "deformation")
            {
                GUILayout.BeginHorizontal(LibraryGUIStyle.backgroundSubHeader);
                EHandleUi.CreateLabelField("<b>DEFORMATION</b>", LibraryGUIStyle.textSubHeader, true);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal(EHandleUi.GetBackgroundStyle());
                EHandleUi.CreateInfoMessageIcon(LibraryGUIContent.infoMsgSplineComponentMode);
                EHandleUi.CreatePopupField("Component:", 110, (int)spline.componentMode, optionsComponentMode, (int newValue) =>
                {
                    EHandleSelection.UpdatedSelectedSplinesRecordUndo((selected) =>
                    {
                        selected.componentMode = (ComponentMode)newValue;
                        if (selected.componentMode == ComponentMode.REMOVE_FROM_BUILD)
                        {
                            foreach(SplineObject so in spline.splineObjects)
                            {
                                EHandleUndo.RecordNow(so);
                                so.componentMode = ComponentMode.REMOVE_FROM_BUILD;
                            }
                        }
                        else if (selected.componentMode == ComponentMode.INACTIVE)
                        {
                            foreach (SplineObject so in spline.splineObjects)
                            {
                                EHandleUndo.RecordNow(so);
                                if((selected.deformationMode == DeformationMode.GENERATE || selected.deformationMode == DeformationMode.DO_NOTHING) &&
                                    so.type == SplineObject.Type.DEFORMATION)
                                {
                                    so.componentMode = ComponentMode.INACTIVE;
                                }
                                else
                                    so.componentMode = ComponentMode.REMOVE_FROM_BUILD;
                            }
                        }

                        EditorUtility.SetDirty(selected);
                    }, "Change component mode");
                }, -1, true);
                GUILayout.EndHorizontal();

                if (spline.deformations > 0)
                {
                    GUILayout.BeginHorizontal(EHandleUi.GetBackgroundStyle());

                    if (EHandlePrefab.IsPrefabStageActive() || EHandlePrefab.IsPartOfAnyPrefab(spline.gameObject))
                    {
                        EHandleUi.CreateInfoMessageIcon(LibraryGUIContent.infoMsgSplineDeformationModePrefab);
                        EHandleUi.CreatePopupField("Deformations:", 110, 0, optionsDeformedMeshModePrefab, (int newValue) =>
                        {
                        }, -1, true, false);
                    }
                    else
                    {
                        if (spline.deformationMode == DeformationMode.GENERATE && spline.componentMode == ComponentMode.REMOVE_FROM_BUILD)
                            EHandleUi.CreateErrorWarningMessageIcon(LibraryGUIContent.errorMsgSplineGenerateDeformationsRuntime);
                        else
                            EHandleUi.CreateInfoMessageIcon(LibraryGUIContent.infoMsgSplineDeformationMode);
                        EHandleUi.CreatePopupField("Deformations:", 110, (int)spline.deformationMode, optionsDeformedMeshMode, (int newValue) =>
                        {
                            EHandleSelection.UpdatedSelectedSplinesRecordUndo((selected) =>
                            {
                                selected.deformationMode = (DeformationMode)newValue;
                                if (selected.componentMode != ComponentMode.REMOVE_FROM_BUILD && selected.deformationMode == DeformationMode.GENERATE)
                                {
                                    foreach (SplineObject so in spline.splineObjects)
                                    {
                                        EHandleUndo.RecordNow(so);
                                        if (so.type == SplineObject.Type.DEFORMATION && so.componentMode == ComponentMode.REMOVE_FROM_BUILD)
                                            so.componentMode = ComponentMode.INACTIVE;
                                    }
                                }

                                EditorUtility.SetDirty(selected);
                            }, "Change deformations mode");
                        }, -1, true);
                    }
                    GUILayout.EndHorizontal();
                }

                GUILayout.BeginHorizontal(EHandleUi.GetBackgroundStyle());
                EHandleUi.CreateInfoMessageIcon(LibraryGUIContent.infoMsgDeformationType);
                EHandleUi.CreatePopupField("Spline type:", 110, (int)spline.normalType, optionsNormalType, (int newValue) =>
                {
                    EHandleSelection.UpdatedSelectedSplinesRecordUndo((selected) =>
                    {
                        selected.normalType = (Spline.NormalType)newValue;
                        EditorUtility.SetDirty(selected);
                        EHandleSegment.UpdateLoopEndData(spline, spline.segments[0]);
                    }, "Change deformation type");
                }, -1, true);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal(EHandleUi.GetBackgroundStyle());
                EHandleUi.CreatePopupField("Noise group:", 110, (int)spline.noiseGroupMesh, EHandleUi.optionsNoiseGroupsAndNone, (int newValue) =>
                {
                    EHandleSelection.UpdatedSelectedSplinesRecordUndo((selected) =>
                    {
                        selected.noiseGroupMesh = (NoiseLayer.Group)newValue;
                        EditorUtility.SetDirty(selected);
                    }, "Change noise group");
                }, -1, true);
                GUILayout.EndHorizontal();

                if (spline.normalType == Spline.NormalType.DYNAMIC)
                {
                    //Normal resolution
                    GUILayout.BeginHorizontal(EHandleUi.GetBackgroundStyle());
                    EHandleUi.CreateInfoMessageIcon(LibraryGUIContent.infoMsgNormalResolution);

                    EHandleUi.CreateSliderAndInputField("Normal resolution:", spline.GetResolutionNormal(true), ref containerNormalResolution, (newValue) => {
                        EHandleSelection.UpdatedSelectedSplinesRecordUndo((selected) =>
                        {
                            selected.SetResolutionNormal(Mathf.Round(newValue));
                            EditorUtility.SetDirty(selected);
                        }, "Changed normal resolution");
                    }, 100, 5000, 70, 38, 0, true);
                    GUILayout.EndHorizontal();
                }

                //Spline resolution
                GUILayout.BeginHorizontal(EHandleUi.GetBackgroundStyle());
                EHandleUi.CreateInfoMessageIcon(LibraryGUIContent.infoMsgSplineResolution);
                EHandleUi.CreateSliderAndInputField("Spline resolution:", spline.GetResolutionSpline(true), ref splineResolution, (newValue) => {
                    EHandleSelection.UpdatedSelectedSplinesRecordUndo((selected) =>
                    {
                        selected.SetResolutionSplineData(Mathf.Round(newValue));
                        EditorUtility.SetDirty(spline);
                    }, "Changed spline resolution");
                }, 10, 5000, 70, 38, 0, true);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal(EHandleUi.GetBackgroundStyle());
                EHandleUi.CreateToggleField("Cache positions:", spline.calculateLocalPositions, (newValue) =>
                {
                    EHandleSelection.UpdatedSelectedSplinesRecordUndo((selected) =>
                    {
                        selected.calculateLocalPositions = newValue;
                    }, "Toggle Cache positions");
                }, true, true, 103);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal(EHandleUi.GetBackgroundStyle());
                EHandleUi.CreateSliderAndInputField("R", spline.color.r, ref containerRed, (newValue) => {
                    EHandleSelection.UpdatedSelectedSplinesRecordUndo((selected) =>
                    {
                        selected.color.r = newValue;
                        selected.color.a = 1;
                        EditorUtility.SetDirty(selected);
                    }, "Changed spline color"); 
                }, 0, 1, 30, 32, 0, true);

                EHandleUi.CreateSliderAndInputField("G", spline.color.g, ref containerGreen, (newValue) => {
                    EHandleSelection.UpdatedSelectedSplinesRecordUndo((selected) =>
                    {
                        selected.color.g = newValue;
                        selected.color.a = 1;
                        EditorUtility.SetDirty(selected);
                    }, "Changed spline color");
                }, 0, 1, 30, 32, 0, true);

                EHandleUi.CreateSliderAndInputField("B", spline.color.b, ref containerBlue, (newValue) => {
                    EHandleSelection.UpdatedSelectedSplinesRecordUndo((selected) =>
                    {
                        selected.color.b = newValue;
                        selected.color.a = 1;
                        EditorUtility.SetDirty(selected);
                    }, "Changed spline color");
                }, 0, 1, 30, 32, 0, true);
                GUILayout.EndHorizontal();
            }
            #endregion

            #region Noise
            // SPLINE MENU NOISE
            else if (spline.selectedSplineMenu == "noise")
            {
                GUILayout.BeginHorizontal(LibraryGUIStyle.backgroundSubHeader);
                EHandleUi.CreateLabelField("<b>NOISE EFFECTS</b>", LibraryGUIStyle.textSubHeader, true);
                GUILayout.EndHorizontal();

                for (int i = 0; i < spline.noises.Count; i++)
                {
                    NoiseLayer noise = spline.noises[i];

                    GUIStyle backgroundStyleHeader = EHandleUi.GetBackgroundStyle();

                    if (noise.selected) backgroundStyleHeader = LibraryGUIStyle.backgroundSelectedLayerHeader;

                    GUILayout.BeginHorizontal(backgroundStyleHeader);

                    string text = EConversionUtility.CapitalizeString($"{noise.type}");
                    string text2 = EConversionUtility.CapitalizeString($"{noise.group}");
                    EHandleUi.CreateLabelField($"{i + 1} {text} ({text2})", noise.selected ? LibraryGUIStyle.textDefaultBlack : LibraryGUIStyle.textDefault, true);

                    EHandleUi.CreateEmpty(12);

                    //Move down
                    EHandleUi.CreateButton(EHandleUi.ButtonType.DEFAULT, LibraryGUIContent.iconDownArrow, 22, 19, () =>
                    {
                        EHandleUndo.RecordNow(spline, "Moved noise layer down");
                        NoiseLayer noiseA = spline.noises[i];
                        NoiseLayer noiseB = spline.noises[i + 1];
                        spline.noises[i] = noiseB;
                        spline.noises[i + 1] = noiseA;
                    }, i < (spline.noises.Count - 1) && spline.noises.Count > 1);

                    //Move up
                    EHandleUi.CreateButton(EHandleUi.ButtonType.DEFAULT, LibraryGUIContent.iconUpArrow, 22, 19, () =>
                    {
                        EHandleUndo.RecordNow(spline, "Moved noise layer up");
                        NoiseLayer noiseA = spline.noises[i];
                        NoiseLayer noiseB = spline.noises[i - 1];
                        spline.noises[i] = noiseB;
                        spline.noises[i - 1] = noiseA;
                    }, i > 0 && spline.noises.Count > 1);

                    EHandleUi.CreateButton(EHandleUi.ButtonType.DEFAULT_RED, LibraryGUIContent.iconX, 22, 19, () =>
                    {
                        EHandleSelection.UpdatedSelectedSplinesRecordUndo((selected) =>
                        {
                            if (selected.noises.Count > i)
                                selected.RemoveNoise(i);
                        }, "Removed noise effect");
                    });

                    EHandleUi.CreateEmpty(11);

                    EHandleUi.CreateButton(EHandleUi.ButtonType.DEFAULT_GREEN, noise.selected ? LibraryGUIContent.iconMinimize : LibraryGUIContent.iconSelectLayer, 22, 19, () =>
                    {
                        EHandleSelection.UpdatedSelectedSplinesRecordUndo((selected) =>
                        {
                            if (selected.noises.Count > i)
                            {
                                bool value = !selected.noises[i].selected;
                                selected.DeselectAllNoiseLayers();

                                NoiseLayer nl = selected.noises[i];
                                nl.selected = value;
                                selected.noises[i] = nl;
                            }
                        }, "Selected noise layer.");
                    });

                    EHandleUi.CreateToggleField("", noise.enabled, (newValue) =>
                    {
                        EHandleSelection.UpdatedSelectedSplinesRecordUndo((selected) =>
                        {
                            if (selected.noises.Count > i)
                            {
                                NoiseLayer noise12 = selected.noises[i];
                                noise12.enabled = newValue;
                                selected.noises[i] = noise12;
                            }
                        }, "Toggled layer");
                    }, true, true, 0, 16);

                    GUILayout.EndHorizontal();

                    if (!noise.selected)
                        continue;

                    GUIStyle backgroundStyle = LibraryGUIStyle.backgroundSelectedLayer;

                    EHandleUi.CreateBlackLine();

                    GUILayout.BeginHorizontal(backgroundStyle);
                    EHandleUi.CreateEmpty(18);
                    EHandleUi.CreatePopupField("Noise:", 85, (int)noise.type, EHandleUi.optionsNoiseType, (int newValue) =>
                    {
                        EHandleSelection.UpdatedSelectedSplinesRecordUndo((selected) =>
                        {
                            NoiseLayer noise3 = selected.noises[i];
                            noise3.type = (NoiseLayer.Type)newValue;
                            selected.noises[i] = noise3;
                        }, "Changed noise type");
                    }, 46, true, true, false);


                    EHandleUi.CreateEmpty(2);
                    EHandleUi.CreatePopupField("Group:", 85, (int)noise.group - 1, EHandleUi.optionsNoiseGroups, (int newValue) =>
                    {
                        EHandleSelection.UpdatedSelectedSplinesRecordUndo((selected) =>
                        {
                            NoiseLayer noise0 = selected.noises[i];
                            noise0.group = (NoiseLayer.Group)(newValue + 1);
                            selected.noises[i] = noise0;
                        }, "Changed noise group");
                    }, 51, true, true, false);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal(backgroundStyle);
                    EHandleUi.CreateEmpty(21);
                    EHandleUi.CreateInputField("Seed:", noise.seed, ref containerNoiseSeed[i], (newValue) => {
                        EHandleSelection.UpdatedSelectedSplinesRecordUndo((selected) =>
                        {
                            if (selected.noises.Count > i)
                            {
                                NoiseLayer noise5 = selected.noises[i];
                                noise5.seed = newValue;
                                selected.noises[i] = noise5;
                            }
                        }, "Changed noise seed");
                    }, 32, 42, true);

                    EHandleUi.CreateEmpty(61);
                    EHandleUi.CreateXYZInputFields("Scale:", noise.scale, ref containerNoiseScaleX[i], ref containerNoiseScaleY[i], ref containerNoiseScaleZ[i], (newValue, dif) => {
                        EHandleSelection.UpdatedSelectedSplinesRecordUndo((selected) =>
                        {
                            if (selected.noises.Count > i)
                            {
                                NoiseLayer noise4 = selected.noises[i];
                                noise4.scale = newValue;
                                selected.noises[i] = noise4;
                            }
                        }, "Changed noise scale");
                    }, 32, 43, 3, false, false, false, true, false);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal(backgroundStyle);
                    bool amplitude = noise.type == NoiseLayer.Type.DOMAIN_WARPED_NOISE;
                    bool fullSettings = noise.type == NoiseLayer.Type.FMB_NOISE || noise.type == NoiseLayer.Type.HYBRID_MULTI_FRACTAL || noise.type == NoiseLayer.Type.RIDGED_PERLIN_NOISE;

                    GUI.enabled = fullSettings || amplitude;

                    EHandleUi.CreateInputField("Amplitude:", noise.amplitude, ref containerAmplitude[i], (newValue) => {
                        EHandleSelection.UpdatedSelectedSplinesRecordUndo((selected) =>
                        {
                            if (selected.noises.Count > i)
                            {
                                NoiseLayer noise6 = selected.noises[i];
                                noise6.amplitude = newValue;
                                selected.noises[i] = noise6;
                            }
                        }, "Changed noise amplitude");
                    }, 32, 71, true);

                    EHandleUi.CreateEmpty(31);

                    GUI.enabled = fullSettings;

                    EHandleUi.CreateInputField("Frequency:", noise.frequency, ref containerFrequency[i], (newValue) => {
                        EHandleSelection.UpdatedSelectedSplinesRecordUndo((selected) =>
                        {
                            if (selected.noises.Count > i)
                            {
                                NoiseLayer noise7 = selected.noises[i];
                                noise7.frequency = newValue;
                                selected.noises[i] = noise7;
                            }
                        }, "Changed noise frequency");
                    }, 32, 75, true);

                    EHandleUi.CreateEmpty(41);

                    EHandleUi.CreateInputField("Octaves:", noise.octaves, ref containerOctaves[i], (newValue) => {
                        EHandleSelection.UpdatedSelectedSplinesRecordUndo((selected) =>
                        {
                            if (selected.noises.Count > i)
                            {
                                NoiseLayer noise8 = selected.noises[i];
                                noise8.octaves = Mathf.RoundToInt(newValue);
                                selected.noises[i] = noise8;
                            }
                        }, "Changed noise octaves");
                    }, 32, 62, true);
                    GUI.enabled = true;
                    GUILayout.EndHorizontal();

                    EHandleUi.CreateBlackLine();
                }

                GUILayout.BeginHorizontal(LibraryGUIStyle.backgroundMainLayerButtons);

                //Remove all
                GUILayout.FlexibleSpace();
                EHandleUi.CreateButton(EHandleUi.ButtonType.DEFAULT, LibraryGUIContent.textDeleteLayers, 84, 18, () =>
                {
                    EHandleSelection.UpdatedSelectedSplinesRecordUndo((selected) =>
                    {
                        selected.noises.Clear();
                    }, "Removed all noise layer");
                }, spline.noises.Count > 0);

                //Create layer
                EHandleUi.CreateButton(EHandleUi.ButtonType.DEFAULT, LibraryGUIContent.textCreateLayer, 80, 18, () =>
                {
                    EHandleSelection.UpdatedSelectedSplinesRecordUndo((selected) =>
                    {
                        selected.CreateNoise(NoiseLayer.Type.PERLIN_NOISE, Vector3.one, 0);

                        selected.DeselectAllNoiseLayers();

                        NoiseLayer nl = selected.noises[selected.noises.Count - 1];
                        nl.selected = true;
                        selected.noises[selected.noises.Count - 1] = nl;
                    }, "Created noise");
                }, spline.noises.Count < maxNoisesLayers);

                GUILayout.EndHorizontal();
            }
            #endregion

            #region Info
            // SPLINE MENU INFO
            else if (spline.selectedSplineMenu == "info")
            {
                float splineData = EHandleSpline.GetSplineMemoryUsage(spline);
                float componentData = EHandleSpline.GetComponentMemoryUsage(spline);
                float deformationData = spline.deformationsMemoryUsage;
                float length = spline.length;
                float vertecies = spline.vertecies;
                float deformations = spline.deformations;
                float followers = spline.followers;
                float deformationsInBuild = spline.deformationsInBuild;
                float followersInBuild = spline.followersInBuild;

                foreach (Spline spline2 in EHandleSelection.selectedSplines)
                {
                    splineData += EHandleSpline.GetSplineMemoryUsage(spline2);
                    componentData = EHandleSpline.GetComponentMemoryUsage(spline2);
                    deformationData += spline2.deformationsMemoryUsage;
                    length += spline2.length;
                    vertecies += spline2.vertecies;
                    deformations += spline2.deformations;
                    followers += spline2.followers;
                    deformationsInBuild = spline.deformationsInBuild;
                    followersInBuild = spline.followersInBuild;
                }

                GUILayout.BeginHorizontal(LibraryGUIStyle.backgroundSubHeader);
                EHandleUi.CreateLabelField("<b>INFO</b>", LibraryGUIStyle.textSubHeader, true);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal(EHandleUi.GetBackgroundStyle());
                EHandleUi.CreateInfoMessageIcon(LibraryGUIContent.infoMsgSplineData);
                EHandleUi.CreateLabelField("Spline data: " + EHandleSpline.GetMemorySizeFormat(splineData), LibraryGUIStyle.textDefault, true);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal(EHandleUi.GetBackgroundStyle());
                EHandleUi.CreateInfoMessageIcon(LibraryGUIContent.infoMsgComponentData);
                EHandleUi.CreateLabelField("Component data: " + EHandleSpline.GetMemorySizeFormat(componentData), LibraryGUIStyle.textDefault, true);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal(EHandleUi.GetBackgroundStyle());
                EHandleUi.CreateInfoMessageIcon(LibraryGUIContent.infoMsgMeshData);
                if (spline.deformationMode == DeformationMode.GENERATE)
                    EHandleUi.CreateLabelField($"Mesh data: {EHandleSpline.GetMemorySizeFormat(deformationData)} (disk 0 byte)", LibraryGUIStyle.textDefault, true);
                else
                    EHandleUi.CreateLabelField($"Mesh data: {EHandleSpline.GetMemorySizeFormat(deformationData)}", LibraryGUIStyle.textDefault, true);
                GUILayout.EndHorizontal();

                EHandleUi.CreateLabelField("Length: " + length.ToString(), LibraryGUIStyle.textDefault);
                EHandleUi.CreateLabelField("Vertecies: " + vertecies.ToString(), LibraryGUIStyle.textDefault);
                EHandleUi.CreateLabelField($"Deformations: {deformations} ({deformationsInBuild} in build)", LibraryGUIStyle.textDefault);
                EHandleUi.CreateLabelField($"Followers: {followers} ({followersInBuild} in build)", LibraryGUIStyle.textDefault);
            }
            #endregion

            #region addons
            else
            {
                bool foundAddon = false;

                for (int i = 0; i < addonsDrawWindow.Count; i++)
                {
                    if (addonsDrawWindow[i].Item1 == spline.selectedSplineMenu)
                    {
                        foundAddon = true;
                        addonsDrawWindow[i].Item2.Invoke(spline);
                    }
                }

                if(foundAddon == false)
                    spline.selectedSplineMenu = "deformation";
            }
            #endregion

            #region Bottom
            //Bottom
            GUILayout.Box("", LibraryGUIStyle.lineGrey, GUILayout.ExpandWidth(true));

            GUILayout.BeginHorizontal(LibraryGUIStyle.backgroundBottomMenu);

            //Deformation
            EHandleUi.CreateButtonToggle(EHandleUi.ButtonType.SUB_MENU, LibraryGUIContent.iconSpline, LibraryGUIContent.iconSplineActive, 35, 18, () => 
            {
                EHandleUndo.RecordNow(spline, "Changed sub menu");
                spline.selectedSplineMenu = "deformation";
            }, spline.selectedSplineMenu == "deformation");

            //Addons
            for (int i = 0; i < addonsButtons.Count; i++)
            {
                EHandleUi.CreateButtonToggle(EHandleUi.ButtonType.SUB_MENU, addonsButtons[i].Item2, addonsButtons[i].Item3, 35, 18, () =>
                {
                    EHandleUndo.RecordNow(spline, "Changed sub menu");
                    spline.selectedSplineMenu = addonsButtons[i].Item1;
                }, spline.selectedSplineMenu == addonsButtons[i].Item1);
            }

            //Noise
            EHandleUi.CreateButtonToggle(EHandleUi.ButtonType.SUB_MENU, LibraryGUIContent.iconNoise, LibraryGUIContent.iconNoiseActive, 35, 18, () =>
            {
                EHandleUndo.RecordNow(spline, "Changed sub menu");
                spline.selectedSplineMenu = "noise";
            }, spline.selectedSplineMenu == "noise");

            //Info
            EHandleUi.CreateButtonToggle(EHandleUi.ButtonType.SUB_MENU, LibraryGUIContent.iconInfo, LibraryGUIContent.iconInfoActive, 35, 18, () =>
            {
                EHandleUndo.RecordNow(spline, "Changed sub menu");
                spline.selectedSplineMenu = "info";
            }, spline.selectedSplineMenu == "info");

            GUILayout.EndHorizontal();
            GUILayout.Box("", LibraryGUIStyle.lineGrey, GUILayout.ExpandWidth(true));
            #endregion
        }

        private static void UpdateRect(Spline spline)
        {
            //Deformation
            if (spline.selectedSplineMenu == "deformation")
            {
                splineWindowRect.height = LibraryGUIStyle.menuItemHeight * 10 + 16;
                splineWindowRect.width = 270;

                if (spline.normalType == Spline.NormalType.DYNAMIC)
                    splineWindowRect.height += LibraryGUIStyle.menuItemHeight * 1;

                if(spline.deformations == 0)
                    splineWindowRect.height -= LibraryGUIStyle.menuItemHeight * 1;
            }
            //Noise
            else if (spline.selectedSplineMenu == "noise")
            {
                splineWindowRect.height = LibraryGUIStyle.menuItemHeight * 4 + 16;
                foreach (NoiseLayer nl in spline.noises)
                {
                    if (!nl.selected) splineWindowRect.height += LibraryGUIStyle.menuItemHeight * 1;
                    else
                    {
                        splineWindowRect.height += 2;
                        splineWindowRect.height += LibraryGUIStyle.menuItemHeight * 4;
                    }
                }

                if(spline.noises.Count > 0)
                    splineWindowRect.width = 440;
                else
                    splineWindowRect.width = 271;
            }
            //Info
            else if (spline.selectedSplineMenu == "info")
            {
                splineWindowRect.height = LibraryGUIStyle.menuItemHeight * 10 + 16;
                splineWindowRect.width = 271;
            }
            //Addons
            else
            {
                for (int i = 0; i < addonsCalcWindowSize.Count; i++)
                {
                    if (spline.selectedSplineMenu == addonsCalcWindowSize[i].Item1)
                    {
                        Rect rect = addonsCalcWindowSize[i].Item2.Invoke(spline);
                        splineWindowRect.height = rect.height;
                        splineWindowRect.width = rect.width;
                        break;
                    }
                }
            }

            if (GlobalSettings.GetGridVisibility() && splineWindowRect.width < 308)
                splineWindowRect.width = 308;

            // Calculate the size of the label.
            guiContentContainer.text = spline.name;
            Vector2 labelSize = LibraryGUIStyle.textHeader.CalcSize(guiContentContainer);
            labelSize.x += LibraryGUIStyle.textHeader.padding.left + LibraryGUIStyle.textHeader.padding.right;
            if (labelSize.x > splineWindowRect.width)
                splineWindowRect.width = labelSize.x;

            Rect generalWindowRect = MenuGeneral.GetRect();

            splineWindowRect.x = generalWindowRect.x + generalWindowRect.width + 3;
            splineWindowRect.y = generalWindowRect.y - (splineWindowRect.height - generalWindowRect.height);
        }

        public static Rect GetRect()
        {
            return splineWindowRect;
        }

        public static void AddSubMenu(string id, GUIContent button, GUIContent buttonActive, Action<Spline> DrawWindow, Func<Spline, Rect> calcWindowSize)
        {
            for (int i = 0; i < addonsDrawWindow.Count; i++)
            {
                if (addonsDrawWindow[i].Item1 == id)
                {
                    addonsDrawWindow.RemoveAt(i);
                    addonsButtons.RemoveAt(i);
                    addonsCalcWindowSize.RemoveAt(i);
                }
            }

            addonsDrawWindow.Add((id, DrawWindow));
            addonsButtons.Add((id, button, buttonActive));
            addonsCalcWindowSize.Add((id, calcWindowSize));
        }
    }
}
