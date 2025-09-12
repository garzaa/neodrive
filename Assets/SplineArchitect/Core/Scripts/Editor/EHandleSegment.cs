// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: EHandleSegment.cs
//
// Author: Mikael Danielsson
// Date Created: 03-03-2024
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using SplineArchitect.Objects;
using SplineArchitect.Utility;
using System;
using System.Collections.Generic;
using System.Drawing;
using UnityEditor;
using UnityEngine;

namespace SplineArchitect
{
    public class EHandleSegment
    {
        public struct SegmentIndicatorData
        {
            public Vector3 anchor;
            public Vector3 tangent;
            public Vector3 newAnchor;
            public Vector3 newTangentA;
            public Vector3 newTangentB;
            public bool originFromStart;

            //Only grid
            public Spline gridSpline;
        }

        private static List<int> markedSegments = new List<int>();
        private static Plane projectionPlane = new Plane(Vector3.up, Vector3.zero);
        private static Vector3[] normalsContainer = new Vector3[3];

        public static void HandleDeletion(Spline spline, Event e)
        {
            if (EHandleModifier.DeleteActive(e))
            {
                MarkForDeletion(SplineUtility.ControlPointIdToSegmentIndex(spline.selectedControlPoint));

                foreach (int i in spline.selectedAnchors)
                {
                    MarkForDeletion(SplineUtility.ControlPointIdToSegmentIndex(i));
                }

                DeleteAndUnlinkMarked(spline, true);
                spline.selectedAnchors.Clear();
                e.Use();
            }


            //Dont delete Selection.activeTransform if controlHandle is selected.
            if (spline.selectedControlPoint > 0 && e.type == EventType.KeyDown && e.keyCode == KeyCode.Delete)
                e.Use();

#if UNITY_EDITOR_OSX
            //Dont delete Selection.activeTransform if controlHandle is selected.
            if (spline.selectedControlPoint > 0 && e.command && e.keyCode == KeyCode.Backspace && e.type == EventType.KeyDown)
                e.Use();
#endif
        }

        public static void MarkForDeletion(int segement)
        {
            markedSegments.Add(segement);
        }

        public static void DeleteAndUnlinkMarked(Spline spline, bool updateControlPointSelection)
        {
            markedSegments.Sort();

            for (int i = markedSegments.Count - 1; i >= 0; i--)
            {
                int segmentIndex = markedSegments[i];

                if (segmentIndex < 0 || segmentIndex >= spline.segments.Count)
                    continue;

                Segment s = spline.segments[segmentIndex];

                if(s.links != null)
                {
                    //Unlink on other segments
                    foreach (Segment s2 in s.links)
                    {
                        if (s2 == s)
                            continue;

                        if (s2.links.Count > 2)
                            continue;

                        EHandleUndo.RecordNow(s2.splineParent, "Delete segement: " + segmentIndex);
                        s2.linkTarget = Segment.LinkTarget.NONE;
                    }
                }

                //If deleteing an Spline very fast after selecting it, the segement will be -333 and it will go into this if statement if "segement >= 0" is not here.
                //In this case the Spline should be deleted.
                if (spline.segments.Count > 1 && segmentIndex >= 0)
                {
                    EHandleUndo.RecordNow(spline, "Delete segement: " + segmentIndex);
                    if (segmentIndex > spline.segments.Count - 1)
                        spline.RemoveSegment(spline.segments.Count - 1);
                    else
                        spline.RemoveSegment(segmentIndex);

                    if (spline.loop)
                    {
                        if(segmentIndex == 0)
                        {
                            spline.segments[spline.segments.Count - 1].SetPosition(Segment.ControlHandle.ANCHOR, spline.segments[0].GetPosition(Segment.ControlHandle.ANCHOR));
                            spline.segments[spline.segments.Count - 1].SetPosition(Segment.ControlHandle.TANGENT_A, spline.segments[0].GetPosition(Segment.ControlHandle.TANGENT_A));
                            spline.segments[spline.segments.Count - 1].SetPosition(Segment.ControlHandle.TANGENT_B, spline.segments[0].GetPosition(Segment.ControlHandle.TANGENT_B));
                        }

                        if (spline.segments.Count == 2)
                        {
                            spline.RemoveSegment(spline.segments.Count - 1);
                            spline.loop = false;
                        }
                    }

                    if (updateControlPointSelection)
                    {
                        //If last selected cp was deleted and the spline is looped we need to select the second last cp.
                        if (spline.loop && segmentIndex >= spline.segments.Count - 1)
                            EHandleSelection.SelectPrimaryControlPoint(spline, SplineUtility.SegmentIndexToControlPointId(spline.segments.Count - 2, Segment.ControlHandle.ANCHOR));
                        else if (segmentIndex == 0)
                            EHandleSelection.SelectPrimaryControlPoint(spline, SplineUtility.SegmentIndexToControlPointId(0, Segment.ControlHandle.ANCHOR));
                        else if (segmentIndex < spline.segments.Count && segmentIndex > 0)
                            EHandleSelection.SelectPrimaryControlPoint(spline, SplineUtility.SegmentIndexToControlPointId(segmentIndex, Segment.ControlHandle.ANCHOR));
                        else if (segmentIndex >= spline.segments.Count - 1)
                            EHandleSelection.SelectPrimaryControlPoint(spline, SplineUtility.SegmentIndexToControlPointId(spline.segments.Count - 1, Segment.ControlHandle.ANCHOR));
                    }

                }
                else
                {
                    EHandleUndo.RecordNow(spline);
                    spline.RemoveSegment(0);

                    EHandleUndo.MarkSplineForDestroy(spline);
                }
            }

            markedSegments.Clear();
        }

