// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: Spline.cs
//
// Author: Mikael Danielsson
// Date Created: 25-03-2023
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System;

using Unity.Collections;
using UnityEngine;
using Unity.Mathematics;
using Unity.Jobs;

using SplineArchitect.Utility;
using SplineArchitect.Monitor;
using SplineArchitect.Jobs;

using Vector3 = UnityEngine.Vector3;
using LineUtility = SplineArchitect.Utility.LineUtility;
using static SplineArchitect.Objects.Segment;

namespace SplineArchitect.Objects
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public partial class Spline : MonoBehaviour
    {
        public const int dataUsage = 24 +
                                     1 + 4 + 1 + 40 + 40 + 1 + 1 + 4 + 1 + 1 + MonitorSpline.dataUsage + 40 + 72 +
                                     1 + 1 + 1 + 1 + 1 + 24 + 24 + 32 + 32 + 32 + 32 + 32 + 4 + 4 + 4 + 4 + 4 + 4;

        public enum NormalType : byte
        {
            STATIC_3D,
            STATIC_2D,
            DYNAMIC
        }

        //Public stored data
        //General data
        [HideInInspector]
        public bool loop;
        [HideInInspector]
        public float length;
        [HideInInspector]
        public NormalType normalType;
        [HideInInspector]
        public List<Segment> segments;
        [HideInInspector]
        public List<NoiseLayer> noises;
        [HideInInspector]
        public NoiseLayer.Group noiseGroupMesh = NoiseLayer.Group.A;
        [HideInInspector]
        public DeformationMode deformationMode = DeformationMode.SAVE_IN_BUILD;
        [HideInInspector]
        public ComponentMode componentMode = ComponentMode.REMOVE_FROM_BUILD;

        //Runtime data
        [NonSerialized]
        private bool initalized;
        [NonSerialized]
        public MonitorSpline monitor;
        [NonSerialized]
        public List<SplineObject> followerUpdateList = new List<SplineObject>(); // Need to be created before the spline is Initalized
        [NonSerialized]
        public List<SplineObject> splineObjects = new List<SplineObject>(); // Need to be created before the spline is Initalized
        [NonSerialized]
        private HashSet<SplineObject> splineObjectsSet = new HashSet<SplineObject>(); // Need to be created before the spline is Initalized
        [NonSerialized]
        private Vector3[] normalsContainer = new Vector3[3];

        private void OnEnable()
        {
            Initalize();
        }

        private void OnDestroy()
        {
            DisposeCachedData();
            UnlinkAll();
            HandleRegistry.RemoveSpline(this);
#if UNITY_EDITOR
            EHandleEvents.InvokeDestroySpline(this);
#endif

            void UnlinkAll()
            {

#if UNITY_EDITOR
                if (EHandlePrefab.prefabStageClosedLastFrame)
                    return;

                if (EHandleEvents.sceneIsClosing)
                    return;
#endif

                for (int i = 0; i < segments.Count; i++)
                {
                    Segment segment = segments[i];

                    //CHECKS
                    if (segment.links == null)
                        continue;

                    if (segment.links.Count == 0)
                        continue;
                    else
                    {
                        bool foundSelf = false;

                        foreach (Segment s2 in segment.links)
                        {
                            if (s2.splineParent == this)
                            {
                                foundSelf = true;
                                break;
                            }
                        }

                        //Need to found self, else its a spline that was copied that will be unlinked. It can have the links left from the original spline becouse how unity undo works.
                        if (!foundSelf)
                            continue;
                    }
                    //CHECKS END

                    //Unlink
                    for (int i3 = segment.links.Count - 1; i3 >= 0; i3--)
                    {
                        Segment linkedSegment = segment.links[i3];

                        //Skip self
                        if (linkedSegment == segment)
                            continue;

                        if (linkedSegment.links.Count <= 2)
                        {
#if UNITY_EDITOR
                            UnityEditor.Undo.RecordObject(linkedSegment.splineParent, "Unlink");
#endif

                            linkedSegment.links.Clear();
                            linkedSegment.linkTarget = LinkTarget.NONE;
                        }
                        else
                        {
                            for (int i2 = 0; i2 < linkedSegment.links.Count; i2++)
                            {
                                Segment linkedSegment2 = linkedSegment.links[i2];

                                if (linkedSegment2.links.Count <= i2)
                                    continue;

                                if (linkedSegment2 == segment)
                                {
                                    linkedSegment2.links.RemoveAt(i2);
                                    break;
                                }
                            }
                        }
                    }

                    segment.links.Clear();
                }
            }
        }

        private void Start()
        {
#if UNITY_EDITOR
            if (EHandleEvents.dragActive)
                return;
#endif

            EstablishLinks();

            if (Application.isPlaying) 
                DeformationUtility.TryDeformRealtime(this);

#if !UNITY_EDITOR
            //1. Handle SplineObject components.
            for (int i = splineObjects.Count - 1; i >= 0; i--)
            {
                SplineObject so = splineObjects[i];

                if (so.componentMode != ComponentMode.INACTIVE || componentMode == ComponentMode.INACTIVE)
                    continue;

                RemoveAtSplineObject(i);
                so.enabled = false;
            }

            //2. Handle Spline component.
            if (componentMode == ComponentMode.INACTIVE)
            {
                splineObjects.Clear();
                enabled = false;
                DisposeCachedData();
                HandleRegistry.RemoveSpline(this);
            }
#endif
        }

        private void Update()
        {
#if UNITY_EDITOR
            if (EHandleEvents.dragActive)
                return;

            if (segments.Count < 2)
                return;

            if (EHandleEvents.playModeStateChange == UnityEditor.PlayModeStateChange.ExitingEditMode ||
                EHandleEvents.playModeStateChange == UnityEditor.PlayModeStateChange.ExitingPlayMode)
                return;
#endif

            DeformationUtility.TryDeformRealtime(this);
        }

        public void Initalize()
        {
#if UNITY_EDITOR
            if (EHandleEvents.dragActive)
            {
                EHandleEvents.InitalizeAfterDrag(this);
                return;
            }
#endif

            if (initalized)
                return;

            initalized = true;

            if (segments == null)
                segments = new List<Segment>();

            for (int i = 0; i < segments.Count; i++)
            {
                Segment s = segments[i];
                s.splineParent = this;
                s.localSpace = transform;
            }

            if (noises == null)
                noises = new List<NoiseLayer>();

            if (monitor == null)
                monitor = new MonitorSpline(this);

            UpdateCachedData();
            HandleRegistry.AddSpline(this);
        }

        /// <summary>
        /// Establishes links for all linked segments in the spline. 
        /// Skips already established links.
        /// </summary>
        /// <param name="force">If true, re-establishes all links even if already initialized.</param>
        public void EstablishLinks()
        {
#if UNITY_EDITOR
            if (initalizedLinks)
                return;

            initalizedLinks = true;
            FixUnityPrefabBoundsCase();
#endif
            int count = -1;
            foreach (Segment s in segments)
            {
                count++;
#if UNITY_EDITOR
                s.oldLinkTarget = s.linkTarget;

                if(!Application.isPlaying) s.linkCreatedThisFrameOnly = true;
#endif
                if (s.linkTarget == Segment.LinkTarget.ANCHOR)
                {
                    //Has allready been established by another spline during Start.
                    if (s.links != null && s.links.Count > 0)
                        continue;

                    s.LinkToAnchor(s.GetPosition(Segment.ControlHandle.ANCHOR), false);

                    if (s.links != null && s.links.Count < 2)
                    {
                        s.linkTarget = Segment.LinkTarget.NONE;
                        s.links.Clear();
                    }
                }
                else if (s.linkTarget == Segment.LinkTarget.SPLINE_CONNECTOR)
                {
                    if (s.splineConnector != null)
                        continue;

                    s.LinkToConnector(s.GetPosition(Segment.ControlHandle.ANCHOR));

                    if (s.splineConnector == null)
                    {
                        s.linkTarget = Segment.LinkTarget.NONE;
                    }
                }

#if UNITY_EDITOR
                if (!Application.isPlaying) s.linkCreatedThisFrameOnly = false;
#endif
            }
        }

        /// <summary>
        /// Updates monitor values; optionally skips position, scale, and rotation.
        /// </summary>
        /// <param name="skipPosScaleRot">If true, skips updating position, scale, and rotation.</param>
        public void UpdateMonitor(bool skipPosScaleRot = false)
        {
            monitor.UpdateMaps();
            monitor.UpdateSegement();
            monitor.UpdateZRotContSaddleScale();
            monitor.UpdateNoise();
            monitor.UpdateNormalType();

            if (!skipPosScaleRot)
            {
                monitor.UpdatePosRotValues();
                monitor.UpdateScale();
            }
        }

        /// <summary>
        /// Checks if the component is active and enabled.
        /// </summary>
        /// <returns>True if active and enabled; otherwise false.</returns>
        public bool IsEnabled()
        {
            if (gameObject != null && gameObject.activeInHierarchy && enabled)
                return true;

            return false;
        }

        /// <summary>
        /// Adds an SplineObject at a specific index.
        /// </summary>
        /// <param name="so">The object to add.</param>
        /// <param name="index">Insertion index; -1 adds to the end.</param>
        public void AddSplineObject(SplineObject so, int index = -1)
        {
#if UNITY_EDITOR
            if (so == null)
                return;
#endif
            if (splineObjectsSet.Contains(so))
                return;

            splineObjectsSet.Add(so);

            if (index == -1)
                splineObjects.Add(so);
            else
                splineObjects.Insert(index, so);

#if UNITY_EDITOR
            if (so.canUpdateSelection) EHandleEvents.ForceUpdateSelection(so);
            EHandleEvents.MarkForInfoUpdate(this);
#endif
        }

        /// <summary>
        /// Removes an SplineObject from the collection.
        /// </summary>
        /// <param name="so">The object to remove.</param>
        public void RemoveSplineObject(SplineObject so)
        {
            if (!splineObjectsSet.Contains(so))
                return;

            splineObjectsSet.Remove(so);
            splineObjects.Remove(so);

#if UNITY_EDITOR
            if (so.canUpdateSelection) EHandleEvents.ForceUpdateSelection(so);
            EHandleEvents.MarkForInfoUpdate(this);
#endif
        }

        /// <summary>
        /// Check if the SplineObject is contained within the Spline.
        /// </summary>
        /// <param name="so">The object to check.</param>
        /// <returns>True if contained; otherwise false.</returns>
        public bool ContainsSplineObject(SplineObject so)
        {
            if (so == null)
                return false;

            return splineObjectsSet.Contains(so);
        }

        /// <summary>
        /// Removes an SplineObject at a given index.
        /// </summary>
        /// <param name="index">The index of the object to remove.</param>
        public void RemoveAtSplineObject(int index)
        {
            splineObjectsSet.Remove(splineObjects[index]);
            splineObjects.RemoveAt(index);

#if UNITY_EDITOR
            if (splineObjects.Count > index && splineObjects[index].canUpdateSelection) EHandleEvents.ForceUpdateSelection(splineObjects[index]);
            EHandleEvents.MarkForInfoUpdate(this);
#endif
        }

        /// <summary>
        /// Creates a segment at a specific index with given positions.
        /// </summary>
        /// <param name="index">Insertion index.</param>
        /// <param name="anchorPos">Anchor position.</param>
        /// <param name="tangentAPos">First tangent position.</param>
        /// <param name="tangentBPos">Second tangent position.</param>
        public void CreateSegment(int index, Vector3 anchorPos, Vector3 tangentAPos, Vector3 tangentBPos)
        {
            Segment segment = new Segment(this, anchorPos, tangentAPos, tangentBPos, Space.World);
            segments.Insert(index, segment);

#if UNITY_EDITOR
            SetInterpolationModeForNewSegment(segment, index);
#endif
        }

        /// <summary>
        /// Adds a segment with specified positions.
        /// </summary>
        /// <param name="anchorPos">Anchor position.</param>
        /// <param name="tangentAPos">First tangent position.</param>
        /// <param name="tangentBPos">Second tangent position.</param>
        public void CreateSegment(Vector3 anchorPos, Vector3 tangentAPos, Vector3 tangentBPos)
        {
            Segment segment = new Segment(this, anchorPos, tangentAPos, tangentBPos, Space.World);
            segments.Add(segment);

#if UNITY_EDITOR
            SetInterpolationModeForNewSegment(segment, segments.Count - 2);
#endif
        }

        /// <summary>
        /// Removes a segment at the specified index.
        /// </summary>
        /// <param name="index">The index of the segment to remove.</param>
        public void RemoveSegment(int index)
        {
            segments.RemoveAt(index);
        }

        public void CreateNoise(NoiseLayer.Type type, Vector3 scale, float seed)
        {
            noises.Add(new NoiseLayer(type, scale, seed));
        }

        public void RemoveNoise(int index)
        {
            noises.RemoveAt(index);
        }

        /// <summary>
        /// Moves an Spline to its center position, calculated from all control points, and adjusts its segments accordingly.
        /// </summary>
        /// <param name="spline">The Spline to be moved.</param>
        /// <param name="dif">The output difference between the original and new positions.</param>
        public void TransformToCenter(out Vector3 dif)
        {
            dif = Vector3.zero;
            Vector3 center = GetCenter();

            if (GeneralUtility.IsEqual(center, transform.position, 0.01f))
                return;

            Vector3 oldPos = transform.position;
            transform.position = center;
            dif = oldPos - center;

            foreach (Segment s in segments)
            {
                s.Translate(Segment.ControlHandle.ANCHOR, -dif);
                s.Translate(Segment.ControlHandle.TANGENT_A, -dif);
                s.Translate(Segment.ControlHandle.TANGENT_B, -dif);
            }

#if UNITY_EDITOR
            Vector3 p = transform.TransformPoint(gridCenterPoint) + dif;
            gridCenterPoint = transform.InverseTransformPoint(p);
            EHandleEvents.InvokeTransformToCenter(this, dif);
#endif
        }

        /// <summary>
        /// Calculates the center position of all control points in the spline.
        /// </summary>
        /// <param name="space">The coordinate space (World or Local) to use for the calculation.</param>
        /// <returns>The calculated center position as a Vector3.</returns>
        public Vector3 GetCenter(Space space = Space.World)
        {
            Vector3 center = Vector3.zero;
            foreach (Segment s in segments)
            {
                if (loop && s == segments[segments.Count - 1])
                    continue;

                center += s.GetPosition(Segment.ControlHandle.ANCHOR, space);
            }

            return center / (segments.Count - (loop ? 1 : 0));
        }

        /// <summary>
        /// Calculates the position along a path for a given time (0 - 1).
        /// </summary>
        /// <returns>The global or local position at the given time.</returns>
        public Vector3 GetPosition(float time, Space space = Space.World)
        {
            int segment = GetSegment(time);
            return GetSegmentPosition(segment - 1, SplineUtility.GetSegmentTime(segment, segments.Count, time), space);
        }

        /// <summary>
        /// Quickly retrieves an interpolated position from a pre-mapped list based on time (0 - 1). If the positionMapLocal does not exist, GetPosition will be used.
        /// </summary>
        /// <returns>The interpolated position.</returns>
        public Vector3 GetPositionFastLocal(float time)
        {
            int count = positionMapLocal.Length;
            if (count == 0)
                return GetPosition(time, Space.Self);

            float resolution = GetResolutionSpline();
            float rawIndex = time / resolution;
            rawIndex = Mathf.Clamp(rawIndex, 0f, count - 1);

            int i0 = Mathf.FloorToInt(rawIndex);
            int i1 = Mathf.Min(i0 + 1, count - 1);
            float frac = rawIndex - i0;

            return Vector3.Lerp(positionMapLocal[i0], positionMapLocal[i1], frac);
        }

        /// <summary>
        /// Calculates the extended position, allowing for extrapolation beyond the start or end points based on the given time (< 0 || > 1).
        /// </summary>
        /// <returns>The extrapolated position along the splines extension line.</returns>
        public Vector3 GetPositionExtended(float time)
        {
            int segementIndex = 0;
            Vector3 position = segments[0].GetPosition(Segment.ControlHandle.ANCHOR);
            if (time >= 1)
            {
                segementIndex = segments.Count - 1;
                position = segments[segementIndex].GetPosition(Segment.ControlHandle.ANCHOR);
            }

            Vector3 direction = -segments[segementIndex].GetDirection();

            if (time < 0)
            {
                direction = -direction;
                time = Mathf.Abs(time);
            }
            else if (time > 1)
                time -= 1;
            else
                return position;

            return position + direction * (time * length);
        }

        /// <summary>
        /// Calculates the closest point on the spline to a given point, with options for ignoring the Y-axis and using fixed time.
        /// </summary>
        /// <returns>The closest position on the spline.</returns>
        public Vector3 GetNearestPointRough(Vector3 point, float steps, out float timeValue, bool ignoreYAxel = false, bool useFixedTime = false)
        {
            timeValue = -1;
            float distance = 999999;
            Vector3 position = Vector3.zero;

            for (float t = 0; t < 1; t += steps)
            {
                float dCheck;
                float t2 = t;

                //Calcuate closest point from fixed time instead.
                //Remmeber that the regular time can still be needed for other calculations.
                if (useFixedTime)
                    t2 = TimeToFixedTime(t);

                Vector3 bezierPoint = GetPosition(t2, Space.World);

                if (ignoreYAxel)
                    dCheck = Vector2.Distance(new Vector2(bezierPoint.x, bezierPoint.z), new Vector2(point.x, point.z));
                else
                    dCheck = Vector3.Distance(bezierPoint, point);

                if (dCheck < distance)
                {
                    timeValue = t;
                    distance = dCheck;
                    position = bezierPoint;
                }
            }

            return position;
        }

        /// <summary>
        /// Determines the closest point on the spline to a given point, refining the position iteratively for accuracy. Optionally ignores the Y-axis and utilizes fixed-time mapping.
        /// </summary>
        /// <returns>The closest position on the spline.</returns>
        public Vector3 GetNearestPoint(Vector3 point, out float timeValue, int precision = 8, float steps = 5, bool ignoreYAxel = false, bool useFixedTime = false)
        {
            steps = 100 / length / steps;
            if (steps > 0.2f) steps = 0.2f;
            if (steps < 0.0001f) steps = 0.0001f;

            Vector3 position = GetNearestPointRough(point, steps, out timeValue, ignoreYAxel, useFixedTime);

            for (int i = precision; i > 0; i--)
            {
                steps = steps / 1.66f;
                float timeForwards = timeValue + steps;
                float timeBackwards = timeValue - steps;

                //Only snap when not looping. Will prevent the position from getting stuck close to 0/1.
                if (!loop)
                {
                    if (timeForwards > 1) timeForwards = 1;
                    if (timeBackwards < 0) timeBackwards = 0;
                }

                Vector3 pForward = GetPosition(useFixedTime ? TimeToFixedTime(timeForwards) : timeForwards);

                float dForward = ignoreYAxel ? Vector2.Distance(new Vector2(pForward.x, pForward.z), new Vector2(point.x, point.z)) : Vector3.Distance(pForward, point);

                Vector3 pBackwards = GetPosition(useFixedTime ? TimeToFixedTime(timeBackwards) : timeBackwards);

                float dBackwards = ignoreYAxel ? Vector2.Distance(new Vector2(pBackwards.x, pBackwards.z), new Vector2(point.x, point.z)) : Vector3.Distance(pBackwards, point);

                if (dForward > dBackwards)
                {
                    position = pBackwards;
                    timeValue = timeBackwards;
                }
                else
                {
                    position = pForward;
                    timeValue = timeForwards;
                }
            }

            return position;
        }

        public Vector3 GetNearestPoint(Vector3 point)
        {
            return GetNearestPoint(point, out _);
        }

        /// <summary>
        /// Returns the normalized directional vector of the spline.
        /// calculated from the segment'linkedSegment anchor point towards its tangent point.
        /// </summary>
        /// <returns>The normalized direction vector.</returns>
        public Vector3 GetDirection(float time, bool backwards = false, Space space = Space.World)
        {
            int segment = GetSegment(time);
            if (segment < 1) segment = 1;

            float segementTime = SplineUtility.GetSegmentTime(segment, segments.Count, time);
            Vector3 forwardDirection = BezierUtility.GetTangent(segments[segment - 1].GetPosition(Segment.ControlHandle.ANCHOR, space),
                                                  segments[segment - 1].GetPosition(Segment.ControlHandle.TANGENT_A, space),
                                                  segments[segment].GetPosition(Segment.ControlHandle.TANGENT_B, space),
                                                  segments[segment].GetPosition(Segment.ControlHandle.ANCHOR, space), segementTime);

            return backwards ? -forwardDirection : forwardDirection;
        }

        /// <summary>
        /// Calculates the position on a segment at a specified time (0 - 1).
        /// </summary>
        /// <returns>The interpolated position.</returns>
        public Vector3 GetSegmentPosition(int segment, float time, Space space = Space.World)
        {
            if (segment == segments.Count - 1) segment--;
            Vector3 a = segments[segment].GetPosition(Segment.ControlHandle.ANCHOR, space);
            Vector3 ata = segments[segment].GetPosition(Segment.ControlHandle.TANGENT_A, space);
            Vector3 b = segments[segment + 1].GetPosition(Segment.ControlHandle.ANCHOR, space);
            Vector3 btb = segments[segment + 1].GetPosition(Segment.ControlHandle.TANGENT_B, space);

            return BezierUtility.CubicLerp(a, ata, btb, b, time);
        }

        /// <summary>
        /// Determines the index of the segment corresponds to a given time (0 - 1) value, 
        /// </summary>
        /// <returns>The index of the segment that aligns with the specified time.</returns>
        public int GetSegment(float time)
        {
            return SplineUtility.GetSegment(segments.Count, time);
        }

        public float GetValidatedTime(float time)
        {
            return SplineUtility.GetValidatedTime(time, loop);
        }

        public Vector3 GetValidatedPosition(Vector3 position)
        {
            return SplineUtility.GetValidatedPosition(position, length, loop);
        }

        public float GetValidatedZDistance(Vector3 point1, Vector3 point2)
        {
            return SplineUtility.GetValidatedZDistance(point1, point2, length, loop);
        }

        public float TimeToFixedTime(float time)
        {
            return SplineUtilityNative.TimeToFixedTime(distanceMap, GetResolutionSpline(), time, loop);
        }

        /// <summary>
        /// Generates the normals at a specific point on the spline based on the given time value.
        /// </summary>
        /// <returns>An array of three normalized vectors representing the right, up, and forward directions at the specified spline position.</returns>
        public void GetNormalsNonAlloc(Vector3[] normals, float fixedTime, Space space = Space.World)
        {
            normals[1] = Vector3.up;
            normals[2] = Vector3.forward;
            normals[0] = Vector3.right;

            if (segments.Count == 1)
            {
                normals[1] = normalType == NormalType.STATIC_2D ? -transform.forward : transform.up;
                normals[2] = segments[0].GetDirection();
                normals[0] = Vector3.Cross(normals[2], -normals[1]).normalized;
                return;
            }

            if (normalType != NormalType.DYNAMIC)
            {
                //Z
                normals[2] = GetDirection(fixedTime, false, space);
                //X
                normals[0] = Vector3.Cross(normals[2], normalType == NormalType.STATIC_2D ? transform.forward : -transform.up).normalized;
                //Y
                normals[1] = Vector3.Cross(normals[2], normals[0]).normalized;

                //Add roation

                float degrees = math.degrees(-GetZRotationDegrees(fixedTime));
                Quaternion rotation = Quaternion.AngleAxis(degrees, -normals[2]);
                normals[0] = rotation * normals[0];
                normals[1] = rotation * normals[1];
            }
            else
            {
                if (normalsLocal.Length == 0)
                    return;

                //Get index
                float n = fixedTime * (normalsLocal.Length / 3);
                int normalIndex = (int)math.floor(n);

                if (normalIndex < 0 || (normalIndex * 3) + 1 >= normalsLocal.Length)
                    normalIndex = Mathf.Clamp(normalIndex, 0, (normalsLocal.Length / 3) - 1);

                //Get directions
                normals[0] = normalsLocal[normalIndex * 3];
                normals[1] = normalsLocal[normalIndex * 3 + 1];
                normals[2] = normalsLocal[normalIndex * 3 + 2];

                float degrees = math.degrees(-GetZRotationDegrees(fixedTime));
                Quaternion rotation = Quaternion.Inverse(Quaternion.AngleAxis(degrees, -normals[2]));
                normals[0] = rotation * normals[0];
                normals[1] = rotation * normals[1];

                if (space == Space.World)
                {
                    normals[0] = transform.TransformDirection(normals[0]);
                    normals[1] = transform.TransformDirection(normals[1]);
                    normals[2] = transform.TransformDirection(normals[2]);
                }
            }
        }

        /// <summary>
        /// Computes the interpolated Z-axis rotation in degrees for a specific time along the spline.
        /// </summary>
        /// <returns>The Z-axis rotation in degrees.</returns>
        public float GetZRotationDegrees(float timeValue)
        {
            int segment = Mathf.Clamp(GetSegment(timeValue), 1, segments.Count - 1);
            float segementTime = SplineUtility.GetSegmentTime(segment, segments.Count, timeValue);

            float contrast = segments[segment - 1].contrast - segments[segment].contrast;
            contrast = segments[segment].contrast + contrast - (contrast * segementTime);

            //Smooth the rotation closer to segment start/end.
            float numerator = Mathf.Pow(segementTime, contrast);
            float denominator = numerator + Mathf.Pow(1 - segementTime, contrast);
            segementTime = numerator / denominator;

            //Get rotation value
            float rotDif = segments[segment - 1].zRotation - segments[segment].zRotation;
            return math.radians(segments[segment].zRotation + rotDif - (rotDif * segementTime));
        }

        /// <summary>
        /// Joins the Spline with another spline.
        /// </summary>
        /// <param name="spline">The secondary Spline to join this spline.</param>
        /// <param name="joinType">The type of join operation to perform (e.g., END_TO_START, END_TO_END).</param>
        public void Join(Spline spline, JoinType joinType)
        {
            float skipFirstDistance = 2.5f;

            if (joinType == JoinType.END_TO_START)
            {
                float distance = Vector3.Distance(segments[segments.Count - 1].GetPosition(Segment.ControlHandle.ANCHOR), spline.segments[0].GetPosition(Segment.ControlHandle.ANCHOR));
                for (int i = 0; i < spline.segments.Count; i++)
                {
                    if (GeneralUtility.IsZero(distance, skipFirstDistance) && i == 0)
                        continue;

                    Segment s = spline.segments[i];
                    s.SetSplineParent(this);
                    segments.Add(s);
                }
            }
            else if (joinType == JoinType.END_TO_END)
            {
                float distance = Vector3.Distance(segments[segments.Count - 1].GetPosition(Segment.ControlHandle.ANCHOR), spline.segments[spline.segments.Count - 1].GetPosition(Segment.ControlHandle.ANCHOR));
                for (int i = spline.segments.Count - 1; i >= 0; i--)
                {
                    if (GeneralUtility.IsZero(distance, skipFirstDistance) && i == spline.segments.Count - 1)
                        continue;

                    Segment s = spline.segments[i];
                    s.SetSplineParent(this);
                    segments.Add(s);
                    Vector3 tangentA = s.GetPosition(Segment.ControlHandle.TANGENT_A);
                    Vector3 tangentB = s.GetPosition(Segment.ControlHandle.TANGENT_B);
                    s.SetPosition(Segment.ControlHandle.TANGENT_A, tangentB);
                    s.SetPosition(Segment.ControlHandle.TANGENT_B, tangentA);
                }
            }
            else if (joinType == JoinType.START_TO_START)
            {
                float distance = Vector3.Distance(segments[0].GetPosition(Segment.ControlHandle.ANCHOR), spline.segments[0].GetPosition(Segment.ControlHandle.ANCHOR));
                for (int i = 0; i < spline.segments.Count; i++)
                {
                    if (GeneralUtility.IsZero(distance, skipFirstDistance) && i == 0)
                        continue;

                    Segment s = spline.segments[i];
                    s.SetSplineParent(this);
                    Vector3 tangentA = s.GetPosition(Segment.ControlHandle.TANGENT_A);
                    Vector3 tangentB = s.GetPosition(Segment.ControlHandle.TANGENT_B);
                    s.SetPosition(Segment.ControlHandle.TANGENT_A, tangentB);
                    s.SetPosition(Segment.ControlHandle.TANGENT_B, tangentA);
                    segments.Insert(0, s);
                }
            }
            else if (joinType == JoinType.START_TO_END)
            {
                float distance = Vector3.Distance(segments[0].GetPosition(Segment.ControlHandle.ANCHOR), spline.segments[spline.segments.Count - 1].GetPosition(Segment.ControlHandle.ANCHOR));
                for (int i = spline.segments.Count - 1; i >= 0; i--)
                {
                    if (GeneralUtility.IsZero(distance, skipFirstDistance) && i == spline.segments.Count - 1)
                        continue;

                    Segment s = spline.segments[i];
                    s.SetSplineParent(this);
                    segments.Insert(0, s);
                }
            }
        }

        /// <summary>
        /// Splits the Spline at the specified segment index and transfers the remaining segments to a new spline.
        /// </summary>
        /// <param name="newSpline">The new Spline that will receive the segments after the newSpline.</param>
        /// <param name="segmentIndex">The index of the segment at which to newSpline the original spline.</param>
        public void Split(Spline newSpline, int segmentIndex)
        {
            newSpline.segments.Clear();

            for (int i = segmentIndex; i < segments.Count; i++)
            {
                Segment s = segments[i];

                if (i == segmentIndex)
                    newSpline.CreateSegment(0, s.GetPosition(Segment.ControlHandle.ANCHOR), s.GetPosition(Segment.ControlHandle.TANGENT_A), s.GetPosition(Segment.ControlHandle.TANGENT_B));
                else
                {
                    s.SetSplineParent(newSpline);
                    newSpline.segments.Add(s);
                }
            }

            for (int i = segments.Count - 1; i > segmentIndex; i--)
                segments.RemoveAt(i);

            if (transform.parent != null)
                newSpline.transform.parent = transform.parent;

            monitor.MarkDirty();
            newSpline.monitor.MarkDirty();
        }

        /// <summary>
        /// Attempts to find link crossings based on the provided spline positions.
        /// </summary>
        /// <param name="splinePosition">The current position of the spline.</param>
        /// <param name="previousSplinePosition">The previous position of the spline.</param>
        /// <param name="linkFlags">Flags used determine rules for what links to look for.</param>
        /// <param name="currentSegment">The fromSegment where a link crossing is found, if any.</param>
        /// <returns>A list of links at the crossing fromSegment, or an empty list if none are found.</returns>
        public void FindLinkCrossingsNonAlloc(List<Segment> links, Vector3 splinePosition, Vector3 previousSplinePosition, LinkFlags linkFlags, out Segment currentSegment)
        {
            //Handle loop
            if (loop && splinePosition.z > length)
            {
                int loops = Mathf.FloorToInt(splinePosition.z / length);
                splinePosition.z -= length * loops;
                previousSplinePosition.z -= length * loops;
            }

            float minZ = Mathf.Min(previousSplinePosition.z, splinePosition.z);
            float maxZ = Mathf.Max(previousSplinePosition.z, splinePosition.z);
            currentSegment = null;

            for (int i = 0; i < segments.Count; i++)
            {
                Segment s = segments[i];

                if (s.links == null)
                    continue;

                if (s.links.Count == 0)
                    continue;

                if ((s.zPosition > minZ && s.zPosition < maxZ) || splinePosition.z == s.zPosition)
                {
                    for (int i2 = 0; i2 < s.links.Count; i2++)
                    {
                        Segment s2 = s.links[i2];

                        if (linkFlags != LinkFlags.NONE)
                        {
                            Spline spline2 = s2.splineParent;
                            Segment s2Link = s2;
                            Segment s2First = spline2.segments[0];
                            Segment s2Last = spline2.segments[spline2.segments.Count - 1];


                            if (linkFlags.HasFlag(LinkFlags.SKIP_LAST) && s2Last == s2Link)
                                continue;

                            if (linkFlags.HasFlag(LinkFlags.SKIP_FIRST) && s2First == s2Link)
                                continue;

                            if (linkFlags.HasFlag(LinkFlags.SKIP_SELF) && s == s2Link)
                                continue;
                        }

                        links.Add(s2);
                    }

                    currentSegment = s;
                    return;
                }
            }
        }

        public void FindLinkCrossingsNonAlloc(List<Segment> links, Vector3 splinePosition, Vector3 previousSplinePosition)
        {
            FindLinkCrossingsNonAlloc(links, splinePosition, previousSplinePosition, LinkFlags.NONE, out _);
        }

        public float CalculateLinkCrossingZPosition(Vector3 splinePosition, Segment fromSegment, Segment toSegment)
        {
            float validatedZPosition = GetValidatedPosition(splinePosition).z;
            float dif = validatedZPosition - fromSegment.zPosition;
            return toSegment.zPosition + dif;
        }

        /// <summary>
        /// Creates an deforamtion SplineObject for the spline.
        /// </summary>
        /// <param name="go">The GameObject that will be turned into an SplineObject deformation.</param>
        /// <param name="localSplinePosition">The initial position of the object relative to the spline.</param>
        /// <returns>The created SplineObject configured for deformation.</returns>
        public SplineObject CreateDeformation(GameObject go, Vector3 localSplinePosition, Quaternion localSplineRotation, bool skipRuntimeUpdates = false, Transform parent = null)
        {
#if UNITY_EDITOR
            disableOnTransformChildrenChanged = true;
#endif
            SplineObject.defaultType = SplineObject.Type.DEFORMATION;

            if (parent == null) go.transform.parent = transform;
            else go.transform.parent = parent;
            SplineObject so = go.AddComponent<SplineObject>();

            so.localSplinePosition = localSplinePosition;
            so.localSplineRotation = localSplineRotation;
            so.componentMode = ComponentMode.ACTIVE;

            if (skipRuntimeUpdates)
            {
                RemoveSplineObject(so);
                HandleDeformationWorker.Deform(so, DeformationWorker.Type.RUNETIME, this);
                so.componentMode = ComponentMode.INACTIVE;
            }
            else
                so.monitor.ForceUpdate();

#if UNITY_EDITOR
            disableOnTransformChildrenChanged = false;
#endif

            return so;
        }

        /// <summary>
        /// Creates an follower SplineObject for the spline.
        /// </summary>
        /// <param name="go">The GameObject that will be turned into an follower.</param>
        /// <param name="localSplinePosition">The initial position of the object relative to the spline.</param>
        /// <returns>The created SplineObject configured as a follower.</returns>
        public SplineObject CreateFollower(GameObject go, Vector3 localSplinePosition, Quaternion localSplineRotation, bool skipRuntimeUpdates = false, Transform parent = null)
        {
#if UNITY_EDITOR
            disableOnTransformChildrenChanged = true;
#endif
            SplineObject.defaultType = SplineObject.Type.FOLLOWER;

            if (parent == null) go.transform.parent = transform;
            else go.transform.parent = parent;
            SplineObject so = go.AddComponent<SplineObject>();

            so.localSplinePosition = localSplinePosition;
            so.localSplineRotation = localSplineRotation;
            so.componentMode = ComponentMode.ACTIVE;

            if (skipRuntimeUpdates)
            {
                RemoveSplineObject(so);
                followerUpdateList.Add(so);
                so.componentMode = ComponentMode.INACTIVE;
            }
            else
                so.monitor.ForceUpdate();

#if UNITY_EDITOR
            disableOnTransformChildrenChanged = false;
#endif

            return so;
        }

        public void DistanceToClosestSplineObjectNonAlloc(List<SplineObject> splineObjectsToSearch, List<SearchData> searchData, Vector3 splinePosition, int maxAmount, SearchFlags searchFlags)
        {
            splinePosition = GetValidatedPosition(splinePosition);
            Check(this, splinePosition, 0, false);

            if (!searchFlags.HasFlag(SearchFlags.SEARCH_CLOSEST_LINK_FORWARD) && !searchFlags.HasFlag(SearchFlags.SEARCH_CLOSEST_LINK_BACKWARD))
                return;

            Segment closestSegment = null;
            float segmentDistanceCheck = 99999;
            float segmentOffset = 99999;

            //Get closest segment
            foreach (Segment s in segments)
            {
                float d = GetValidatedZDistance(splinePosition, new Vector3(0, 0, s.zPosition));

                if (segmentDistanceCheck > Mathf.Abs(d))
                {
                    if (s.links == null)
                        continue;

                    if (!searchFlags.HasFlag(SearchFlags.SEARCH_BACKWARD) && d <= 0)
                        continue;

                    if (!searchFlags.HasFlag(SearchFlags.SEARCH_FORWARD) && d >= 0)
                        continue;

                    closestSegment = s;
                    segmentDistanceCheck = Mathf.Abs(d);
                    segmentOffset = d;
                }
            }

            if (closestSegment == null)
                return;

            //Check all links on closest segment if it has any.
            foreach (Segment s in closestSegment.links)
            {
                if (s == null)
                    continue;

                if (s.splineParent == this)
                    continue;

                Check(s.splineParent, new Vector3(0, 0, s.zPosition), segmentOffset, true);
            }

            void Check(Spline splineToCheck, Vector3 splinePositionToCheck, float offset, bool isLink)
            {
                for (int i = 0; i < splineObjectsToSearch.Count; i++)
                {
                    SplineObject so = splineObjectsToSearch[i];
                    if (so.transform.parent != transform)
                        continue;

                    if (!so.IsEnabled())
                        continue;

                    float d = GetValidatedZDistance(splinePositionToCheck, so.splinePosition);

                    if (searchFlags.HasFlag(SearchFlags.NEED_SAME_X_POSITION) && !GeneralUtility.IsEqual(splinePositionToCheck.x, so.splinePosition.x))
                        continue;

                    if (searchFlags.HasFlag(SearchFlags.NEED_SAME_Y_POSITION) && !GeneralUtility.IsEqual(splinePositionToCheck.y, so.splinePosition.y))
                        continue;

                    if (!isLink && !searchFlags.HasFlag(SearchFlags.SEARCH_BACKWARD) && d < 0)
                        continue;

                    if (!isLink && !searchFlags.HasFlag(SearchFlags.SEARCH_FORWARD) && d > 0)
                        continue;

                    if (isLink && !searchFlags.HasFlag(SearchFlags.SEARCH_CLOSEST_LINK_BACKWARD) && d < 0)
                        continue;

                    if (isLink && !searchFlags.HasFlag(SearchFlags.SEARCH_CLOSEST_LINK_FORWARD) && d > 0)
                        continue;

                    float d2 = Mathf.Abs(d) + offset;

                    if (searchData.Count == 0)
                    {
                        searchData.Add(new SearchData(offset, d * -1, d2, so, isLink, d > 0));
                        continue;
                    }

                    bool inserted = false;
                    for (int i2 = 0; i2 < searchData.Count; i2++)
                    {
                        if (Mathf.Abs(searchData[i2].distanceToClosest) > d2)
                        {
                            searchData.Insert(i2, new SearchData(offset, d * -1, d2, so, isLink, d > 0));
                            inserted = true;
                            break;
                        }
                    }

                    if (!inserted) searchData.Add(new SearchData(offset, d * -1, d2, so, isLink, d > 0));
                    if (searchData.Count > maxAmount) searchData.RemoveAt(searchData.Count - 1);
                }
            }
        }

        /// <summary>
        /// Converts a position on a spline to a world position.
        /// </summary>
        /// <returns>The world position.</returns>
        public Vector3 SplinePositionToWorldPosition(Transform parentTransform, Vector3 splinePosition, float4x4 matrix, bool alignToEnd = false)
        {
            Vector3 worldPosition;
            NativeArray<Vector3> point = new NativeArray<Vector3>(1, Allocator.TempJob);
            NativeHashMap<int, float4x4> localSpaces = new NativeHashMap<int, float4x4>(1, Allocator.TempJob);
            NativeArray<int> localSpaceMap = new NativeArray<int>(1, Allocator.TempJob);
            NativeArray<bool> mirrorMap = new NativeArray<bool>(0, Allocator.TempJob);
            NativeArray<bool> alignToEndMap = new NativeArray<bool>(1, Allocator.TempJob);

            point[0] = splinePosition;
            localSpaces.Add(0, matrix);
            localSpaceMap[0] = 0;
            alignToEndMap[0] = alignToEnd;

            DeformJob deformJob = DeformationUtility.CreateDeformJob(this, point, nativeSegmentsLocal, localSpaces, localSpaceMap, mirrorMap, alignToEndMap, SplineObject.Type.FOLLOWER);

            JobHandle jobHandle = deformJob.Schedule(1, 1);
            jobHandle.Complete();

            worldPosition = parentTransform.TransformPoint(deformJob.vertices[0]);
            DeformationUtility.DisposeDeformJob(deformJob);

            return worldPosition;
        }

        public Vector3 SplinePositionToWorldPosition(Vector3 splinePosition)
        {
            return SplinePositionToWorldPosition(transform, splinePosition, float4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one));
        }

        /// <summary>
        /// Transforms a world position to its corresponding position on a spline, accounting for both fixed and dynamic time calculations and handling potential spline extensions at both ends.
        /// </summary>
        /// <returns>The spline position.</returns>
        public Vector3 WorldPositionToSplinePosition(Vector3 worldPosition, int precision, float steps = 5)
        {
            if (segments.Count == 1)
                return worldPosition;

            //Closest point needs to be calculated in fixedTime and time.
            //Get fixedTime for z position
            Vector3 fixedPoint = GetNearestPoint(worldPosition, out float fixedTime, precision, steps, false, true);
            Vector3 point = GetNearestPoint(worldPosition, out float time, precision, steps, false, false);

            Vector3 localPoint = transform.InverseTransformPoint(worldPosition);
            Vector3 backDirection = segments[0].GetDirection();
            Vector3 frontDirection = -segments[segments.Count - 1].GetDirection();
            float extendedDistance = 0;

            if (!loop)
            {
                Vector3 extendBackPoint = LineUtility.GetNearestPoint(segments[0].GetPosition(Segment.ControlHandle.ANCHOR),
                                                                  backDirection,
                                                                  worldPosition,
                                                                  out float extendBackLine);

                Vector3 extendFrontPoint = LineUtility.GetNearestPoint(segments[segments.Count - 1].GetPosition(Segment.ControlHandle.ANCHOR),
                                                                   frontDirection,
                                                                   worldPosition,
                                                                   out float extendFrontLine);

                float distanceToExtendBack = Vector3.Distance(worldPosition, extendBackPoint);
                float distanceToExtendFront = Vector3.Distance(worldPosition, extendFrontPoint);
                float distanceToPoint = Vector3.Distance(worldPosition, fixedPoint);

                bool extendingBack = extendBackLine >= 0 && distanceToExtendBack < distanceToPoint && distanceToExtendBack < distanceToExtendFront;
                bool extendingFront = extendFrontLine >= 0 && distanceToExtendFront < distanceToPoint && distanceToExtendFront < distanceToExtendBack;

                if (time == 0) extendingBack = true;
                if (time == 1) extendingFront = true;

                if (extendingBack)
                {
                    extendedDistance = -Vector3.Distance(localPoint, segments[0].GetPosition(Segment.ControlHandle.ANCHOR, Space.Self));
                    fixedTime = 0;
                    time = 0;
                }
                else if (extendingFront)
                {
                    extendedDistance = Vector3.Distance(localPoint, segments[segments.Count - 1].GetPosition(Segment.ControlHandle.ANCHOR, Space.Self));
                    fixedTime = 1;
                    time = 1;
                }
            }

            //GetNormalsNonAlloc() allready calculates zRotation. Dont do it again.
            GetNormalsNonAlloc(normalsContainer, fixedTime, Space.World);
            //Needs to be time here else the position will be offseted and the x axel will be wrong.
            Vector3 p1 = GetPosition(Mathf.Clamp(time, 0, 1), Space.World);

            Vector3 xDirection = normalsContainer[0];
            Vector3 yDirection = normalsContainer[1];

            Vector3 closestXPos = LineUtility.GetNearestPoint(p1, xDirection, worldPosition, out float timeX);
            Vector3 closestYPos = LineUtility.GetNearestPoint(p1, yDirection, worldPosition, out float timeY);

            Vector3 splinePosition;
            splinePosition.x = Vector3.Distance(p1, closestXPos) * Mathf.Sign(timeX);
            splinePosition.y = Vector3.Distance(p1, closestYPos) * Mathf.Sign(timeY);
            splinePosition.z = length * fixedTime + extendedDistance;

            //Apply scale. This is perfect as long the Spline is scaled the same on all axels. Will still work if not but can be scewed.
            splinePosition.x /= transform.localScale.x;
            splinePosition.y /= transform.localScale.y;

            return splinePosition;
        }

        public Quaternion SplineRotationToWorldRotation(Quaternion rotation, float time)
        {
            float fixedTime = TimeToFixedTime(time);
            GetNormalsNonAlloc(normalsContainer, fixedTime);
            Quaternion splineForwardRot = Quaternion.LookRotation(normalsContainer[2], normalsContainer[1]);
            return splineForwardRot * rotation;
        }

        /// <summary>
        /// Converts world rotation to spline roation.
        /// </summary>
        /// <returns>A new Quaternion thats converted from world rotation to spline rotation.</returns>
        public Quaternion WorldRotationToSplineRotation(Quaternion rotation, float time)
        {
            //Rotation
            float fixedTime = TimeToFixedTime(time);
            GetNormalsNonAlloc(normalsContainer, fixedTime);
            Quaternion splineLocalRotation = Quaternion.LookRotation(normalsContainer[2], normalsContainer[1]);
            return Quaternion.Inverse(splineLocalRotation) * rotation;
        }

        public void ReverseSegments()
        {
            segments.Reverse();
            foreach (Segment s in segments)
            {
                Vector3 tangetA = s.GetPosition(Segment.ControlHandle.TANGENT_A);
                Vector3 tangetB = s.GetPosition(Segment.ControlHandle.TANGENT_B);
                s.SetPosition(Segment.ControlHandle.TANGENT_A, tangetB);
                s.SetPosition(Segment.ControlHandle.TANGENT_B, tangetA);
            }

            UpdateCachedData();
        }
    }
}
