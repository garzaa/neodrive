// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: EHandleEvents.cs
//
// Author: Mikael Danielsson
// Date Created: 04-02-2024
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

#if UNITY_EDITOR

using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

using SplineArchitect.Objects;

namespace SplineArchitect
{
    public class EHandleEvents
    {
        public static bool updateSelection;
        public static bool sceneIsClosing;
        public static bool sceneIsLoadedPlaymode;
        public static bool buildRunning;
        public static bool dragActive;
        public static bool undoActive;
        public static PlayModeStateChange playModeStateChange;

        private static List<Spline> markedInfoUpdates = new List<Spline>();
        private static List<(SplineObject, bool)> detachList = new List<(SplineObject, bool)>();
        private static List<Spline> InitalizeAfterDragSplines = new List<Spline>();
        private static List<SplineConnector> InitalizeAfterDragSplineConnectors = new List<SplineConnector>();
        private static List<SplineObject> InitalizeAfterDragSplineObjects = new List<SplineObject>();

        //Events
        public static event Action OnFirstUpdate;
        public static event Action OnUpdateEarly;
        public static event Action<Spline, Vector3> OnTransformToCenter;
        public static event Action<Spline> OnUndoSelectedSplines;
        public static event Action<Spline> OnInitalizeSplineEditor;
        public static event Action<Spline> OnDestroySpline;
        public static event Action<Spline> OnSplineJoin;
        public static event Action<Spline> OnSplineReverse;
        public static event Action<Spline> OnSplineLoop;
        public static event Action<Spline, Spline> OnSplineSplit;
        public static event Action<SplineObject> AfterSplineObjectActivatePositionTool;
        public static event Action<SplineObject> OnSplineObjectParentChanged;
        public static event Action<Segment> OnSegmentRemoved;
        public static event Action<Spline> OnCopiedSpline;
        public static event Action<Spline> OnUpdateLoopEndData;
        public static event Action<Spline, SplineObject> OnSplineObjectSCeneGUI;
        public static event Action OnDisposeDeformJob;

        public static void InitAfterDrag(SceneView sceneView)
        {
            if (Event.current.type == EventType.DragUpdated)
            {
                dragActive = true;
            }
            else if (Event.current.type == EventType.DragPerform || Event.current.type == EventType.DragExited)
            {
                dragActive = false;
            }

            if (Event.current.type == EventType.DragPerform)
            {
                foreach (Spline spline in InitalizeAfterDragSplines)
                {
                    if (spline == null)
                        continue;

                    spline.Initalize();
                }

                foreach (SplineConnector sc in InitalizeAfterDragSplineConnectors)
                {
                    if (sc == null)
                        continue;

                    sc.Initalize();
                }

                foreach (SplineObject so in InitalizeAfterDragSplineObjects)
                {
                    if (so == null)
                        continue;

                    so.Initalize();
                }

                InitalizeAfterDragSplines.Clear();
                InitalizeAfterDragSplineObjects.Clear();
            }
        }

        public static void InvokeDisposeDeformJob()
        {
            OnDisposeDeformJob?.Invoke();
        }

        public static void InvokeFirstUpdate()
        {
            OnFirstUpdate?.Invoke();
        }

        public static void InvokeUpdateEarly()
        {
            OnUpdateEarly?.Invoke();
        }

        public static void InvokeSegmentRemoved(Segment segment)
        {
            OnSegmentRemoved?.Invoke(segment);
        }

        public static void InvokeTransformToCenter(Spline spline, Vector3 dif)
        {
            OnTransformToCenter?.Invoke(spline, dif);
        }

        public static void InvokeUndoSelection(Spline spline)
        {
            OnUndoSelectedSplines?.Invoke(spline);
        }

        public static void InvokeInitalizeSplineEditor(Spline spline)
        {
            OnInitalizeSplineEditor?.Invoke(spline);
        }

        public static void InvokeSplineSplit(Spline spline, Spline newSpline)
        {
            OnSplineSplit?.Invoke(spline, newSpline);
        }

        public static void InvokeSplineJoin(Spline spline)
        {
            OnSplineJoin?.Invoke(spline);
        }

        public static void InvokeSplineLoop(Spline spline)
        {
            OnSplineLoop?.Invoke(spline);
        }

        public static void InvokeSplineReverse(Spline spline)
        {
            OnSplineReverse?.Invoke(spline);
        }

        public static void InvokeDestroySpline(Spline spline)
        {
            OnDestroySpline?.Invoke(spline);
        }

        public static void InvokeCopiedSpline(Spline spline)
        {
            OnCopiedSpline?.Invoke(spline);
        }

        public static void InvokeUpdateLoopEndData(Spline spline)
        {
            OnUpdateLoopEndData?.Invoke(spline);
        }

        public static void InvokeSplineObjectParentChanged(SplineObject so)
        {
            OnSplineObjectParentChanged?.Invoke(so);
        }

        public static void InvokeSplineObjectSceneGUI(Spline spline, SplineObject splineObject)
        {
            OnSplineObjectSCeneGUI?.Invoke(spline, splineObject);
        }

        public static void InvokeAfterSplineObjectActivatePositionTool(SplineObject splineObject)
        {
            AfterSplineObjectActivatePositionTool?.Invoke(splineObject);
        }

        public static void MarkForInfoUpdate(Spline spline)
        {
            markedInfoUpdates.Add(spline);
        }

        public static List<Spline> GetMarkedForInfoUpdates()
        {
            return markedInfoUpdates;
        }

        public static void ClearMarkedForInfoUpdates()
        {
            markedInfoUpdates.Clear();
        }

        public static void ForceUpdateSelection(SplineObject so)
        {       
            updateSelection = true;
        }

        public static void MarkForDetach(SplineObject so, bool world)
        {
            detachList.Add((so, world));
        }

        public static List<(SplineObject, bool)> GetDetachList()
        {
            return detachList;
        }

        public static void ClearDetachList()
        {
            detachList.Clear();
        }

        public static void InitalizeAfterDrag(Spline spline)
        {
            if (InitalizeAfterDragSplines.Contains(spline))
                return;

            InitalizeAfterDragSplines.Add(spline);
        }

        public static void InitalizeAfterDrag(SplineConnector splineConnector)
        {
            if (InitalizeAfterDragSplineConnectors.Contains(splineConnector))
                return;

            InitalizeAfterDragSplineConnectors.Add(splineConnector);
        }

        public static void InitalizeAfterDrag(SplineObject splineObject)
        {
            if (InitalizeAfterDragSplineObjects.Contains(splineObject))
                return;

            InitalizeAfterDragSplineObjects.Add(splineObject);
        }
    }
}

#endif
