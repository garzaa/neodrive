// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: EHandleDeformation.cs
//
// Author: Mikael Danielsson
// Date Created: 04-02-2023
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System;

using UnityEngine;

using SplineArchitect.Utility;
using SplineArchitect.Objects;
using SplineArchitect.CustomTools;

namespace SplineArchitect
{
    public static class EHandleDeformation
    {
        //Settings
        private static int verticesPerNormalsUpdate = 1000 * Environment.ProcessorCount;

        //General
        private static List<SplineObject> orthoNormalWorkers = new List<SplineObject>();
        private static List<DeformationWorker> activeWorkersList = new List<DeformationWorker>();

        public static void RunWorkers()
        {
            HandleDeformationWorker.GetActiveWorkers(DeformationWorker.Type.EDITOR, activeWorkersList);

            if (activeWorkersList.Count == 0)
                return;

            int workersPerFrame = activeWorkersList.Count;

            for (int i = 0; i < workersPerFrame; i++)
            {
                DeformationWorker dw = activeWorkersList[i];
                dw.Start();

                dw.Complete((so, mc, vertices) => 
                {
                    Vector3 combinedScale = SplineObjectUtility.GetCombinedParentScales(so);

                    if (GeneralUtility.IsZero(combinedScale.x) ||
                        GeneralUtility.IsZero(combinedScale.y) ||
                        GeneralUtility.IsZero(combinedScale.z))
                        return;

                    Mesh mesh = mc.GetInstanceMesh();

                    if (mesh == null)
                        return;

                    //Internally, this makes the Mesh use "dynamic buffers" in the underlying graphics API, which are more efficient when Mesh data changes often.
                    //However does not seem to make any difference for performence.
                    mesh.MarkDynamic();
                    mesh.SetVertices(vertices);
                    mesh.RecalculateBounds();

                    if (mc.IsMeshFilter())
                    {
                        foreach (MeshContainer mc2 in so.meshContainers)
                        {
                            Mesh instanceMesh = mc2.GetInstanceMesh();
                            if (instanceMesh == null) continue;
                            //Need to set mesh like this else MeshColliders will not update properly. MeshFilter will work fine without this.
                            if (instanceMesh == mesh) mc2.SetInstanceMesh(mesh);
                        }
                    }
                    else
                    {
                        mc.SetInstanceMesh(mesh);
                    }

                    CreateOrthoNormalsWorker(so);
                });
            }
        }

        public static void RunOrthoNormalsWorkers()
        {
            if (activeWorkersList.Count > 0)
                return;

            int vertexCount = 0;
            for (int i = orthoNormalWorkers.Count - 1; i >= 0; i--)
            {
                SplineObject so = orthoNormalWorkers[i];
                foreach (MeshContainer mc in so.meshContainers)
                {
                    //Shared mesh is null directly after build complete.
                    if (mc == null || mc.GetInstanceMesh() == null)
                        continue;

                    MeshUtility.HandleOrthoNormals(mc, so);
                    int count = mc.GetInstanceMesh().vertexCount;

                    if (so.tangentType == SplineObject.TangentType.DONT_CALCULATE)
                        vertexCount += count / 10;
                    else
                        vertexCount += count;
                }

                orthoNormalWorkers.RemoveAt(i);
                if (vertexCount >= verticesPerNormalsUpdate)
                    break;
            }
        }