        public static void HandleLinking(Spline spline)
        {
            for (int i = 0; i < spline.segments.Count; i++)
            {
                Segment s = spline.segments[i];

                if (s.linkTarget != s.oldLinkTarget)
                {
                    s.oldLinkTarget = s.linkTarget;

                    //In unity 2022 when appying changes to a prefab, the spline parent will be null.
                    s.splineParent = spline;

                    if (s.linkTarget == Segment.LinkTarget.ANCHOR)
                    {
                        s.LinkToAnchor(s.GetPosition(Segment.ControlHandle.ANCHOR));
                    }
                    else if (s.linkTarget == Segment.LinkTarget.SPLINE_CONNECTOR)
                    {
                        s.LinkToConnector(s.GetPosition(Segment.ControlHandle.ANCHOR));
                    }
                    else
                    {
                        s.Unlink();
                    }
                }
            }
        }

        public static void LinkMovementAll(Spline spline)
        {
            foreach (Segment s in spline.segments)
            {
                if (s.links == null)
                    continue;

                if (s.links.Count == 0)
                    continue;

                LinkMovement(s);
            }
        }

        public static void LinkMovement(Segment s)
        {
            if (s.links == null)
                return;

            Vector3 newPosition = s.GetPosition(Segment.ControlHandle.ANCHOR);

            foreach (Segment s2 in s.links)
            {
                if (s2 == s)
                    continue;

                if (s2.localSpace == null)
                {
                    Debug.LogWarning("Segment has no local space set.");
                    continue;
                }

                Vector3 dif = s2.GetPosition(Segment.ControlHandle.ANCHOR) - newPosition;

                EHandleUndo.RecordNow(s2.splineParent);
                s2.Translate(Segment.ControlHandle.ANCHOR, dif);
                s2.Translate(Segment.ControlHandle.TANGENT_A, dif);
                s2.Translate(Segment.ControlHandle.TANGENT_B, dif);

                if(s2.splineParent.loop && s2.splineParent.segments[0] == s2)
                {
                    int last = s2.splineParent.segments.Count - 1;

                    s2.splineParent.segments[last].Translate(Segment.ControlHandle.ANCHOR, dif);
                    s2.splineParent.segments[last].Translate(Segment.ControlHandle.TANGENT_A, dif);
                    s2.splineParent.segments[last].Translate(Segment.ControlHandle.TANGENT_B, dif);
                }
            }
        }

        public static void SegmentMovement(Spline spline, Segment segment, Segment.ControlHandle controlHandle, Vector3 newPosition)
        {
            //In some cases when working with prefabs the spline parent can be null. So we set it here before handling the segment.
            segment.splineParent = spline;

            HandleType handleType = GlobalSettings.GetHandleType();
            spline.monitor.UpdateSegement();

            if (handleType == HandleType.CONTINUOUS)
            {
                if (controlHandle == Segment.ControlHandle.ANCHOR)
                {
                    Vector3 dif = segment.GetPosition(Segment.ControlHandle.ANCHOR) - newPosition;
                    EHandleSelection.UpdateSelectedAnchors(spline, (selected) =>
                    {
                        selected.TranslateAnchor(dif);
                        LinkMovement(selected);
                    });
                }
                else
                {
                    segment.SetContinuousPosition(controlHandle, newPosition);
                }
            }
            else if (handleType == HandleType.MIRRORED)
            {
                if (controlHandle == Segment.ControlHandle.ANCHOR)
                {
                    Vector3 dif = segment.GetPosition(Segment.ControlHandle.ANCHOR) - newPosition;
                    EHandleSelection.UpdateSelectedAnchors(spline, (selected) =>
                    {
                        selected.TranslateAnchor(dif);
                        LinkMovement(selected);
                    });
                }
                else
                {
                    segment.SetMirroredPosition(controlHandle, newPosition);
                }
            }
            else if (handleType == HandleType.BROKEN)
            {
                if (controlHandle == Segment.ControlHandle.ANCHOR)
                {
                    Vector3 dif = segment.GetPosition(Segment.ControlHandle.ANCHOR) - newPosition;
                    EHandleSelection.UpdateSelectedAnchors(spline, (selected) =>
                    {
                        selected.TranslateAnchor(dif);
                        LinkMovement(selected);
                    });
                }
                else
                    segment.SetBrokenPosition(controlHandle, newPosition);
            }
        }

