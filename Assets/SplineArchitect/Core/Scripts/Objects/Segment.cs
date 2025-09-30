// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: Segment.cs
//
// Author: Mikael Danielsson
// Date Created: 12-02-2023
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

using UnityEngine;
using Vector3 = UnityEngine.Vector3;

using SplineArchitect.Utility;

namespace SplineArchitect.Objects
{
    [Serializable]
    public partial class Segment
    {
        public enum LinkTarget : byte
        {
            NONE,
            ANCHOR,
            SPLINE_CONNECTOR
        }

        public enum ControlHandle : byte
        {
            NONE,
            ANCHOR,
            TANGENT_A,
            TANGENT_B
        }

        public enum InterpolationType : byte
        {
            SPLINE,
            LINE
        }

        public const float defaultZRotation = 0;
        public const float defaultContrast = 1.5f;
        public const float defaultNoise = 1;
        public const float defaultSaddleSkewX = 0;
        public const float defaultSaddleSkewY = 0;
        public const float defaultScale = 1;

        public const int DataUsage = 24 + 
                                     12 + 12 + 12 + 8 + 4 + 4 + 4 + 12 + 12 + 4 + 4 + 1 + 40 + 8 + 40;

        //Space
        [HideInInspector]
        [SerializeField]
        private Vector3 anchor;
        [HideInInspector]
        [SerializeField]
        private Vector3 tangentA;
        [HideInInspector]
        [SerializeField]
        private Vector3 tangentB;
        [HideInInspector]
        [SerializeField]
        private InterpolationType interpolationType;
        [HideInInspector]
        public Transform localSpace;

        //Deformation
        [HideInInspector]
        public float zRotation = defaultZRotation;
        [HideInInspector]
        public float contrast = defaultContrast;
        [HideInInspector]
        public float noise = defaultNoise;
        [HideInInspector]
        public Vector2 saddleSkew = new Vector2(defaultSaddleSkewX, defaultSaddleSkewY);
        [HideInInspector]
        public Vector2 scale = new Vector2(defaultScale, defaultScale);

        //General, stored
        [HideInInspector]
        public float length;
        [HideInInspector]
        public float zPosition;
        [HideInInspector]
        public LinkTarget linkTarget;
        [HideInInspector]
        public bool mirrorConnector;

        //General runtime
        [NonSerialized]
        public List<Segment> links;
        [NonSerialized]
        public Spline splineParent;
        [NonSerialized]
        public SplineConnector splineConnector;
        [NonSerialized]
        private List<Segment> closestSegmentContainer;
        [HideInInspector]
        public float oldDisTangentA = 3;
        [HideInInspector]
        public float oldDisTangentB = 3;
        [HideInInspector]
        public Vector3 oldDirTangentA = Vector3.right;
        [HideInInspector]
        public Vector3 oldDirTangentB = -Vector3.right;
#if UNITY_EDITOR
        [NonSerialized]
        public LinkTarget oldLinkTarget;
        public bool linkCreatedThisFrameOnly { private get; set; }
#endif

        private const float ContrastMin = -50f;
        private const float ContrastMax = 50f;
        private const float NoiseMin = 0;
        private const float NoiseMax = 1;
        private const float NoiseCenterSmoothMin = 0;
        private const float NoiseCenterSmoothMax = 10;

        public Segment(Spline splineParent, Vector3 anchor, Vector3 tangentA, Vector3 tangentB, Space space)
        {
            this.splineParent = splineParent;
            localSpace = splineParent.transform;
            SetPosition(ControlHandle.ANCHOR, anchor, space);
            SetPosition(ControlHandle.TANGENT_A, tangentA, space);
            SetPosition(ControlHandle.TANGENT_B, tangentB, space);
        }

