// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: SplineConnector.cs
//
// Author: Mikael Danielsson
// Date Created: 22-05-2025
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

using UnityEngine;

using SplineArchitect.Monitor;

namespace SplineArchitect.Objects
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class SplineConnector : MonoBehaviour
    {
        //Runtime data
        [NonSerialized]
        public List<Segment> connections;
        [NonSerialized]
        private bool initalized;
        [NonSerialized]
        public MonitorSplineConnector monitor;

        private void OnEnable()
        {
            Initalize();
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

            if (connections == null)
                connections = new List<Segment>();

            if (monitor == null)
                monitor = new MonitorSplineConnector(this);

            HandleRegistry.AddSplineConnector(this);
        }

        private void OnDestroy()
        {
            HandleRegistry.RemoveSplineConnector(this);

            // Clear connections
            if (connections != null)
            {
                for (int i = connections.Count - 1; i >= 0; i--)
                {
                    Segment s = connections[i];

#if UNITY_EDITOR
                    if(s != null && s.splineParent != null)
                        UnityEditor.Undo.RecordObject(s.splineParent, "Remove Spline Connector");
#endif

                    if (s != null)
                    {
                        s.linkTarget = Segment.LinkTarget.NONE;
                        s.splineConnector = null;
                    }
                }
                connections.Clear();
            }
        }

        private void Update()
        {
#if UNITY_EDITOR
            if (EHandleEvents.dragActive)
                return;

            if (!Application.isPlaying)
                return;
#endif

            bool posChange = monitor.PosChange(true);
            bool rotChange = monitor.RotChange(true);

            for (int i = connections.Count - 1; i >= 0; i--)
            {
                Segment s = connections[i];

                if (s == null || s.linkTarget != Segment.LinkTarget.SPLINE_CONNECTOR)
                {
                    connections.RemoveAt(i);
                    continue;
                }

                if (posChange || rotChange)
                {
                    AlignSegment(s);
#if !UNITY_EDITOR
                    s.splineParent.monitor.MarkDirty();
#endif
                }
            }
        }

        public void RemoveConnection(Segment segment)
        {
            connections.Remove(segment);
        }

        public void AddConnection(Segment segment)
        {
            if (connections.Contains(segment))
                return;

            connections.Add(segment);
        }

        public void AlignSegment(Segment segment)
        {
            Vector3 connectionPoint = transform.position;

            Vector3 a = segment.GetPosition(Segment.ControlHandle.ANCHOR);
            segment.SetPosition(Segment.ControlHandle.ANCHOR, connectionPoint);

            Vector3 ta = segment.GetPosition(Segment.ControlHandle.TANGENT_A);
            Vector3 tb = segment.GetPosition(Segment.ControlHandle.TANGENT_B);

            float taDistance = Vector3.Distance(a, ta);
            float tbDistance = Vector3.Distance(a, tb);

            Vector3 fDir = transform.forward;
            if (segment.mirrorConnector) fDir = Quaternion.Euler(0, 0, 90) * transform.forward;

            segment.SetPosition(Segment.ControlHandle.TANGENT_A, connectionPoint - fDir * taDistance);
            segment.SetPosition(Segment.ControlHandle.TANGENT_B, connectionPoint + fDir * tbDistance);

            float zRotation = transform.rotation.eulerAngles.z;

            if (zRotation > 180)
                zRotation -= 360;

            segment.zRotation = -zRotation;
        }
    }
}