        public static void UpdateIndicator3DGrid(Spline spline, Event e, ref SegmentIndicatorData segementIndicatorData)
        {
            Vector3 direction = Vector3.forward;
            Vector3 anchor = Vector3.zero;
            Vector3 tangent = Vector3.zero;
            bool start = false;

            Ray mouseRay = EMouseUtility.GetMouseRay(e.mousePosition);

            projectionPlane.SetNormalAndPosition(spline.transform.up, spline.transform.TransformPoint(spline.gridCenterPoint));
            if (projectionPlane.Raycast(mouseRay, out float enter))
            {
                Vector3 point = mouseRay.GetPoint(enter);

                direction = -spline.segments[spline.segments.Count - 1].GetDirection();
                anchor = spline.segments[spline.segments.Count - 1].GetPosition(Segment.ControlHandle.ANCHOR);
                tangent = spline.segments[spline.segments.Count - 1].GetPosition(Segment.ControlHandle.TANGENT_A);

                float distanceToStart = Vector3.Distance(spline.segments[0].GetPosition(Segment.ControlHandle.ANCHOR) + direction, point);
                float distanceToEnd = Vector3.Distance(spline.segments[spline.segments.Count - 1].GetPosition(Segment.ControlHandle.ANCHOR) - direction, point);
                start = distanceToStart < distanceToEnd;

                if (spline.segments.Count == 1)
                    start = !start;

                if (start)
                {
                    direction = -spline.segments[0].GetDirection();
                    anchor = spline.segments[0].GetPosition(Segment.ControlHandle.ANCHOR);
                    tangent = spline.segments[0].GetPosition(Segment.ControlHandle.TANGENT_B);
                }
            }

            projectionPlane.SetNormalAndPosition(spline.transform.up, anchor);
            if (projectionPlane.Raycast(mouseRay, out float enter2))
            {
                Vector3 point = mouseRay.GetPoint(enter2);

                point = EHandleGrid.SnapPoint(spline, point);

                Vector3 direction90 = Vector3.Cross(direction, spline.transform.up);
                Vector3 closestPoint = Utility.LineUtility.GetNearestPoint(anchor, direction, point, out _);
                Utility.LineUtility.GetNearestPoint(anchor, direction90, point, out float time);
                float sign = Mathf.Sign(time) * (start ? -1 : 1);
                direction90 = direction90 * sign;

                segementIndicatorData.gridSpline = spline;
                segementIndicatorData.anchor = anchor;
                segementIndicatorData.tangent = tangent;
                segementIndicatorData.newAnchor = point;
                segementIndicatorData.originFromStart = start;

                if (GeneralUtility.IsEqual(closestPoint, point, 0.1f))
                {
                    Vector3 newTa = point + direction * Vector3.Distance(anchor, tangent);
                    newTa = EHandleGrid.SnapPoint(spline, newTa);

                    Vector3 newTb = point - direction * Vector3.Distance(anchor, tangent);
                    newTb = EHandleGrid.SnapPoint(spline, newTb);

                    segementIndicatorData.newTangentA = newTa;
                    segementIndicatorData.newTangentB = newTb;
                }
                else
                {
                    Vector3 newTa = point + direction90 * Vector3.Distance(anchor, tangent);
                    newTa = EHandleGrid.SnapPoint(spline, newTa);

                    Vector3 newTb = point - direction90 * Vector3.Distance(anchor, tangent);
                    newTb = EHandleGrid.SnapPoint(spline, newTb);

                    segementIndicatorData.newTangentA = newTa;
                    segementIndicatorData.newTangentB = newTb;
                }

                if ((start && spline.segments[0].GetInterpolationType() == Segment.InterpolationType.LINE) ||
                    (!start && spline.segments[spline.segments.Count - 1].GetInterpolationType() == Segment.InterpolationType.LINE))
                {
                    segementIndicatorData.tangent = anchor;
                    segementIndicatorData.newTangentA = point;
                    segementIndicatorData.newTangentB = point;
                }
            }
        }