        /// <summary>
        /// Gets newPosition of the specified controlHandle (anchor, tangentA, or tangentB).
        /// </summary>
        /// <param name="controlHandle">The controlHandle of the point (anchor, tangentA, or tangentB).</param>
        /// <returns>The world or local newPosition of the specified point.</returns>
        public Vector3 GetPosition(ControlHandle controlHandle, Space space = Space.World)
        {
            if(space == Space.Self)
                return GetLocalPosition(controlHandle);

            return localSpace.TransformPoint(GetLocalPosition(controlHandle));
        }

        public Vector3 GetDirection(Space space = Space.World)
        {
            return (GetPosition(ControlHandle.ANCHOR, space) - GetPosition(ControlHandle.TANGENT_A, space)).normalized;
        }

        /// <summary>
        /// Sets the world newPosition of the specified controlPoint (anchor, tangentA, or tangentB).
        /// </summary>
        /// <param name="controlHandle">The controlHandle of the point (anchor, tangentA, or tangentB).</param>
        /// <param name="newPosition">The new world or local newPosition of the point.</param>
        public void SetPosition(ControlHandle controlHandle, Vector3 newPosition, Space space = Space.World)
        {
            if(space == Space.Self)
                SetLocalPosition(controlHandle, newPosition);
            else
                SetLocalPosition(controlHandle, localSpace.InverseTransformPoint(newPosition));
        }

        /// <summary>
        /// Translates the specified controlPoint (anchor, tangentA, or tangentB) by a given value in world or local space.
        /// </summary>
        /// <param name="controlHandle">The controlHandle of the point (anchor, tangentA, or tangentB).</param>
        /// <param name="value">The translation vector in world space.</param>
        public void Translate(ControlHandle controlHandle, Vector3 value, Space space = Space.World)
        {
            SetPosition(controlHandle, GetPosition(controlHandle, space) - value, space);
        }

        public void SetAnchorPosition(Vector3 newPosition, Space space = Space.World)
        {
            Vector3 oldPosition = GetPosition(ControlHandle.ANCHOR, space);
            SetPosition(ControlHandle.ANCHOR, newPosition, space);
            Translate(ControlHandle.TANGENT_A, oldPosition - newPosition, space);
            Translate(ControlHandle.TANGENT_B, oldPosition - newPosition, space);

            TryUpdateLoopData();
        }

        public void TranslateAnchor(Vector3 value, Space space = Space.World)
        {
            Translate(ControlHandle.ANCHOR, value, space);
            Translate(ControlHandle.TANGENT_A, value, space);
            Translate(ControlHandle.TANGENT_B, value, space);

            TryUpdateLoopData();
        }

        /// <summary>
        /// Calculates the newPosition of the opposite tangent to maintain continuous symmetry relative to the anchor point.
        /// </summary>
        /// <param name="controlHandle">The controlHandle of the control point being moved (Tangent A or Tangent B).</param>
        /// <param name="newPosition">The new newPosition of the control point being moved.</param>
        /// <returns>The calculated newPosition of the opposite tangent to maintain continuity.</returns>
        public void SetContinuousPosition(ControlHandle controlHandle, Vector3 newPosition, Space space = Space.World)
        {
            if(controlHandle == ControlHandle.ANCHOR)
            {
                SetAnchorPosition(newPosition, space);
            }
            else if (controlHandle == ControlHandle.TANGENT_A || controlHandle == ControlHandle.TANGENT_B)
            {
                ControlHandle opositeType = SplineUtility.GetOppositeTangentType(controlHandle);
                float distance = Vector3.Distance(GetPosition(opositeType, space), GetPosition(ControlHandle.ANCHOR, space));
                Vector3 direction = (newPosition - GetPosition(ControlHandle.ANCHOR, space)).normalized;
                Vector3 oppositePosition = GetPosition(ControlHandle.ANCHOR, space) - direction * distance;

                if(!GeneralUtility.IsEqual(newPosition, GetPosition(ControlHandle.ANCHOR, space)))
                    SetPosition(opositeType, oppositePosition, space);

                SetPosition(controlHandle, newPosition, space);
            }

            TryUpdateLoopData(space);
        }

