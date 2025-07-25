// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: MenuSplineObject.cs
//
// Author: Mikael Danielsson
// Date Created: 15-10-2023
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

using SplineArchitect.Objects;
using SplineArchitect.Utility;
using SplineArchitect.Libraries;

namespace SplineArchitect.Ui
{
    public class MenuSplineObject
    {
        //Object window
        private static Rect splineObjectWindowRect = new Rect(300, 100, 150, 0);
        private static string[] typeOptions = new string[] { "Deformation", "Follower", "None" };
        private static string[] normalsOption = new string[] { "Default", "Seamless", "Don't calculate" };
        private static string[] tangentsOptions = new string[] { "Default", "Don't calculate" };

        private static string containerXPos = "0";
        private static string containerYPos = "0";
        private static string containerZPos = "0";
        private static string containerXRot = "0";
        private static string containerYRot = "0";
        private static string containerZRot = "0";
        private static string containerSnapLengthStart = "0";
        private static string containerSnapLengthEnd = "0";
        private static string containerSnapOffsetStart = "0";
        private static string containerSnapOffsetEnd = "0";

        private static string[] componentModeOptions = new string[]
        {
            "Remove from build",
            "Inactive",
            "Active"
        };

        //Addons
        private static List<(string, Action<Spline, SplineObject>)> addonsDrawWindow = new();
        private static List<(string, GUIContent, GUIContent)> addonsButtons = new();
        private static List<(string, Func<Spline, SplineObject, Rect>)> addonsCalcWindowSize = new();

        public static void OnSceneGUI(Spline spline, SplineObject selected)
        {
            UpdateRect(spline, selected);
            GUILayout.Window(2, splineObjectWindowRect, windowID => DrawWindow(windowID, selected, spline), "", LibraryGUIStyle.empty);
        }