        public static void TryDeform(Spline spline, bool deformLinks = true)
        {
            bool isEditorDirty = spline.monitor.IsEditorDirty();
            bool segmentChange = spline.monitor.SegementChange();
            bool normalTypeChange = spline.monitor.NormalTypeChange();
            bool resolutionChange = spline.monitor.ResolutionChange();
            bool zRotContSaddleScaleChange = spline.monitor.ZRotContSaddleScaleChange();
            bool noiseChange = spline.monitor.NoiseChange();
            bool splineChange = isEditorDirty || segmentChange || normalTypeChange || resolutionChange || zRotContSaddleScaleChange || noiseChange;

            for (int i = 0; i < spline.splineObjects.Count; i++)
            {
                SplineObject so = spline.splineObjects[i];

                if (so == null || !so.IsEnabled() || so.monitor == null)
                    continue;

                if (spline.monitor.PosRotChange() && !isEditorDirty)
                {
                    //When rotation and moving the Spline we need to update oc:s monitor. Else they will be deformed but its no need for it.
                    so.monitor.UpdatePosRotSplineSpace();
                    so.monitor.UpdatePosRot();
                    so.monitor.UpdateScaleMirrorNormalTangent();
                    so.monitor.UpdateSplineLength(spline.length);
                    continue;
                }

                if(so.autoType && PositionTool.activePart == PositionTool.Part.NONE)
                    EHandleSplineObject.UpdateTypeAuto(spline, so);

                //Check for state changes
                if (so.monitor.StateChange(out SplineObject.Type oldType))
                    EHandleSplineObject.Convert(spline, so, oldType);

                //Monitor
                bool combinedParentPosRotScaleChange = so.monitor.CombinedParentPosRotScaleChange(true);
                bool posRotSplineSpaceChange = so.monitor.PosRotSplineSpaceChange(true);
                bool scaleChange = so.monitor.ScaleChange(true);
                bool tangentChange = so.monitor.TangentChange(true);
                bool normalsChange = so.monitor.NormalChange(true);
                bool mirrorChange = so.monitor.MirrorChange(true);
                bool extraChange = so.monitor.ExtraChange(true);
                bool splineLengthChange = so.monitor.SplineLengthChange(spline.length);
                bool soChange = combinedParentPosRotScaleChange || posRotSplineSpaceChange || mirrorChange || extraChange || scaleChange || tangentChange || normalsChange || splineLengthChange;

                if (mirrorChange)
                {
                    so.ReverseTrianglesOnAll();
                }

                if(normalsChange)
                {
                    if (so.normalType == SplineObject.NormalType.DONT_CALCULATE)
                    {
                        so.SetOriginNormalsOnAll();
                        so.SetOriginTrianglesOnAll();
                    }
                    else if (so.normalType == SplineObject.NormalType.SEAMLESS)
                        MeshUtility.RecalculateNormalsSeamlessWithJobs(so);
                    else if (so.normalType == SplineObject.NormalType.DEFAULT)
                        so.SetOriginTrianglesOnAll();
                }

                if (tangentChange)
                {
                    if (so.tangentType == SplineObject.TangentType.DONT_CALCULATE)
                        so.SetOriginTangentsOnAll();
                }

                //DEFORMATION
                if (so.type == SplineObject.Type.DEFORMATION)
                {
                    if (!splineChange && !soChange)
                        continue;

                    //Handle none mesh deformations
                    if (so.meshContainers == null || so.meshContainers.Count == 0 || !EHandleSplineObject.CanBeDeformed(so))
                    {
                        so.transform.localPosition = so.localSplinePosition;
                        so.transform.localRotation = so.localSplineRotation;
                        so.monitor.UpdateSplineLength(spline.length);

                        //Need to delay UpdateExternalComponents else sometimes everything has not initialized and the LOD Groyp may recalculate bounds from an undeformed mesh.
                        EActionDelayed.Add(() => 
                        {
                            if (so == null)
                                return;

                            so.UpdateExternalComponents();
                        }, 0, 0, EActionDelayed.Type.FRAMES);
                        continue;
                    }

                    HandleDeformationWorker.Deform(so, DeformationWorker.Type.EDITOR, spline);
                }
                //FOLLOWER
                else if (so.type == SplineObject.Type.FOLLOWER)
                {
                    if (!splineChange && !soChange)
                        continue;

                    spline.followerUpdateList.Add(so);
                    so.monitor.UpdateSplineLength(spline.length);
                }
                //NONE. Only update its spline space position
                else if (so.type == SplineObject.Type.NONE)
                {
                    if (!splineChange && !so.monitor.PosRotChange())
                        continue;

                    so.splinePosition = spline.WorldPositionToSplinePosition(so.transform.position, 12);
                    so.splineRotation = spline.WorldRotationToSplineRotation(so.transform.rotation, so.splinePosition.z / spline.length);
                    so.monitor.UpdateSplineLength(spline.length);
                }
            }

            DeformationUtility.UpdateFollowers(spline);
            if ((isEditorDirty || segmentChange) && deformLinks) TryDeformLinks(spline);
        }

        public static void TryDeformLinks(Spline spline)
        {
            foreach(Segment s in spline.segments)
            {
                if (s.links == null)
                    continue;

                foreach(Segment s2 in s.links)
                {
                    Spline spline2 = s2.splineParent;

                    if (spline2 == null)
                        continue;

                    if (spline2 == spline)
                        continue;

                    EHandleSpline.UpdateCachedData(spline2, false);
                    TryDeform(spline2, false);
                    //Need to  skip updating rot scale and pos in monitor here. Else link positions will not update correctly when multi selecting and moveing spline:s.
                    spline2.UpdateMonitor(true);
                }
            }
        }

        public static void CreateOrthoNormalsWorker(SplineObject so)
        {
            if (orthoNormalWorkers.Contains(so))
                orthoNormalWorkers.Remove(so);

            orthoNormalWorkers.Add(so);
        }
    }
}