// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: Spline_Editor.cs
//
// Author: Mikael Danielsson
// Date Created: 25-03-2023
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

using UnityEngine;

using SplineArchitect.Utility;
using SplineArchitect.Monitor;

using Vector3 = UnityEngine.Vector3;

namespace SplineArchitect.Objects
{
    public partial class Spline : MonoBehaviour
    {
#if UNITY_EDITOR
        //ONLY EDITOR DATA

        //General, stored
        [HideInInspector]
        public Color color = new Color(0, 0, 0, 1);
        [HideInInspector]
        public int editorDirty;
        //General, runtime
        [NonSerialized]
        public string editorId;
        [NonSerialized]
        public int vertecies;
        [NonSerialized]
        public int deformations;
        [NonSerialized]
        public int deformationsInBuild;
        [NonSerialized]
        public int followers;
        [NonSerialized]
        public int followersInBuild;
        [NonSerialized]
        public float deformationsMemoryUsage;
        [NonSerialized]
        public bool initalizedEditor;
        [NonSerialized]
        public bool initalizedLinks;
        [NonSerialized]
        public bool disableOnTransformChildrenChanged;

        //Grid data, stored
        [HideInInspector]
        public Vector3 gridCenterPoint;

        //Selection data, stored
        [HideInInspector]
        public int selectedControlPoint;
        [HideInInspector]
        public List<int> selectedAnchors = new List<int>();
        [HideInInspector]
        public string selectedSplineMenu = "deformation";
        [HideInInspector]
        public string selectedAnchorMenu = "general";
        [HideInInspector]
        public string selectedObjectMenu = "general";
        //Selection data, runtime
        [NonSerialized]
        public int indicatorSegement;
        [NonSerialized]
        public Vector3 indicatorPosition;
        [NonSerialized]
        public float indicatorTime;
        [NonSerialized]
        public Vector3 indicatorDirection;
        [NonSerialized]
        public float indicatorDistanceToSpline;

        private void OnTransformChildrenChanged()
        {
            if (disableOnTransformChildrenChanged)
                return;

            monitor.ChildCountChange(out int dif);
            monitor.UpdateChildCount();

            if (dif > 0)
            {
                for (int i = 0; i < transform.childCount; i++)
                {
                    Transform child = transform.GetChild(i);
                    ESplineObjectUtility.TryAttacheOnTransformEditor(this, child, false);

                    SplineObject so = child.GetComponent<SplineObject>();

                    if (so == null || so.type != SplineObject.Type.DEFORMATION)
                        continue;

                    foreach (Transform child2 in child.GetComponentsInChildren<Transform>())
                    {
                        if (child2 == child)
                            continue;

                        ESplineObjectUtility.TryAttacheOnTransformEditor(this, child2, true);
                    }
                }
            }
        }

        public void DeselectAllNoiseLayers()
        {
            for (int i = 0; i < noises.Count; i++)
            {
                NoiseLayer nl = noises[i];
                nl.selected = false;
                noises[i] = nl;
            }
        }

        public Vector3 GetPositionFastWorld(float time)
        {
            if (positionMap.Length <= 0)
                return GetPosition(time, Space.World);

            float indexValue = time / GetResolutionSpline();

            int lmIndex = (int)Mathf.Floor(indexValue);
            int lmIndex2 = lmIndex + 1;
            float mod = indexValue;
            if (indexValue > 1) mod = indexValue % lmIndex;

            if (lmIndex > positionMap.Length - 1)
                lmIndex = positionMap.Length - 1;

            if (lmIndex < 0)
                lmIndex = 0;

            if (lmIndex2 > positionMap.Length - 1)
                lmIndex2 = positionMap.Length - 1;

            if (lmIndex2 < 0)
                lmIndex2 = 0;

            return positionMap[lmIndex] + ((positionMap[lmIndex2] - positionMap[lmIndex]) * mod);
        }

