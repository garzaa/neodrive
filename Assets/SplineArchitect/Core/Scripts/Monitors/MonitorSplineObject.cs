// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: MonitorSplineObject.cs
//
// Author: Mikael Danielsson
// Date Created: 30-01-2023
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using UnityEngine;

using SplineArchitect.Objects;
using SplineArchitect.Utility;

namespace SplineArchitect.Monitor
{
    public class MonitorSplineObject
    {
        public const int dataUsage = 24 + 
                                     1 + 4 + 12 + 12 + 12 + 16 + 16 + 12 + 12 + 12 + 1 + 1 + 1 + 1 + 1;

        private bool mirrored;
        private bool lockPosition;
        private SplineObject.SnapMode snapMode;
        private float snapLengthStart;
        private float snapLengthEnd;
        private float snapOffsetStart;
        private float snapOffsetEnd;
        private float splineLength;
        private Vector3 position;
        private Vector3 localScale;
        private Vector3 localSplinePosition;
        private Quaternion rotation;
        private Quaternion localSplineRotation;
        private Vector3 combinedParentPositionInSplineSpace;
        private Vector3 combinedParentEulerInSplineSpace;
        private Vector3 combinedParentLocalScale;
        private Transform parent;
        private SplineObject.NormalType normalType;
        private SplineObject.TangentType tangentType;
        private SplineObject.Type type;
        private SplineObject so;

        public MonitorSplineObject(SplineObject so)
        {
            this.so = so;
            UpdatePosRotSplineSpace();
            UpdatePosRot();
            UpdateScaleMirrorNormalTangent();
            UpdateState();
            UpdateParent();
            UpdateCombinedParentPosRotScaleChange();
            UpdateSplineLength(so.splineParent.length);
            UpdateExtra();

#if UNITY_EDITOR
            UpdateComponentCount();
            UpdateChildCount();
#endif
        }

        public bool StateChange(out SplineObject.Type oldType)
        {
            bool foundChange = false;
            if (so.type != type)
                foundChange = true;

            oldType = type;
            type = so.type;

            return foundChange;
        }

        public bool ParentChange(bool forceUpdate = false)
        {
            bool foundChange = false;
            if (so.transform.parent != parent)
                foundChange = true;

            if (forceUpdate)
                parent = so.transform.parent;

            return foundChange;
        }

        public bool NormalChange(bool forceUpdate = false)
        {
            bool foundChange = false;
            if (so.normalType != normalType)
                foundChange = true;

            if (forceUpdate)
                normalType = so.normalType;

            return foundChange;
        }

        public bool TangentChange(bool forceUpdate = false)
        {
            bool foundChange = false;
            if (so.tangentType != tangentType)
                foundChange = true;

            if (forceUpdate)
                tangentType = so.tangentType;

            return foundChange;
        }

        public bool MirrorChange(bool forceUpdate = false)
        {
            bool foundChange = false;
            if (so.mirrorDeformation != mirrored)
                foundChange = true;

            if (forceUpdate)
                mirrored = so.mirrorDeformation;

            return foundChange;
        }

        public bool ExtraChange(bool forceUpdate = false)
        {
            bool foundChange = false;

            if (so.lockPosition != lockPosition)
                foundChange = true;

            if (so.snapMode != snapMode)
                foundChange = true;

            if(!GeneralUtility.IsEqual(so.snapLengthStart, snapLengthStart))
                foundChange = true;

            if (!GeneralUtility.IsEqual(so.snapLengthEnd, snapLengthEnd))
                foundChange = true;

            if (!GeneralUtility.IsEqual(so.snapOffsetStart, snapOffsetStart))
                foundChange = true;

            if (!GeneralUtility.IsEqual(so.snapOffsetEnd, snapOffsetEnd))
                foundChange = true;

            if (forceUpdate)
            {
                snapMode = so.snapMode;
                lockPosition = so.lockPosition;
                snapLengthStart = so.snapLengthStart;
                snapLengthEnd = so.snapLengthEnd;
                snapOffsetStart = so.snapOffsetStart;
                snapOffsetEnd = so.snapOffsetEnd;
            }

            return foundChange;
        }

        public bool PosChange(bool forceUpdate = false)
        {
            bool foundChange = false;
            if (!GeneralUtility.IsEqual(position, so.transform.position)) foundChange = true;

            if (forceUpdate)
                position = so.transform.position;

            return foundChange;
        }

        public bool RotChange(bool forceUpdate = false)
        {
            bool foundChange = false;
            if (!GeneralUtility.IsEqual(rotation.eulerAngles, so.transform.rotation.eulerAngles)) foundChange = true;

            if(forceUpdate)
                rotation = so.transform.rotation;

            return foundChange;
        }

        public bool ScaleChange(bool forceUpdate = false)
        {
            bool foundChange = false;
            if (!GeneralUtility.IsEqual(localScale, so.transform.localScale)) foundChange = true;

            if (forceUpdate)
                localScale = so.transform.localScale;

            return foundChange;
        }

        public bool SplineLengthChange(float newSplineLength, bool forceUpdate = false)
        {
            bool foundChange = false;
            if (!GeneralUtility.IsEqual(splineLength, newSplineLength)) foundChange = true;

            if (forceUpdate)
                splineLength = newSplineLength;

            return foundChange;
        }

