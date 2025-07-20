// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: EHandleSplineObject.cs
//
// Author: Mikael Danielsson
// Date Created: 18-02-2024
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using SplineArchitect.Objects;
using SplineArchitect.Utility;
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEditor;
using UnityEngine;

namespace SplineArchitect
{
    public static class EHandleSplineObject
    {
        private static List<Mesh> meshCountContainer = new List<Mesh>();
        private static List<Segment> segmentContainer = new List<Segment>();

        public static void OnSceneGUI(Spline spline, Event e)
        {
            if (e.type == EventType.Layout)
            {
                for (int i = spline.splineObjects.Count - 1; i >= 0; i--)
                {
                    //Many spline objects can be deleted or added during the same frame so we need this check
                    if (i > spline.splineObjects.Count - 1)
                        continue;

                    SplineObject so = spline.splineObjects[i];

                    if (so == null)
                    {
                        spline.RemoveSplineObject(so);
                        continue;
                    }

                    EHandleMeshContainer.TryUpdate(spline, so);
                    EHandleEvents.InvokeOnSplineObjectSceneGUI(spline, so);
                }
            }
        }

        public static void Update(Spline spline)
        {
            foreach(SplineObject so in spline.splineObjects)
            {
                if (so == null)
                    continue;

                if (so.monitor.ComponentCountChange(true))
                {
                    EHandleMeshContainer.Initialize(so);
                    EHandleMeshContainer.DeleteNull(so);
                }
            }

            List<(SplineObject, bool)> detachList = EHandleEvents.GetDetachList();

            for (int i = detachList.Count - 1; i >= 0; i--)
            {
                (SplineObject, bool) tuple = detachList[i];

                if (tuple.Item1 == null)
                    continue;

                EHandleUndo.RecordNow(tuple.Item1, "Detach SplineObject");
                if (tuple.Item2) tuple.Item1.DetachToWorld(false);
                else tuple.Item1.DetachToLocal();
                EHandleUndo.DestroyObjectImmediate(tuple.Item1);
            }

            detachList.Clear();
        }

        public static void OnHierarchyChange()
        {
            SplineObject selectedSplineObject = EHandleSelection.selectedSplineObject;

            if (selectedSplineObject == null)
                return;

            ESplineObjectUtility.CleanPrimitiveColliders(selectedSplineObject);
            ESplineObjectUtility.TryAttachPrimitiveColliders(selectedSplineObject);

            foreach(SplineObject selectedSplineObject2 in EHandleSelection.selectedSplineObjects)
            {
                ESplineObjectUtility.CleanPrimitiveColliders(selectedSplineObject2);
                ESplineObjectUtility.TryAttachPrimitiveColliders(selectedSplineObject2);
            }
        }

        public static void UpdateInfo(SplineObject so)
        {
            meshCountContainer.Clear();
            so.deformedVertecies = 0;
            so.deformations = 0;

            if (so.type != SplineObject.Type.DEFORMATION)
                return;

            if (so.meshContainers == null)
                return;

            foreach (MeshContainer mc in so.meshContainers)
            {
                Mesh originMesh = mc.GetOriginMesh();
                if (mc != null &&
                    mc.MeshContainerExist() &&
                    originMesh != null &&
                    !meshCountContainer.Contains(originMesh))
                {
                    so.deformations++;
                    so.deformedVertecies += originMesh.vertexCount;
                    meshCountContainer.Add(originMesh);
                }
            }
        }

        public static bool HasReadWriteAccessEnabled(SplineObject so)
        {
            foreach(MeshContainer mc in so.meshContainers)
            {
                Mesh instanceMesh = mc.GetInstanceMesh();

                if(instanceMesh != null)

                if(!instanceMesh.isReadable)
                    return false;
            }

            return true;
        }

        public static void ToSplineCenter(SplineObject so)
        {
            so.localSplinePosition = new Vector3(0, 0, GeneralUtility.RoundToClosest(so.localSplinePosition.z, GlobalSettings.GetSnapIncrement()));
            so.activationPosition = so.localSplinePosition;
            so.localSplineRotation.eulerAngles = new Vector3(0, 0, 0);
        }

