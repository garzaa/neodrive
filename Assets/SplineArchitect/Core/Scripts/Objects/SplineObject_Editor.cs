// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: SplineObject_Editor.cs
//
// Author: Mikael Danielsson
// Date Created: 28-03-2023
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System;

using UnityEngine;

using SplineArchitect.Utility;

namespace SplineArchitect.Objects
{
    public partial class SplineObject : MonoBehaviour
    {
#if UNITY_EDITOR
        //General stored data
        [HideInInspector]
        public bool hasPrimitiveCollider = false;
        [HideInInspector]
        public bool autoType = false;
        [HideInInspector]
        public bool canUpdateSelection = true;
        //General runtime data
        [NonSerialized]
        public bool skipUndoOnNextAttache;
        [NonSerialized]
        public int deformedVertecies;
        [NonSerialized]
        public int deformations;
        [NonSerialized]
        public Vector3 activationPosition = Vector3.zero;
        [NonSerialized]
        public List<Vector3> normalsContainer = new List<Vector3>();
        [NonSerialized]
        public List<Vector4> tangentsContainer = new List<Vector4>();
        [NonSerialized]
        public List<int> trianglesContainer = new List<int>();
        [NonSerialized]
        public bool disableOnTransformChildrenChanged;
        [NonSerialized]
        public bool initalizedThisFrame = false;
        [NonSerialized]
        private bool readWriteWarningTriggered;
        [NonSerialized]
        private bool componentModeWarningTriggered;

        private void Start()
        {
            initalizedThisFrame = false;
        }

        private void OnTransformChildrenChanged()
        {
            if (disableOnTransformChildrenChanged)
                return;

            if (splineParent == null)
                return;

            monitor.ChildCountChange(out int dif);
            monitor.UpdateChildCount();

            if (type != Type.DEFORMATION)
                return;

            if (dif > 0)
            {
                for (int i = 0; i < transform.childCount; i++)
                {
                    Transform child = transform.GetChild(i);

                    //Should not parent splines to deformations. However you can do that to followers.
                    if(type == Type.DEFORMATION)
                    {
                        Spline childSpline = child.GetComponent<Spline>();

                        if(childSpline != null)
                        {
                            Debug.LogWarning($"[Spline Architect] Can't parent spline to SplineObject with type Deformation.");
                            child.parent = null;
                            continue;
                        }
                    }

                    ESplineObjectUtility.TryAttacheOnTransformEditor(splineParent, child, false, skipUndoOnNextAttache);
                    SplineObject so = child.GetComponent<SplineObject>();

                    if (so == null)
                        continue;

                    if (so.type != Type.DEFORMATION)
                        continue;

                    foreach (Transform child2 in child.GetComponentsInChildren<Transform>())
                    {
                        if (child2 == child)
                            continue;

                        ESplineObjectUtility.TryAttacheOnTransformEditor(splineParent, child2, true, skipUndoOnNextAttache);
                    }
                }

                skipUndoOnNextAttache = false;
            }
        }

        public void InitalizeEditor()
        {
            initalizedThisFrame = true;

            //Check for invalid meshes
            if (splineParent.deformationMode == DeformationMode.GENERATE)
            {
                foreach (MeshContainer mc in meshContainers)
                {
                    if (!mc.GetOriginMesh().isReadable)
                    {
                        Debug.LogError($"[Spline Architect] SplineObject \"{name}\" has an invalid mesh.\n Enable 'Read/Write Enabled' in the import settings to allow runtime deformation.");
                        break;
                    }
                }
            }

            //Deform mesh during build and store it in the built application.
            if (type == Type.DEFORMATION && splineParent.deformationMode == DeformationMode.SAVE_IN_BUILD && EHandleEvents.buildRunning && meshContainers.Count > 0)
            {
                splineParent.Initalize();
                HandleDeformationWorker.Deform(this, DeformationWorker.Type.EDITOR, splineParent);

                HandleDeformationWorker.GetActiveWorkers(DeformationWorker.Type.EDITOR, DeformationUtility.activeWorkersList);
                foreach (DeformationWorker dw in DeformationUtility.activeWorkersList)
                {
                    dw.Start();

                    dw.Complete((so, mc, vertices) =>
                    {
                        Mesh mesh = mc.GetInstanceMesh();
                        if (mesh != null)
                        {
                            mesh.MarkDynamic();
                            mesh.SetVertices(vertices);
                            mesh.RecalculateBounds();
                            mc.SetInstanceMesh(mesh);
                            MeshUtility.HandleOrthoNormals(mc, so);
                        }
                    });
                }
            }

            activationPosition = localSplinePosition;

            if(type == Type.DEFORMATION)
            {
                //Initalizes meschContainers.
                foreach (MeshContainer mc in meshContainers)
                {
                    //1. Gets the correct time stamp. Is used for detecting asset modifications.
                    mc.TryUpdateTimestamp();
                    //2. Update resourceKey, the timestamps is part of the key.
                    mc.UpdateResourceKey();
                    //3. The instanceMeshs name is the resourceKey.
                    mc.UpdateInstanceMeshName();
                }
            }

            //If part of hierarchy this data needs to be the same for all spline objects in the hierarchy.
            if (soParent != null)
            {
                alignToEnd = soParent.alignToEnd;
                componentMode = soParent.componentMode;
            }
        }