        public static void UpdateIndicator3D(Spline spline, Event e, ref SegmentIndicatorData segementIndicatorData)
        {
            //Data
            float disBetweenTangents = 10f;
            Vector3 direction = -spline.segments[spline.segments.Count - 1].GetDirection();
            Vector3 anchor = spline.segments[spline.segments.Count - 1].GetPosition(Segment.ControlHandle.ANCHOR);
            Vector3 tangent = spline.segments[spline.segments.Count - 1].GetPosition(Segment.ControlHandle.TANGENT_A) + direction * DistanceModifier(spline.segments[spline.segments.Count - 1].GetPosition(Segment.ControlHandle.TANGENT_A), disBetweenTangents);

            //Try hit terrain
            Vector3 anchorHitPoint = Vector3.zero;
            bool hitTerrain = false;
            RaycastHit hit;
            Ray mouseRay = EMouseUtility.GetMouseRay(e.mousePosition);
            if (Physics.Raycast(mouseRay, out hit))
            {
                anchorHitPoint = hit.point;
                hitTerrain = true;
            }
            else
            {
                Vector3 center = spline.GetCenter();
                projectionPlane.SetNormalAndPosition(Vector3.up, new Vector3(0, center.y, 0));
                if (!hitTerrain && projectionPlane.Raycast(mouseRay, out float enter))
                    anchorHitPoint = mouseRay.GetPoint(enter);
            }

            //Closest to start or end
            float disLastSegement = Vector3.Distance(spline.segments[spline.segments.Count - 1].GetPosition(Segment.ControlHandle.ANCHOR) + direction, anchorHitPoint);
            float disFirstSegement = Vector3.Distance(spline.segments[0].GetPosition(Segment.ControlHandle.ANCHOR) - direction, anchorHitPoint);
            bool start = disLastSegement > disFirstSegement;

            //Get y height of start or end for project plane
            Vector3 planPos = start ? spline.segments[0].GetPosition(Segment.ControlHandle.ANCHOR) : spline.segments[spline.segments.Count - 1].GetPosition(Segment.ControlHandle.ANCHOR);
            projectionPlane.SetNormalAndPosition(Vector3.up, new Vector3(0, planPos.y, 0));
            if (!hitTerrain && projectionPlane.Raycast(mouseRay, out float enter2))
                anchorHitPoint = mouseRay.GetPoint(enter2);

            //If start switch data direction
            if (start)
            {
                direction = -spline.segments[0].GetDirection();
                anchor = spline.segments[0].GetPosition(Segment.ControlHandle.ANCHOR);
                tangent = spline.segments[0].GetPosition(Segment.ControlHandle.TANGENT_B) - direction * DistanceModifier(spline.segments[0].GetPosition(Segment.ControlHandle.TANGENT_B), disBetweenTangents);
            }

            //Set control point data
            Vector3 newAnchor = anchorHitPoint;
            Vector3 cubicLerpPos1 = BezierUtility.CubicLerp(anchor, tangent, newAnchor, newAnchor, 0.99f);
            Vector3 cubicLerpPos2 = BezierUtility.CubicLerp(anchor, tangent, newAnchor, newAnchor, 1);
            Vector3 newEndDirection = (cubicLerpPos1 - cubicLerpPos2).normalized;
            Vector3 newFlatEndDirection = (new Vector3(cubicLerpPos1.x, newAnchor.y, cubicLerpPos1.z) - new Vector3(cubicLerpPos2.x, newAnchor.y, cubicLerpPos2.z)).normalized;

            //Set tangents
            float tangentDistance = DistanceModifier(newAnchor, disBetweenTangents);
            Vector3 newTangentA = newAnchor + newFlatEndDirection * (tangentDistance * (start ? 1 : -1));
            Vector3 newTangentB = newAnchor + newFlatEndDirection * (tangentDistance * (start ? -1 : 1));

            if (hitTerrain)
            {
                Vector3 editorCameraPosition = EHandleSceneView.GetCurrent().camera.transform.position;
                Vector3 origin = newAnchor + newFlatEndDirection * tangentDistance;
                Ray tangentRay = new Ray(new Vector3(origin.x, editorCameraPosition.y, origin.z), -Vector3.up);
                if (Physics.Raycast(tangentRay, out hit))
                {
                    if (start)
                    {
                        newTangentA = hit.point;
                        newTangentB = newAnchor + (newAnchor - newTangentA).normalized * tangentDistance;
                    }
                    else
                    {
                        newTangentB = hit.point;
                        newTangentA = newAnchor + (newAnchor - newTangentB).normalized * tangentDistance;
                    }
                }
            }

            //Set segment to closest spline direction
            if (EHandleModifier.CtrlActive(e))
            {
                Spline closest = SplineUtility.GetClosest(anchorHitPoint, HandleRegistry.GetSplines(), 20 * DistanceModifier(anchorHitPoint), out float time, out _);

                if (closest != null)
                {
                    closest.GetNormalsNonAlloc(normalsContainer, time);
                    float dot = Vector3.Dot(normalsContainer[2], newEndDirection);
                    if (dot < 0)
                    {
                        newTangentA = newAnchor - normalsContainer[2] * DistanceModifier(newAnchor, disBetweenTangents);
                        newTangentB = newAnchor + normalsContainer[2] * DistanceModifier(newAnchor, disBetweenTangents);
                    }
                    else
                    {
                        newTangentA = newAnchor + normalsContainer[2] * DistanceModifier(newAnchor, disBetweenTangents);
                        newTangentB = newAnchor - normalsContainer[2] * DistanceModifier(newAnchor, disBetweenTangents);
                    }
                }
            }

            //Set indicator data
            segementIndicatorData.anchor = anchor;
            segementIndicatorData.tangent = tangent;
            segementIndicatorData.newAnchor = newAnchor;
            segementIndicatorData.newTangentA = newTangentA;
            segementIndicatorData.newTangentB = newTangentB;
            segementIndicatorData.originFromStart = start;

            //If line
            if((start && spline.segments[0].GetInterpolationType() == Segment.InterpolationType.LINE) ||
               (!start && spline.segments[spline.segments.Count - 1].GetInterpolationType() == Segment.InterpolationType.LINE))
            {
                segementIndicatorData.tangent = anchor;
                segementIndicatorData.newTangentA = newAnchor;
                segementIndicatorData.newTangentB = newAnchor;
            }
        }