        public bool PosSplineSpaceChange(bool forceUpdate = false)
        {
            bool foundChange = false;
            if (!GeneralUtility.IsEqual(localSplinePosition, so.localSplinePosition)) foundChange = true;

            if (forceUpdate)
                localSplinePosition = so.localSplinePosition;

            return foundChange;
        }

        public bool RotSplineSpaceChange(bool forceUpdate = false)
        {
            bool foundChange = false;
            if (!GeneralUtility.IsEqual(localSplineRotation.eulerAngles, so.localSplineRotation.eulerAngles)) foundChange = true;

            if (forceUpdate)
                localSplineRotation = so.localSplineRotation;

            return foundChange;
        }

        public bool PosRotChange(bool forceUpdate = false)
        {
            bool foundChange = false;
            if (PosChange(forceUpdate)) foundChange = true;
            if (RotChange(forceUpdate)) foundChange = true;

            return foundChange;
        }

        public bool PosRotSplineSpaceChange(bool forceUpdate = false)
        {
            bool foundChange = false;
            if (PosSplineSpaceChange(forceUpdate)) foundChange = true;
            if (RotSplineSpaceChange(forceUpdate)) foundChange = true;

            return foundChange;
        }

        public bool CombinedParentPosRotScaleChange(bool forceUpdate = false)
        {
            bool foundChange = false;
            SplineObject currAco = so;

            Vector3 pos = Vector3.zero;
            Vector3 euler = Vector3.zero;
            Vector3 scale = Vector3.zero;

            for (int i = 0; i < 25; i++)
            {
                currAco = currAco.soParent;
                if (currAco == null)
                    break;

                pos += currAco.localSplinePosition;
                euler += currAco.localSplineRotation.eulerAngles;
                scale += currAco.transform.localScale;
            }

            if (!GeneralUtility.IsEqual(pos, combinedParentPositionInSplineSpace)) foundChange = true;
            if (!GeneralUtility.IsEqual(euler, combinedParentEulerInSplineSpace)) foundChange = true;
            if (!GeneralUtility.IsEqual(scale, combinedParentLocalScale)) foundChange = true;

            if(forceUpdate)
            {
                combinedParentPositionInSplineSpace = pos;
                combinedParentEulerInSplineSpace = euler;
                combinedParentLocalScale = scale;
            }

            return foundChange;
        }

        public bool ScaleNormalTangentChange(bool forceUpdate = false)
        {
            bool foundChange = false;
            if (ScaleChange(forceUpdate)) foundChange = true;
            if (NormalChange(forceUpdate)) foundChange = true;
            if (TangentChange(forceUpdate)) foundChange = true;

            return foundChange;
        }

        public void UpdateState()
        {
            type = so.type;
        }

        public void UpdateParent()
        {
            parent = so.transform.parent;
        }

        public void UpdatePosRotSplineSpace()
        {
            localSplinePosition = so.localSplinePosition;
            localSplineRotation = so.localSplineRotation;
        }

        public void UpdatePosRot()
        {
            position = so.transform.position;
            rotation = so.transform.rotation;
        }

        public void UpdateCombinedParentPosRotScaleChange()
        {
            SplineObject currAco = so;
            Vector3 pos = Vector3.zero;
            Vector3 euler = Vector3.zero;
            Vector3 scale = Vector3.zero;

            for (int i = 0; i < 25; i++)
            {
                currAco = currAco.soParent;
                if (currAco == null)
                    break;

                pos += currAco.localSplinePosition;
                euler += currAco.localSplineRotation.eulerAngles;
                scale += currAco.transform.localScale;
            }

            combinedParentPositionInSplineSpace = pos;
            combinedParentEulerInSplineSpace = euler;
            combinedParentLocalScale = scale;
        }

        public void UpdateScaleMirrorNormalTangent()
        {
            mirrored = so.mirrorDeformation;
            localScale = so.transform.localScale;
            normalType = so.normalType;
            tangentType = so.tangentType;
        }

        public void UpdateSplineLength(float newSplineLength)
        {
            splineLength = newSplineLength;
        }

        public void UpdateExtra()
        {
            snapMode = so.snapMode;
            lockPosition = so.lockPosition;
            snapLengthStart = so.snapLengthStart;
            snapLengthEnd = so.snapLengthEnd;
            snapOffsetStart = so.snapOffsetStart;
            snapOffsetEnd = so.snapOffsetEnd;
        }

        public void ForceUpdate()
        {
            localSplinePosition.x++;
        }

#if UNITY_EDITOR
        private int childCount;
        private int componentCount;

        public bool ChildCountChange(out int dif, bool forceUpdate = false)
        {
            dif = 0;

            bool foundChange = false;
            if (so.transform.childCount != childCount)
            {
                dif = so.transform.childCount - childCount;
                foundChange = true;
            }

            if (forceUpdate)
                childCount = so.transform.childCount;

            return foundChange;
        }

        public void UpdateChildCount()
        {
            childCount = so.transform.childCount;
        }

        public bool ComponentCountChange(bool forceUpdate = false)
        {
            bool foundChange = false;
            if (so.gameObject.GetComponentCount() != componentCount)
            {
                foundChange = true;
            }

            if (forceUpdate)
                componentCount = so.gameObject.GetComponentCount();

            return foundChange;
        }

        public void UpdateComponentCount()
        {
            componentCount = so.gameObject.GetComponentCount();
        }
#endif
    }
}
