// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: EHandleObjectCloning.cs
//
// Author: Mikael Danielsson
// Date Created: 21-04-2025
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

using UnityEditor;
using UnityEngine;

using SplineArchitect.Objects;
using SplineArchitect.Ui;
using SplineArchitect.Utility;

namespace SplineArchitect
{
    public class EHandleObjectCloning
    {
        const string name = "Object Cloning";
        const string version = "1.0";

        private static List<float> childContainer = new List<float>();

        public static Texture2D textureClone { get; private set; }
        public static Texture2D textureCloneActive { get; private set; }
        public static GUIContent iconClone { get; private set; }
        public static GUIContent iconCloneActive { get; private set; }

        [UnityEditor.Callbacks.DidReloadScripts]
        private static void AftertAssemblyReload()
        {
            //Events
            EHandleEvents.OnSplineObjectSCeneGUI += OnSplineObjectSceneGUI;
            EHandleEvents.OnFirstUpdate += OnFirstUpdate;

            //Addon display name
            MenuGeneral.DisplayAddonName($"{name} {version}");
        }

        private static void OnFirstUpdate()
        {
            string mainFolderPath = EHandleMainFolder.GetFolderPath();

            //Deform terrain
            textureClone = AssetDatabase.LoadAssetAtPath<Texture2D>($"{mainFolderPath}/Addons/ObjectCloning/Textures/cloneIcon.png");
            textureCloneActive = AssetDatabase.LoadAssetAtPath<Texture2D>($"{mainFolderPath}/Addons/ObjectCloning/Textures/cloneIcon_active.png");
            iconClone = new GUIContent(textureClone, "Object Cloning");
            iconCloneActive = new GUIContent(textureCloneActive, "Object Cloning");
            MenuSplineObject.AddSubMenu("objectCloning", iconClone, iconCloneActive, ObjectCloningUi.DrawSplineObjectWindow, ObjectCloningUi.CalcSplineObjectWindowSize);
        }

        private static void OnSplineObjectSceneGUI(Spline spline, SplineObject so)
        {
            HandleClones(spline, so);
        }

        private static void HandleClones(Spline spline, SplineObject so)
        {
            if (!so.IsEnabled())
                return;

            if (!so.isCloneHead)
            {
                if (so.clones != null)
                {
                    DeleteClones(so);
                }

                return;
            }
            else
            {
                if (so.clones == null)
                    InitalizeCloneData(so);

                if (so.clones == null)
                    EnableCloning(spline, so);
            }

            UpdateCloneSectionData(spline, so);
            UpdateCloneAmount(spline, so);
        }

        public static void CloneSelection(Spline spline, SplineObject origin)
        {
            Transform originParent = origin.transform.parent;
            SplineObject originParentSo = originParent.GetComponent<SplineObject>();

            GameObject cloneHeadGo = new GameObject();
            EHandleUndo.RegisterCreatedObject(cloneHeadGo);
            cloneHeadGo.name = $"{origin.name} cloneParent";

            EHandleUndo.RecordNow(cloneHeadGo.transform, "Cloned");

            //Set parent for clone head
            if (originParentSo != null) originParentSo.disableOnTransformChildrenChanged = true;
            spline.disableOnTransformChildrenChanged = true;
            EHandleUndo.SetTransformParent(cloneHeadGo.transform, originParent);
            spline.disableOnTransformChildrenChanged = false;
            if (originParentSo != null) originParentSo.disableOnTransformChildrenChanged = false;

            SplineObject cloneHead = cloneHeadGo.GetComponent<SplineObject>();
            if (cloneHead == null) cloneHead = EHandleUndo.AddComponent<SplineObject>(cloneHeadGo);

            EHandleUndo.RecordNow(cloneHead, "Cloned");
            cloneHead.type = SplineObject.Type.DEFORMATION;
            cloneHead.splinePosition = origin.splinePosition;
            cloneHead.splineRotation = Quaternion.identity;
            cloneHead.cloneAmount = origin.cloneAmount;
            cloneHead.cloneDirection = origin.cloneDirection;
            cloneHead.cloneOffset = origin.cloneOffset;
            cloneHead.cloneUseFixedAmount = origin.cloneUseFixedAmount;
            cloneHead.cloneMenuAmount = origin.cloneMenuAmount;

            EHandleUndo.RecordNow(origin, "Cloned");
            origin.isClone = false;
            EHandleUndo.SetTransformParent(origin.transform, cloneHeadGo.transform);
            foreach (SplineObject so in EHandleSelection.selectedSplineObjects)
            {
                EHandleUndo.RecordNow(so, "Cloned");
                so.isClone = false;
                EHandleUndo.SetTransformParent(so.transform, cloneHeadGo.transform);
            }

            EnableCloning(spline, cloneHead);
            Selection.activeTransform = cloneHead.transform;
            EHandleSelection.ForceUpdate();
        }