        public static void UpdateIndicator2D(Spline spline, Event e, ref SegmentIndicatorData segementIndicatorData)
        {
            float disBetweenTangents = 10f;
            Vector3 direction = Vector3.forward;
            Vector3 anchor = Vector3.zero;
            Vector3 tangent = Vector3.zero;
            bool start = false;

            Ray mouseRay = EMouseUtility.GetMouseRay(e.mousePosition);

            Vector3 center = (spline.segments[0].GetPosition(Segment.ControlHandle.ANCHOR) + spline.segments[spline.segments.Count - 1].GetPosition(Segment.ControlHandle.ANCHOR)) / 2;
            projectionPlane.SetNormalAndPosition(-spline.transform.forward, center);
            if (projectionPlane.Raycast(mouseRay, out float enter))
            {
                Vector3 point = mouseRay.GetPoint(enter);

                direction = -spline.segments[spline.segments.Count - 1].GetDirection();
                anchor = spline.segments[spline.segments.Count - 1].GetPosition(Segment.ControlHandle.ANCHOR);
                tangent = spline.segments[spline.segments.Count - 1].GetPosition(Segment.ControlHandle.TANGENT_A);

                float distanceToStart = Vector3.Distance(spline.segments[0].GetPosition(Segment.ControlHandle.ANCHOR) + direction, point);
                float distanceToEnd = Vector3.Distance(spline.segments[spline.segments.Count - 1].GetPosition(Segment.ControlHandle.ANCHOR) - direction, point);
                start = distanceToStart < distanceToEnd;

                if (spline.segments.Count == 1)
                    start = !start;

                if (start)
                {
                    direction = -spline.segments[0].GetDirection();
                    anchor = spline.segments[0].GetPosition(Segment.ControlHandle.ANCHOR);
                    tangent = spline.segments[0].GetPosition(Segment.ControlHandle.TANGENT_B);
                }
            }

            Vector3 anchorHitPoint = Vector3.zero;
            projectionPlane.SetNormalAndPosition(-spline.transform.forward, anchor);
            if (projectionPlane.Raycast(mouseRay, out float enter2))
                anchorHitPoint = mouseRay.GetPoint(enter2);

            //Set control point data
            Vector3 newAnchor = anchorHitPoint;
            Vector3 cubicLerpPos1 = BezierUtility.CubicLerp(anchor, tangent, newAnchor, newAnchor, 0.99f);
            Vector3 cubicLerpPos2 = BezierUtility.CubicLerp(anchor, tangent, newAnchor, newAnchor, 1);
            Vector3 newEndDirection = (cubicLerpPos1 - cubicLerpPos2).normalized;

            newAnchor = SnapPointToSplinePlane(newAnchor, spline.transform);
            newEndDirection = SnapDirToSplinePlane(newEndDirection, spline.transform);

            //Set tangents
            float tangentDistance = DistanceModifier(newAnchor, disBetweenTangents);
            Vector3 newTangentA = newAnchor + newEndDirection * (tangentDistance * (start ? 1 : -1));
            Vector3 newTangentB = newAnchor + newEndDirection * (tangentDistance * (start ? -1 : 1));

            newTangentA = SnapPointToSplinePlane(newTangentA, spline.transform);
            newTangentB = SnapPointToSplinePlane(newTangentB, spline.transform);

            //Set segment to closest spline direction
            if (EHandleModifier.CtrlActive(e))
            {
                Spline closest = SplineUtility.GetClosest(anchorHitPoint, HandleRegistry.GetSplines(), DistanceModifier(anchorHitPoint) * 20, out float time, out _);

                if (closest != null)
                {
                    closest.GetNormalsNonAlloc(normalsContainer, time);
                    float dot = Vector3.Dot(normalsContainer[2], newEndDirection);
                    if (dot < 0)
                    {
                        newTangentA = newAnchor - normalsContainer[2] * DistanceModifier(newAnchor, disBetweenTangents);
                        newTangentB = newAnchor + normalsContainer[2] * DistanceModifier(newAnchor, disBetweenTangents);
                    }
                    else
                    {
                        newTangentA = newAnchor + normalsContainer[2] * DistanceModifier(newAnchor, disBetweenTangents);
                        newTangentB = newAnchor - normalsContainer[2] * DistanceModifier(newAnchor, disBetweenTangents);
                    }
                }
            }

            //Set indicator data
            segementIndicatorData.anchor = anchor;
            segementIndicatorData.tangent = tangent;
            segementIndicatorData.newAnchor = newAnchor;
            segementIndicatorData.newTangentA = newTangentA;
            segementIndicatorData.newTangentB = newTangentB;
            segementIndicatorData.originFromStart = start;

            //If line
            if ((start && spline.segments[0].GetInterpolationType() == Segment.InterpolationType.LINE) ||
               (!start && spline.segments[spline.segments.Count - 1].GetInterpolationType() == Segment.InterpolationType.LINE))
            {
                segementIndicatorData.tangent = anchor;
                segementIndicatorData.newTangentA = newAnchor;
                segementIndicatorData.newTangentB = newAnchor;
            }

            Vector3 SnapPointToSplinePlane(Vector3 worldPoint, Transform t)
            {
                var lp = t.InverseTransformPoint(worldPoint);
                lp.z = 0f;
                return t.TransformPoint(lp);
            }

            Vector3 SnapDirToSplinePlane(Vector3 worldDir, Transform t)
            {
                var ld = t.InverseTransformDirection(worldDir);
                ld.z = 0f;
                ld.Normalize();
                return t.TransformDirection(ld);
            }
        }