        /// <summary>
        /// Calculates the mirrored position of a tangent relative to the anchor point.
        /// </summary>
        /// <param name="controlHandle">The type of the tangent being mirrored (Tangent A or Tangent B).</param>
        /// <param name="newPosition">The new position of the tangent being mirrored.</param>
        /// <returns>The calculated mirrored position of the tangent.</returns>
        public void SetMirroredPosition(ControlHandle controlHandle, Vector3 newPosition, Space space = Space.World)
        {
            if (controlHandle == ControlHandle.ANCHOR)
            {
                SetAnchorPosition(newPosition, space);
            }
            else if (controlHandle == ControlHandle.TANGENT_A || controlHandle == ControlHandle.TANGENT_B)
            {
                float distance = Vector3.Distance(newPosition, GetPosition(ControlHandle.ANCHOR, space));

                Vector3 direction = (newPosition - GetPosition(ControlHandle.ANCHOR, space)).normalized;
                SetPosition(SplineUtility.GetOppositeTangentType(controlHandle), GetPosition(ControlHandle.ANCHOR) - direction * distance, space);
                SetPosition(controlHandle, newPosition, space);
            }

            TryUpdateLoopData(space);
        }

        public void SetBrokenPosition(ControlHandle controlHandle, Vector3 newPosition, Space space = Space.World)
        {
            if (controlHandle == ControlHandle.ANCHOR)
            {
                SetAnchorPosition(newPosition, space);
            }
            else if (controlHandle == ControlHandle.TANGENT_A || controlHandle == ControlHandle.TANGENT_B)
            {
                SetPosition(controlHandle, newPosition, space);
            }

            TryUpdateLoopData(space);
        }

        /// <summary>
        /// Sets the contrast value, clamping it within the specified minimum and maximum range.
        /// </summary>
        /// <param name="value">The new contrast value.</param>
        public void SetContrast(float value)
        {
            contrast = Mathf.Clamp(value, ContrastMin, ContrastMax);
        }

        public void SetNoise(float value)
        {
            noise = Mathf.Clamp(value, NoiseMin, NoiseMax);
        }

        /// <summary>
        /// Adds a link to the segment's list of links.
        /// </summary>
        /// <param name="l">The link to add.</param>
        public void AddLink(Segment s)
        {
            links.Add(s);
        }

        /// <summary>
        /// Sets the spline parent for the segment and updates the positions of the anchor and tangents.
        /// </summary>
        /// <param name="splineParent">The new spline parent.</param>
        public void SetSplineParent(Spline splineParent)
        {
            Vector3 anchor = GetPosition(ControlHandle.ANCHOR);
            Vector3 tangentA = GetPosition(ControlHandle.TANGENT_A);
            Vector3 tangentB = GetPosition(ControlHandle.TANGENT_B);

            this.splineParent = splineParent;
            localSpace = splineParent.transform;

            SetPosition(ControlHandle.ANCHOR, anchor);
            SetPosition(ControlHandle.TANGENT_A, tangentA);
            SetPosition(ControlHandle.TANGENT_B, tangentB);
        }