        private static void DrawWindow(int windowID, SplineObject so, Spline spline)
        {
            #region top
            if (so == null || so.transform == null)
                return;

            bool isDeformation = so.type == SplineObject.Type.DEFORMATION;
            bool isFollower = so.type == SplineObject.Type.FOLLOWER;
            bool isNone = so.type == SplineObject.Type.NONE;

            EHandleUi.ResetGetBackgroundStyleId();

            //Selected border
            GUILayout.Box("", LibraryGUIStyle.lineYellowThick, GUILayout.ExpandWidth(true));

            //Header
            int selectionCount = EHandleSelection.selectedSplineObjects.Count;
            string headerText = so.transform.name + (selectionCount > 0 ? " + (" + selectionCount + ")" : "");
            GUILayout.BeginHorizontal(LibraryGUIStyle.backgroundHeader);

            //Error, no read/write access
            if (!EHandleSplineObject.HasReadWriteAccessEnabled(so) && so.type == SplineObject.Type.DEFORMATION)
                EHandleUi.CreateErrorWarningMessageIcon(LibraryGUIContent.warningMsgCantDoRealtimeDeformation);

            EHandleUi.CreateLabelField("<b>" + headerText + "</b> ", LibraryGUIStyle.textHeaderBlack, true);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal(LibraryGUIStyle.backgroundToolbar);

            //Snap
            bool enableSnap = isDeformation && so.meshContainers != null && so.meshContainers.Count > 0;
            EHandleUi.CreateButtonToggle(EHandleUi.ButtonType.DEFAULT, LibraryGUIContent.iconMagnet,
                                         so.snapMode == SplineObject.SnapMode.SPLINE_OBJECTS ? LibraryGUIContent.iconMagnetActive2 : LibraryGUIContent.iconMagnetActive, 35, 19, () =>
            {
                EHandleSelection.UpdatedSelectedSplineObjectsRecordUndo((selected) =>
                {
                    if (selected.snapMode == SplineObject.SnapMode.NONE) selected.snapMode = SplineObject.SnapMode.CONTROL_POINTS;
                    else if (selected.snapMode == SplineObject.SnapMode.CONTROL_POINTS) selected.snapMode = SplineObject.SnapMode.SPLINE_OBJECTS;
                    else if (selected.snapMode == SplineObject.SnapMode.SPLINE_OBJECTS) selected.snapMode = SplineObject.SnapMode.NONE;
                }, "Toggle snap deformation");
            }, so.snapMode > 0 && enableSnap, isDeformation && so.meshContainers != null && so.meshContainers.Count > 0);

            //Mirror
            EHandleUi.CreateButtonToggle(EHandleUi.ButtonType.DEFAULT, LibraryGUIContent.iconMirrorDeformation, LibraryGUIContent.iconMirrorDeformationActive, 35, 19, () =>
            {
                EHandleSelection.UpdatedSelectedSplineObjectsRecordUndo((selected) =>
                {
                    selected.mirrorDeformation = !selected.mirrorDeformation;
                }, "Toggle mirror deformation");
            }, so.mirrorDeformation, isDeformation && so.meshContainers.Count > 0);

            EHandleUi.CreateSeparator();

            //Objects to spline center
            EHandleUi.CreateButton(EHandleUi.ButtonType.DEFAULT, LibraryGUIContent.iconToCenter, 35, 19, () =>
            {
                EHandleSelection.UpdatedSelectedSplineObjectsRecordUndo((selected) =>
                {
                    EHandleSplineObject.ToSplineCenter(selected);
                }, "Object to spline center");

                EActionToLateSceneGUI.Add(() =>
                {
                    EHandleTool.UpdateOrientationForPositionTool(EHandleSceneView.GetSceneView(), spline);
                }, EventType.Layout);
            }, isFollower || isDeformation);

            //Export mesh
            EHandleUi.CreateButton(EHandleUi.ButtonType.DEFAULT, LibraryGUIContent.iconExport, 35, 19, () =>
            {
                string path = EditorUtility.SaveFilePanelInProject("Export mesh", $"{so.name}", "asset", "Assets/");
                int count = 0;

                if (!string.IsNullOrEmpty(path))
                {
                    //For some very odd reason I need to do this. Likely becouse of EditorUtility.SaveFilePanelInProject.
                    EHandleSelection.stopUpdateSelection = true;
                    EHandleSelection.UpdatedSelectedSplineObjects((selected) =>
                    {
                        string[] data = path.Split('/');

                        path = "";
                        for (int i = 0; i < data.Length - 1; i++)
                            path += $"{data[i]}/";

                        if (count == 0) EHandleSplineObject.ExportMeshes(selected, $"{path}{so.name}");
                        else EHandleSplineObject.ExportMeshes(selected, $"{path}{so.name}{count}");
                        count++;
                    });
                    EHandleSelection.stopUpdateSelection = false;
                }
            });

            GUILayout.EndHorizontal();
            #endregion

            #region general
            if (spline.selectedObjectMenu == "general")
            {
                GUILayout.BeginHorizontal(LibraryGUIStyle.backgroundSubHeader);
                EHandleUi.CreateLabelField("<b>GENERAL</b>", LibraryGUIStyle.textSubHeader, true);
                GUILayout.EndHorizontal();

                EHandleUi.CreateXYZInputFields("Position", so.localSplinePosition, ref containerXPos, ref containerYPos, ref containerZPos, (position, dif) =>
                {
                    EHandleSelection.UpdatedSelectedSplineObjectsRecordUndo((selected) =>
                    {
                        selected.localSplinePosition -= dif;
                        selected.activationPosition = so.localSplinePosition;
                    }, "Updated position");

                    EActionToLateSceneGUI.Add(() =>
                    {
                        EHandleTool.UpdateOrientationForPositionTool(EHandleSceneView.GetSceneView(), spline);
                    }, EventType.Layout);
                }, 50, -1, -1, isNone, isNone, isNone);

                //Rotation
                bool rotFieldDisabled = so.followAxels.x == 0 || so.followAxels.y == 0 || so.followAxels.z == 0 || isNone;
                Vector3 localSplineRotation = so.localSplineRotation.eulerAngles;

                if(rotFieldDisabled)
                {
                    localSplineRotation = spline.WorldRotationToSplineRotation(so.transform.rotation, so.localSplinePosition.z / spline.length).eulerAngles;
                }

                EHandleUi.CreateXYZInputFields("Rotation", localSplineRotation, ref containerXRot, ref containerYRot, ref containerZRot, (euler, dif) =>
                {
                    EHandleSelection.UpdatedSelectedSplineObjectsRecordUndo((selected) =>
                    {
                        selected.localSplineRotation.eulerAngles = euler;
                    }, "Updated rotation");
                }, 50, -1, -1, rotFieldDisabled, rotFieldDisabled, rotFieldDisabled);

                if(so.snapMode > 0 && enableSnap)
                {
                    GUILayout.BeginHorizontal(EHandleUi.GetBackgroundStyle());
                    EHandleUi.CreateLabelField("Snap length:", LibraryGUIStyle.textDefault, true, 122);

                    //Snap length start
                    EHandleUi.CreateInputField("Start", so.snapLengthStart, ref containerSnapLengthStart, (newValue) => {
                        //Update selected an record undo
                        EHandleSelection.UpdatedSelectedSplineObjectsRecordUndo((selected) =>
                        {
                            selected.snapLengthStart = newValue;
                        }, "Set snap length start");
                    }, 37, 35, true);

                    EHandleUi.CreateEmpty(0);

                    //Snap length end
                    EHandleUi.CreateInputField("End", so.snapLengthEnd, ref containerSnapLengthEnd, (newValue) => {
                        //Update selected an record undo
                        EHandleSelection.UpdatedSelectedSplineObjectsRecordUndo((selected) =>
                        {
                            selected.snapLengthEnd = newValue;
                        }, "Set snap length end");
                    }, 37, 30, true);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal(EHandleUi.GetBackgroundStyle());
                    GUILayout.Space(2);
                    EHandleUi.CreateLabelField("Snap offset:", LibraryGUIStyle.textDefault, true, 120);

                    //Snap length start
                    EHandleUi.CreateInputField("Start", so.snapOffsetStart, ref containerSnapOffsetStart, (newValue) => {
                        //Update selected an record undo
                        EHandleSelection.UpdatedSelectedSplineObjectsRecordUndo((selected) =>
                        {
                            selected.snapOffsetStart = newValue;
                        }, "Set snap offset start");
                    }, 37, 35, true);

                    EHandleUi.CreateEmpty(0);

                    //Snap length end
                    EHandleUi.CreateInputField("End", so.snapOffsetEnd, ref containerSnapOffsetEnd, (newValue) => {
                        //Update selected an record undo
                        EHandleSelection.UpdatedSelectedSplineObjectsRecordUndo((selected) =>
                        {
                            selected.snapOffsetEnd = newValue;
                        }, "Set snap offset end");
                    }, 37, 30, true);
                    GUILayout.EndHorizontal();
                }

                if(isFollower)
                {
                    //Follow rotation
                    EHandleUi.CreateToggleXYZField("Follow rotation: ", so.followAxels, (Vector3Int newValue) => {

                        EHandleSelection.UpdatedSelectedSplineObjectsRecordUndo((selected) =>
                        {
                            selected.followAxels = newValue;

                            if (selected.followAxels == Vector3.one)
                            {
                                selected.localSplineRotation = spline.WorldRotationToSplineRotation(selected.transform.rotation, selected.localSplinePosition.z / spline.length);
                            }
                        }, "Update follower axels");
                    });
                }

                if (isDeformation)
                {
                    //Normals
                    GUILayout.BeginHorizontal(EHandleUi.GetBackgroundStyle());
                    EHandleUi.CreatePopupField("Normals: ", 70, (int)so.normalType, normalsOption, (int newValue) =>
                    {
                        EHandleSelection.UpdatedSelectedSplineObjectsRecordUndo((selected) =>
                        {
                            selected.normalType = (SplineObject.NormalType)newValue;
                        }, "Update normal state");
                    }, 60, true);

                    GUILayout.Space(14);

                    //Tangents
                    EHandleUi.CreatePopupField("Tangents: ", 70, (int)so.tangentType, tangentsOptions, (int newValue) =>
                    {
                        EHandleSelection.UpdatedSelectedSplineObjectsRecordUndo((selected) =>
                        {
                            selected.tangentType = (SplineObject.TangentType)newValue;
                        }, "Update tangent state");
                    }, 66, true);
                    GUILayout.EndHorizontal();
                }

                GUILayout.BeginHorizontal(EHandleUi.GetBackgroundStyle());
                if (isFollower)
                {
                    //World position mode
                    EHandleUi.CreateLabelField("Lock position: ", LibraryGUIStyle.textDefault, true, 88);
                    EHandleUi.CreateToggle(so.lockPosition, (bool newValue) => {

                        EHandleSelection.UpdatedSelectedSplineObjectsRecordUndo((selected) =>
                        {
                            selected.lockPosition = newValue;
                        }, "Toggled lock position");
                    });
                }

                GUILayout.FlexibleSpace();
                if (EHandlePrefab.IsPrefabStageActive() && spline.componentMode == ComponentMode.INACTIVE)
                {
                    EHandleUi.CreatePopupField("Component:", 70, 1, componentModeOptions, (int newValue) =>
                    {
                    }, 80, true, false);
                }
                else
                {
                    //Component
                    if (spline.deformationMode == DeformationMode.GENERATE &&
                        so.componentMode == ComponentMode.REMOVE_FROM_BUILD &&
                        so.type == SplineObject.Type.DEFORMATION &&
                        so.meshContainers != null &&
                        so.meshContainers.Count > 0)
                    {
                        EHandleUi.CreateErrorWarningMessageIcon(LibraryGUIContent.errorMsgSplineObjectGenerateDeformationsRuntime);
                    }
                    else EHandleUi.CreateInfoMessageIcon(LibraryGUIContent.infoMsgACOComponentMode);
                    EHandleUi.CreatePopupField("Component:", 70, (int)so.componentMode, componentModeOptions, (int newValue) =>
                    {
                        EHandleSelection.UpdatedSelectedSplineObjectsRecordUndo((selected) =>
                        {
                            selected.componentMode = (ComponentMode)newValue;

                            //Set component mode for whole hierarchy
                            SplineObject firstSo = selected;
                            for (int i = 0; i < 25; i++)
                            {
                                if (firstSo.soParent == null)
                                    break;

                                firstSo = firstSo.soParent;
                            }

                            EHandleUndo.RecordNow(firstSo);
                            firstSo.componentMode = (ComponentMode)newValue;

                            foreach (SplineObject so2 in so.splineParent.splineObjects)
                            {
                                if (firstSo.IsParentTo(so2))
                                {
                                    EHandleUndo.RecordNow(so2);
                                    so2.componentMode = (ComponentMode)newValue;
                                }
                            }
                        }, "Change component mode");

                        EHandleSpline.MarkForInfoUpdate(spline);
                    }, 80, true, spline.componentMode == ComponentMode.ACTIVE);
                }

                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal(EHandleUi.GetBackgroundStyle());

                EHandleUi.CreateEmpty(13);
                EHandleUi.CreateToggleField("Auto type: ", so.autoType, (value) =>
                {
                    EHandleSelection.UpdatedSelectedSplineObjectsRecordUndo((selected) =>
                    {
                        selected.autoType = value;
                    }, "Toggled auto type");
                }, so.type != SplineObject.Type.NONE, true, 67);

                //Type
                EHandleUi.CreatePopupField("Type:", 70, (int)so.type, typeOptions, (int newValue) =>
                {
                    SplineObject.Type state = (SplineObject.Type)newValue;

                    EHandleSelection.UpdatedSelectedSplineObjectsRecordUndo((selected) =>
                    {
                        selected.type = state;
                        EditorUtility.SetDirty(selected);
                    }, "Update type");

                    EHandleSpline.MarkForInfoUpdate(spline);
                }, -1, true, !so.autoType);
                GUILayout.EndHorizontal();
            }
            #endregion

            #region info
            else if (spline.selectedObjectMenu == "info")
            {
                GUILayout.BeginHorizontal(LibraryGUIStyle.backgroundSubHeader);
                EHandleUi.CreateLabelField("<b>INFO</b>", LibraryGUIStyle.textSubHeader, true);
                GUILayout.EndHorizontal();

                //Vertecies label
                int vertecies = so.deformedVertecies;
                int deformations = so.deformations;
                List<SplineObject> selection = EHandleSelection.selectedSplineObjects;
                if (selection.Count > 0)
                {
                    foreach (SplineObject oc2 in selection)
                    {
                        vertecies += oc2.deformedVertecies;
                        deformations += oc2.deformations;
                    }
                }
                EHandleUi.CreateLabelField("Vertecies: " + vertecies, LibraryGUIStyle.textDefault);

                //Deformations label
                EHandleUi.CreateLabelField("Deformations: " + deformations, LibraryGUIStyle.textDefault);
            }
            #endregion

            #region addons
            else
            {
                bool foundAddon = false;

                for (int i = 0; i < addonsDrawWindow.Count; i++)
                {
                    if (addonsDrawWindow[i].Item1 == spline.selectedObjectMenu)
                    {
                        foundAddon = true;
                        addonsDrawWindow[i].Item2.Invoke(spline, so);
                    }
                }

                if (foundAddon == false)
                    spline.selectedObjectMenu = "general";
            }
            #endregion

            #region bottom
            GUILayout.Box("", LibraryGUIStyle.lineGrey, GUILayout.ExpandWidth(true));

            GUILayout.BeginHorizontal(LibraryGUIStyle.backgroundBottomMenu);

            //General
            EHandleUi.CreateButtonToggle(EHandleUi.ButtonType.SUB_MENU, LibraryGUIContent.iconGeneral, LibraryGUIContent.iconGeneralActive, 35, 18, () =>
            {
                EHandleUndo.RecordNow(spline, "Changed object menu");
                spline.selectedObjectMenu = "general";
            }, spline.selectedObjectMenu == "general");

            //Addons
            for (int i = 0; i < addonsButtons.Count; i++)
            {
                EHandleUi.CreateButtonToggle(EHandleUi.ButtonType.SUB_MENU, addonsButtons[i].Item2, addonsButtons[i].Item3, 35, 18, () =>
                {
                    EHandleUndo.RecordNow(spline, "Changed sub menu");
                    spline.selectedObjectMenu = addonsButtons[i].Item1;
                }, spline.selectedObjectMenu == addonsButtons[i].Item1);
            }

            //Info
            EHandleUi.CreateButtonToggle(EHandleUi.ButtonType.SUB_MENU, LibraryGUIContent.iconInfo, LibraryGUIContent.iconInfoActive, 35, 18, () =>
            {
                EHandleUndo.RecordNow(spline, "Changed object menu");
                spline.selectedObjectMenu = "info";
            }, spline.selectedObjectMenu == "info");

            GUILayout.EndHorizontal();
            GUILayout.Box("", LibraryGUIStyle.lineGrey, GUILayout.ExpandWidth(true));
            #endregion
        }

