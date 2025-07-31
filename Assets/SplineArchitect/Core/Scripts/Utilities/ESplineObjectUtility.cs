// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: SplineObjectUtility.cs
//
// Author: Mikael Danielsson
// Date Created: 11-09-2024
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

#if UNITY_EDITOR
using UnityEngine;

using SplineArchitect.Objects;

namespace SplineArchitect.Utility
{
    public class ESplineObjectUtility
    {
        public static bool TryDetachFromTransformEditor(SplineObject so)
        {
            //Detect detach
            if (so.transform.parent == null)
            {
                EHandleEvents.MarkForDetach(so, true);
                return true;
            }

            Spline splineParent = so.TryFindSpline();
            SplineObject soParent = so.transform.parent.GetComponent<SplineObject>();

            if (splineParent == null && soParent != null)
            {
                EHandleEvents.MarkForDetach(so, false);
                return true;
            }

            if(splineParent == null && soParent == null)
            {
                EHandleEvents.MarkForDetach(so, true);
                return true;
            }

            return false;
        }

        public static void TryAttacheOnTransformEditor(Spline spline, Transform transform, bool hasParent, bool skipUndo = false)
        {
            //Ignore Splines
            if (transform.GetComponent<Spline>() != null)
                return;

            SplineObject soParent = null;

            if (transform.parent != null)
                soParent = transform.parent.GetComponent<SplineObject>();

            if (soParent != null && soParent.type == SplineObject.Type.FOLLOWER)
                return;

            SplineObject newSo = null;

            //Try get so
            if (newSo == null)
                newSo = transform.GetComponent<SplineObject>();

            //Create so
            if (newSo == null)
            {
                if(skipUndo)
                    newSo = transform.gameObject.AddComponent<SplineObject>();
                else
                {
                    newSo = UnityEditor.Undo.AddComponent<SplineObject>(transform.gameObject);
                    UnityEditor.Undo.RecordObject(newSo, "Attache SplineObject");
                }

                if (hasParent)
                {
                    newSo.localSplinePosition = newSo.transform.localPosition;
                    newSo.localSplineRotation = newSo.transform.localRotation;
                }
                else
                {
                    if (transform.gameObject.GetComponentCount() > 2 || transform.childCount > 0)
                    {
                        newSo.splinePosition = spline.WorldPositionToSplinePosition(newSo.transform.position, 12);
                        newSo.splineRotation = spline.WorldRotationToSplineRotation(newSo.transform.rotation, newSo.splinePosition.z / spline.length);
                        newSo.transform.localPosition = newSo.localSplinePosition;
                        newSo.transform.localRotation = newSo.localSplineRotation;
                    }
                    else
                    {
                        newSo.splinePosition = Vector3.zero;
                        newSo.splineRotation = Quaternion.identity;
                    }
                }

                newSo.monitor.UpdatePosRotSplineSpace();

                if (newSo.type != SplineObject.Type.DEFORMATION)
                    return;

                newSo.monitor.ForceUpdate();

                //Auto set type to FOLLOWER if no mesh containers are present
                if (newSo.meshContainers.Count == 0 && newSo.gameObject.GetComponentCount() > 2)
                    newSo.type = SplineObject.Type.FOLLOWER;
            }
        }

        public static Mesh GetOriginMeshFromMeshNameId(Mesh mesh)
        {
            Mesh originMesh = null;

            string[] data = mesh.name.Split('*');
            if (data.Length == 3)
            {
                if (int.TryParse(data[1], out int id))
                {
                    Object obj = UnityEditor.EditorUtility.InstanceIDToObject(id);
                    if (obj is Mesh) originMesh = (Mesh)UnityEditor.EditorUtility.InstanceIDToObject(id);
                }
            }

            return originMesh;
        }
    }
}
#endif