        public void SetInterpolationMode(InterpolationType interpolationMode)
        {
            if (splineParent == null)
                return;

            if (this.interpolationType == InterpolationType.SPLINE && interpolationMode == InterpolationType.LINE)
            {
                oldDisTangentA = Vector3.Distance(anchor, tangentA);
                oldDisTangentB = Vector3.Distance(anchor, tangentB);
                oldDirTangentA = (tangentA - anchor).normalized;
                oldDirTangentB = (tangentB - anchor).normalized;
            }

            this.interpolationType = interpolationMode;

            if (splineParent.segments.Count == 1)
                return;

            //Get this segment id
            int id = 0;
            for (int i = 0; i < splineParent.segments.Count; i++)
            {
                Segment s = splineParent.segments[i];
                if (s == this)
                {
                    id = i;
                    break;
                }

            }

            Vector3 direction;
            if (id == 0) direction = splineParent.segments[1].GetPosition(ControlHandle.ANCHOR, Space.Self) - anchor;
            else direction = anchor - splineParent.segments[id - 1].GetPosition(ControlHandle.ANCHOR, Space.Self);
            direction = direction.normalized;
            if (this.interpolationType == InterpolationType.LINE)
            {
                if (id == 1)
                {
                    Segment firstSegment = splineParent.segments[0];

                    if (firstSegment.GetInterpolationType() == InterpolationType.LINE)
                    {
                        firstSegment.SetPosition(ControlHandle.TANGENT_A, firstSegment.GetPosition(ControlHandle.ANCHOR) + direction * 0.1f);
                        firstSegment.SetPosition(ControlHandle.TANGENT_B, firstSegment.GetPosition(ControlHandle.ANCHOR) - direction * 0.1f);
                    }
                }

                if (id == splineParent.segments.Count - 2)
                {
                    Segment lastSegment = splineParent.segments[splineParent.segments.Count - 1];

                    if (lastSegment.GetInterpolationType() == InterpolationType.LINE)
                    {
                        direction = lastSegment.GetPosition(ControlHandle.ANCHOR, Space.Self) - anchor;
                        direction = direction.normalized;
                        lastSegment.SetPosition(ControlHandle.TANGENT_A, lastSegment.GetPosition(ControlHandle.ANCHOR) + direction * 0.1f);
                        lastSegment.SetPosition(ControlHandle.TANGENT_B, lastSegment.GetPosition(ControlHandle.ANCHOR) - direction * 0.1f);
                    }
                }

                tangentA = anchor + direction * 0.1f;
                tangentB = anchor - direction * 0.1f;
            }
            else
            {
                if (GeneralUtility.IsZero(oldDirTangentA) || GeneralUtility.IsZero(oldDirTangentB) || oldDisTangentA < 0.5f || oldDisTangentB < 0.5f)
                {
                    tangentA = anchor + direction * 3f;
                    tangentB = anchor - direction * 3f;
                }
                else
                {
                    tangentA = anchor + oldDirTangentA * oldDisTangentA;
                    tangentB = anchor + oldDirTangentB * oldDisTangentB;
                }
            }
        }

        public InterpolationType GetInterpolationType()
        {
            return interpolationType;
        }

        /// <summary>
        /// Links all control points on all splines at the specified link point.
        /// </summary>
        /// <param name="linkPoint">The newPosition at which the control points will be linked.</param>
        public bool LinkToAnchor(Vector3 linkPoint, bool ForceLinkUnlinked = true)
        {
            SetAnchorPosition(linkPoint);
            splineParent.CalculateControlpointBounds();

            if (closestSegmentContainer == null)
                closestSegmentContainer = new();

#if UNITY_EDITOR
            SplineUtility.GetSegmentsAtPointNoAlloc(closestSegmentContainer, linkCreatedThisFrameOnly ? HandleRegistry.GetSplinesRegistredThisFrame() : HandleRegistry.GetSplines(), linkPoint);
#else
            SplineUtility.GetSegmentsAtPointNoAlloc(closestSegmentContainer, HandleRegistry.GetSplines(), linkPoint);
#endif
            bool sucessfullLink = closestSegmentContainer.Count > 1;
            if (sucessfullLink) linkTarget = LinkTarget.ANCHOR;

            //Update links
            foreach (Segment s in closestSegmentContainer)
            {
                if (!ForceLinkUnlinked && s.linkTarget != LinkTarget.ANCHOR)
                    continue;

                if (s.links == null)
                    s.links = new List<Segment>();

                s.links.Clear();
                s.linkTarget = LinkTarget.ANCHOR;

                foreach (Segment s2 in closestSegmentContainer)
                {
                    if (!ForceLinkUnlinked && s2.linkTarget != LinkTarget.ANCHOR)
                        continue;

                    //Add segment to ForceLinkUnlinked
                    s.links.Add(s2);
                }
            }

            return sucessfullLink;
        }

