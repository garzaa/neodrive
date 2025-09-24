// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: SplineObject.cs
//
// Author: Mikael Danielsson
// Date Created: 28-03-2023
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System;

using UnityEngine;
using Unity.Mathematics;

using SplineArchitect.Utility;
using SplineArchitect.Monitor;

namespace SplineArchitect.Objects
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public partial class SplineObject : MonoBehaviour
    {
        public enum Type : byte
        {
            DEFORMATION,
            FOLLOWER,
            NONE
        }

        public enum NormalType : byte
        {
            DEFAULT,
            SEAMLESS,
            DONT_CALCULATE
        }

        public enum TangentType : byte
        {
            DEFAULT,
            DONT_CALCULATE
        }

        public enum SnapMode : byte
        {
            NONE,
            CONTROL_POINTS,
            SPLINE_OBJECTS
        }

        public const int dataUsage = 24 + 
                                     1 + 12 + 16 + 12 + 1 + 1 + 1 + 8 + 8 + 40 + 40 + 1 + 4 + 1 + MonitorSplineObject.dataUsage + 1 + 1;

        //Static
        public static Type defaultType = Type.DEFORMATION;

        //Public stored data
        [HideInInspector]
        public bool mirrorDeformation;
        [HideInInspector]
        public Vector3 localSplinePosition;
        [HideInInspector]
        public Quaternion localSplineRotation = Quaternion.identity;
        [HideInInspector]
        public Vector3Int followAxels = Vector3Int.one;
        [HideInInspector]
        public bool lockPosition;
        [HideInInspector]
        public bool alignToEnd;
        [HideInInspector]
        public SnapMode snapMode;
        [HideInInspector]
        public float snapLengthStart = 1;
        [HideInInspector]
        public float snapLengthEnd = 1;
        [HideInInspector]
        public float snapOffsetStart = 0;
        [HideInInspector]
        public float snapOffsetEnd = 0;
        [HideInInspector]
        public Type type = Type.DEFORMATION;
        [HideInInspector]
        public NormalType normalType;
        [HideInInspector]
        public TangentType tangentType;
        [HideInInspector]
        public Spline splineParent;
        [HideInInspector]
        public SplineObject soParent;
        [HideInInspector]
        public List<MeshContainer> meshContainers;
        [HideInInspector]
        public ComponentMode componentMode = ComponentMode.REMOVE_FROM_BUILD;

        //Runtime data
        [NonSerialized]
        public MonitorSplineObject monitor;
        [NonSerialized]
        private bool initalized = false;

        public Vector3 splinePosition
        {
            get
            {
                if (soParent == null) return localSplinePosition;
                return math.transform(SplineObjectUtility.GetCombinedParentMatrixs(soParent), localSplinePosition);
            }
            set
            {
                localSplinePosition = math.transform(math.inverse(SplineObjectUtility.GetCombinedParentMatrixs(soParent)), value);
            }
        }
        public Quaternion splineRotation
        {
            get
            {
                if (soParent == null) return localSplineRotation;
                return SplineObjectUtility.GetCombinedParentRotations(soParent) * localSplineRotation;
            }
            set
            {
                localSplineRotation = Quaternion.Inverse(SplineObjectUtility.GetCombinedParentRotations(soParent)) * value;
            }
        }

        private void OnEnable()
        {
            Initalize();
        }

        private void OnDestroy()
        {
#if UNITY_EDITOR
            if (splineParent == null)
                return;
#endif
            splineParent.RemoveSplineObject(this);
        }

        private void OnTransformParentChanged()
        {
            //During copying this will run before OnEnable and the monitor will be null. So we can't run the code below.
            if (!initalized)
                return;

#if UNITY_EDITOR
            if (!ESplineObjectUtility.TryDetachFromTransformEditor(this))
#endif
                TryChangeParent();
        }

        public void Initalize()
        {
#if UNITY_EDITOR
            if (splineParent != null && splineParent.deformationMode != DeformationMode.SAVE_IN_BUILD && EHandleEvents.buildRunning)
                return;

            if (EHandleEvents.dragActive)
            {
                EHandleEvents.InitalizeAfterDrag(this);
                return;
            }
#endif

            if (initalized)
                return;

            initalized = true;

            if (splineParent == null)
            {
                SyncParentData();
                type = defaultType;
            }

#if UNITY_EDITOR
            if (splineParent == null)
            {
                Debug.LogError($"[Spline Architect] Found SplineObject ({name}) outside it's spline! You should remove it.");
                return;
            }
#endif

            if (monitor == null)
                monitor = new MonitorSplineObject(this);

            if (meshContainers == null) 
                meshContainers = new List<MeshContainer>();

            if (type == Type.DEFORMATION)
            {
#if UNITY_EDITOR
                //Turn of static when playing in the editor for all deformations. Else the static batached mesh will be offseted (only in editor playmode).
                if (Application.isPlaying && gameObject.isStatic)
                    gameObject.isStatic = false;

                //Important to wait after EHandleEvents.sceneIsLoadedPlaymodeduring Playmode in the editor. Else we cant access vertices on meshes with isReadable = false;
                if (!Application.isPlaying || EHandleEvents.sceneIsLoadedPlaymode)
                {
#endif
                    CacheUntrackedInstanceMeshes();
                    SyncMeshContainers();
                    if (!Application.isPlaying || splineParent.deformationMode != DeformationMode.DO_NOTHING)
                        SyncInstanceMeshesFromCache();
#if UNITY_EDITOR
                }
#endif
            }

            splineParent.AddSplineObject(this);

#if UNITY_EDITOR
            InitalizeEditor();
#endif
        }

        private void TryChangeParent()
        {
#if UNITY_EDITOR
            //Fixes undo case
            if (initalizedThisFrame)
            {
                SyncParentData();
                return;
            }
#endif
            SplineObject oldSoParent = soParent;

            //Updates for old parent
            splineParent.RemoveSplineObject(this);
#if UNITY_EDITOR
            splineParent.monitor.UpdateChildCount();

#endif

            //Update parent data
            SyncParentData();

            //Updates for new parent
            splineParent.AddSplineObject(this);
#if UNITY_EDITOR
            splineParent.monitor.UpdateChildCount();
#endif

            //Apply right order for deformation if the so is parented to another so.
            if (soParent != null) ReorderForParentHierarchy();

            if (transform.parent != null)
            {
                float4x4 combinedMatrixOld = SplineObjectUtility.GetCombinedParentMatrixs(oldSoParent);
                float4x4 combinedMatrix = SplineObjectUtility.GetCombinedParentMatrixs(soParent);
                localSplinePosition = math.transform(combinedMatrixOld, localSplinePosition);
                localSplinePosition = math.transform(math.inverse(combinedMatrix), localSplinePosition);
                Quaternion combinedRotations = Quaternion.Inverse(SplineObjectUtility.GetCombinedParentRotations(soParent)) * SplineObjectUtility.GetCombinedParentRotations(soParent);
                localSplineRotation = Quaternion.Inverse(combinedRotations) * localSplineRotation;

#if UNITY_EDITOR
                activationPosition = localSplinePosition;
                EHandleEvents.InvokeSplineObjectParentChanged(this);
#endif
            }

            //Force deformation
            monitor.ForceUpdate();
        }

        /// <summary>
        /// Checks if the SplineObject is enabled and active in the hierarchy.
        /// </summary>
        /// <returns>True if the object is active and enabled, otherwise false.</returns>
        public bool IsEnabled()
        {
#if UNITY_EDITOR
            if (gameObject == null)
                return false;
#endif
            if (gameObject.activeInHierarchy && enabled)
                return true;

            return false;
        }

        public void CacheUntrackedInstanceMeshes()
        {
            foreach (MeshContainer mc in meshContainers)
            {
                if (mc == null)
                    continue;

                Mesh instanceMesh = mc.GetInstanceMesh();
                Mesh originMesh = mc.GetOriginMesh();

                if (instanceMesh == null || originMesh == null)
                    continue;

                if (instanceMesh == originMesh)
                    continue;

                if (HandleCachedResources.IsInstanceMeshCached(instanceMesh))
                    continue;

                HandleCachedResources.AddOrUpdateInstanceMesh(mc);
            }
        }

        public void SyncMeshContainers()
        {
            for (int i = 0; i < gameObject.GetComponentCount(); i++)
            {
                Component component = gameObject.GetComponentAtIndex(i);
                MeshFilter meshFilter = component as MeshFilter;
                MeshCollider meshCollider = component as MeshCollider;

                if (meshFilter == null && meshCollider == null)
                    continue;

                MeshContainer mc = null;

                foreach (MeshContainer mc2 in meshContainers)
                {
                    if (mc2.Contains(component))
                    {
                        mc = mc2;
                        break;
                    }
                }

                if (mc == null)
                {
                    mc = new MeshContainer(component);
                    AddMeshContainer(mc);
                }
            }
        }

        public void SyncInstanceMeshesFromCache()
        {
            foreach (MeshContainer mc in meshContainers)
            {
                if (mc == null)
                    continue;

                Mesh oldInstanceMesh = mc.GetInstanceMesh();
                Mesh instanceMesh = HandleCachedResources.FetchInstanceMesh(mc);
                mc.SetInstanceMesh(instanceMesh);

                if (oldInstanceMesh == instanceMesh)
                    continue;

                monitor.ForceUpdate();
            }
        }

        /// <summary>
        /// Adds a MeshContainer to the SplineObject's list of mesh containers.
        /// </summary>
        /// <param name="mc">The MeshContainer to add.</param>
        public void AddMeshContainer(MeshContainer mc)
        {
            if (mc.IsMeshFilter()) meshContainers.Insert(0, mc);
            else meshContainers.Add(mc);
        }

        /// <summary>
        /// Removes a MeshContainer from the SplineObject's list of mesh containers.
        /// </summary>
        /// <param name="mc">The MeshContainer to remove.</param>
        public void RemoveMeshContainer(MeshContainer mc)
        {
            if (meshContainers.Contains(mc))
                meshContainers.Remove(mc);
        }

        /// <summary>
        /// Checks if the SplineObject contains the specified MeshContainer.
        /// </summary>
        /// <param name="mc">The MeshContainer to check.</param>
        /// <returns>True if the container exists, otherwise false.</returns>
        public bool ContainsMeshContainer(MeshContainer mc)
        {
            return meshContainers.Contains(mc);
        }

        /// <summary>
        /// Checks if the SplineObject contains a valid mesh in its mesh containers.
        /// </summary>
        /// <returns>True if a valid mesh is found, otherwise false.</returns>
        public bool ContainsValidMesh()
        {
            foreach (MeshContainer mc in meshContainers)
            {
                if (mc.GetInstanceMesh() != null)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if the SplineObject contains a specific instance mesh.
        /// </summary>
        /// <param name="instanceMesh">The instance mesh to check.</param>
        /// <returns>True if the mesh is contained, otherwise false.</returns>
        public bool ContainsInstanceMesh(Mesh instanceMesh)
        {
            if (type != Type.DEFORMATION)
                return false;

            foreach (MeshContainer mc in meshContainers)
            {
                if (mc.GetInstanceMesh() == mc.GetOriginMesh())
                    continue;

                if (mc.GetInstanceMesh() == instanceMesh)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Sets all instance meshes in the SplineObject to their origin meshes.
        /// </summary>
        public void SetInstanceMeshesToOriginMesh()
        {
            foreach (MeshContainer mc in meshContainers)
            {
                if (mc == null)
                    continue;

                Mesh originMesh = mc.GetOriginMesh();
                if (originMesh == null) continue;

                mc.SetInstanceMeshToOriginMesh();
            }
        }

        /// <summary>
        /// Reverses the triangle winding order on all instance meshes in the SplineObject.
        /// </summary>
        public void ReverseTrianglesOnAll()
        {
            foreach (MeshContainer mc in meshContainers)
            {
                Mesh sharedMesh = mc.GetInstanceMesh();
                Mesh originMesh = mc.GetOriginMesh();

                if (sharedMesh == null)
                    continue;

                if (sharedMesh == originMesh)
                    continue;

                Utility.MeshUtility.ReverseTriangles(sharedMesh);
            }
        }

        /// <summary>
        /// Updates both the Spline and SplineObject parent references.
        /// </summary>
        public void SyncParentData()
        {
            soParent = transform.parent?.GetComponent<SplineObject>();
            splineParent = TryFindSpline();
        }

        public Spline TryFindSpline()
        {
            Transform _transform = transform;
            Spline spline = null;
            SplineObject so;

            for (int i = 0; i < 25; i++)
            {
                if (_transform.parent == null)
                    break;

                _transform = _transform.parent;
                spline = _transform.GetComponent<Spline>();
                so = _transform.GetComponent<SplineObject>();

                //Skip if parent is not spline deformation
                if (so != null && so.type != Type.DEFORMATION)
                    break;

                if (spline != null)
                    return spline;
            }

            return spline;
        }

        /// <summary>
        /// Converts the SplineObject to a follower type.
        /// </summary>
        public void ConvertToFollower()
        {
            if (type == Type.FOLLOWER)
            {
                Debug.LogWarning("[Spline Architect] Spline object is allready a follower!");
                return;
            }

            type = Type.FOLLOWER;

            foreach (MeshContainer mc in meshContainers)
            {
                Mesh originMesh = mc.GetOriginMesh();
                if (originMesh != null) mc.SetInstanceMeshToOriginMesh();
            }

            splineParent.followerUpdateList.Add(this);
        }

        /// <summary>
        /// Converts the SplineObject to a deformation type.
        /// </summary>
        /// <param name="so">The SplineObject to be converted to a deformation.</param>
        /// <param name="initalizeMeshContainers">Specifies whether the mesh containers should be initialized. Default is true.</param>
        public void ConvertToDeformation()
        {
            if (type == Type.DEFORMATION)
            {
                Debug.LogWarning("[Spline Architect] Spline object is allready a deformation!");
                return;
            }

            type = Type.DEFORMATION;
            SyncMeshContainers();

            foreach (MeshContainer mc in meshContainers)
            {
                Mesh instanceMesh = HandleCachedResources.FetchInstanceMesh(mc);
                mc.SetInstanceMesh(instanceMesh);
            }

            HandleDeformationWorker.Deform(this, DeformationWorker.Type.RUNETIME, splineParent);
        }

        /// <summary>
        /// Reorders the SplineObject within its parent Spline to ensure it is positioned directly after its parent SplineObject in the hierarchy.
        /// </summary>
        /// <remarks>
        /// If this where not done the child might not behave corretly when moving its parent.
        /// </remarks>
        public void ReorderForParentHierarchy()
        {
            splineParent.RemoveSplineObject(this);

            for (int i = 0; i < splineParent.splineObjects.Count; i++)
            {
                if (splineParent.splineObjects[i] != soParent)
                    continue;

                //Always add child directly after parent in list
                if (i + 1 >= splineParent.splineObjects.Count)
                    splineParent.AddSplineObject(this);
                else
                    splineParent.AddSplineObject(this, i + 1);
                break;
            }
        }

        /// <summary>
        /// Detaches the SplineObject from its parent Spline and updates its position and rotation to world space.
        /// </summary>
        public void DetachToWorld(bool setParentToNull = true)
        {
            foreach (MeshContainer mc in meshContainers)
            {
                Mesh originMesh = mc.GetOriginMesh();
                if (originMesh != null) mc.SetInstanceMeshToOriginMesh();
            }

            Vector3 point = splinePosition;
            transform.rotation = splineParent.SplineRotationToWorldRotation(splineRotation, point.z / splineParent.length);
            transform.position = splineParent.SplinePositionToWorldPosition(point);
            if (setParentToNull)
            {
                transform.parent = null;
                monitor.UpdateParent();
            }

            UpdateExternalComponents(true);
        }

        /// <summary>
        /// Detaches the SplineObject to maintain its position and rotation in local space, useful for handling hierarchies where objects are deformed within other deformations.
        /// </summary>
        public void DetachToLocal()
        {
            foreach (MeshContainer mc in meshContainers)
            {
                Mesh originMesh = mc.GetOriginMesh();
                if (originMesh != null) mc.SetInstanceMeshToOriginMesh();
            }

            transform.localPosition = localSplinePosition;
            transform.localRotation = localSplineRotation;

            UpdateExternalComponents(true);
        }

        /// <summary>
        /// Detaches the SplineObject from its parent Spline and destroys it, preserving its position and rotation based on the specified space.
        /// </summary>
        public void DetachAndDestroy(Space space)
        {
            if (space == Space.World) DetachToWorld();
            else DetachToLocal();
            splineParent?.RemoveSplineObject(this);
            Destroy(this);
        }

        public void UpdateExternalComponents(bool useOriginMesh = false)
        {
            for(int i = 0; i < gameObject.GetComponentCount(); i++)
            {
                Component component = gameObject.GetComponentAtIndex(i);

                if (component == null)
                    continue;

                //Update primitive colliders
                if (component is Collider)
                {
                    Mesh mesh = null;

                    if(meshContainers != null && meshContainers.Count > 0)
                    {
                        mesh = meshContainers[0].GetInstanceMesh();

                        if (useOriginMesh)
                            mesh = meshContainers[0].GetOriginMesh();
                    }

                    Collider collider = component as Collider;

                    if (collider is BoxCollider)
                    {
                        BoxCollider boxCollider = collider as BoxCollider;
                        if(mesh == null) boxCollider.center = transform.InverseTransformPoint(splineParent.SplinePositionToWorldPosition(splinePosition));
                        else
                        {
                            boxCollider.center = mesh.bounds.center;
                            boxCollider.size = mesh.bounds.size;
                        }
                    }
                    else if (collider is SphereCollider)
                    {
                        SphereCollider sphereCollider = collider as SphereCollider;
                        if (mesh == null) sphereCollider.center = transform.InverseTransformPoint(splineParent.SplinePositionToWorldPosition(splinePosition));
                        else
                        {
                            sphereCollider.center = mesh.bounds.center;
                            sphereCollider.radius = Mathf.Max(mesh.bounds.extents.x, mesh.bounds.extents.y, mesh.bounds.extents.z);
                        }
                    }
                    else if (collider is CapsuleCollider)
                    {
                        CapsuleCollider capsuleCollider = collider as CapsuleCollider;
                        if (mesh == null) capsuleCollider.center = transform.InverseTransformPoint(splineParent.SplinePositionToWorldPosition(splinePosition));
                        else
                        {
                            capsuleCollider.center = mesh.bounds.center;
                            capsuleCollider.radius = Mathf.Max(mesh.bounds.extents.x, mesh.bounds.extents.z);
                            capsuleCollider.height = Mathf.Max(mesh.bounds.size.y, capsuleCollider.radius);
                        }
                    }
                }
                //Update LODGroup
                else if(component is LODGroup)
                {
                    LODGroup lodGroup = component as LODGroup;
                    lodGroup.RecalculateBounds();

                    if (lodGroup.animateCrossFading && type == Type.DEFORMATION)
                        Debug.LogWarning("[Spline Architect] Using Animate Cross-fading on a Deformation with LOD Group may have undesired consequences.");
                }
            }
        }

        public SnapData CalculateSnapData()
        {
            SnapData snapData = new SnapData();

            if (meshContainers == null || meshContainers.Count == 0)
            {
                return snapData;
            }

            Bounds localBounds = meshContainers[0].GetOriginMesh().bounds;
            Bounds transformedBounds = GeneralUtility.TransformBounds(localBounds, SplineObjectUtility.GetCombinedParentMatrixs(this));

            snapData.soStartPoint = splinePosition.z - transformedBounds.extents.z + localBounds.center.z;
            snapData.soEndPoint = splinePosition.z + transformedBounds.extents.z + localBounds.center.z;

            snapData.snapStartPoint = GetClosestPoint(snapMode, snapData.soStartPoint, out float startDistance);
            snapData.snapEndPoint = GetClosestPoint(snapMode, snapData.soEndPoint, out float endDistance);

            float midPoint = (snapData.soStartPoint + snapData.soEndPoint) / 2;

            if (midPoint > snapData.snapStartPoint && startDistance < snapLengthStart)
            {
                snapData.start = true;
            }

            if (midPoint < snapData.snapEndPoint && endDistance < snapLengthEnd)
            {
                snapData.end = true;
            }

            snapData.snapStartPoint += snapOffsetStart;
            snapData.snapEndPoint += snapOffsetEnd;

            return snapData;

            float GetClosestPoint(SnapMode snapMode, float point, out float distance)
            {
                float closestPoint = 0;
                float dCheck = 99999;

                distance = dCheck;

                if(snapMode == SnapMode.CONTROL_POINTS)
                {
                    foreach (Segment s in splineParent.segments)
                    {
                        float zPoint = s.zPosition;
                        if (alignToEnd) zPoint = s.splineParent.length - s.zPosition; 
                        float d = Mathf.Abs(zPoint - point);

                        if (dCheck > d)
                        {
                            dCheck = d;
                            distance = d;
                            closestPoint = zPoint;
                        }
                    }
                }
                else if(snapMode == SnapMode.SPLINE_OBJECTS)
                {
                    foreach (SplineObject so in splineParent.splineObjects)
                    {
                        if (so == this)
                            continue;

                        MeshFilter meshFilter = null;

                        for (int i = 0; i < so.gameObject.GetComponentCount(); i++)
                        {
                            Component comp = so.gameObject.GetComponentAtIndex(i);

                            if (comp is MeshFilter)
                            {
                                meshFilter = comp as MeshFilter;
                                break;
                            }
                        }

                        if (meshFilter == null)
                        {
                            float startPoint = so.splinePosition.z;
                            if(alignToEnd != so.alignToEnd) startPoint = splineParent.length - so.splinePosition.z;
                            float d2 = Mathf.Abs(startPoint - point);
                            if (dCheck > d2)
                            {
                                dCheck = d2;
                                distance = d2;
                                closestPoint = startPoint;
                            }
                        }
                        else
                        {
                            if (so.type == Type.DEFORMATION)
                                continue;

                            Bounds lBounds = meshFilter.sharedMesh.bounds;
                            Bounds tBounds = GeneralUtility.TransformBounds(lBounds, SplineObjectUtility.GetCombinedParentMatrixs(so, true));

                            float startPoint = so.splinePosition.z - tBounds.extents.z + lBounds.center.z;
                            if (alignToEnd != so.alignToEnd) startPoint = splineParent.length - startPoint;

                            float d2 = Mathf.Abs(startPoint - point);
                            if (dCheck > d2)
                            {
                                dCheck = d2;
                                distance = d2;
                                closestPoint = startPoint;
                            }

                            float endPoint = so.splinePosition.z + tBounds.extents.z + lBounds.center.z;
                            if (alignToEnd != so.alignToEnd) endPoint = splineParent.length - endPoint;
                            d2 = Mathf.Abs(endPoint - point);
                            if (dCheck > d2)
                            {
                                dCheck = d2;
                                distance = d2;
                                closestPoint = endPoint;
                            }
                        }
                    }
                }

                return closestPoint;
            }
        }
    }
}
