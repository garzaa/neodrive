// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: WindowControlpanel.cs
//
// Author: Mikael Danielsson
// Date Created: 05-08-2025
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

using UnityEditor;
using UnityEngine;

using SplineArchitect.Libraries;
using SplineArchitect.Objects;
using SplineArchitect.Utility;

namespace SplineArchitect.Ui
{
    public class WindowSpline : WindowBase
    {
        const int maxSplineNameLength = 25;
        const int maxNoisesLayers = 16;
        const float extendedMenuMargin = 3;

        private string windowTitle = "";
        private static GUIContent guiContentContainer = new GUIContent("Spline");

        private static string[] optionsNormalType = new string[] { "Static 3D", "Static 2D", "Dynamic" };
        private static string[] optionsComponentMode = new string[] { "Remove from build", "Inactive", "Active" };
        private static string[] optionsDeformedMeshMode = new string[] { "Save in build", "Save in scene", "Generate", "Do nothing" };
        private static string[] optionsDeformedMeshModePrefab = new string[] { "Generate" };

        //Addons
        private static List<(string, Action<Spline>)> addonsDrawWindow = new();
        private static List<(string, GUIContent, GUIContent)> addonsButtons = new();
        private static List<(string, Func<Spline, Rect>)> addonsCalcWindowSize = new();

