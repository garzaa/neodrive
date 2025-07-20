// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: MonitorSpline.cs
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
    public class MonitorSpline
    {
        public const int dataUsage = 24 
                                     + 12 + 12 + 12 + 12 + 4 + 4 + 4 + 4 + 4 + 4 + 4 + 4 + 12 + 12 + 16 + 1 + 1 + 4 + 4 + 4 + 1 + 8 + 8 + 8 + 18 + 36;

        private Vector3 achorSum;
        private Vector3 tangentASum;
        private Vector3 tangentBSum;
        private Vector3 anchor0;
        private int segmentCount;
        private float scaleXSum;
        private float scaleYSum;
        private float saddleSkewXSum;
        private float saddleSkewYSum;
        private float zRotationSum;
        private float contrastSum;
        private float length;
        private float noiseSum;
        private Vector3 position;
        private Vector3 scale;
        private Quaternion rotation;
        private bool calculateLocalPositions;
        private float resolutionNormal;
        private float resolutionSpline;
        private Spline.NormalType normalType;

        private ulong dirty;
        private ulong oldDirty;

        private Spline spline;

        private float[] floatContainer = new float[6];
        private Vector3[] Vector3Container = new Vector3[3];

        public MonitorSpline(Spline spline)
        {
            this.spline = spline;
            UpdateMaps();
            UpdatePosRotValues();
            UpdateZRotContSaddleScale();
            UpdateNoise();
            UpdateSegement();
            UpdateScale();
            UpdateLength();
#if UNITY_EDITOR
            UpdateChildCount();
            UpdateEditorDirty();
#endif
        }

        private void GetControlPointSums(Vector3[] sums)
        {
            sums[0].x = 0; sums[0].y = 0; sums[0].z = 0;
            sums[1].x = 0; sums[1].y = 0; sums[1].z = 0;
            sums[2].x = 0; sums[2].y = 0; sums[2].z = 0;

            foreach (Segment s in spline.segments)
            {
                sums[0] += s.GetPosition(Segment.ControlHandle.ANCHOR);
                sums[1] += s.GetPosition(Segment.ControlHandle.TANGENT_A);
                sums[2] += s.GetPosition(Segment.ControlHandle.TANGENT_B);
            }
        }

        private void GetZRotContSaddleSums(float[] sums)
        {
            sums[0] = 0; sums[1] = 0; sums[2] = 0; sums[3] = 0; sums[4] = 0; sums[5] = 0;

            foreach (Segment s in spline.segments)
            {
                sums[0] += s.zRotation;
                sums[1] += s.contrast;
                sums[2] += s.saddleSkew.x;
                sums[3] += s.saddleSkew.y;
                sums[4] += s.scale.x;
                sums[5] += s.scale.y;
            }
        }

        public float GetNoiseSum()
        {
            float value = 0;

            foreach (Segment s in spline.segments)
            {
                value += s.noise;
            }

            if (GeneralUtility.IsZero(value))
                return value;

            for (int i = 0; i < spline.noises.Count; i++)
            {
                if (!spline.noises[i].enabled)
                    continue;

                if (spline.noises[i].group != spline.noiseGroupMesh)
                    continue;

                value += Mathf.Abs(spline.noises[i].scale.x);
                value += Mathf.Abs(spline.noises[i].scale.y);
                value += Mathf.Abs(spline.noises[i].scale.z);
                value += Mathf.Abs(spline.noises[i].octaves);
                value += Mathf.Abs(spline.noises[i].frequency);
                value += Mathf.Abs(spline.noises[i].amplitude);
                value += spline.noises[i].seed;
                value += (int)spline.noises[i].type;
            }

            return value;
        }

        public bool IsDirty()
        {
            bool foundChange = false;
            if (oldDirty != dirty) foundChange = true;

            oldDirty = dirty;

            return foundChange;
        }

        public bool LengthChange(bool forceUpdate = false)
        {
            bool foundChange = false;
            if (!GeneralUtility.IsEqual(length, spline.length))
                foundChange = true;

            if (forceUpdate)
                length = spline.length;

            return foundChange;
        }

        public bool PosChange(bool forceUpdate = false)
        {
            bool foundChange = false;
            if (!GeneralUtility.IsEqual(position, spline.transform.position)) foundChange = true;

            if (forceUpdate)
                position = spline.transform.position;

            return foundChange;
        }

        public bool ScaleChange(bool forceUpdate = false)
        {
            bool foundChange = false;
            if (!GeneralUtility.IsEqual(scale, spline.transform.localScale)) foundChange = true;

            if (forceUpdate)
                scale = spline.transform.localScale;

            return foundChange;
        }

        public bool RotChange(bool forceUpdate = false)
        {
            bool foundChange = false;
            if (!GeneralUtility.IsEqual(rotation.eulerAngles, spline.transform.rotation.eulerAngles)) foundChange = true;

            if (forceUpdate)
                rotation = spline.transform.rotation;

            return foundChange;
        }

        public bool NormalTypeChange(bool forceUpdate = false)
        {
            bool foundChange = false;
            if (normalType != spline.normalType) foundChange = true;

            if (forceUpdate)
                normalType = spline.normalType;

            return foundChange;
        }

        public bool ResolutionChange()
        {
            bool foundChange = false;

            if(calculateLocalPositions != spline.calculateLocalPositions) foundChange = true;
            else if (!GeneralUtility.IsEqual(resolutionNormal, spline.GetResolutionNormal(true))) foundChange = true;
            else if (!GeneralUtility.IsEqual(resolutionSpline, spline.GetResolutionSpline(true))) foundChange = true;

            return foundChange;
        }

        public bool SegmentCountChange(out int dif, bool forceUpdate = false)
        {
            bool foundChange = false;
            dif = spline.segments.Count - segmentCount;

            if (segmentCount != spline.segments.Count) foundChange = true;

            if (forceUpdate)
                segmentCount = spline.segments.Count;

            return foundChange;
        }

        public bool SegementChange(bool forceUpdate = false)
        {
            bool foundChange = false;

            GetControlPointSums(Vector3Container);
            if(!GeneralUtility.IsEqual(spline.segments[0].GetPosition(Segment.ControlHandle.ANCHOR), anchor0)) foundChange = true;
            else if (!GeneralUtility.IsEqual(Vector3Container[0], achorSum)) foundChange = true;
            else if (!GeneralUtility.IsEqual(Vector3Container[1], tangentASum)) foundChange = true;
            else if (!GeneralUtility.IsEqual(Vector3Container[2], tangentBSum)) foundChange = true;

            if(forceUpdate)
            {
                achorSum = Vector3Container[0];
                tangentASum = Vector3Container[1];
                tangentBSum = Vector3Container[2];
            }

            return foundChange;
        }

        public bool ZRotContSaddleScaleChange(bool forceUpdate = false)
        {
            bool foundChange = false;

            GetZRotContSaddleSums(floatContainer);

            if (!GeneralUtility.IsEqual(floatContainer[0], zRotationSum)) foundChange = true;
            else if (!GeneralUtility.IsEqual(floatContainer[1], contrastSum)) foundChange = true;
            else if (!GeneralUtility.IsEqual(floatContainer[2], saddleSkewXSum)) foundChange = true;
            else if (!GeneralUtility.IsEqual(floatContainer[3], saddleSkewYSum)) foundChange = true;
            else if (!GeneralUtility.IsEqual(floatContainer[4], scaleXSum)) foundChange = true;
            else if (!GeneralUtility.IsEqual(floatContainer[5], scaleYSum)) foundChange = true;

            if (forceUpdate)
            {
                zRotationSum = floatContainer[0];
                contrastSum = floatContainer[1];
                saddleSkewXSum = floatContainer[2];
                saddleSkewYSum = floatContainer[3];
                scaleXSum = floatContainer[4];
                scaleYSum = floatContainer[5];
            }

            return foundChange;
        }

        public bool PosRotChange(bool forceUpdate = false)
        {
            bool foundChange = false;
            if (PosChange(forceUpdate)) foundChange = true;
            else if (RotChange(forceUpdate)) foundChange = true;

            return foundChange;
        }

        public bool NoiseChange(bool forceUpdate = false)
        {
            bool foundChange = false;

            float sum = GetNoiseSum();

            if (!GeneralUtility.IsEqual(sum, noiseSum)) foundChange = true;

            if (forceUpdate)
                noiseSum = sum;

            return foundChange;
        }

        public void UpdateNoise()
        {
            float sum = GetNoiseSum();
            noiseSum = sum;
        }

        public void UpdatePosRotValues()
        {
            position = spline.transform.position;
            rotation = spline.transform.rotation;
        }

        public void UpdateMaps()
        {
            calculateLocalPositions = spline.calculateLocalPositions;
            resolutionNormal = spline.GetResolutionNormal(true);
            resolutionSpline = spline.GetResolutionSpline(true);
        }

        public void UpdateZRotContSaddleScale()
        {
            GetZRotContSaddleSums(floatContainer);
            zRotationSum = floatContainer[0];
            contrastSum = floatContainer[1];
            saddleSkewXSum = floatContainer[2];
            saddleSkewYSum = floatContainer[3];
            scaleXSum = floatContainer[4];
            scaleYSum = floatContainer[5];
        }

        public void UpdateSegement()
        {
            GetControlPointSums(Vector3Container);

            achorSum = Vector3Container[0];
            tangentASum = Vector3Container[1];
            tangentBSum = Vector3Container[2];

            if(spline.segments.Count > 0)
                anchor0 = spline.segments[0].GetPosition(Segment.ControlHandle.ANCHOR);

            segmentCount = spline.segments.Count;
        }

        public void UpdateNormalType()
        {
            normalType = spline.normalType;
        }

        public void UpdateScale()
        {
            scale = spline.transform.localScale;
        }

        public void UpdateLength()
        {
            length = spline.length;
        }

        public void MarkDirty()
        {
            dirty++;
        }

#if UNITY_EDITOR
        private int childCount;
        private int editorDirty;

        public bool ChildCountChange(out int dif, bool forceUpdate = false)
        {
            dif = 0;

            bool foundChange = false;
            if (spline.transform.childCount != childCount)
            {
                dif = spline.transform.childCount - childCount;
                foundChange = true;
            }

            if (forceUpdate)
                childCount = spline.transform.childCount;

            return foundChange;
        }

        public void UpdateChildCount()
        {
            childCount = spline.transform.childCount;
        }

        public bool IsEditorDirty()
        {
            bool foundChange = false;
            if (editorDirty != spline.editorDirty) foundChange = true;

            editorDirty = spline.editorDirty;

            return foundChange;
        }

        private void UpdateEditorDirty()
        {
            editorDirty = spline.editorDirty;
        }

        public void MarkEditorDirty()
        {
            spline.editorDirty++;
        }
#endif
    }
}