        public void SetOriginNormalsOnAll()
        {
            foreach (MeshContainer mc in meshContainers)
            {
                Mesh sharedMesh = mc.GetInstanceMesh();
                Mesh originMesh = mc.GetOriginMesh();

                if (sharedMesh == null)
                    continue;

                if (sharedMesh == originMesh)
                    continue;

                normalsContainer.Clear();
                originMesh.GetNormals(normalsContainer);
                sharedMesh.SetNormals(normalsContainer);
            }
        }

        public void SetOriginTangentsOnAll()
        {
            foreach (MeshContainer mc in meshContainers)
            {
                Mesh sharedMesh = mc.GetInstanceMesh();
                Mesh originMesh = mc.GetOriginMesh();

                if (sharedMesh == null)
                    continue;

                if (sharedMesh == originMesh)
                    continue;

                normalsContainer.Clear();
                originMesh.GetTangents(tangentsContainer);
                sharedMesh.SetTangents(tangentsContainer);
            }
        }

        public void SetOriginTrianglesOnAll()
        {
            foreach (MeshContainer mc in meshContainers)
            {
                Mesh sharedMesh = mc.GetInstanceMesh();
                Mesh originMesh = mc.GetOriginMesh();

                if (sharedMesh == null)
                    continue;

                if (sharedMesh == originMesh)
                    continue;

                for (int i = 0; i < originMesh.subMeshCount; i++)
                {
                    trianglesContainer.Clear();
                    originMesh.GetTriangles(trianglesContainer, i);
                    sharedMesh.SetTriangles(trianglesContainer, i);
                }
            }
        }

        public bool ValidForRuntimeDeformation(bool runWarnings)
        {
            if(runWarnings && !componentModeWarningTriggered && !initalizedThisFrame && Application.isPlaying && componentMode != ComponentMode.ACTIVE)
            {
                componentModeWarningTriggered = true;
                Debug.LogWarning($"[Spline Architect] Component mode is not set to Active on {name}! Animating this object will not work in your built game.");
            }
            else if (runWarnings && !componentModeWarningTriggered && Application.isPlaying && componentMode == ComponentMode.REMOVE_FROM_BUILD)
            {
                componentModeWarningTriggered = true;
                Debug.LogWarning($"[Spline Architect] Component mode is not set to Active or Inactive on {name}! Generating this object will not work in your built game.");
            }

            foreach (MeshContainer mc in meshContainers)
            {
                Mesh instanceMesh = mc.GetInstanceMesh();
                Mesh originMesh = mc.GetOriginMesh();

                if (instanceMesh == null || originMesh == null)
                    return false;

                if (originMesh == instanceMesh)
                    return false;

                if (!instanceMesh.isReadable)
                {
                    if (runWarnings && !readWriteWarningTriggered && Application.isPlaying && type == Type.DEFORMATION)
                    {
                        readWriteWarningTriggered = true;
                        Debug.LogWarning($"[Spline Architect] No read/write access on {name}! Generating or animating this object will not work in your built game.");
                    }

                    return false;
                }
            }

            return true;
        }

        public bool IsParentTo(SplineObject so)
        {
            SplineObject parent = so.soParent;

            for (int i = 0; i < 25; i++)
            {
                if (parent == null)
                    return false;

                if (parent == this)
                    return true;

                parent = parent.soParent;
            }

            return false;
        }
#endif
    }
}