        public static void UpdateIndicator2DGrid(Spline spline, Event e, ref SegmentIndicatorData segementIndicatorData)
        {
            Vector3 direction = Vector3.forward;
            Vector3 anchor = Vector3.zero;
            Vector3 tangent = Vector3.zero;
            bool start = false;

            Ray mouseRay = EMouseUtility.GetMouseRay(e.mousePosition);

            projectionPlane.SetNormalAndPosition(-spline.transform.forward, spline.transform.TransformPoint(spline.gridCenterPoint));
            if (projectionPlane.Raycast(mouseRay, out float enter))
            {
                Vector3 point = mouseRay.GetPoint(enter);

                direction = -spline.segments[spline.segments.Count - 1].GetDirection();
                anchor = spline.segments[spline.segments.Count - 1].GetPosition(Segment.ControlHandle.ANCHOR);
                tangent = spline.segments[spline.segments.Count - 1].GetPosition(Segment.ControlHandle.TANGENT_A);

                float distanceToStart = Vector3.Distance(spline.segments[0].GetPosition(Segment.ControlHandle.ANCHOR) + direction, point);
                float distanceToEnd = Vector3.Distance(spline.segments[spline.segments.Count - 1].GetPosition(Segment.ControlHandle.ANCHOR) - direction, point);
                start = distanceToStart < distanceToEnd;

                if (spline.segments.Count == 1)
                    start = !start;

                if (start)
                {
                    direction = -spline.segments[0].GetDirection();
                    anchor = spline.segments[0].GetPosition(Segment.ControlHandle.ANCHOR);
                    tangent = spline.segments[0].GetPosition(Segment.ControlHandle.TANGENT_B);
                }
            }

            projectionPlane.SetNormalAndPosition(-spline.transform.forward, anchor);
            if (projectionPlane.Raycast(mouseRay, out float enter2))
            {
                Vector3 point = mouseRay.GetPoint(enter2);

                point = EHandleGrid.SnapPoint(spline, point);

                Vector3 direction90 = Vector3.Cross(direction, spline.transform.forward);
                Vector3 closestPoint = Utility.LineUtility.GetNearestPoint(anchor, direction, point, out _);
                Utility.LineUtility.GetNearestPoint(anchor, direction90, point, out float time);
                float sign = Mathf.Sign(time) * (start ? -1 : 1);
                direction90 = direction90 * sign;

                segementIndicatorData.gridSpline = spline;
                segementIndicatorData.anchor = anchor;
                segementIndicatorData.tangent = tangent;
                segementIndicatorData.newAnchor = point;
                segementIndicatorData.originFromStart = start;

                if (GeneralUtility.IsEqual(closestPoint, point, 0.1f))
                {
                    Vector3 newTa = point + direction * Vector3.Distance(anchor, tangent);
                    newTa = EHandleGrid.SnapPoint(spline, newTa);

                    Vector3 newTb = point - direction * Vector3.Distance(anchor, tangent);
                    newTb = EHandleGrid.SnapPoint(spline, newTb);

                    segementIndicatorData.newTangentA = newTa;
                    segementIndicatorData.newTangentB = newTb;
                }
                else
                {
                    Vector3 newTa = point + direction90 * Vector3.Distance(anchor, tangent);
                    newTa = EHandleGrid.SnapPoint(spline, newTa);

                    Vector3 newTb = point - direction90 * Vector3.Distance(anchor, tangent);
                    newTb = EHandleGrid.SnapPoint(spline, newTb);

                    segementIndicatorData.newTangentA = newTa;
                    segementIndicatorData.newTangentB = newTb;
                }

                if ((start && spline.segments[0].GetInterpolationType() == Segment.InterpolationType.LINE) ||
                    (!start && spline.segments[spline.segments.Count - 1].GetInterpolationType() == Segment.InterpolationType.LINE))
                {
                    segementIndicatorData.tangent = anchor;
                    segementIndicatorData.newTangentA = point;
                    segementIndicatorData.newTangentB = point;
                }
            }
        }

        public static void GetIndicatorExtendedData(Spline spline, Vector3 startLinePos, Vector3 endLinePos, Ray mouseRay, out Vector3 closestPoint, out float distance, out float time)
        {
            float lineLength = Vector3.Distance(spline.segments[0].GetPosition(Segment.ControlHandle.ANCHOR), spline.segments[0].GetPosition(Segment.ControlHandle.TANGENT_B));
            Vector3 startDirection = spline.segments[0].GetDirection();
            Vector3 newClosestPoint = Utility.LineUtility.GetNearestPointOnLineFromLine(startLinePos, -startDirection, mouseRay.origin, mouseRay.direction, lineLength, true);
            distance = EMouseUtility.MouseDistanceToPoint(newClosestPoint, mouseRay);
            closestPoint = newClosestPoint;
            time = 0;

            lineLength = Vector3.Distance(spline.segments[spline.segments.Count - 1].GetPosition(Segment.ControlHandle.ANCHOR), spline.segments[spline.segments.Count - 1].GetPosition(Segment.ControlHandle.TANGENT_A));
            Vector3 endDirection = spline.segments[spline.segments.Count - 1].GetDirection();
            newClosestPoint = Utility.LineUtility.GetNearestPointOnLineFromLine(endLinePos, endDirection, mouseRay.origin, mouseRay.direction, lineLength, true);
            float distanceToExtendedEnd = EMouseUtility.MouseDistanceToPoint(newClosestPoint, mouseRay);
            if (distanceToExtendedEnd < distance)
            {
                distance = distanceToExtendedEnd;
                closestPoint = newClosestPoint;
                time = 1;
            }
        }

        public static void CreateWithAutoSmooth(Spline spline, int segementId, float time)
        {
            if (segementId < 1 || segementId >= spline.segments.Count)
            {
                Debug.LogError("Segment id outside range!");
                return;
            }

            float segementTime = SplineUtility.GetSegmentTime(segementId, spline.segments.Count, time);
            Vector3 p1 = Vector3.Lerp(spline.segments[segementId - 1].GetPosition(Segment.ControlHandle.ANCHOR), spline.segments[segementId - 1].GetPosition(Segment.ControlHandle.TANGENT_A), segementTime);
            Vector3 p2 = Vector3.Lerp(spline.segments[segementId - 1].GetPosition(Segment.ControlHandle.TANGENT_A), spline.segments[segementId].GetPosition(Segment.ControlHandle.TANGENT_B), segementTime);
            Vector3 p3 = Vector3.Lerp(spline.segments[segementId].GetPosition(Segment.ControlHandle.TANGENT_B), spline.segments[segementId].GetPosition(Segment.ControlHandle.ANCHOR), segementTime);
            Vector3 newTangentBPoint = Vector3.Lerp(p1, p2, segementTime);
            Vector3 newTangentAPoint = Vector3.Lerp(p2, p3, segementTime);
            Vector3 newAnchorPoint = Vector3.Lerp(newTangentBPoint, newTangentAPoint, segementTime);

            CreateSegmentUpdateControlPoint(spline, segementId, newAnchorPoint, newTangentAPoint, newTangentBPoint, () => 
            {
                spline.segments[segementId].SetPosition(Segment.ControlHandle.TANGENT_B, p3);
                spline.segments[segementId - 1].SetPosition(Segment.ControlHandle.TANGENT_A, p1);

                //Set B tangent on first segement if looping.
                if (spline.loop && segementId == spline.segments.Count - 1)
                    spline.segments[0].SetPosition(Segment.ControlHandle.TANGENT_B, p3);
            });
        }