        public static bool CanBeDeformed(SplineObject so)
        {
            if (so.meshContainers.Count == 0)
                return false;

            foreach(MeshContainer mc in so.meshContainers)
            {
                Mesh instanceMesh = mc.GetInstanceMesh();
                Mesh originMesh = mc.GetOriginMesh();

                if (originMesh == null) return false;
                if (instanceMesh == null) return false;
                if (instanceMesh.vertexCount != originMesh.vertexCount) return false;

                if (instanceMesh == originMesh)
                {
                    //Debug.LogWarning("Can't deform mesh: " + instanceMesh.name + " Instance mesh is same as OriginMesh!");
                    return false;
                }
            }

            return true;
        }

        public static void UpdateTypeAuto(Spline spline, SplineObject so)
        {
            if (so.type == SplineObject.Type.NONE)
                return;

            Mesh originMesh = null;

            if (so.meshContainers == null || so.meshContainers.Count == 0)
            {
                MeshFilter meshFilter = so.gameObject.GetComponent<MeshFilter>();

                if(meshFilter == null)
                {
                    MeshCollider meshCollider = so.gameObject.GetComponent<MeshCollider>();

                    if(meshCollider != null)
                        originMesh = meshCollider.sharedMesh;
                }
                else
                    originMesh = meshFilter.sharedMesh;
            }
            else
            {
                originMesh = so.meshContainers[0].GetOriginMesh();
            }

            if (originMesh == null)
                return;

            float zExtend = originMesh.bounds.extents.z;

            if(so.splinePosition.z < -zExtend || so.splinePosition.z > zExtend + spline.length)
            {
                so.type = SplineObject.Type.FOLLOWER;
                return;
            }

            segmentContainer.Clear();

            for (int i = 1; i < spline.segments.Count; i++)
            {
                Segment s1 = spline.segments[i - 1];
                Segment s2 = spline.segments[i];

                float bak = so.splinePosition.z - zExtend;
                float front = so.splinePosition.z + zExtend;

                if (front > s1.zPosition && bak < s2.zPosition)
                {
                    if(!segmentContainer.Contains(s1))
                        segmentContainer.Add(s1);

                    if (!segmentContainer.Contains(s2))
                        segmentContainer.Add(s2);
                }
            }

            bool sameDirection = true;
            Vector3 originDirection = segmentContainer[0].GetPosition(Segment.ControlHandle.ANCHOR) - segmentContainer[0].GetPosition(Segment.ControlHandle.TANGENT_A);
            originDirection = originDirection.normalized;

            for (int i = 1; i < segmentContainer.Count; i++)
            {
                if (segmentContainer[i].GetInterpolationType() == Segment.InterpolationType.LINE)
                    continue;

                Vector3 d = segmentContainer[i].GetPosition(Segment.ControlHandle.ANCHOR) - segmentContainer[i].GetPosition(Segment.ControlHandle.TANGENT_A);
                d = d.normalized;

                if(!GeneralUtility.IsEqual(d, originDirection))
                {
                    sameDirection = false;
                    break;
                }
            }

            if (sameDirection)
                so.type = SplineObject.Type.FOLLOWER;
            else
                so.type = SplineObject.Type.DEFORMATION;
        }

        public static void Convert(Spline spline, SplineObject so, SplineObject.Type oldType)
        {
            if (so.type == SplineObject.Type.DEFORMATION)
                ConvertToDeformation(spline, so, oldType);
            else if (so.type == SplineObject.Type.FOLLOWER)
                ConvertToFollower(spline, so, oldType);
            else if (so.type == SplineObject.Type.NONE)
                ConvertToNone(spline, so, oldType);

            EHandleSpline.MarkForInfoUpdate(spline);
        }