        protected override void OnGUIExtended()
        {
            EUiUtility.ResetGetBackgroundStyleId();
            Spline spline = EHandleSelection.selectedSpline;

            if (spline == null)
            {
                GUILayout.BeginHorizontal(EUiUtility.GetBackgroundStyle());
                EUiUtility.CreateLabelField($"<b>No selected spline</b> ", LibraryGUIStyle.textDefault, true);
                GUILayout.EndHorizontal();

                return;
            }
            else if(GlobalSettings.GetControlPanelWindowMinimized())
            {
                //Minimize
                EUiUtility.CreateButton(ButtonType.SUB_MENU, LibraryGUIContent.iconMaximize, 25, 25, () =>
                {
                    EActionToSceneGUI.Add(() =>
                    {
                        GlobalSettings.SetControlPanelWindowMinimized(false);
                    }, EActionToSceneGUI.Type.LATE, EventType.Repaint);
                    EHandleSceneView.RepaintCurrent();
                });

                return;
            }

            #region Top
            GUILayout.BeginHorizontal(LibraryGUIStyle.backgroundHeader);

            //Title
            windowTitle = spline.name;
            if (windowTitle.Length > maxSplineNameLength) windowTitle = $"{windowTitle.Substring(0, maxSplineNameLength)}..";
            if (EHandleSelection.selectedSplines.Count > 0) windowTitle = $"{windowTitle} + ({EHandleSelection.selectedSplines.Count})"; 
            EUiUtility.CreateLabelField($"<b>{windowTitle}</b>", LibraryGUIStyle.textHeaderBlack, true);

            GUILayout.FlexibleSpace();
            //Minimize
            EUiUtility.CreateButton(ButtonType.DEFAULT, LibraryGUIContent.iconMinimize, 19, 14, () =>
            {
                EActionToSceneGUI.Add(() =>
                {
                    GlobalSettings.SetControlPanelWindowMinimized(true);
                }, EActionToSceneGUI.Type.LATE, EventType.Repaint);
                EHandleSceneView.RepaintCurrent();
            });
            //Close
            EUiUtility.CreateButton(ButtonType.DEFAULT, LibraryGUIContent.iconClose, 19, 14, () =>
            {
                EActionToSceneGUI.Add(() =>
                {
                    if (extendedWindow != null) extendedWindow.Close();
                    toolbarToggleBase.SetValueWithoutNotify(false);
                    Close();
                }, EActionToSceneGUI.Type.LATE, EventType.Repaint);
                EHandleSceneView.RepaintCurrent();
            });
            GUILayout.EndHorizontal();

            EUiUtility.CreateHorizontalBlackLine();

            //Toolbar
            GUILayout.BeginHorizontal(LibraryGUIStyle.backgroundToolbar);
            //Toggle loop
            EUiUtility.CreateButtonToggle(ButtonType.DEFAULT, LibraryGUIContent.iconLoop, LibraryGUIContent.iconLoopActive, 35, 19, () =>
            {
                EHandleSelection.UpdatedSelectedSplinesRecordUndo((selected) =>
                {
                    selected.loop = !selected.loop;
                    EHandleSpline.EnableDisableLoop(selected, selected.loop);
                }, "Toggle loop");
                EHandleSceneView.RepaintCurrent();
            }, spline.loop);

            EUiUtility.CreateSeparator();

            //Reverse Control points
            EUiUtility.CreateButton(ButtonType.DEFAULT, LibraryGUIContent.iconReverse, 35, 19, () =>
            {
                EHandleSelection.UpdatedSelectedSplinesRecordUndo((selected) =>
                {
                    selected.ReverseSegments();
                    selected.selectedAnchors.Clear();
                    selected.selectedControlPoint = 0;

                    foreach (Segment s in selected.segments)
                    {
                        s.linkTarget = Segment.LinkTarget.NONE;
                        if (s.splineConnector != null)
                        {
                            s.splineConnector.RemoveConnection(s);
                            s.splineConnector = null;
                        }
                    }

                }, "Reverse control points");

                EHandleTool.ActivatePositionToolForControlPoint(spline);
                EHandleSceneView.RepaintCurrent();
            });

            //Flatten Control points
            EUiUtility.CreateButton(ButtonType.DEFAULT, LibraryGUIContent.iconFlatten, 35, 19, () =>
            {
                EHandleSelection.UpdatedSelectedSplinesRecordUndo((selected) =>
                {
                    EHandleSpline.FlattenControlPoints(selected);
                    EHandleSegment.LinkMovementAll(selected);
                }, "Flatten control points");

                EHandleTool.ActivatePositionToolForControlPoint(spline);
                EHandleSceneView.RepaintCurrent();
            });

            //Align Control points
            EUiUtility.CreateButton(ButtonType.DEFAULT, LibraryGUIContent.iconAlign, 35, 19, () =>
            {
                EHandleUndo.RecordNow(spline, "Aligned control points");
                EHandleSpline.AlignSelectedSegments(spline);
                EHandleSceneView.RepaintCurrent();
            }, spline.selectedAnchors.Count > 0);

            //Select spline
            EUiUtility.CreateButton(ButtonType.DEFAULT, LibraryGUIContent.iconSelectSpline, 35, 19, () =>
            {
                EHandleUndo.RecordNow(spline, "Selected Spline");
                Selection.objects = null;
                Selection.activeTransform = spline.transform;
                spline.selectedControlPoint = 0;
                spline.selectedAnchors.Clear();

                //Also need to record the transform becouse of transform.screenPos change
                EHandleUndo.RecordNow(spline.transform, "Selected Spline");
                spline.TransformToCenter(out Vector3 dif);
                if (!GeneralUtility.IsZero(dif))
                    spline.monitor.MarkEditorDirty();

                EHandleSelection.ForceUpdate();
            });

            //Select all control points
            EUiUtility.CreateButton(ButtonType.DEFAULT, LibraryGUIContent.iconSelectAll, 35, 19, () =>
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
                EHandleSceneView.RepaintCurrent();
            });

            //Join selected splines
            EUiUtility.CreateButton(ButtonType.DEFAULT, LibraryGUIContent.iconJoin, 35, 19, () =>
            {
                EHandleSpline.JoinSelection();
                EHandleSceneView.RepaintCurrent();
            }, EHandleSelection.selectedSplines.Count > 0);

            if (GlobalSettings.GetGridVisibility())
            {
                if (EHandleSelection.selectedSplines.Count > 0)
                {
                    //Align grids
                    EUiUtility.CreateButton(ButtonType.DEFAULT, LibraryGUIContent.iconAlignGrid, 35, 19, () =>
                    {
                        EHandleSelection.UpdatedSelectedSplinesRecordUndo((spline2) =>
                        {
                            EHandleGrid.AlignGrid(spline, spline2);
                        }, "Aligned grids");
                        EHandleSceneView.RepaintCurrent();
                    });
                }
                else
                {
                    //Center grids
                    EUiUtility.CreateButton(ButtonType.DEFAULT, LibraryGUIContent.iconCenterGrid, 35, 19, () =>
                    {
                        EHandleSelection.UpdatedSelectedSplinesRecordUndo((ac2) =>
                        {
                            EHandleGrid.GridToCenter(spline);
                        }, "Centered grid");
                        EHandleSceneView.RepaintCurrent();
                    });
                }
            }
            GUILayout.EndHorizontal();