        private static void UpdateRect(Spline spline, SplineObject selected)
        {
            if (spline.selectedObjectMenu == "general")
            {
                if (selected.type == SplineObject.Type.NONE)
                    splineObjectWindowRect.height = LibraryGUIStyle.menuItemHeight * 6 + 20;
                else if(selected.snapMode > 0 && selected.type == SplineObject.Type.DEFORMATION && selected.meshContainers != null && selected.meshContainers.Count > 0)
                    splineObjectWindowRect.height = LibraryGUIStyle.menuItemHeight * 9 + 20;
                else
                    splineObjectWindowRect.height = LibraryGUIStyle.menuItemHeight * 7 + 20;

                splineObjectWindowRect.width = 288;
            }
            else if (spline.selectedObjectMenu == "info")
            {
                splineObjectWindowRect.height = LibraryGUIStyle.menuItemHeight * 4 + 20;
                splineObjectWindowRect.width = 156;
            }
            //Addons
            else
            {
                for (int i = 0; i < addonsCalcWindowSize.Count; i++)
                {
                    if (spline.selectedObjectMenu == addonsCalcWindowSize[i].Item1)
                    {
                        Rect rect = addonsCalcWindowSize[i].Item2.Invoke(spline, selected);
                        splineObjectWindowRect.height = rect.height;
                        splineObjectWindowRect.width = rect.width;
                        break;
                    }
                }
            }

            Rect generalWindowRect = MenuGeneral.GetRect();
            Rect splineWindowRect = MenuSpline.GetRect();

            splineObjectWindowRect.x = (generalWindowRect.x + generalWindowRect.width + 3) + (splineWindowRect.width + 3);
            splineObjectWindowRect.y = generalWindowRect.y - (splineObjectWindowRect.height - generalWindowRect.height + 18);
        }

        public static Rect GetRect()
        {
            return splineObjectWindowRect;
        }

        public static void AddSubMenu(string id, GUIContent button, GUIContent buttonActive, Action<Spline, SplineObject> DrawWindow, Func<Spline, SplineObject, Rect> calcWindowSize)
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