        public static void CloneChildren(Spline spline, SplineObject cloneHead)
        {
            bool foundClone = false;

            //Need to check if the game object allready was a clone. Will get undo errors if this step is not done.
            for(int i = 0; i < cloneHead.transform.childCount; i++)
            {
                Transform transform = cloneHead.transform.GetChild(i);
                SplineObject originClone = transform.GetComponent<SplineObject>();

                if (originClone == null)
                    continue;

                if (originClone.isClone)
                {
                    Debug.LogWarning($"{originClone.name} allready was a clone! Removed clone data.");
                    EHandleUndo.RecordNow(originClone, "Cloned");
                    originClone.isClone = false;
                    foundClone = true;
                }
            }

            if(!foundClone) 
                EnableCloning(spline, cloneHead);
            else
                Debug.LogWarning($"Please try clone children again!");
        }

        private static void InitalizeCloneData(SplineObject so)
        {
            if (!so.isCloneHead)
                return;

            for (int i = 0; i < so.transform.childCount; i++)
            {
                Transform child = so.transform.GetChild(i);
                SplineObject childSo = child.GetComponent<SplineObject>();

                if (childSo == null)
                    continue;

                if (childSo.isClone)
                {
                    if (so.clones == null)
                        so.clones = new List<SplineObject>();

                    so.clones.Add(childSo);
                }
            }

            so.oldCloneAmount = so.cloneAmount;
            so.oldCloneOffset = so.cloneOffset;
            so.oldCloneDirection = so.cloneDirection;
        }

        private static void EnableCloning(Spline spline, SplineObject cloneHead)
        {
            bool hasSetFirstStartEnd = false;
            float end = 0;
            float start = 0;
            float maxCloneZScale = 0;

            cloneHead.isCloneHead = true;
            cloneHead.cloneAmount = cloneHead.cloneMenuAmount;
            cloneHead.oldCloneAmount = 0;
            cloneHead.oldCloneDirection = cloneHead.cloneDirection;
            cloneHead.oldCloneOffset = cloneHead.cloneOffset;

            if (!EHandleUndo.UndoTriggered())
            {
                cloneHead.originClones.Clear();
                childContainer.Clear();

                for (int i = 0; i < cloneHead.transform.childCount; i++)
                {
                    SplineObject originClone = cloneHead.transform.GetChild(i).GetComponent<SplineObject>();

                    if (originClone == null)
                        continue;

                    originClone.isOriginClone = true;
                    cloneHead.originClones.Add(originClone);

                    childContainer.Add(originClone.localSplinePosition.z);

                    if (originClone.transform.localScale.z > maxCloneZScale)
                        maxCloneZScale = originClone.transform.localScale.z;

                    for (int i2 = 0; i2 < originClone.transform.childCount; i2++)
                    {
                        Transform childChildTran = originClone.transform.GetChild(i2);
                        SplineObject childChildAco = childChildTran.GetComponent<SplineObject>();

                        if (childChildAco == null)
                            childContainer.Add(childChildTran.localPosition.z + originClone.localSplinePosition.z);
                        else
                            childContainer.Add(childChildAco.localSplinePosition.z + originClone.localSplinePosition.z);
                    }
                }

                foreach (float zPoint in childContainer)
                {
                    if (!hasSetFirstStartEnd)
                    {
                        hasSetFirstStartEnd = true;
                        end = zPoint;
                        start = zPoint;
                        continue;
                    }

                    if (end < zPoint)
                        end = zPoint;

                    if (start > zPoint)
                        start = zPoint;
                }

                cloneHead.cloneSectionLength = end - start;
                cloneHead.cloneShiftForward = start;
                cloneHead.cloneShiftBackward = -end;

                if (cloneHead.cloneSectionLength < 1)
                    cloneHead.cloneSectionLength = maxCloneZScale;

                if (cloneHead.cloneSectionLength < 1)
                    cloneHead.cloneSectionLength = 1;
            }

            if (cloneHead.clones == null)
                cloneHead.clones = new List<SplineObject>();

            UpdateCloneAmount(spline, cloneHead);
        }