        public static void CreateFromWorldPoint(Spline spline, SegmentIndicatorData segementIndicatorData)
        {
            int segementId = 0;
            if (!segementIndicatorData.originFromStart)
                segementId = spline.segments.Count;

            CreateSegmentUpdateControlPoint(spline, segementId, segementIndicatorData.newAnchor,
                                                            segementIndicatorData.newTangentA,
                                                            segementIndicatorData.newTangentB);

            if (!segementIndicatorData.originFromStart)
                spline.segments[spline.segments.Count - 2].SetPosition(Segment.ControlHandle.TANGENT_A, segementIndicatorData.tangent);
            else
                spline.segments[1].SetPosition(Segment.ControlHandle.TANGENT_B, segementIndicatorData.tangent);
        }

        public static void CreateExtended(Spline spline, bool createAtStart)
        {
            if (createAtStart)
            {
                Vector3 correctedTangentPos = spline.segments[0].GetPosition(Segment.ControlHandle.ANCHOR) - (spline.segments[0].GetPosition(Segment.ControlHandle.ANCHOR) - spline.indicatorPosition) / 2;
                spline.segments[0].SetPosition(Segment.ControlHandle.TANGENT_B, correctedTangentPos);
                CreateSegmentUpdateControlPoint(spline, 0, spline.indicatorPosition,
                                     correctedTangentPos,
                                     spline.indicatorPosition + spline.indicatorDirection * DistanceModifier(spline.indicatorPosition) * 12);
            }
            else
            {
                Vector3 correctedTangentPos = spline.segments[spline.segments.Count - 1].GetPosition(Segment.ControlHandle.ANCHOR) - (spline.segments[spline.segments.Count - 1].GetPosition(Segment.ControlHandle.ANCHOR) - spline.indicatorPosition) / 2;
                spline.segments[spline.segments.Count - 1].SetPosition(Segment.ControlHandle.TANGENT_A, correctedTangentPos);
                CreateSegmentUpdateControlPoint(spline, spline.segments.Count, spline.indicatorPosition,
                                     spline.indicatorPosition - spline.indicatorDirection * DistanceModifier(spline.indicatorPosition) * 12,
                                     correctedTangentPos);
            }
        }

        public static void CreateSegmentUpdateControlPoint(Spline spline, int segement, Vector3 anchorPos, Vector3 tangentAPos, Vector3 tangentBPos, Action beforeCreation = null)
        {
            EHandleUndo.RecordNow(spline, "Create segement: " + spline.indicatorSegement);
            if (beforeCreation != null)
                beforeCreation.Invoke();

            spline.CreateSegment(segement, anchorPos, tangentAPos, tangentBPos);
            EHandleSelection.selectedSplineObject = null;
            EHandleSelection.selectedSplineObjects.Clear();
            Selection.activeTransform = spline.transform;
            EHandleSelection.SelectPrimaryControlPoint(spline, SplineUtility.SegmentIndexToControlPointId(segement, Segment.ControlHandle.ANCHOR));
        }

        public static void CreateSegmentsFromEditorCameraDirection(Spline spline)
        {
            Transform editorCamera = EHandleSceneView.GetCurrent().camera.transform;

            spline.transform.position = editorCamera.position + editorCamera.forward * 50;
            Vector3 anchor1 = spline.transform.position + editorCamera.transform.right * 12;
            Vector3 anchor2 = spline.transform.position - editorCamera.transform.right * 12;

            spline.CreateSegment(0, anchor1, anchor1 - editorCamera.transform.right * 5, anchor1 + editorCamera.transform.right * 5);
            spline.CreateSegment(1, anchor2, anchor2 - editorCamera.transform.right * 5, anchor2 + editorCamera.transform.right * 5);

            spline.UpdateCachedData();
        }

        public static void UpdateLoopEndData(Spline spline, Segment segement)
        {
            if (!spline.loop)
                return;

            if (spline.segments[0] != segement)
                return;

            //Deformation
            spline.segments[spline.segments.Count - 1].scale = spline.segments[0].scale;
            if (spline.normalType != Spline.NormalType.DYNAMIC)
                spline.segments[spline.segments.Count - 1].zRotation = spline.segments[0].zRotation;
            spline.segments[spline.segments.Count - 1].saddleSkew = spline.segments[0].saddleSkew;
            spline.segments[spline.segments.Count - 1].SetNoise(spline.segments[0].noise);
            spline.segments[spline.segments.Count - 1].SetContrast(spline.segments[0].contrast);

            EHandleEvents.InvokeUpdateLoopEndData(spline);
        }

        public static void AlignTangents(List<Segment> segments)
        {
            Vector3 tagentA = segments[0].GetPosition(Segment.ControlHandle.TANGENT_A);
            Vector3 tagentB = segments[0].GetPosition(Segment.ControlHandle.TANGENT_B);
            Vector3 direction = segments[0].GetDirection();

            for (int i = 1; i < segments.Count; i++)
            {
                float dot = Vector3.Dot(direction, segments[i].GetDirection());

                if (dot < 0)
                {
                    segments[i].SetPosition(Segment.ControlHandle.TANGENT_A, tagentB);
                    segments[i].SetPosition(Segment.ControlHandle.TANGENT_B, tagentA);
                }
                else
                {
                    segments[i].SetPosition(Segment.ControlHandle.TANGENT_A, tagentA);
                    segments[i].SetPosition(Segment.ControlHandle.TANGENT_B, tagentB);
                }
            }

            EHandleDeformation.TryDeformLinks(segments[0].splineParent);
        }

