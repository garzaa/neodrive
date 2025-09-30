// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: EHandleObjectCloning.cs
//
// Author: Mikael Danielsson
// Date Created: 21-04-2025
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System.Collections.Generic;

using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

using SplineArchitect.Objects;
using SplineArchitect.Ui;
using SplineArchitect.Utility;
using SplineArchitect.CustomTools;

namespace SplineArchitect
{
    public class EHandleObjectCloning
    {
        const string name = "Object Cloning";
        const string version = "1.2.3";

        public static Texture2D textureClone { get; private set; }
        public static Texture2D textureCloneActive { get; private set; }
        public static GUIContent iconClone { get; private set; }
        public static GUIContent iconCloneActive { get; private set; }

        public static bool endCloneIsFollower { get; private set; }

        private static List<SplineObject> splineObjectContainer = new List<SplineObject>();
        private static List<Spline> splineContainer = new List<Spline>();

        [UnityEditor.Callbacks.DidReloadScripts]
        private static void AftertAssemblyReload()
        {
            //Events
            EHandleEvents.OnFirstUpdate += OnFirstUpdate;
            EHandleEvents.OnSegmentRemoved += OnSegmentDeleted;
            EHandleEvents.OnSplineSplit += OnSplineSplit;
            EHandleEvents.OnSplineJoin += OnSplineChange;
            EHandleEvents.OnSplineReverse += OnSplineChange;
            EHandleEvents.OnSplineLoop += OnSplineChange;
            EHandleEvents.AfterSplineObjectActivatePositionTool += AfterSplineObjectActivatePositionTool;
            EHandleEvents.OnSplineObjectParentChanged += OnSplineObjectParentChanged;
            SceneView.beforeSceneGui += BeforeSceneGUI;
            Undo.undoRedoPerformed += OnUndoRedoPerformed;

            //Addon display name
            WindowInfo.DisplayAddonName($"{name} {version}");
        }

        private static void OnFirstUpdate()
        {
            string mainFolderPath = EHandleMainFolder.GetFolderPath();

            //Deform terrain
            textureClone = AssetDatabase.LoadAssetAtPath<Texture2D>($"{mainFolderPath}/Addons/ObjectCloning/Textures/cloneIcon.png");
            textureCloneActive = AssetDatabase.LoadAssetAtPath<Texture2D>($"{mainFolderPath}/Addons/ObjectCloning/Textures/cloneIcon_active.png");
            iconClone = new GUIContent(textureClone, "Object Cloning");
            iconCloneActive = new GUIContent(textureCloneActive, "Object Cloning");
            WindowExtended.AddSubMenuSplineObject("objectCloning", iconClone, iconCloneActive, ObjectCloningUi.DrawSplineObjectWindow, ObjectCloningUi.CalcSplineObjectWindowSize);
        }