        private static void UpdateCloneAmount(Spline spline, SplineObject cloneHead)
        {
            if (!cloneHead.cloneUseFixedAmount)
            {
                float offsetZ = cloneHead.cloneOffset.z * cloneHead.transform.localScale.z;
                float shiftBackward = cloneHead.cloneShiftBackward * cloneHead.transform.localScale.z;
                float shiftForward = cloneHead.cloneShiftForward * cloneHead.transform.localScale.z;

                // Length from clone parent to spline end
                float splineLengthFromCloneParent = cloneHead.splinePosition.z + offsetZ - shiftBackward;
                if (cloneHead.cloneDirection == SplineObject.CloneDirection.FORWARD)
                    splineLengthFromCloneParent = spline.length - cloneHead.splinePosition.z + offsetZ - shiftForward;

                //Clone section length
                float cloneSectionLength = (cloneHead.cloneSectionLength + cloneHead.cloneOffset.z) * cloneHead.transform.localScale.z;

                //Check devided by zero
                if (GeneralUtility.IsZero(splineLengthFromCloneParent)) splineLengthFromCloneParent = 1;
                if (GeneralUtility.IsZero(cloneSectionLength)) cloneSectionLength = 1;

                //Amount
                int amount = (int)(splineLengthFromCloneParent / cloneSectionLength) - 1;
                if (amount < 0) amount = 0;
                cloneHead.cloneMenuAmount = amount;
                cloneHead.cloneAmount = amount;
            }

            //Update sections
            if (cloneHead.cloneAmount != cloneHead.oldCloneAmount)
            {
                int cloneAmountDif = cloneHead.cloneAmount - cloneHead.oldCloneAmount;
                cloneHead.oldCloneAmount = cloneHead.cloneAmount;
                if (cloneAmountDif < 0)
                {
                    DestroyCloneSections(cloneHead, Mathf.Abs(cloneAmountDif));
                }
                else
                {
                    CreateCloneSections(spline, cloneHead, cloneAmountDif);
                }
            }
        }

        private static void UpdateCloneSectionData(Spline spline, SplineObject cloneHead)
        {
            //Update offset
            if (!GeneralUtility.IsEqual(cloneHead.cloneOffset, cloneHead.oldCloneOffset))
            {
                Vector3 cloneOffsetDif = cloneHead.cloneOffset - cloneHead.oldCloneOffset;
                if (cloneHead.cloneDirection == SplineObject.CloneDirection.BACKWARD)
                    cloneOffsetDif = -cloneOffsetDif;

                int origins = cloneHead.originClones.Count;
                int count = 0;
                int section = 0;

                for (int i = 0; i < cloneHead.clones.Count; i++)
                {
                    SplineObject clone = cloneHead.clones[i];
                    clone.localSplinePosition += cloneOffsetDif * (section + 1);

                    count++;

                    if (count >= origins)
                    {
                        count = 0;
                        section++;
                    }
                }

                cloneHead.oldCloneOffset = cloneHead.cloneOffset;
            }

            //Update direction
            if (cloneHead.cloneDirection != cloneHead.oldCloneDirection)
            {
                for (int i = cloneHead.clones.Count - 1; i >= 0; i--)
                {
                    SplineObject clone = cloneHead.clones[i];

                    if (clone == null)
                        continue;

                    if (clone.isClone)
                        UnityEngine.Object.DestroyImmediate(clone.gameObject);
                }

                cloneHead.clones.Clear();

                if (!cloneHead.cloneUseFixedAmount)
                    cloneHead.oldCloneAmount = 0;

                cloneHead.oldCloneAmount = 0;
                UpdateCloneAmount(spline, cloneHead);
                cloneHead.oldCloneDirection = cloneHead.cloneDirection;
            }
        }

        private static void DestroyCloneSections(SplineObject cloneHead, int amount)
        {
            amount = cloneHead.originClones.Count * amount;

            if (amount > cloneHead.clones.Count)
            {
                Debug.LogError("Tried to destroy more clones than what exists!");
                return;
            }

            amount = cloneHead.clones.Count - amount;
            for (int i = cloneHead.clones.Count - 1; i >= amount; i--)
            {
                SplineObject clone = cloneHead.clones[i];

                if (clone != null)
                {
                    GameObject CloneGo = cloneHead.clones[i].gameObject;

                    if (CloneGo != null)
                        UnityEngine.Object.DestroyImmediate(cloneHead.clones[i].gameObject);
                }

                cloneHead.clones.RemoveAt(i);
            }
        }