            EUiUtility.CreateHorizontalBlackLine();
            #endregion

            #region Deforamtion
            // SPLINE MENU DEFORMATION
            if (spline.selectedSplineMenu == "deformation")
            {
                GUILayout.BeginHorizontal(LibraryGUIStyle.backgroundSubHeader);
                EUiUtility.CreateLabelField("<b>DEFORMATION</b>", LibraryGUIStyle.textSubHeader, true);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal(EUiUtility.GetBackgroundStyle());
                EUiUtility.CreateInfoMessageIcon(LibraryGUIContent.infoMsgSplineComponentMode);
                EUiUtility.CreatePopupField("Component:", 110, (int)spline.componentMode, optionsComponentMode, (int newValue) =>
                {
                    EHandleSelection.UpdatedSelectedSplinesRecordUndo((selected) =>
                    {
                        selected.componentMode = (ComponentMode)newValue;
                        if (selected.componentMode == ComponentMode.REMOVE_FROM_BUILD)
                        {
                            foreach (SplineObject so in spline.splineObjects)
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
                                if ((selected.deformationMode == DeformationMode.GENERATE || selected.deformationMode == DeformationMode.DO_NOTHING) &&
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
                    GUILayout.BeginHorizontal(EUiUtility.GetBackgroundStyle());

                    if (EHandlePrefab.IsPrefabStageActive() || EHandlePrefab.IsPartOfAnyPrefab(spline.gameObject))
                    {
                        EUiUtility.CreateInfoMessageIcon(LibraryGUIContent.infoMsgSplineDeformationModePrefab);
                        EUiUtility.CreatePopupField("Deformations:", 110, 0, optionsDeformedMeshModePrefab, (int newValue) =>
                        {
                        }, -1, true, false);
                    }
                    else
                    {
                        if (spline.deformationMode == DeformationMode.GENERATE && spline.componentMode == ComponentMode.REMOVE_FROM_BUILD)
                            EUiUtility.CreateErrorWarningMessageIcon(LibraryGUIContent.errorMsgSplineGenerateDeformationsRuntime);
                        else
                            EUiUtility.CreateInfoMessageIcon(LibraryGUIContent.infoMsgSplineDeformationMode);
                        EUiUtility.CreatePopupField("Deformations:", 110, (int)spline.deformationMode, optionsDeformedMeshMode, (int newValue) =>
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

                GUILayout.BeginHorizontal(EUiUtility.GetBackgroundStyle());
                EUiUtility.CreateInfoMessageIcon(LibraryGUIContent.infoMsgDeformationType);
                EUiUtility.CreatePopupField("Spline type:", 110, (int)spline.normalType, optionsNormalType, (int newValue) =>
                {
                    EHandleSelection.UpdatedSelectedSplinesRecordUndo((selected) =>
                    {
                        selected.normalType = (Spline.NormalType)newValue;
                        EditorUtility.SetDirty(selected);
                        EHandleSegment.UpdateLoopEndData(spline, spline.segments[0]);
                    }, "Change deformation type");
                }, -1, true);
                GUILayout.EndHorizontal();

                //Spline color
                EUiUtility.CreateColorField("Spline color:", spline.color, (newColor) => 
                {
                    EHandleSelection.UpdatedSelectedSplinesRecordUndo((selected) =>
                    {
                        selected.color = newColor;
                        EditorUtility.SetDirty(selected);
                    }, "Changed spline color");
                }, 110);

                GUILayout.BeginHorizontal(EUiUtility.GetBackgroundStyle());
                EUiUtility.CreatePopupField("Noise group:", 110, (int)spline.noiseGroupMesh, EHandleUi.optionsNoiseGroupsAndNone, (int newValue) =>
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
                    GUILayout.BeginHorizontal(EUiUtility.GetBackgroundStyle());
                    EUiUtility.CreateInfoMessageIcon(LibraryGUIContent.infoMsgNormalResolution);

                    EUiUtility.CreateSliderAndInputField("Normal resolution:", spline.GetResolutionNormal(true), (newValue) => {
                        EHandleSelection.UpdatedSelectedSplinesRecordUndo((selected) =>
                        {
                            selected.SetResolutionNormal(Mathf.Round(newValue));
                            EditorUtility.SetDirty(selected);
                        }, "Changed normal resolution");
                    }, 100, 5000, 70, 38, 0, true);
                    GUILayout.EndHorizontal();
                }

                //Spline resolution
                GUILayout.BeginHorizontal(EUiUtility.GetBackgroundStyle());
                EUiUtility.CreateInfoMessageIcon(LibraryGUIContent.infoMsgSplineResolution);
                EUiUtility.CreateSliderAndInputField("Spline resolution:", spline.GetResolutionSpline(true), (newValue) => {
                    EHandleSelection.UpdatedSelectedSplinesRecordUndo((selected) =>
                    {
                        selected.SetResolutionSplineData(Mathf.Round(newValue));
                        EditorUtility.SetDirty(spline);
                    }, "Changed spline resolution");
                }, 10, 5000, 70, 38, 0, true);
                GUILayout.EndHorizontal();

                //Cache positions
                GUILayout.BeginHorizontal(EUiUtility.GetBackgroundStyle());
                EUiUtility.CreateToggleField("Cache positions:", spline.calculateLocalPositions, (newValue) =>
                {
                    EHandleSelection.UpdatedSelectedSplinesRecordUndo((selected) =>
                    {
                        selected.calculateLocalPositions = newValue;
                    }, "Toggle Cache positions");
                }, true, true, 103);
                GUILayout.EndHorizontal();
            }
            #endregion

            #region Noise
            // SPLINE MENU NOISE
            else if (spline.selectedSplineMenu == "noise")
            {
                GUILayout.BeginHorizontal(LibraryGUIStyle.backgroundSubHeader);
                EUiUtility.CreateLabelField("<b>NOISE EFFECTS</b>", LibraryGUIStyle.textSubHeader, true);
                GUILayout.EndHorizontal();

                for (int i = 0; i < spline.noises.Count; i++)
                {
                    NoiseLayer noise = spline.noises[i];

                    GUIStyle backgroundStyleHeader = EUiUtility.GetBackgroundStyle();

                    if (noise.selected) backgroundStyleHeader = LibraryGUIStyle.backgroundSelectedLayerHeader;

                    GUILayout.BeginHorizontal(backgroundStyleHeader);

                    string text = EConversionUtility.CapitalizeString($"{noise.type}");
                    string text2 = EConversionUtility.CapitalizeString($"{noise.group}");
                    EUiUtility.CreateLabelField($"{i + 1} {text} ({text2})", noise.selected ? LibraryGUIStyle.textDefaultBlack : LibraryGUIStyle.textDefault, true);

                    EUiUtility.CreateEmpty(12);

                    //Move down
                    EUiUtility.CreateButton(ButtonType.DEFAULT, LibraryGUIContent.iconDownArrow, 22, 19, () =>
                    {
                        EHandleUndo.RecordNow(spline, "Moved noise layer down");
                        NoiseLayer noiseA = spline.noises[i];
                        NoiseLayer noiseB = spline.noises[i + 1];
                        spline.noises[i] = noiseB;
                        spline.noises[i + 1] = noiseA;
                    }, i < (spline.noises.Count - 1) && spline.noises.Count > 1);

                    //Move up
                    EUiUtility.CreateButton(ButtonType.DEFAULT, LibraryGUIContent.iconUpArrow, 22, 19, () =>
                    {
                        EHandleUndo.RecordNow(spline, "Moved noise layer up");
                        NoiseLayer noiseA = spline.noises[i];
                        NoiseLayer noiseB = spline.noises[i - 1];
                        spline.noises[i] = noiseB;
                        spline.noises[i - 1] = noiseA;
                    }, i > 0 && spline.noises.Count > 1);

                    EUiUtility.CreateButton(ButtonType.DEFAULT_RED, LibraryGUIContent.iconRemove, 22, 19, () =>
                    {
                        EHandleSelection.UpdatedSelectedSplinesRecordUndo((selected) =>
                        {
                            if (selected.noises.Count > i)
                                selected.RemoveNoise(i);
                        }, "Removed noise effect");
                    });

                    EUiUtility.CreateEmpty(11);

                    EUiUtility.CreateButton(ButtonType.DEFAULT_GREEN, noise.selected ? LibraryGUIContent.iconMinimize : LibraryGUIContent.iconSelectLayer, 22, 19, () =>
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

                    EUiUtility.CreateToggleField("", noise.enabled, (newValue) =>
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

                    EUiUtility.CreateHorizontalBlackLine();

                    GUILayout.BeginHorizontal(backgroundStyle);
                    EUiUtility.CreateEmpty(18);
                    EUiUtility.CreatePopupField("Noise:", 85, (int)noise.type, EHandleUi.optionsNoiseType, (int newValue) =>
                    {
                        EHandleSelection.UpdatedSelectedSplinesRecordUndo((selected) =>
                        {
                            NoiseLayer noise3 = selected.noises[i];
                            noise3.type = (NoiseLayer.Type)newValue;
                            selected.noises[i] = noise3;
                        }, "Changed noise type");
                    }, 46, true, true, false);


                    EUiUtility.CreateEmpty(2);
                    EUiUtility.CreatePopupField("Group:", 85, (int)noise.group - 1, EHandleUi.optionsNoiseGroups, (int newValue) =>
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
                    EUiUtility.CreateEmpty(21);
                    EUiUtility.CreateFloatFieldWithLabel("Seed:", noise.seed, (newValue) => {
                        EHandleSelection.UpdatedSelectedSplinesRecordUndo((selected) =>
                        {
                            if (selected.noises.Count > i)
                            {
                                NoiseLayer noise5 = selected.noises[i];
                                noise5.seed = newValue;
                                selected.noises[i] = noise5;
                            }
                        }, "Changed noise seed");
                    }, 40, 42, true);

                    EUiUtility.CreateEmpty(46);
                    EUiUtility.CreateXYZInputFields("Scale:", noise.scale, (newValue, dif) => {
                        EHandleSelection.UpdatedSelectedSplinesRecordUndo((selected) =>
                        {
                            if (selected.noises.Count > i)
                            {
                                NoiseLayer noise4 = selected.noises[i];
                                noise4.scale = newValue;
                                selected.noises[i] = noise4;
                            }
                        }, "Changed noise scale");
                    }, 50, 10, 52, false, true);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal(backgroundStyle);
                    bool amplitude = noise.type == NoiseLayer.Type.DOMAIN_WARPED_NOISE;
                    bool fullSettings = noise.type == NoiseLayer.Type.FMB_NOISE || noise.type == NoiseLayer.Type.HYBRID_MULTI_FRACTAL || noise.type == NoiseLayer.Type.RIDGED_PERLIN_NOISE;

                    GUI.enabled = fullSettings || amplitude;

                    EUiUtility.CreateFloatFieldWithLabel("Amplitude:", noise.amplitude, (newValue) => {
                        EHandleSelection.UpdatedSelectedSplinesRecordUndo((selected) =>
                        {
                            if (selected.noises.Count > i)
                            {
                                NoiseLayer noise6 = selected.noises[i];
                                noise6.amplitude = newValue;
                                selected.noises[i] = noise6;
                            }
                        }, "Changed noise amplitude");
                    }, 40, 68, true);

                    EUiUtility.CreateEmpty(19);

                    GUI.enabled = fullSettings;

                    EUiUtility.CreateFloatFieldWithLabel("Frequency:", noise.frequency, (newValue) => {
                        EHandleSelection.UpdatedSelectedSplinesRecordUndo((selected) =>
                        {
                            if (selected.noises.Count > i)
                            {
                                NoiseLayer noise7 = selected.noises[i];
                                noise7.frequency = newValue;
                                selected.noises[i] = noise7;
                            }
                        }, "Changed noise frequency");
                    }, 40, 71, true);

                    EUiUtility.CreateEmpty(20);

                    EUiUtility.CreateFloatFieldWithLabel("Octaves:", noise.octaves, (newValue) => {
                        EHandleSelection.UpdatedSelectedSplinesRecordUndo((selected) =>
                        {
                            if (selected.noises.Count > i)
                            {
                                NoiseLayer noise8 = selected.noises[i];
                                noise8.octaves = Mathf.RoundToInt(newValue);
                                selected.noises[i] = noise8;
                            }
                        }, "Changed noise octaves");
                    }, 40, 58, true);
                    GUI.enabled = true;
                    GUILayout.EndHorizontal();

                    EUiUtility.CreateHorizontalBlackLine();
                }

                GUILayout.BeginHorizontal(LibraryGUIStyle.backgroundMainLayerButtons);

                //Remove all
                GUILayout.FlexibleSpace();
                EUiUtility.CreateButton(ButtonType.DEFAULT, LibraryGUIContent.textDeleteLayers, 84, 18, () =>
                {
                    EHandleSelection.UpdatedSelectedSplinesRecordUndo((selected) =>
                    {
                        selected.noises.Clear();
                    }, "Removed all noise layer");
                }, spline.noises.Count > 0);

                //Create layer
                EUiUtility.CreateButton(ButtonType.DEFAULT, LibraryGUIContent.textCreateLayer, 80, 18, () =>
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
                EUiUtility.CreateLabelField("<b>INFO</b>", LibraryGUIStyle.textSubHeader, true);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal(EUiUtility.GetBackgroundStyle());
                EUiUtility.CreateInfoMessageIcon(LibraryGUIContent.infoMsgSplineData);
                EUiUtility.CreateLabelField("Spline data: " + EHandleSpline.GetMemorySizeFormat(splineData), LibraryGUIStyle.textDefault, true);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal(EUiUtility.GetBackgroundStyle());
                EUiUtility.CreateInfoMessageIcon(LibraryGUIContent.infoMsgComponentData);
                EUiUtility.CreateLabelField("Component data: " + EHandleSpline.GetMemorySizeFormat(componentData), LibraryGUIStyle.textDefault, true);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal(EUiUtility.GetBackgroundStyle());
                EUiUtility.CreateInfoMessageIcon(LibraryGUIContent.infoMsgMeshData);
                if (spline.deformationMode == DeformationMode.GENERATE)
                    EUiUtility.CreateLabelField($"Mesh data: {EHandleSpline.GetMemorySizeFormat(deformationData)} (disk 0 byte)", LibraryGUIStyle.textDefault, true);
                else
                    EUiUtility.CreateLabelField($"Mesh data: {EHandleSpline.GetMemorySizeFormat(deformationData)}", LibraryGUIStyle.textDefault, true);
                GUILayout.EndHorizontal();

                EUiUtility.CreateLabelField("Length: " + length.ToString(), LibraryGUIStyle.textDefault);
                EUiUtility.CreateLabelField("Vertecies: " + vertecies.ToString(), LibraryGUIStyle.textDefault);
                EUiUtility.CreateLabelField($"Deformations: {deformations} ({deformationsInBuild} in build)", LibraryGUIStyle.textDefault);
                EUiUtility.CreateLabelField($"Followers: {followers} ({followersInBuild} in build)", LibraryGUIStyle.textDefault);
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

                if (foundAddon == false)
                    spline.selectedSplineMenu = "deformation";
            }
            #endregion

            #region Bottom
            EUiUtility.CreateHorizontalBlackLine();
            GUILayout.BeginHorizontal(LibraryGUIStyle.backgroundBottomMenu);

            //Deformation
            EUiUtility.CreateButtonToggle(ButtonType.SUB_MENU, LibraryGUIContent.iconCurve, LibraryGUIContent.iconCurveActive, 35, 18, () =>
            {
                EHandleUndo.RecordNow(spline, "Changed sub menu");
                spline.selectedSplineMenu = "deformation";
            }, spline.selectedSplineMenu == "deformation");

            //Addons
            for (int i = 0; i < addonsButtons.Count; i++)
            {
                EUiUtility.CreateButtonToggle(ButtonType.SUB_MENU, addonsButtons[i].Item2, addonsButtons[i].Item3, 35, 18, () =>
                {
                    EHandleUndo.RecordNow(spline, "Changed sub menu");
                    spline.selectedSplineMenu = addonsButtons[i].Item1;
                }, spline.selectedSplineMenu == addonsButtons[i].Item1);
            }

            //Noise
            EUiUtility.CreateButtonToggle(ButtonType.SUB_MENU, LibraryGUIContent.iconNoise, LibraryGUIContent.iconNoiseActive, 35, 18, () =>
            {
                EHandleUndo.RecordNow(spline, "Changed sub menu");
                spline.selectedSplineMenu = "noise";
            }, spline.selectedSplineMenu == "noise");

            //Info
            EUiUtility.CreateButtonToggle(ButtonType.SUB_MENU, LibraryGUIContent.iconInfo, LibraryGUIContent.iconInfoActive, 35, 18, () =>
            {
                EHandleUndo.RecordNow(spline, "Changed sub menu");
                spline.selectedSplineMenu = "info";
            }, spline.selectedSplineMenu == "info");

            GUILayout.EndHorizontal();
            #endregion
        }

        protected override void UpdateWindowSize()
        {
            Spline spline = EHandleSelection.selectedSpline;

            //Size when no spline is selected
            cachedRect.width = 136;
            cachedRect.height = menuItemHeight + 2;

            if (spline == null)
                return;

            if(GlobalSettings.GetControlPanelWindowMinimized())
            {
                cachedRect.width = 27;
                cachedRect.height = 27;
            }
            else
            {
                if (spline.selectedSplineMenu == "deformation")
                {
                    cachedRect.width = 269;
                    cachedRect.height = menuItemHeight * 10 + 10;

                    if (spline.normalType == Spline.NormalType.DYNAMIC)
                        cachedRect.height += menuItemHeight;

                    if (spline.deformations == 0)
                        cachedRect.height -= menuItemHeight;
                }
                else if (spline.selectedSplineMenu == "noise")
                {
                    cachedRect.height = menuItemHeight * 4 + 10;
                    foreach (NoiseLayer nl in spline.noises)
                    {
                        if (!nl.selected) cachedRect.height += menuItemHeight * 1;
                        else
                        {
                            cachedRect.height += 2;
                            cachedRect.height += menuItemHeight * 4;
                        }
                    }

                    if (spline.noises.Count > 0)
                        cachedRect.width = 388;
                    else
                        cachedRect.width = 269;
                }
                else if (spline.selectedSplineMenu == "info")
                {
                    cachedRect.height = menuItemHeight * 10 + 10;
                    cachedRect.width = 269;
                }
                else
                {
                    for (int i = 0; i < addonsCalcWindowSize.Count; i++)
                    {
                        if (spline.selectedSplineMenu == addonsCalcWindowSize[i].Item1)
                        {
                            Rect rect = addonsCalcWindowSize[i].Item2.Invoke(spline);
                            cachedRect.height = rect.height;
                            cachedRect.width = rect.width;
                            break;
                        }
                    }
                }

                if(GlobalSettings.GetGridVisibility() && cachedRect.width < 306)
                    cachedRect.width = 306;

                //Expand window for title.
                guiContentContainer.text = windowTitle;
                Vector2 labelSize = LibraryGUIStyle.textHeader.CalcSize(guiContentContainer);
                labelSize.x += LibraryGUIStyle.textHeader.padding.left + LibraryGUIStyle.textHeader.padding.right + 70;
                if (labelSize.x > cachedRect.width) cachedRect.width = labelSize.x;
            }
        }

        protected override void HandleExtendedWindow()
        {
            if (!EHandleUi.initialized)
                return;

            Spline spline = EHandleSelection.selectedSpline;

            if (spline == null)
            {
                if (extendedWindow != null)
                    extendedWindow.Close();

                return;
            }

            SplineObject so = EHandleSelection.selectedSplineObject;

            if(!EHandleSceneView.mouseDragEnabled)
            {
                //Create extended window
                if ((spline.selectedControlPoint != 0 || so != null) && extendedWindow == null)
                {
                    extendedWindow = CreateInstance<WindowExtended>();
                    extendedWindow.OpenWindow(false);
                    extendedWindow.toolbarToggleBase = toolbarToggleBase;
                }

                //Close extended window
                if (spline.selectedControlPoint == 0 && so == null && extendedWindow != null)
                    extendedWindow.Close();
            }

            //Updated position on extended window
            Vector2 newExtendedMenuPosition = cachedRect.position + new Vector2(cachedRect.size.x + extendedMenuMargin, 0);
            if (!GlobalSettings.GetWindowHorizontalOrder()) newExtendedMenuPosition = cachedRect.position + new Vector2(0, cachedRect.size.y + extendedMenuMargin);
            if (extendedWindow != null && !GeneralUtility.IsEqual(newExtendedMenuPosition, extendedWindow.cachedRect.position))
            {
                extendedWindow.UpdateChacedPosition(newExtendedMenuPosition);
            }
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