        public bool LinkToConnector(Vector3 linkPoint)
        {
            SetAnchorPosition(linkPoint);

#if UNITY_EDITOR
            HashSet<SplineConnector> connectors = HandleRegistry.GetSplineConnectors();
            HashSet<SplineConnector> thisFrameConnectors = HandleRegistry.GetSplineConnectorsRegistredThisFrame();
            if (thisFrameConnectors.Count > 0 || (linkCreatedThisFrameOnly && !EHandleEvents.undoActive)) connectors = thisFrameConnectors;
            SplineConnector closest = SplineConnectorUtility.GetFirstConnectorAtPoint(linkPoint, connectors);
#else
            SplineConnector closest = SplineConnectorUtility.GetClosest(linkPoint, HandleRegistry.GetSplineConnectors());
#endif
            bool sucessfullLink = closest != null;

            if (sucessfullLink)
            {
                closest.AddConnection(this);
                splineConnector = closest;
                linkTarget = LinkTarget.SPLINE_CONNECTOR;
            }

            return sucessfullLink;
        }

        public void Unlink()
        {
            if (splineConnector != null)
            {
                splineConnector.RemoveConnection(this);
                splineConnector = null;
            }

            if (links == null)
                return;

            for (int i2 = 0; i2 < links.Count; i2++)
            {
                Segment s = links[i2];

                //Skip self
                if (s == this)
                    continue;

                if (s.links.Count <= 2)
                {
                    s.links.Clear();
                    s.linkTarget = LinkTarget.NONE;
                }
                else
                {
                    for (int i = 0; i < s.links.Count; i++)
                    {
                        Segment s2 = s.links[i];

                        if (s2 == this)
                        {
                            s.links.RemoveAt(i);
                            break;
                        }
                    }
                }
            }

            links.Clear();
        }

        /// <summary>
        /// Retrieves the local newPosition of the specified control point controlHandle (Anchor, Tangent A, or Tangent B).
        /// </summary>
        /// <param name="controlHandle">The controlHandle of control point to retrieve.</param>
        /// <returns>The local newPosition of the specified control point.</returns>
        private Vector3 GetLocalPosition(ControlHandle controlHandle)
        {
            if (controlHandle == ControlHandle.TANGENT_A)
                return tangentA;
            else if (controlHandle == ControlHandle.TANGENT_B)
                return tangentB;
            else
                return anchor;
        }

        /// <summary>
        /// Sets the local newPosition of the specified control point controlHandle (Anchor, Tangent A, or Tangent B).
        /// </summary>
        /// <param name="controlHandle">The controlHandle of control point to set.</param>
        /// <param name="newPosition">The new newPosition to assign to the control point.</param>
        private void SetLocalPosition(ControlHandle controlHandle, Vector3 newPosition)
        {

            if (controlHandle == ControlHandle.TANGENT_A)
            {
                tangentA = newPosition;
            }
            else if (controlHandle == ControlHandle.TANGENT_B)
            {
                tangentB = newPosition;
            }
            else
            {
                Vector3 dif = anchor - newPosition;
                anchor = newPosition;
            }
        }

        private void TryUpdateLoopData(Space space = Space.World)
        {
            if(splineParent == null)
            {
                Debug.LogError("[Spline Architect] splineParent is null!");
                return;
            }

            if (!splineParent.loop || this != splineParent.segments[0])
                return;

            splineParent.segments[splineParent.segments.Count - 1].SetPosition(ControlHandle.ANCHOR, GetPosition(ControlHandle.ANCHOR, space), space);
            splineParent.segments[splineParent.segments.Count - 1].SetPosition(ControlHandle.TANGENT_A, GetPosition(ControlHandle.TANGENT_A, space), space);
            splineParent.segments[splineParent.segments.Count - 1].SetPosition(ControlHandle.TANGENT_B, GetPosition(ControlHandle.TANGENT_B, space), space);
        }
    }
}