        public float GetNearestTimeRough(Vector3 point, float startTime, float endTime, float stepSize, bool ignoreYAxel = false)
        {
            float timeValue = -1;
            float distance = 999999;

            for (float t = startTime; t < endTime; t += stepSize)
            {
                Calculate(t);
            }

            void Calculate(float time)
            {
                //Even out big spaces by using fixed time instead.
                float fixedT = TimeToFixedTime(time);
                Vector3 bezierPoint = GetPositionFastWorld(fixedT);
                float d2 = ignoreYAxel ? Vector2.Distance(new Vector2(bezierPoint.x, bezierPoint.z), new Vector2(point.x, point.z)) : Vector3.Distance(bezierPoint, point);

                if (d2 < distance)
                {
                    timeValue = fixedT;
                    distance = d2;
                }
            }

            return timeValue;
        }

        public float GetNearestTime(Vector3 point, int precision, float startTime, float endTime, float steps = 5, bool ignoreYAxel = false)
        {
            steps = 100 / length / steps;
            if (steps > 0.2f) steps = 0.2f;
            if (steps < 0.0001f) steps = 0.0001f;
            float timeValue = GetNearestTimeRough(point, startTime, endTime, steps, ignoreYAxel);

            for (int i = precision; i > 0; i--)
            {
                //Needs to be lower then 1.999f here. Cant get fixedTime to work, likely dont work in this case. Thats why 1.999f cant be used. 1.35f - 1.66f seems to work good with extreme splines.
                steps = steps / 1.66f;
                float timeForwards = timeValue + steps;
                float timeBackwards = timeValue - steps;
                timeForwards = SplineUtility.GetValidatedTime(timeForwards, loop);
                timeBackwards = SplineUtility.GetValidatedTime(timeBackwards, loop);

                Vector3 pForward = GetPositionFastWorld(timeForwards);
                float dForward = ignoreYAxel ? Vector2.Distance(new Vector2(pForward.x, pForward.z), new Vector2(point.x, point.z)) : Vector3.Distance(pForward, point);

                Vector3 pBackwards = GetPositionFastWorld(timeBackwards);
                float dBackwards = ignoreYAxel ? Vector2.Distance(new Vector2(pBackwards.x, pBackwards.z), new Vector2(point.x, point.z)) : Vector3.Distance(pBackwards, point);

                if (dForward > dBackwards)
                {
                    timeValue = timeBackwards;
                }
                else
                {
                    timeValue = timeForwards;
                }
            }

            return timeValue;
        }

        private void SetInterpolationModeForNewSegment(Segment segment, int index)
        {
            if (segments.Count > 1)
            {
                if (index > 0)
                {
                    if(index == segments.Count - 1)
                    {
                        if (segments[index - 1].GetInterpolationType() == Segment.InterpolationType.LINE)
                            segment.SetInterpolationMode(Segment.InterpolationType.LINE);

                        return;
                    }

                    float d1 = Vector3.Distance(segments[index].GetPosition(Segment.ControlHandle.ANCHOR), segments[index - 1].GetPosition(Segment.ControlHandle.ANCHOR));
                    float d2 = Vector3.Distance(segments[index].GetPosition(Segment.ControlHandle.ANCHOR), segments[index + 1].GetPosition(Segment.ControlHandle.ANCHOR));

                    if (d1 > d2 && segments[index + 1].GetInterpolationType() == Segment.InterpolationType.LINE)
                        segment.SetInterpolationMode(Segment.InterpolationType.LINE);
                    else if (d2 > d1 && segments[index - 1].GetInterpolationType() == Segment.InterpolationType.LINE)
                        segment.SetInterpolationMode(Segment.InterpolationType.LINE);
                }
                else if (index == 0 && segments[index + 1].GetInterpolationType() == Segment.InterpolationType.LINE)
                    segment.SetInterpolationMode(Segment.InterpolationType.LINE);
            }
        }
#endif
    }
}