        private static void BeforeSceneGUI(SceneView sceneView)
        {
            Event e = Event.current;

            if (e.type == EventType.MouseUp && e.button == 0)
            {
                Spline spline = EHandleSelection.selectedSpline;

                if(spline != null)
                {
                    for (int i = spline.splineObjects.Count - 1; i >= 0; i--)
                    {
                        if (i >= spline.splineObjects.Count)
                            continue;

                        UpdateClones(spline.splineObjects[i]);
                    }

                    //Update clones for links
                    foreach (Segment s in spline.segments)
                    {
                        if (s.links != null && s.links.Count > 0)
                        {
                            foreach (Segment link in s.links)
                            {
                                if (link == s)
                                    continue;

                                if (link.splineParent == null)
                                {
                                    Debug.LogError("[Spline Architect] Spline parent is null!");
                                    continue;
                                }

                                for (int i2 = link.splineParent.splineObjects.Count - 1; i2 >= 0; i2--)
                                {
                                    UpdateClones(link.splineParent.splineObjects[i2]);
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (Selection.activeGameObject == null)
                        return;

                    SplineConnector[] connectors = Selection.activeGameObject.GetComponentsInChildren<SplineConnector>();

                    splineContainer.Clear();
                    foreach (SplineConnector connector in connectors)
                    {
                        if (connector == null) 
                            continue;

                        foreach(Segment s in connector.connections)
                        {
                            if (s == null)
                                continue;

                            if (s.linkTarget == Segment.LinkTarget.NONE)
                                continue;

                            if (s.splineParent == null)
                            {
                                Debug.LogError($"[Spline Architect] Could not find spline parent for segment! {connectors.Length} {connector.name}");
                                continue;
                            }

                            if(!splineContainer.Contains(s.splineParent))
                                splineContainer.Add(s.splineParent);
                        }
                    }

                    foreach (Spline spline2 in splineContainer)
                    {
                        for (int i = spline2.splineObjects.Count - 1; i >= 0; i--)
                        {
                            if (i >= spline2.splineObjects.Count)
                                continue;

                            UpdateClones(spline2.splineObjects[i]);
                        }

                        EHandleDeformation.TryDeform(spline2, false);
                    }
                }
            }

            void UpdateClones(SplineObject so)
            {
                if (so == null)
                    return;

                if (so.type != SplineObject.Type.DEFORMATION)
                    return;

                if (!so.cloningEnabled)
                    return;

                UpdateCloneAmount(so);
            }
        }

        private static void OnSplineSplit(Spline spline, Spline newSpline)
        {
            if (spline == null)
                return;

            foreach(SplineObject so in spline.splineObjects)
            {
                if (so == null)
                    continue;

                if (so.type != SplineObject.Type.DEFORMATION)
                    continue;

                if (!so.cloningEnabled)
                    continue;

                EActionDelayed.Add(() => { 
                    UpdateCloneAmount(so);
                    EHandleDeformation.TryDeform(so.splineParent, false);
                }, 0, 0, EActionDelayed.Type.FRAMES);
            }
        }

        private static void OnSplineChange(Spline spline)
        {
            if (spline == null)
                return;

            foreach (SplineObject so in spline.splineObjects)
            {
                if (so == null)
                    continue;

                if (so.type != SplineObject.Type.DEFORMATION)
                    continue;

                if (!so.cloningEnabled)
                    continue;

                EActionDelayed.Add(() => { 
                    UpdateCloneAmount(so);
                    EHandleDeformation.TryDeform(so.splineParent, false);
                }, 0, 0, EActionDelayed.Type.FRAMES);
            }
        }

        private static void OnSegmentDeleted(Segment segment)
        {
            if (segment == null)
                return;

            Spline spline = segment.splineParent;

            if (spline == null)
                return;

            for (int i = spline.splineObjects.Count - 1; i >= 0; i--)
            {
                if (i >= spline.splineObjects.Count)
                    continue;

                SplineObject so = spline.splineObjects[i];

                if (so == null)
                    continue;

                if (so.type != SplineObject.Type.DEFORMATION)
                    continue;

                if (!so.cloningEnabled)
                    continue;

                //We need to delay UpdateCloneAmount to the next update. Else the spline have not updated its data.
                EActionDelayed.Add(() => 
                {
                    UpdateCloneAmount(so);
                }, 0, 0, EActionDelayed.Type.FRAMES);
            }
        }

        private static void OnUndoRedoPerformed()
        {
            SplineObject selectedSo = EHandleSelection.selectedSplineObject;

            if (selectedSo == null)
                return;

            if (selectedSo.clones != null && selectedSo.clones.Count > 0)
            {
                selectedSo.monitor.ForceUpdate();

                foreach (SplineObject so in selectedSo.clones)
                {
                    if (so == null)
                        continue;

                    so.monitor.ForceUpdate();

                    SplineObject[] soChilds = so.transform.GetComponentsInChildren<SplineObject>();
                    foreach (SplineObject soChild in soChilds)
                        soChild.monitor.ForceUpdate();
                }

                foreach (SplineObject so in selectedSo.originClones)
                {
                    if (so == null)
                        continue;

                    so.monitor.ForceUpdate();

                    SplineObject[] soChilds = so.transform.GetComponentsInChildren<SplineObject>();
                    foreach (SplineObject soChild in soChilds)
                        soChild.monitor.ForceUpdate();
                }
            }
        }

        private static void OnSplineObjectParentChanged(SplineObject so)
        {
            if (so == null)
                return;

            if (so.type != SplineObject.Type.DEFORMATION)
                return;

            if (!so.cloningEnabled)
                return;

            EActionDelayed.Add(() => {
                UpdateCloneAmount(so);
                EHandleDeformation.TryDeform(so.splineParent, false);
            }, 0, 0, EActionDelayed.Type.FRAMES);
        }

        private static void AfterSplineObjectActivatePositionTool(SplineObject so)
        {
            //Lock position tool when a clone is selected
            SplineObject selectedSo = EHandleSelection.selectedSplineObject;
            if (selectedSo != null)
            {
                if (IsClone(selectedSo))
                {
                    PositionTool.lockedWarningMsg = "[Spline Architect] Can't move cloned GameObject!";
                    PositionTool.locked = true;
                }
            }
        }

        private static int GetCurrentCloneSections(SplineObject cloneParent)
        {
            return cloneParent.clones.Count / cloneParent.originClones.Count + 1;
        }

        private static void CreateClones(SplineObject cloneParent, int amount, float sectionLength)
        {
            //Amount check menu
            if (amount * cloneParent.originClones.Count > 999)
            {
                int option = EditorUtility.DisplayDialogComplex(
                    "Spline Architect Warning",
                    $"You are about to clone {amount * cloneParent.originClones.Count} GameObject(s). Do you want to continue?",
                    "Yes",
                    "No",
                    null
                );

                if (option == 1)
                {
                    amount = 0;
                    DeleteClones(cloneParent, GetCurrentCloneSections(cloneParent) - 1);
                    EHandleUndo.RecordNow(cloneParent);
                    cloneParent.cloningEnabled = false;
                }
            }

            int totalSections = GetCurrentCloneSections(cloneParent);

            for (int i = totalSections; i < totalSections + amount; i++)
            {
                foreach (SplineObject originClone in cloneParent.originClones)
                {
                    //Create clone
                    cloneParent.disableOnTransformChildrenChanged = true;
                    GameObject goClone = Object.Instantiate(originClone.gameObject, cloneParent.transform);
                    cloneParent.disableOnTransformChildrenChanged = false;

                    GameObject prefab = PrefabUtility.GetCorrespondingObjectFromOriginalSource(originClone.gameObject);
                    if (prefab != null && PrefabUtility.IsAnyPrefabInstanceRoot(originClone.gameObject))
                    {
                        ConvertToPrefabInstanceSettings settings = new();
                        settings.componentsNotMatchedBecomesOverride = true;
                        settings.objectMatchMode = ObjectMatchMode.ByHierarchy;
                        settings.recordPropertyOverridesOfMatches = true;
                        settings.logInfo = false;
                        settings.changeRootNameToAssetName = false;
                        PrefabUtility.ConvertToPrefabInstance(goClone, prefab, settings, InteractionMode.AutomatedAction);
                    }

                    EHandleUndo.RegisterCreatedObject(goClone, "Created clones");
                    SplineObject soClone = goClone.GetComponent<SplineObject>();

                    EHandleUndo.RecordNow(soClone);
                    if (cloneParent.cloneDirection == SplineObject.CloneDirection.BACKWARD)
                    {
                        soClone.localSplinePosition -= new Vector3(0, 0, sectionLength) * i;
                    }
                    else
                    {
                        soClone.localSplinePosition += new Vector3(0, 0, sectionLength) * i;
                    }
                    soClone.transform.localScale = originClone.transform.localScale;
                    EHandleUndo.RecordNow(cloneParent);
                    cloneParent.clones.Add(soClone);
                }
            }

            EHandleUndo.RecordNow(cloneParent);
            cloneParent.cloneAmount = GetCurrentCloneSections(cloneParent);
        }

        public static void DeleteClones(SplineObject cloneParent, int amount)
        {
            if (cloneParent.clones == null)
                return;

            EHandleUndo.RecordNow(cloneParent, "Deleted clones", EHandleUndo.RecordType.REGISTER_COMPLETE_OBJECT);

            int stop = cloneParent.clones.Count - (amount * cloneParent.originClones.Count);
            if (stop < 0) stop = 0;

            for (int i = cloneParent.clones.Count - 1; i >= stop; i--)
            {
                SplineObject soChild = cloneParent.clones[i];
                if (soChild != null) EHandleUndo.DestroyObjectImmediate(soChild.gameObject);

                cloneParent.clones.RemoveAt(i);
            }

            EditorUtility.SetDirty(cloneParent);

            if(!cloneParent.cloneUseFixedAmount)
            {
                EHandleUndo.RecordNow(cloneParent);
                if (cloneParent.clones.Count == 0 || cloneParent.originClones.Count == 0) cloneParent.cloneAmount = 0;
                else cloneParent.cloneAmount = GetCurrentCloneSections(cloneParent);
            }
        }

        private static (int, float) GetAmountAndSectionLength(SplineObject cloneParent)
        {
            float start = 99999;
            float end = -99999;
            Bounds endBounds = new Bounds();
            Bounds startBounds = new Bounds();

            endCloneIsFollower = false;

            foreach (SplineObject originClone in cloneParent.originClones)
            {
                if(originClone == null ||originClone.transform == null)
                    continue;

                Bounds transformedBounds = GetTransformedBounds(originClone);
                float l = transformedBounds.center.z - transformedBounds.extents.z;
                float h = transformedBounds.center.z + transformedBounds.extents.z;
                if (l < start)
                {
                    start = l;
                    startBounds = transformedBounds;
                }
                if (h > end)
                {
                    end = h;
                    endBounds = transformedBounds;
                }
            }

            //Section length
            float sectionLength = end - start + cloneParent.cloneOffset.z;
            //Offset adjustment for origin clones
            float adjustment = (cloneParent.splinePosition.z - start) + (sectionLength / 2);
            if(cloneParent.cloneSnapEnd) adjustment = (cloneParent.splinePosition.z - start) + endBounds.extents.z;
            //Calculate amount of clones
            int amount = Mathf.FloorToInt((cloneParent.splineParent.length - cloneParent.splinePosition.z + adjustment) / sectionLength);
            if (cloneParent.cloneUseFixedAmount)
                amount = cloneParent.cloneAmount;
            else if (cloneParent.cloneDirection == SplineObject.CloneDirection.BACKWARD)
            {
                adjustment = (end - cloneParent.splinePosition.z) + (sectionLength / 2);
                if (cloneParent.cloneSnapEnd) adjustment = (end - cloneParent.splinePosition.z) + startBounds.extents.z;
                amount = Mathf.FloorToInt((cloneParent.splinePosition.z + adjustment) / sectionLength);
            }
            amount = Mathf.FloorToInt(amount / cloneParent.transform.localScale.z);
            if(amount < 0) amount = 0;
            
            return (amount, sectionLength);
        }

        private static Bounds GetTransformedBounds(SplineObject splineObject)
        {
            Bounds transformedBounds = GetBounds(splineObject);

            for(int i = 0; i < splineObject.transform.childCount; i++)
            {
                Transform transformChild = splineObject.transform.GetChild(i);

                if(transformChild == null)
                    continue;

                SplineObject childSo = transformChild.GetComponent<SplineObject>();

                if (childSo == null)
                    continue;

                transformedBounds.Encapsulate(GetBounds(childSo));
            }

            return transformedBounds;

            Bounds GetBounds(SplineObject so)
            {
                Bounds bounds = new Bounds();
                bounds.size = Vector3.one;
                bounds.center = splineObject.splinePosition;

                if (so.type == SplineObject.Type.DEFORMATION)
                {
                    if (so.meshContainers != null && so.meshContainers.Count > 0)
                    {
                        Mesh originMesh = so.meshContainers[0].GetOriginMesh();
                        if (originMesh != null)
                            bounds = GeneralUtility.TransformBounds(originMesh.bounds, SplineObjectUtility.GetCombinedParentMatrixs(so, true));
                    }
                }
                else
                {
                    MeshFilter meshFilter = so.GetComponent<MeshFilter>();
                    if (meshFilter == null)
                    {
                        MeshCollider meshCollider = so.GetComponent<MeshCollider>();
                        if (meshCollider != null && meshCollider.sharedMesh != null)
                            bounds = GeneralUtility.TransformBounds(meshCollider.sharedMesh.bounds, SplineObjectUtility.GetCombinedParentMatrixs(so, true));
                    }
                    else
                    {
                        if (meshFilter.sharedMesh != null)
                            bounds = GeneralUtility.TransformBounds(meshFilter.sharedMesh.bounds, SplineObjectUtility.GetCombinedParentMatrixs(so, true));
                    }
                }

                return bounds;
            }
        }

        public static void UpdateCloneAmount(SplineObject cloneParent)
        {
            if (!cloneParent.cloningEnabled)
                return;

            int currentSections = GetCurrentCloneSections(cloneParent);
            (int, float) amountAndSectionLength = GetAmountAndSectionLength(cloneParent);
            int newAmount = amountAndSectionLength.Item1;
            float sectionLength = amountAndSectionLength.Item2;
            int dif = newAmount - currentSections;

            UpdateCloneOffset(cloneParent, sectionLength);

            if (dif > 0)
            {
                CreateClones(cloneParent, dif, sectionLength);
            }
            else if (dif < 0)
            {
                DeleteClones(cloneParent, -dif);
            }

            UpdateCloneEndSnapping(cloneParent);
        }

        public static void UpdateCloneEndSnapping(SplineObject cloneParent)
        {
            if (!cloneParent.cloneSnapEnd || cloneParent.cloneUseFixedAmount)
            {
                foreach (SplineObject so in cloneParent.clones)
                {
                    EHandleUndo.RecordNow(so);
                    so.snapMode = SplineObject.SnapMode.NONE;
                    so.snapLengthEnd = 1;
                    so.snapLengthStart = 1;
                }
                return;
            }

            int totalOrigins = cloneParent.originClones.Count;
            int totalClones = cloneParent.clones.Count;

            splineObjectContainer.Clear();
            float endDistance = -99999;
            float startDistance = 99999;

            for (int i = 0; i < totalClones; i++)
            {
                SplineObject clone = cloneParent.clones[i];

                if (clone == null)
                    continue;

                Bounds transformedBounds = GetTransformedBounds(clone);
                float end = transformedBounds.center.z + transformedBounds.extents.z;
                float start = transformedBounds.center.z - transformedBounds.extents.z;

                if ((cloneParent.cloneDirection == SplineObject.CloneDirection.FORWARD && GeneralUtility.IsEqual(endDistance, end)) ||
                    (cloneParent.cloneDirection == SplineObject.CloneDirection.BACKWARD && GeneralUtility.IsEqual(startDistance, start)))
                {
                    splineObjectContainer.Add(clone);
                }
                else if ((cloneParent.cloneDirection == SplineObject.CloneDirection.FORWARD && endDistance < end) ||
                         (cloneParent.cloneDirection == SplineObject.CloneDirection.BACKWARD && startDistance > start))
                {
                    endDistance = end;
                    startDistance = start;
                    splineObjectContainer.Clear();
                    splineObjectContainer.Add(clone);
                }
            }

            foreach (SplineObject so in cloneParent.clones)
            {
                if (so == null || so.transform == null)
                    continue;

                SplineObject[] splineObjects = so.transform.GetComponentsInChildren<SplineObject>();
                foreach (SplineObject soChild in splineObjects)
                    ResetSnapData(soChild);

                void ResetSnapData(SplineObject so)
                {
                    EHandleUndo.RecordNow(so);
                    so.snapMode = SplineObject.SnapMode.NONE;
                    so.snapLengthEnd = 1;
                    so.snapLengthStart = 1;
                    so.snapOffsetEnd = 0;
                    so.snapOffsetStart = 0;

                }
            }

            (int, float) amountAndSectionLength = GetAmountAndSectionLength(cloneParent);

            foreach (SplineObject so in splineObjectContainer)
            {
                if (so.type == SplineObject.Type.FOLLOWER)
                    continue;

                SetSnapData(so);

                SplineObject[] splineObjects = so.transform.GetComponentsInChildren<SplineObject>();

                foreach(SplineObject soChild in splineObjects)
                {
                    SetSnapData(soChild);
                }
            }

            void SetSnapData(SplineObject so)
            {
                EHandleUndo.RecordNow(so);
                so.snapMode = SplineObject.SnapMode.CONTROL_POINTS;
                if (cloneParent.cloneDirection == SplineObject.CloneDirection.FORWARD)
                {
                    so.snapLengthEnd = amountAndSectionLength.Item2 * 1.01f;
                    so.snapLengthStart = 0;
                    so.snapOffsetEnd = cloneParent.cloneSnapEndOffset;
                }
                else
                {
                    so.snapLengthStart = amountAndSectionLength.Item2 * 1.01f;
                    so.snapLengthEnd = 0;
                    so.snapOffsetStart = cloneParent.cloneSnapEndOffset;
                }
            }
        }

        public static void ToggleCloneDirection(SplineObject cloneParent)
        {
            if (!cloneParent.cloningEnabled)
                return;

            if (cloneParent.clones != null && cloneParent.clones.Count > 0)
            {
                foreach (SplineObject clone in cloneParent.clones)
                {
                    if (clone == null)
                        continue;

                    EHandleUndo.RecordNow(clone, "Updated clone direction");
                    clone.localSplinePosition.z = clone.localSplinePosition.z * -1;
                }

                foreach (SplineObject originClone in cloneParent.originClones)
                {
                    if (originClone == null)
                        continue;

                    EHandleUndo.RecordNow(originClone, "Updated clone direction");
                    originClone.localSplinePosition.z = originClone.localSplinePosition.z * -1;
                }
            }

            UpdateCloneAmount(cloneParent);
        }

        private static void UpdateCloneOffset(SplineObject cloneParent, float sectionLength)
        {
            if (cloneParent.clones != null && cloneParent.clones.Count > 0)
            {
                for (int i = 0; i < cloneParent.clones.Count; i++)
                {
                    SplineObject clone = cloneParent.clones[i];
                    SplineObject originClone = cloneParent.originClones[i % cloneParent.originClones.Count];

                    if (clone == null || originClone == null)
                        continue;

                    EHandleUndo.RecordNow(clone);
                    int column = 1;
                    if(i > 0) column += Mathf.FloorToInt(i / cloneParent.originClones.Count);
                    Vector3 p = new Vector3(cloneParent.cloneOffset.x * column + originClone.localSplinePosition.x,
                                            cloneParent.cloneOffset.y * column + originClone.localSplinePosition.y,
                                            sectionLength * column + originClone.localSplinePosition.z);
                    clone.localSplinePosition = p;
                }
            }
        }

        private static bool IsClone(SplineObject so)
        {
            SplineObject parent = so.soParent;

            for(int i = 0; i < 25; i++)
            {
                if (parent == null)
                    return false;

                if (parent.cloningEnabled && parent.clones != null && parent.clones.Contains(so))
                    return true;

                parent = parent.soParent;
            }

            return false;
        }

        public static void Enable(Spline spline, SplineObject cloneParent)
        {
            if (cloneParent.clones == null)
                cloneParent.clones = new List<SplineObject>();

            if (cloneParent.originClones == null)
                cloneParent.originClones = new List<SplineObject>();

            cloneParent.clones.Clear();
            cloneParent.originClones.Clear();

            int childCount = cloneParent.transform.childCount;

            for (int i = 0; i < childCount; i++)
            {
                Transform childTransform = cloneParent.transform.GetChild(i);

                if (childTransform == null)
                    continue;

                SplineObject soOriginClone = childTransform.GetComponent<SplineObject>();

                if (soOriginClone == null)
                    continue;

                cloneParent.originClones.Add(soOriginClone);
            }

            EHandleUndo.RecordNow(cloneParent);
            cloneParent.cloningEnabled = true;
            if(!GeneralUtility.IsEqual(cloneParent.transform.localScale, Vector3.one))
            {
                Debug.LogWarning($"[Spline Architect] Clone parent '{cloneParent.name}' did not have a scale of (1,1,1). Scale has been reset to (1,1,1).");
                cloneParent.transform.localScale = Vector3.one;
            }

            (int, float) amountAndSectionLength = GetAmountAndSectionLength(cloneParent);
            int amount = amountAndSectionLength.Item1;
            float sectionLength = amountAndSectionLength.Item2;

            CreateClones(cloneParent, amount - 1, sectionLength);
            UpdateCloneEndSnapping(cloneParent);
        }

        public static void Disable(SplineObject cloneParent)
        {
            DeleteClones(cloneParent, GetCurrentCloneSections(cloneParent) - 1);
            EHandleUndo.RecordNow(cloneParent);
            cloneParent.cloningEnabled = false;
        }
    }
}