        private static void CreateCloneSections(Spline spline, SplineObject cloneHead, int amount)
        {
            //Fixes Undo issue. If this is not here, clones can be offseted when you have cloned with CloneSelection. Maybe a bit ulgy but it works.
            if (!GeneralUtility.IsEqual(cloneHead.splinePosition, cloneHead.transform.position))
                cloneHead.monitor.ForceUpdate();

            for (int i = 0; i < amount; i++)
            {
                foreach (SplineObject originClone in cloneHead.originClones)
                {
                    if (originClone == null)
                        continue;

                    cloneHead.skipUndoOnNextAttache = true;
                    GameObject cloneGo = UnityEngine.Object.Instantiate(originClone.gameObject, cloneHead.transform);

                    GameObject prefab = PrefabUtility.GetCorrespondingObjectFromSource(originClone.gameObject);
                    if (prefab != null)
                    {
                        var settings = new ConvertToPrefabInstanceSettings();
                        settings.componentsNotMatchedBecomesOverride = true;
                        settings.objectMatchMode = ObjectMatchMode.ByHierarchy;
                        settings.recordPropertyOverridesOfMatches = true;
                        settings.logInfo = false;
                        settings.changeRootNameToAssetName = false;
                        PrefabUtility.ConvertToPrefabInstance(cloneGo, prefab, settings, InteractionMode.AutomatedAction);
                    }

                    if (!cloneGo.TryGetComponent<SplineObject>(out var cloneSo))
                    {
                        cloneSo = cloneGo.AddComponent<SplineObject>();
                    }

                    SetNewCloneData(cloneSo);

                    bool isForward = cloneHead.cloneDirection == SplineObject.CloneDirection.FORWARD;
                    int index = CloneIndexToCloneSectionIndex(cloneHead, cloneHead.clones.Count) + 1;
                    cloneSo.localSplinePosition += new Vector3(index * cloneHead.cloneOffset.x, index * cloneHead.cloneOffset.y, (isForward ? index : -index) * cloneHead.cloneOffset.z);
                    cloneSo.localSplinePosition.z += (isForward ? index : -index) * cloneHead.cloneSectionLength;

                    cloneHead.clones.Add(cloneSo);
                }
            }

            void SetNewCloneData(SplineObject clone)
            {
                //Set data
                clone.isCloneHead = false;
                clone.isClone = true;
                clone.isOriginClone = false;
                clone.cloneUseFixedAmount = false;
                clone.cloneOffset = Vector3.zero;
                clone.cloneDirection = SplineObject.CloneDirection.FORWARD;
                clone.cloneAmount = 0;
                clone.cloneMenuAmount = 0;
                clone.canUpdateSelection = false;

                //Parent data
                //clone.transform.parent = cloneHead.transform;
                clone.monitor.UpdateParent();
                clone.soParent = cloneHead;
            }
        }

        private static int CloneIndexToCloneSectionIndex(SplineObject cloneHead, int cloneIndex)
        {
            if (cloneIndex == 0)
                return 0;

            return cloneIndex / cloneHead.originClones.Count;
        }

        private static void DeleteClones(SplineObject cloneHead)
        {
            // handle unseralized data
            for (int i = cloneHead.transform.childCount - 1; i >= 0; i--)
            {
                SplineObject clone = cloneHead.transform.GetChild(i).GetComponent<SplineObject>();

                if (clone == null)
                    continue;

                if (clone.isClone)
                    UnityEngine.Object.DestroyImmediate(clone.gameObject);
            }

            cloneHead.clones = null;

            //Handle seralized data
            if (!EHandleUndo.UndoTriggered())
            {
                if (cloneHead.originClones != null)
                {
                    //Remove isClone tag
                    for (int i = 0; i < cloneHead.originClones.Count; i++)
                    {
                        SplineObject originClone = cloneHead.originClones[i];

                        if (originClone == null)
                            continue;

                        originClone.isOriginClone = false;
                    }
                }

                cloneHead.originClones.Clear();
                cloneHead.isCloneHead = false;
            }
        }
    }
}