        public static void ConvertToFollower(Spline spline, SplineObject so, SplineObject.Type oldType)
        {
            for (int i = 0; i < so.gameObject.GetComponentCount(); i++)
            {
                Component c = so.gameObject.GetComponentAtIndex(i);

                if (c == null)
                    continue;

                MeshFilter meshFilter = c as MeshFilter;
                MeshCollider meshCollider = c as MeshCollider;

                if(meshFilter != null)
                {
                    Mesh originMesh = EHandleMeshContainer.GetOriginMeshFromMeshNameId(meshFilter.sharedMesh);

                    if(originMesh == null)
                        continue;

                    meshFilter.sharedMesh = originMesh;
                }
                else if (meshCollider != null)
                {
                    Mesh originMesh = EHandleMeshContainer.GetOriginMeshFromMeshNameId(meshCollider.sharedMesh);

                    if (originMesh == null)
                        continue;

                    meshCollider.sharedMesh = originMesh;
                }
            }

            if (oldType == SplineObject.Type.DEFORMATION)
            {
                SplineObject[] cildSos = so.transform.GetComponentsInChildren<SplineObject>();

                for (int i = 0; i < cildSos.Length; i++)
                {
                    SplineObject childSo = cildSos[i];

                    if (childSo == null)
                        continue;

                    if (childSo == so)
                        continue;

                    childSo.SetInstanceMeshesToOriginMesh();
                    childSo.transform.localPosition = childSo.localSplinePosition;
                    childSo.transform.localRotation = childSo.localSplineRotation;
                    childSo.CalculatePrimitiveCollidersForDeformation(true);
                    Object.DestroyImmediate(childSo);
                }
            }

            so.meshContainers.Clear();
            so.type = SplineObject.Type.FOLLOWER;
            spline.followerUpdateList.Add(so);
            DeformationUtility.UpdateFollowers(spline);
            EHandleTool.UpdateOrientationForPositionTool(EHandleSceneView.GetSceneView(), spline);
            so.CalculatePrimitiveCollidersForDeformation(true);
        }

        public static void ConvertToDeformation(Spline spline, SplineObject so, SplineObject.Type oldType)
        {
            so.type = SplineObject.Type.DEFORMATION;

            so.SyncMeshContainers();
            Transform[] childs = so.transform.GetComponentsInChildren<Transform>();

            for (int i = 0; i < childs.Length; i++)
            {
                Transform child = childs[i];

                if (child == null)
                    continue;

                if (child == so.transform)
                    continue;

                ESplineObjectUtility.TryAttacheOnTransformEditor(spline, child, true, true);
            }

            so.transform.localPosition = so.localSplinePosition;
            so.transform.localRotation = so.localSplineRotation;
            so.monitor.ForceUpdate();
            EHandleTool.UpdateOrientationForPositionTool(EHandleSceneView.GetSceneView(), spline);
        }

        public static void ConvertToNone(Spline spline, SplineObject so, SplineObject.Type oldType)
        {
            ConvertToFollower(spline, so, oldType);
            so.type = SplineObject.Type.NONE;
        }

        public static void ExportMeshes(SplineObject so, string filePath)
        {
            if (so.meshContainers == null || so.meshContainers.Count == 0)
                return;

            Mesh meshFilterMesh = so.meshContainers[0].GetInstanceMesh();
            int count = 0;

            foreach(MeshContainer mc in so.meshContainers)
            {
                if (count > 0 && mc.GetInstanceMesh() == meshFilterMesh)
                    continue;

                Mesh instanceMesh = Object.Instantiate(mc.GetInstanceMesh());
                Mesh originMesh = mc.GetOriginMesh();

                Vector3[] originVertecies = HandleCachedResources.FetchOriginVertices(mc);
                Vector3[] deformedVertecies = instanceMesh.vertices;

                //Align pivot
                for (int i = 0; i < originVertecies.Length; i++)
                    deformedVertecies[i] += originMesh.bounds.center - instanceMesh.bounds.center;

                instanceMesh.SetVertices(deformedVertecies);
                instanceMesh.RecalculateBounds();
                instanceMesh.RecalculateNormals();
                instanceMesh.RecalculateTangents();

                string p = $"{filePath}";
                if (count > 0) p = $"{filePath}-{count + 1}";

                if(mc.IsMeshFilter()) p = $"{p}.asset";
                else p = $"{p}(collider).asset";

                AssetDatabase.CreateAsset(instanceMesh, p);

                count++;
            }

            AssetDatabase.SaveAssets();
        }
    }
}