        public static float DistanceModifier(Vector3 position, float strength = 1)
        {
            SceneView sceneView = EHandleSceneView.GetCurrent();
            if (sceneView == null) return 1;

            if (sceneView.orthographic)
            {
                float orthoSize = sceneView.camera.orthographicSize;
                return 0.0133f * orthoSize * strength;
            }
            else
            {
                float distance = Vector3.Distance(sceneView.camera.transform.position, position);
                return 0.0066f * distance * strength;
            }
        }

        public static float GetControlPointSize(Vector3 position)
        {
            float controlPointSize = GlobalSettings.GetControlPointSize();
            float size = DistanceModifier(position) * controlPointSize;

            if (size > 1.2f * controlPointSize) size = 1.2f * controlPointSize;
            return size;
        }

        public static int GetDrawLinesCount(Vector3 anchorA, Vector3 tangentA, Vector3 tangentB, Vector3 anchorB, float segmentLength)
        {
            //Settings
            const int testRange = 10;

            //General
            SceneView sceneView = EHandleSceneView.GetCurrent();
            Camera editorCamera = sceneView.camera;
            Vector3 cameraPoint = editorCamera.transform.position;

            //First in view test
            Vector3 vpA = editorCamera.WorldToViewportPoint(anchorA);
            Vector3 vpB = editorCamera.WorldToViewportPoint(anchorB);
            bool vpAInView = vpA.x >= 0f && vpA.x <= 1f && vpA.y >= 0f && vpA.y <= 1f && vpA.z > 0f;
            bool vpBInView = vpB.x >= 0f && vpB.x <= 1f && vpB.y >= 0f && vpB.y <= 1f && vpB.z > 0f;

            Vector3 center = (anchorA + anchorB) / 2;
            float distanceToCamera = Vector3.Distance(center, cameraPoint);

            //Skip
            if (segmentLength < testRange && distanceToCamera > testRange && !vpAInView && !vpBInView)
            {
                return 0;
            }

            //Second in view test
            int times = (int)(segmentLength / testRange);
            bool inView = false;

            for(int i = 1; i < times; i++)
            {
                Vector3 point = BezierUtility.CubicLerp(anchorA, tangentA, tangentB, anchorB, (float)i / times);

                Vector3 vp = editorCamera.WorldToViewportPoint(point);
                if(vp.x >= 0f && vp.x <= 1f && vp.y >= 0f && vp.y <= 1f && vp.z > 0f)
                    inView = true;

                float dCheck = Vector3.Distance(point, cameraPoint);
                if (dCheck < distanceToCamera)
                    distanceToCamera = dCheck;
            }

            //Skip if camera is too far away
            if (distanceToCamera > GlobalSettings.GetSplineViewDistance())
                return 0;

            //Skip
            if (distanceToCamera > testRange && !inView && !vpAInView && !vpBInView)
                return 0;

            Vector3 tADirection = (tangentA - anchorA).normalized;
            Vector3 tBDirection = (anchorB - tangentB).normalized;
            float distanceBetween = Vector3.Distance(anchorA, anchorB);

            //If segment is aligned draw one line
            if (GeneralUtility.IsEqual(tADirection, tBDirection) && GeneralUtility.IsEqual(anchorB, anchorA + tADirection * distanceBetween, 0.01f))
            {
                EHandleSpline.totalLinesDrawn++;
                return 1;
            }

            //User modifier
            float userModifier = GlobalSettings.GetSplineLineResolution() / 100;
            float clampedUserModification = Mathf.Clamp(userModifier, 0.1f, 10);

            //Distance modifier
            float boostedLength = GeneralUtility.BoostedValue(segmentLength, 100 * clampedUserModification, 0.7f);
            float t = distanceToCamera / boostedLength;

            if (editorCamera.orthographic)
                t = editorCamera.orthographicSize * 1.33f / boostedLength;

            float distanceModifier = Mathf.Lerp(13, 0, t);

            //Segment segmentLength modifier
            float time = Mathf.Clamp(segmentLength, 0, 100) / 100;
            float lengthModifier = Mathf.Lerp(0, 15, time) * Mathf.Clamp(100 / distanceToCamera, 0, 1);

            //Length of all splines modifier
            float lengthAllSplinesModifier = Mathf.Clamp(5000 / Mathf.Max((float)EHandleSpline.lengthAllSplines, 5000), 0.4f, 1);

            //Final
            float final = (distanceModifier + lengthModifier) * userModifier * lengthAllSplinesModifier;
            final = Mathf.Clamp(final, 1 + (clampedUserModification * 2), 100);

            EHandleSpline.totalLinesDrawn += (int)final;

            return (int)final;
        }

        public static int GetDrawLinesCount(Segment segmentFrom, Segment segmentTo)
        {
            return GetDrawLinesCount(segmentFrom.GetPosition(Segment.ControlHandle.ANCHOR), segmentFrom.GetPosition(Segment.ControlHandle.TANGENT_A),
                                     segmentTo.GetPosition(Segment.ControlHandle.TANGENT_B), segmentTo.GetPosition(Segment.ControlHandle.ANCHOR),
                                     segmentFrom.length);
        }
    }
}
