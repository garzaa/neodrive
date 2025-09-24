// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: EHandleSpline.cs
//
// Author: Mikael Danielsson
// Date Created: 28-01-2023
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System.Collections.Generic;

using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

using SplineArchitect.CustomTools;
using SplineArchitect.Objects;
using SplineArchitect.Ui;
using SplineArchitect.Utility;

namespace SplineArchitect
{
    public static class EHandleSpline
    {
        public static bool controlPointCreationActive = false;

        private static List<Spline> markedInfoUpdates = new List<Spline>();
        private static List<int> intersectingControlPoints = new List<int>();
        public static EHandleSegment.SegmentIndicatorData segementIndicatorData = new EHandleSegment.SegmentIndicatorData();
        private static Plane projectionPlane = new Plane(Vector3.up, Vector3.zero);
        private static Vector3[] normalsContainer = new Vector3[3];
        public static float lengthAllSplines { private set ; get; }
        public static int totalLinesDrawn;
        public static int hotControlId;

        //Containers
        private static List<Segment> flattenContainer = new List<Segment>();

        public static void BeforeSceneGUIGlobal(SceneView sceneView, Event e)
        {
            if (e.type == EventType.Layout)
            {
                lengthAllSplines = HandleRegistry.GetTotalLengthOfAllSplines();
                totalLinesDrawn = 0;
            }

            if (!controlPointCreationActive)
                return;

            Spline spline = EHandleSelection.selectedSpline;
            bool gridVisiblity = GlobalSettings.GetGridVisibility();

            if (spline == null)
            {
                if (gridVisiblity)
                {
                    if(sceneView.in2DMode) UpdateIndicator2DGrid(e, ref segementIndicatorData);
                    else UpdateIndicator3DGrid(e, ref segementIndicatorData);
                }
                else
                {
                    if (sceneView.in2DMode) UpdateIndicator2D(e, ref segementIndicatorData);
                    else UpdateIndicator3D(e, ref segementIndicatorData);
                }

                //Create spline
                if (e.type == EventType.MouseDown && Event.current.button == 0)
                {
                    if (sceneView.in2DMode)
                    {
                        //Works for grid and non grid
                        CreateSpline2D(e);
                    }
                    else
                    {
                        if (gridVisiblity) CreateSpline3DGrid(e);
                        else CreateSpline3DWorldPoint(e);
                    }

                    e.Use();
                }
            }
            else
            {
                UpdateIndicatorHover(spline, e.mousePosition);

                //Hovering spline
                if (spline.indicatorDistanceToSpline < GetIndicatorActivationDistance(spline))
                {
                    if (e.type != EventType.MouseDown || Event.current.button != 0)
                        return;

                    bool extendBack = spline.indicatorSegement == 0;
                    bool extendFront = spline.indicatorSegement > spline.segments.Count - 1;

                    if (extendFront || extendBack)
                        EHandleSegment.CreateExtended(spline, extendBack);
                    else
                        EHandleSegment.CreateWithAutoSmooth(spline, spline.indicatorSegement, spline.indicatorTime);

                    GUIUtility.hotControl = GetHotControlId();
#if UNITY_6000_0_OR_NEWER
                    e.Use();
#endif
                }
                //Not hovering spline
                else
                {
                    //Updated segment indicator
                    if (spline.normalType == Spline.NormalType.STATIC_2D)
                    {
                        if (gridVisiblity) EHandleSegment.UpdateIndicator2DGrid(spline, e, ref segementIndicatorData);
                        else EHandleSegment.UpdateIndicator2D(spline, e, ref segementIndicatorData);
                    }
                    else
                    {
                        if (gridVisiblity) EHandleSegment.UpdateIndicator3DGrid(spline, e, ref segementIndicatorData);
                        else EHandleSegment.UpdateIndicator3D(spline, e, ref segementIndicatorData);
                    }

                    if (spline.loop || e.type != EventType.MouseDown || Event.current.button != 0)
                        return;

                    EHandleSegment.CreateFromWorldPoint(spline, segementIndicatorData);
                    GUIUtility.hotControl = GetHotControlId();
#if UNITY_6000_0_OR_NEWER
                    e.Use();
#endif
                }
            }
        }

        public static void OnSceneGUI(Spline spline, Event e)
        {
            EHandleSegment.HandleDeletion(spline, e);
        }

        public static void InitalizeEditor(Spline spline, bool firstInitialization)
        {
            if (spline.initalizedEditor)
                return;

            spline.initalizedEditor = true;

            //Create segments if none exists.
            if (spline.segments.Count == 0)
                EHandleSegment.CreateSegmentsFromEditorCameraDirection(spline);

            spline.editorId = GlobalObjectId.GetGlobalObjectIdSlow(spline.transform).targetObjectId.ToString();
            MarkForInfoUpdate(spline);

            if(firstInitialization)
            {
                HandleRegistry.AddInstanceId(spline.transform);
            }
            else
            {
                //Copy case
                if (HandleRegistry.IsNew(spline.transform) && !EHandlePrefab.prefabStageOpenedLastFrame && !EHandlePrefab.prefabStageClosedLastFrame)
                {
                    EHandleEvents.InvokeCopiedSpline(spline);
                    EHandleSelection.ForceUpdate();

                    //Unlink all on new spline
                    //EHandleUndo.RecordNow(spline);
                    //foreach (Segment s in spline.segments)
                    //{
                    //    s.linkTarget = Segment.LinkTarget.NONE;
                    //}
                }
            }

            //Need to update the spline length again. Becouse OnEnable on SplineObjects can run before OnEnable on the Spline. And the splineObjects will get 0 in spline length in the monitor.
            foreach(SplineObject so in spline.splineObjects)
            {
                if (so.type == SplineObject.Type.DEFORMATION)
                {
                    so.CacheUntrackedInstanceMeshes();
                    so.SyncMeshContainers();
                    so.SyncInstanceMeshesFromCache();
                }

                so.monitor.UpdateSplineLength(spline.length);
                so.initalizedThisFrame = false;
            }

            spline.EstablishLinks();
            EHandleDeformation.TryDeform(spline);

            EHandleEvents.InvokeInitalizeSplineEditor(spline);
        }

        public static void UpdateLinksOnTransformMovement(Spline spline)
        {
            if (spline.monitor.PosRotChange() || spline.monitor.ScaleChange())
            {
                EHandleSegment.LinkMovementAll(spline);
            }
        }

        public static void UpdateCachedData(Spline spline, bool forceUpdate)
        {
            bool segementChange = spline.monitor.SegementChange();
            bool normalTypeChange = spline.monitor.NormalTypeChange();
            bool resolitionChange = spline.monitor.ResolutionChange();
            bool posRotChange = spline.monitor.PosRotChange();
            bool zRotContSaddleScaleChange = spline.monitor.ZRotContSaddleScaleChange();
            bool noiseChange = spline.monitor.NoiseChange();

            bool splineChange = forceUpdate || segementChange || normalTypeChange || resolitionChange || posRotChange || zRotContSaddleScaleChange || noiseChange;

            if (splineChange)
                spline.UpdateCachedData();
        }

        private static void UpdateIndicatorHover(Spline spline, Vector3 mousePosition)
        {
            List<Segment> segments = spline.segments;
            Ray mouseRay = HandleUtility.GUIPointToWorldRay(mousePosition);

            if (segments.Count > 1)
            {
                Vector3 closestPos = ClosestMousePoint(spline, mouseRay, 12, out float distanceToSpline, out float time, 40);
                spline.indicatorSegement = spline.GetSegment(time);

                if (!spline.loop)
                {
                    Vector3 startPos = spline.GetSegmentPosition(0, 0);
                    Vector3 endPos = spline.GetSegmentPosition(segments.Count - 1, 1);

                    EHandleSegment.GetIndicatorExtendedData(spline, startPos, endPos, mouseRay, out Vector3 extendedPos, out float extendedDistance, out float extendedTime);

                    if (extendedDistance < distanceToSpline)
                    {
                        distanceToSpline = extendedDistance;
                        closestPos = extendedPos;
                        time = extendedTime;
                    }

                    if (Mathf.Approximately(time, 0) || time < 0) spline.indicatorSegement = 0;
                    else if (Mathf.Approximately(time, 1) || time > 1) spline.indicatorSegement = spline.segments.Count;
                }

                spline.indicatorDistanceToSpline = distanceToSpline;
                spline.indicatorTime = time;
                spline.indicatorPosition = closestPos;
                spline.indicatorDirection = spline.GetDirection(time, true);
            }
            else if (segments.Count == 1)
            {
                Vector3 point = segments[0].GetPosition(Segment.ControlHandle.ANCHOR);
                EHandleSegment.GetIndicatorExtendedData(spline, point, point, mouseRay, out Vector3 extendedPos, out float extendedDistance, out float extendedTime);

                spline.indicatorDistanceToSpline = extendedDistance;
                spline.indicatorTime = extendedTime;
                spline.indicatorPosition = extendedPos;
                spline.indicatorDirection = segments[0].GetDirection();
                spline.indicatorSegement = Mathf.Approximately(extendedTime, 0) ? 0 : 1;
            }
        }

        private static void UpdateIndicator3D(Event e, ref EHandleSegment.SegmentIndicatorData segmentIndicatorData)
        {
            Transform editorCameraTransform = EHandleSceneView.GetCurrent().camera.transform;

            float yPos = editorCameraTransform.position.y - 50;
            Vector3 hitPoint = Vector3.zero;
            Vector3 direction = new Vector3(editorCameraTransform.forward.x, 0, editorCameraTransform.forward.z).normalized;
            Ray mouseRay = EMouseUtility.GetMouseRay(e.mousePosition);

            RaycastHit hit;
            if (Physics.Raycast(mouseRay, out hit))
                hitPoint = hit.point;
            else
            {
                projectionPlane.SetNormalAndPosition(Vector3.up, new Vector3(0, yPos, 0));
                if (projectionPlane.Raycast(mouseRay, out float enter))
                {
                    hitPoint = mouseRay.GetPoint(enter);
                }
            }

            float tangentDistance = EHandleSegment.DistanceModifier(hitPoint, 10);
            segmentIndicatorData.newAnchor = hitPoint;
            segmentIndicatorData.newTangentA = hitPoint - direction * tangentDistance;
            segmentIndicatorData.newTangentB = hitPoint + direction * tangentDistance;
        }

        private static void UpdateIndicator2D(Event e, ref EHandleSegment.SegmentIndicatorData segmentIndicatorData)
        {
            Ray mouseRay = EMouseUtility.GetMouseRay(e.mousePosition);
            Vector3 hitPoint = Vector3.zero;

            projectionPlane.SetNormalAndPosition(-Vector3.forward, new Vector3(0, 0, 0));
            if (projectionPlane.Raycast(mouseRay, out float enter))
            {
                hitPoint = mouseRay.GetPoint(enter);
            }

            float tangentDistance = EHandleSegment.DistanceModifier(hitPoint, 10);
            segmentIndicatorData.newAnchor = hitPoint;
            segmentIndicatorData.newTangentA = hitPoint - Vector3.right * tangentDistance;
            segmentIndicatorData.newTangentB = hitPoint + Vector3.right * tangentDistance;
        }

        private static void UpdateIndicator3DGrid(Event e, ref EHandleSegment.SegmentIndicatorData segmentIndicatorData)
        {
            Transform editorCameraTransform = EHandleSceneView.GetCurrent().camera.transform;
            Vector3 point = editorCameraTransform.position;
            float yPos = point.y - 50;
            Ray mouseRay = EMouseUtility.GetMouseRay(e.mousePosition);
            Vector3 hitPoint = Vector3.zero;

            RaycastHit hit;
            if (Physics.Raycast(mouseRay, out hit))
                hitPoint = hit.point;
            else
            {
                projectionPlane.SetNormalAndPosition(Vector3.up, new Vector3(0, yPos, 0));
                if (projectionPlane.Raycast(mouseRay, out float enter))
                {
                    hitPoint = mouseRay.GetPoint(enter);
                }
            }

            Vector3 editorCameraForward = EHandleSceneView.GetCurrent().camera.transform.forward;
            Vector3 direction = editorCameraForward;
            float closest = -1;

            float dot = Vector3.Dot(editorCameraForward, Vector3.forward);
            if (dot > closest)
            {
                closest = dot;
                direction = Vector3.forward;
            }

            dot = Vector3.Dot(editorCameraForward, Vector3.right);
            if (dot > closest)
            {
                closest = dot;
                direction = Vector3.right;
            }

            dot = Vector3.Dot(editorCameraForward, -Vector3.forward);
            if (dot > closest)
            {
                closest = dot;
                direction = -Vector3.forward;
            }

            dot = Vector3.Dot(editorCameraForward, -Vector3.right);
            if (dot > closest)
            {
                direction = -Vector3.right;
            }

            float tangentDistance = EHandleSegment.DistanceModifier(hitPoint, 10);

            if (tangentDistance < GlobalSettings.GetGridSize())
                tangentDistance = GlobalSettings.GetGridSize();

            segmentIndicatorData.newAnchor = hitPoint;
            segmentIndicatorData.newTangentA = hitPoint - direction * tangentDistance;
            segmentIndicatorData.newTangentB = hitPoint + direction * tangentDistance;
        }

        private static void UpdateIndicator2DGrid(Event e, ref EHandleSegment.SegmentIndicatorData segmentIndicatorData)
        {
            Ray mouseRay = EMouseUtility.GetMouseRay(e.mousePosition);
            Vector3 hitPoint = Vector3.zero;

            projectionPlane.SetNormalAndPosition(-Vector3.forward, new Vector3(0, 0, 0));
            if (projectionPlane.Raycast(mouseRay, out float enter))
            {
                hitPoint = mouseRay.GetPoint(enter);
            }

            float tangentDistance = EHandleSegment.DistanceModifier(hitPoint, 10);

            if (tangentDistance < GlobalSettings.GetGridSize())
                tangentDistance = GlobalSettings.GetGridSize();

            segmentIndicatorData.newAnchor = hitPoint;
            segmentIndicatorData.newTangentA = hitPoint - Vector3.right * tangentDistance;
            segmentIndicatorData.newTangentB = hitPoint + Vector3.right * tangentDistance;
        }

        private static void CreateSpline3DGrid(Event e)
        {
            //Create spline
            GameObject go = new GameObject();
            Spline spline = CreatedForContext(go);

            //Add indicator data
            if (segementIndicatorData.gridSpline != null)
            {
                go.transform.rotation = segementIndicatorData.gridSpline.transform.rotation;
                go.transform.position = segementIndicatorData.gridSpline.transform.position;
            }

            //Create segment data
            EHandleUndo.RecordNow(spline);
            Vector3 anchorPos = segementIndicatorData.newAnchor;
            Vector3 tangentAPos = segementIndicatorData.newTangentA;
            Vector3 tangentBPos = segementIndicatorData.newTangentB;
            spline.CreateSegment(0, anchorPos, tangentAPos, tangentBPos);

            //Set grid data
            spline.gridCenterPoint = spline.GetCenter(Space.Self);
            spline.segments[0].SetPosition(Segment.ControlHandle.ANCHOR, EHandleGrid.SnapPoint(spline, anchorPos));
            spline.segments[0].SetPosition(Segment.ControlHandle.TANGENT_A, EHandleGrid.SnapPoint(spline, tangentAPos));
            spline.segments[0].SetPosition(Segment.ControlHandle.TANGENT_B, EHandleGrid.SnapPoint(spline, tangentBPos));

            //Selection
            Selection.activeTransform = spline.transform;
            EHandleSelection.SelectPrimaryControlPoint(spline, SplineUtility.SegmentIndexToControlPointId(0, Segment.ControlHandle.ANCHOR));
        }

        private static void CreateSpline3DWorldPoint(Event e)
        {
            //Data
            Transform editorCameraTransform = EHandleSceneView.GetCurrent().camera.transform;
            Vector3 point = editorCameraTransform.position;
            Vector3 forward = editorCameraTransform.forward;
            float yPos = point.y - 50;
            Segment segment = GetClosestSegmentToDirection(HandleRegistry.GetSplines(), editorCameraTransform.forward, editorCameraTransform.position, out _, out _);
            if (segment != null) yPos = segment.GetPosition(Segment.ControlHandle.ANCHOR).y;

            //Get hit point
            Vector3 hitPoint = Vector3.zero;
            Ray mouseRay = EMouseUtility.GetMouseRay(e.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(mouseRay, out hit))
                hitPoint = hit.point;
            else
            {
                projectionPlane.SetNormalAndPosition(Vector3.up, new Vector3(0, yPos, 0));

                if (projectionPlane.Raycast(mouseRay, out float enter))
                    hitPoint = mouseRay.GetPoint(enter);
            }

            //Get closest splines forward
            if(EHandleModifier.CtrlActive(e))
            {
                Spline closest = SplineUtility.GetClosest(hitPoint, HandleRegistry.GetSplines(), 20 * EHandleSegment.DistanceModifier(hitPoint), out float time, out _);

                if(closest != null)
                {
                    closest.GetNormalsNonAlloc(normalsContainer, time);
                    forward = normalsContainer[2];
                }
            }

            //Calculate tangent direction
            Vector3 direction = new Vector3(forward.x, 0, forward.z);
            if (GeneralUtility.IsZero(direction, 0.01f)) direction = new Vector3(1, 0, 0);
            direction = direction.normalized;

            //Create spline
            Spline spline = CreatedForContext(new GameObject());

            //Create segemnt
            EHandleUndo.RecordNow(spline, "Created Spline");
            Vector3 anchorPos = hitPoint;
            Vector3 tangentAPos = hitPoint + direction * EHandleSegment.DistanceModifier(hitPoint) * 11;
            Vector3 tangentBPos = hitPoint - direction * EHandleSegment.DistanceModifier(hitPoint) * 11;
            spline.CreateSegment(0, anchorPos, tangentAPos, tangentBPos);

            //Set grid data
            spline.gridCenterPoint = spline.GetCenter(Space.Self);

            //Selection
            Selection.activeTransform = spline.transform;
            EHandleSelection.SelectPrimaryControlPoint(spline, SplineUtility.SegmentIndexToControlPointId(0, Segment.ControlHandle.ANCHOR));
        }

        private static void CreateSpline2D(Event e)
        {
            //Data
            projectionPlane.SetNormalAndPosition(-Vector3.forward, new Vector3(0, 0, 0));
            Ray mouseRay = EMouseUtility.GetMouseRay(e.mousePosition);
            Vector3 forward = Vector2.right;

            //Get hit point
            Vector3 hitPoint = Vector3.zero;
            if (projectionPlane.Raycast(mouseRay, out float enter))
                hitPoint = mouseRay.GetPoint(enter);

            //Get closest splines forward
            if (EHandleModifier.CtrlActive(e))
            {
                Spline closest = SplineUtility.GetClosest(hitPoint, HandleRegistry.GetSplines(), 20 * EHandleSegment.DistanceModifier(hitPoint), out float time, out _);

                if (closest != null)
                {
                    closest.GetNormalsNonAlloc(normalsContainer, time);
                    if(GeneralUtility.IsZero(normalsContainer[2].x)) forward = normalsContainer[2];
                }
            }

            //Calculate tangent direction
            Vector3 direction = new Vector3(forward.x, 0, forward.z);
            if (GeneralUtility.IsZero(direction, 0.01f)) direction = new Vector3(1, 0, 0);
            direction = direction.normalized;

            //Create spline
            Spline spline = CreatedForContext(new GameObject());

            //Create segemnt
            bool dontSnap = false;
            float tangentDistance = EHandleSegment.DistanceModifier(hitPoint, 10);
            if (tangentDistance < GlobalSettings.GetGridSize()) 
            {
                if (!GlobalSettings.GetGridVisibility()) dontSnap = true;
                else tangentDistance = GlobalSettings.GetGridSize();
            }
            EHandleUndo.RecordNow(spline, "Created Spline");
            Vector3 anchorPos = hitPoint;
            Vector3 tangentAPos = hitPoint + direction * tangentDistance;
            Vector3 tangentBPos = hitPoint - direction * tangentDistance;
            spline.CreateSegment(0, anchorPos, tangentAPos, tangentBPos);
            spline.normalType = Spline.NormalType.STATIC_2D;

            //Set grid data
            spline.gridCenterPoint = spline.GetCenter(Space.Self);
            spline.segments[0].SetPosition(Segment.ControlHandle.ANCHOR, dontSnap ? anchorPos : EHandleGrid.SnapPoint(spline, anchorPos));
            spline.segments[0].SetPosition(Segment.ControlHandle.TANGENT_A, dontSnap ? tangentAPos : EHandleGrid.SnapPoint(spline, tangentAPos));
            spline.segments[0].SetPosition(Segment.ControlHandle.TANGENT_B, dontSnap ? tangentBPos : EHandleGrid.SnapPoint(spline, tangentBPos));

            //Selection
            Selection.activeTransform = spline.transform;
            EHandleSelection.SelectPrimaryControlPoint(spline, SplineUtility.SegmentIndexToControlPointId(0, Segment.ControlHandle.ANCHOR));
        }

        public static Spline CreatedForContext(GameObject go)
        {
            go.name = $"Spline ({HandleRegistry.GetSplines().Count + 1})";
            PrefabStage prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null)
            {
                SceneManager.MoveGameObjectToScene(go, prefabStage.scene);
                EHandleUndo.RegisterCreatedObject(go, "Created Spline");
                Undo.SetTransformParent(go.transform, prefabStage.prefabContentsRoot.transform, "Created Spline");
            }
            else
            {
                EHandleUndo.RegisterCreatedObject(go, "Created Spline");
            }

            return EHandleUndo.AddComponent<Spline>(go);
        }

        public static void FlattenControlPoints(Spline spline, Segment specificSegement = null)
        {
            flattenContainer.Clear();
            Vector3 center = spline.GetCenter();

            if (specificSegement == null)
                flattenContainer.AddRange(spline.segments);
            else
            {
                flattenContainer.Add(specificSegement);
                if (spline.segments[0] == specificSegement && spline.loop)
                    flattenContainer.Add(spline.segments[spline.segments.Count - 1]);
            }                

            if(specificSegement != null && spline.normalType != Spline.NormalType.STATIC_2D)
            {
                Vector3 anchor = specificSegement.GetPosition(Segment.ControlHandle.ANCHOR);
                Vector3 tangentA = specificSegement.GetPosition(Segment.ControlHandle.TANGENT_A);
                Vector3 tangentB = specificSegement.GetPosition(Segment.ControlHandle.TANGENT_B);

                tangentA = new Vector3(tangentA.x, anchor.y, tangentA.z);
                tangentB = new Vector3(tangentB.x, anchor.y, tangentB.z);

                specificSegement.SetPosition(Segment.ControlHandle.TANGENT_A, tangentA);
                specificSegement.SetPosition(Segment.ControlHandle.TANGENT_B, tangentB);
            }
            else
            {
                foreach (Segment s in flattenContainer)
                {
                    Vector3 anchor = s.GetPosition(Segment.ControlHandle.ANCHOR);
                    Vector3 tangetA = s.GetPosition(Segment.ControlHandle.TANGENT_A);
                    Vector3 tangetB = s.GetPosition(Segment.ControlHandle.TANGENT_B);

                    if (spline.normalType == Spline.NormalType.STATIC_2D)
                    {
                        anchor = spline.transform.InverseTransformPoint(anchor);
                        anchor.z = spline.gridCenterPoint.z;
                        anchor = spline.transform.TransformPoint(anchor);
                        
                        tangetA = spline.transform.InverseTransformPoint(tangetA);
                        tangetA.z = spline.gridCenterPoint.z;
                        tangetA = spline.transform.TransformPoint(tangetA);

                        tangetB = spline.transform.InverseTransformPoint(tangetB);
                        tangetB.z = spline.gridCenterPoint.z;
                        tangetB = spline.transform.TransformPoint(tangetB);
                    }
                    else
                    {
                        anchor.y = center.y;
                        tangetA.y = center.y;
                        tangetB.y = center.y;
                    }

                    s.SetPosition(Segment.ControlHandle.ANCHOR, anchor);
                    s.SetPosition(Segment.ControlHandle.TANGENT_A, tangetA);
                    s.SetPosition(Segment.ControlHandle.TANGENT_B, tangetB);
                }
            }
        }

        public static void EnableDisableLoop(Spline spline, bool enable)
        {
            if (EHandleUndo.UndoTriggered())
                return;

            if (enable)
            {
                spline.CreateSegment(spline.segments.Count,
                                 spline.segments[0].GetPosition(Segment.ControlHandle.ANCHOR),
                                 spline.segments[0].GetPosition(Segment.ControlHandle.TANGENT_A),
                                 spline.segments[0].GetPosition(Segment.ControlHandle.TANGENT_B));
            }
            else
            {
                EHandleSegment.MarkForDeletion(spline.segments.Count - 1);
                EHandleSegment.DeleteAndUnlinkMarked(spline, false);
            }
        }

        public static Spline Split(Spline spline, int segmentIndex)
        {
            GameObject splineGo = new GameObject(spline.name + "(split)");
            EHandleUndo.RegisterCreatedObject(splineGo, "Splited Spline");
            Spline newSpline = EHandleUndo.AddComponent<Spline>(splineGo);

            EHandleUndo.RecordNow(newSpline);
            EHandleUndo.RecordNow(spline);

            for (int i = segmentIndex; i < spline.segments.Count; i++)
            {
                Segment s = spline.segments[i];

                if(i == segmentIndex)
                    newSpline.CreateSegment(0, s.GetPosition(Segment.ControlHandle.ANCHOR), s.GetPosition(Segment.ControlHandle.TANGENT_A), s.GetPosition(Segment.ControlHandle.TANGENT_B));
                else
                {
                    s.SetSplineParent(newSpline);
                    newSpline.segments.Add(s);
                }
            }

            for (int i = spline.segments.Count - 1; i > segmentIndex; i--)
                spline.segments.RemoveAt(i);

            if(spline.transform.parent != null)
                EHandleUndo.SetTransformParent(splineGo.transform, spline.transform.parent);

            return newSpline;
        }

        public static void JoinSelection()
        {
            Spline selected = EHandleSelection.selectedSpline;
            List<Spline> secondarySelection = new List<Spline>();
            secondarySelection.AddRange(EHandleSelection.selectedSplines);

            //Needs to be: RegisterCompleteObjectUndo. Else Links will disappear during Undo. Seems to not be needed on closestAc at the bottom of this function.
            EHandleUndo.RecordNow(selected, "Join selected Splines", EHandleUndo.RecordType.REGISTER_COMPLETE_OBJECT);

            int iterations = EHandleSelection.selectedSplines.Count;

            while (iterations > 0)
            {
                iterations--;

                Vector3 primaryStart = selected.segments[0].GetPosition(Segment.ControlHandle.ANCHOR);
                Vector3 primaryEnd = selected.segments[selected.segments.Count - 1].GetPosition(Segment.ControlHandle.ANCHOR);

                JoinType joinType = JoinType.END_TO_START;
                float distanceCheck = 999999;
                Spline closestSpline = null;

                for (int i = secondarySelection.Count - 1; i >= 0; i--)
                {
                    Spline spline = secondarySelection[i];

                    Vector3 secondaryStart = spline.segments[0].GetPosition(Segment.ControlHandle.ANCHOR);
                    Vector3 secondaryEnd = spline.segments[spline.segments.Count - 1].GetPosition(Segment.ControlHandle.ANCHOR);

                    float distanceToPoint = Vector3.Distance(primaryStart, secondaryStart);
                    if (distanceToPoint < distanceCheck)
                    {
                        joinType = JoinType.START_TO_START;
                        closestSpline = spline;
                        distanceCheck = distanceToPoint;
                    }

                    distanceToPoint = Vector3.Distance(primaryStart, secondaryEnd);
                    if (distanceToPoint < distanceCheck)
                    {
                        joinType = JoinType.START_TO_END;
                        closestSpline = spline;
                        distanceCheck = distanceToPoint;
                    }

                    distanceToPoint = Vector3.Distance(primaryEnd, secondaryStart);
                    if (distanceToPoint < distanceCheck)
                    {
                        joinType = JoinType.END_TO_START;
                        closestSpline = spline;
                        distanceCheck = distanceToPoint;
                    }

                    distanceToPoint = Vector3.Distance(primaryEnd, secondaryEnd);
                    if (distanceToPoint < distanceCheck)
                    {
                        joinType = JoinType.END_TO_END;
                        closestSpline = spline;
                        distanceCheck = distanceToPoint;
                    }
                }

                secondarySelection.Remove(closestSpline);
                EHandleUndo.RecordNow(closestSpline);
                selected.Join(closestSpline, joinType);
            }

            //Dont know why I can't do this within the while loop above but it does not work.
            foreach (Spline spline in EHandleSelection.selectedSplines)
            {
                EHandleUndo.DestroyObjectImmediate(spline.gameObject);
            }
        }

        public static void MarkForInfoUpdate(Spline spline)
        {
            if (markedInfoUpdates.Contains(spline))
                return;

            markedInfoUpdates.Add(spline);
        }

        public static void ProcessMarkedForInfoUpdates()
        {
            markedInfoUpdates.AddRange(EHandleEvents.GetMarkedForInfoUpdates());
            EHandleEvents.ClearMarkedForInfoUpdates();

            if (markedInfoUpdates.Count == 0)
                return;

            for (int i2 = markedInfoUpdates.Count - 1; i2 >= 0; i2--)
            {
                Spline spline = markedInfoUpdates[i2];

                spline.vertecies = 0;
                spline.deformations = 0;
                spline.deformationsInBuild = 0;
                spline.followers = 0;
                spline.followersInBuild = 0;

                for (int i = spline.splineObjects.Count - 1; i >= 0; i--)
                {
                    SplineObject so = spline.splineObjects[i];

                    if (so == null) 
                        continue;

                    EHandleSplineObject.UpdateInfo(so);

                    if (so.type == SplineObject.Type.NONE)
                        continue;

                    bool check = spline.componentMode == ComponentMode.ACTIVE && so.componentMode != ComponentMode.REMOVE_FROM_BUILD;

                    if (so.type == SplineObject.Type.FOLLOWER)
                    {
                        spline.followers++;

                        if (spline.componentMode == ComponentMode.INACTIVE || check)
                            spline.followersInBuild++;
                    }
                    else
                    {
                        spline.deformations += so.deformations;
                        spline.vertecies += so.deformedVertecies;

                        if (spline.componentMode == ComponentMode.INACTIVE || check)
                            spline.deformationsInBuild += so.deformations;
                    }
                }

                UpdateDeformedMeshMemoryUsage(spline);
            }

            markedInfoUpdates.Clear();
        }

        public static void UpdateDeformedMeshMemoryUsage(Spline spline)
        {
            float size = 0;

            if (spline.deformationMode != DeformationMode.DO_NOTHING)
            {
                foreach (SplineObject so in spline.splineObjects)
                {
                    if (so.type != SplineObject.Type.DEFORMATION)
                        continue;

                    foreach (MeshContainer mc in so.meshContainers)
                    {
                        Component meshContainerComponent = mc.GetMeshContainerComponent();

                        if (meshContainerComponent == null || meshContainerComponent.transform == null)
                            continue;

                        //Skip mc:s that uses the same mesh as so.meshContainers[0].
                        if (mc != so.meshContainers[0] && mc.GetInstanceMesh() == so.meshContainers[0].GetInstanceMesh())
                            continue;

                        size += mc.GetDataUsage();
                    }
                }
            }

            spline.deformationsMemoryUsage = Mathf.Round(size);
        }

        public static string GetMemorySizeFormat(float size)
        {
            string sizeFormat = "bytes";

            if (size > 999)
            {
                size = size / 1024;
                sizeFormat = "KB";
            }
            if (size > 999)
            {
                size = size / 1024;
                sizeFormat = "MB";
            }

            return Mathf.Round(size) + " " +  sizeFormat;
        }

        public static float GetSplineMemoryUsage(Spline spline)
        {
            float size = 0;

            if (spline.componentMode == ComponentMode.REMOVE_FROM_BUILD)
                return size;

            size += spline.segments.Count * Segment.DataUsage;

            if (spline.componentMode == ComponentMode.INACTIVE)
                return size;

            if (spline.distanceMap.IsCreated)
                size += 4 * spline.distanceMap.Length;

            if (spline.positionMapLocal.IsCreated)
                size += 12 * spline.positionMapLocal.Length;

            if (spline.normalType == Spline.NormalType.DYNAMIC && spline.normalsLocal.IsCreated)
                size += 12 * spline.normalsLocal.Length;

            return size;
        }

        public static float GetComponentMemoryUsage(Spline spline)
        {
            float size = 0;

            if (spline.componentMode == ComponentMode.REMOVE_FROM_BUILD)
                return size;

            //Segment data usage
            size = Spline.dataUsage;

            size += (spline.followersInBuild + spline.deformationsInBuild) * SplineObject.dataUsage;
            size += (spline.followersInBuild + spline.deformationsInBuild) * MeshContainer.dataUsage;

            return size;
        }

        public static float GetIndicatorActivationDistance(Spline spline)
        {
            float size = HandleUtility.GetHandleSize(spline.indicatorPosition) * 0.4f;
            size = Mathf.Clamp(size, 0, 4);
            return size;
        }

        public static int GetNextControlPoint(Spline spline, bool backwards = false)
        {
            //General
            spline.selectedAnchors.Clear();
            int cp = spline.selectedControlPoint;
            Segment oldSegment = spline.segments[SplineUtility.ControlPointIdToSegmentIndex(cp)];
            Segment.ControlHandle controlHandle = SplineUtility.GetControlPointType(cp);

            //Go to next control point
            if (controlHandle == Segment.ControlHandle.ANCHOR)
                cp = backwards ? cp + 2: cp + 1;
            else if (controlHandle == Segment.ControlHandle.TANGENT_B)
                cp = backwards ? cp - 4 : cp - 2;
            else if(controlHandle == Segment.ControlHandle.TANGENT_A)
                cp = backwards ? cp - 1 : cp + 4;

            bool outOfRange = RangeCheck(SplineUtility.ControlPointIdToSegmentIndex(cp));

            //If line go to next anchor
            Segment newSegment = spline.segments[SplineUtility.ControlPointIdToSegmentIndex(cp)];
            if (!outOfRange && newSegment.GetInterpolationType() == Segment.InterpolationType.LINE)
            {
                int controlPointAnchorId = SplineUtility.SegmentIndexToControlPointId(SplineUtility.ControlPointIdToSegmentIndex(cp), Segment.ControlHandle.ANCHOR);
                if (oldSegment == newSegment)
                {
                    if(backwards) cp = controlPointAnchorId - 3;
                    else cp = controlPointAnchorId + 3;
                }

                outOfRange = RangeCheck(SplineUtility.ControlPointIdToSegmentIndex(cp));

                //If new segment is spline go back to first tangent. We dont want to jump to the anchor directly.
                newSegment = spline.segments[SplineUtility.ControlPointIdToSegmentIndex(cp)];
                if (!outOfRange && newSegment.GetInterpolationType() != Segment.InterpolationType.LINE)
                {
                    if (backwards) cp += 1;
                    else cp += 2;
                }
            }

            RangeCheck(SplineUtility.ControlPointIdToSegmentIndex(cp));

            //Make sure to go to anchor if line
            if (newSegment.GetInterpolationType() == Segment.InterpolationType.LINE)
            {
                int segmentId = SplineUtility.ControlPointIdToSegmentIndex(cp);
                cp = SplineUtility.SegmentIndexToControlPointId(segmentId, Segment.ControlHandle.ANCHOR);
            }

            return cp;

            bool RangeCheck(int segmentId)
            {
                if (spline.segments.Count == segmentId || (spline.loop && spline.segments.Count - 1 == segmentId))
                {
                    cp = 1002;
                    return true;
                }
                if (cp < 1000)
                {
                    cp = spline.loop ? spline.segments.Count * 3 + 995 : spline.segments.Count * 3 + 998;
                    return true;
                }

                return false;
            }
        }

        public static List<int> GetIntersectingControlPoints(Spline spline, Ray mouseRay)
        {
            intersectingControlPoints.Clear();

            int iterations = spline.segments.Count;

            if (spline.loop)
                iterations--;

            for (int i = 0; i < iterations; i++)
            {
                Vector3 point = spline.segments[i].GetPosition(Segment.ControlHandle.ANCHOR);
#if UNITY_EDITOR_OSX
                float distanceCheck = EHandleSegment.GetControlPointSize(point) * 2.5f;
#else
                float distanceCheck = EHandleSegment.GetControlPointSize(point) * 1.9f;
#endif
                float v = EMouseUtility.MouseDistanceToPoint(point, mouseRay);
                if (v < distanceCheck)
                {
                    intersectingControlPoints.Add(SplineUtility.SegmentIndexToControlPointId(i, Segment.ControlHandle.ANCHOR));
                }

                point = spline.segments[i].GetPosition(Segment.ControlHandle.TANGENT_A);
#if UNITY_EDITOR_OSX
                distanceCheck = EHandleSegment.GetControlPointSize(point) * 2f;
#else
                distanceCheck = EHandleSegment.GetControlPointSize(point) * 1.5f;
#endif
                v = EMouseUtility.MouseDistanceToPoint(point, mouseRay);
                if (v < distanceCheck)
                {
                    intersectingControlPoints.Add(SplineUtility.SegmentIndexToControlPointId(i, Segment.ControlHandle.TANGENT_A));
                }

                point = spline.segments[i].GetPosition(Segment.ControlHandle.TANGENT_B);
#if UNITY_EDITOR_OSX
                distanceCheck = EHandleSegment.GetControlPointSize(point) * 2f;
#else
                distanceCheck = EHandleSegment.GetControlPointSize(point) * 1.5f;
#endif
                v = EMouseUtility.MouseDistanceToPoint(spline.segments[i].GetPosition(Segment.ControlHandle.TANGENT_B), mouseRay);
                if (v < distanceCheck)
                {
                    intersectingControlPoints.Add(SplineUtility.SegmentIndexToControlPointId(i, Segment.ControlHandle.TANGENT_B));
                }
            }

            return intersectingControlPoints;
        }

        public static Segment GetClosestSegment(HashSet<Spline> splines, Vector3 point, out float distance, out Spline spline, Segment segmentToSkip = null)
        {
            distance = 999999;
            Segment segment = null;
            spline = null;

            foreach (Spline spline2 in splines)
            {
                Vector3 closestPoint = spline2.bounds.ClosestPoint(point);
                float distanceToBounds = Vector3.Distance(closestPoint, point);

                if (distanceToBounds > 15)
                    continue;

                foreach(Segment s in spline2.segments)
                {
                    if(spline2.loop && spline2.segments[spline2.segments.Count - 1] == s)
                        continue;

                    if (segmentToSkip == s)
                        continue;

                    float d = Vector3.Distance(s.GetPosition(Segment.ControlHandle.ANCHOR), point);

                    if (d < distance)
                    {
                        spline = spline2;
                        segment = s;
                        distance = d;
                    }
                }
            }

            return segment;
        }

        public static Segment GetClosestSegmentToDirection(HashSet<Spline> splines, Vector3 direction, Vector3 origin, out float distance, out Spline spline, float maxDistance = 125)
        {
            distance = 999999;
            Segment segment = null;
            spline = null;

            foreach (Spline spline2 in splines)
            {
                float distanceToBounds = Vector3.Distance(spline2.transform.position, origin);

                if (distanceToBounds > maxDistance)
                    continue;

                foreach (Segment s in spline2.segments)
                {
                    if (spline2 == EHandleSelection.selectedSpline && spline2.selectedControlPoint != 0)
                    {
                        //Skip self
                        if (s == spline2.segments[SplineUtility.ControlPointIdToSegmentIndex(spline2.selectedControlPoint)])
                            continue;
                    }

                    Vector3 anchor = s.GetPosition(Segment.ControlHandle.ANCHOR);
                    Vector3 point = Utility.LineUtility.GetNearestPoint(origin, direction, anchor, out _);
                    float d = Vector3.Distance(anchor, point);

                    if (d < distance)
                    {
                        spline = spline2;
                        segment = s;
                        distance = d;
                    }
                }
            }

            return segment;
        }

        public static Vector3 ClosestMousePointStepByStep(Spline spline, Ray mouseRay, float steps, out float distance, out float time)
        {
            time = 0;
            distance = 999999;
            float dCheck = 999999;
            Vector3 position = Vector3.zero;
            for (float t = 0; t < 1; t += steps)
            {
                Vector3 point = spline.GetPosition(t);
                float d2 = EMouseUtility.MouseDistanceToPoint(point, mouseRay);

                if (d2 < dCheck)
                {
                    dCheck = d2;
                    distance = d2;
                    time = t;
                    position = point;
                }
            }

            return position;
        }

        public static Vector3 ClosestMousePoint(Spline spline, Ray mouseRay, int precision, out float distance, out float time, float steps = 5)
        {
            steps = 100 / spline.length / steps;
            if (steps > 0.1f) steps = 0.1f;
            if (steps < 0.0001f) steps = 0.0001f;

            Vector3 position = ClosestMousePointStepByStep(spline, mouseRay, steps, out distance, out time);

            for (int i = precision; i > 0; i--)
            {
                steps = steps / 1.6f;
                float timeForwards = time + steps;
                float timeBackwards = time - steps;
                timeForwards = SplineUtility.GetValidatedTime(timeForwards, spline.loop);
                timeBackwards = SplineUtility.GetValidatedTime(timeBackwards, spline.loop);

                if (!spline.loop)
                {
                    if (timeForwards > 1) timeForwards = 1;
                    if (timeBackwards < 0) timeBackwards = 0;
                }

                Vector3 pForward = spline.GetPosition(timeForwards);
                float dForward = EMouseUtility.MouseDistanceToPoint(pForward, mouseRay);

                Vector3 pBackwards = spline.GetPosition(timeBackwards);
                float dBackwards = EMouseUtility.MouseDistanceToPoint(pBackwards, mouseRay);

                if (dForward > dBackwards)
                {
                    position = pBackwards;
                    time = timeBackwards;
                    distance = dBackwards;
                }
                else
                {
                    position = pForward;
                    time = timeForwards;
                    distance = dForward;
                }
            }

            return position;
        }

        public static void AlignSelectedSegments(Spline spline)
        {
            int selectedSegment = SplineUtility.ControlPointIdToSegmentIndex(spline.selectedControlPoint);

            int startSegment = selectedSegment;
            int endSegment = selectedSegment;

            for (int i = 0; i < spline.selectedAnchors.Count; i++)
            {
                int index = SplineUtility.ControlPointIdToSegmentIndex(spline.selectedAnchors[i]);

                if (index < startSegment)
                    startSegment = index;

                if(index > endSegment)
                    endSegment = index;
            }

            Vector3 anchorStart = spline.segments[startSegment].GetPosition(Segment.ControlHandle.ANCHOR);
            Vector3 tangentAStart = spline.segments[startSegment].GetPosition(Segment.ControlHandle.TANGENT_A);
            Vector3 tangentBStart = spline.segments[startSegment].GetPosition(Segment.ControlHandle.TANGENT_B);
            float tangentAStartLength = Vector3.Distance(anchorStart, tangentAStart);
            float tangentBStartLength = Vector3.Distance(anchorStart, tangentBStart);

            Vector3 anchorEnd = spline.segments[endSegment].GetPosition(Segment.ControlHandle.ANCHOR);
            Vector3 tangentAEnd = spline.segments[endSegment].GetPosition(Segment.ControlHandle.TANGENT_A);
            Vector3 tangentBEnd = spline.segments[endSegment].GetPosition(Segment.ControlHandle.TANGENT_B);
            float tangentAEndLength = Vector3.Distance(anchorEnd, tangentAEnd);
            float tangentBEndLength = Vector3.Distance(anchorEnd, tangentBEnd);

            Vector3 direction = (anchorEnd - anchorStart).normalized;

            spline.segments[startSegment].SetPosition(Segment.ControlHandle.TANGENT_A, anchorStart + direction * tangentAStartLength);
            spline.segments[startSegment].SetPosition(Segment.ControlHandle.TANGENT_B, anchorStart - direction * tangentBStartLength);

            spline.segments[endSegment].SetPosition(Segment.ControlHandle.TANGENT_A, anchorEnd + direction * tangentAEndLength);
            spline.segments[endSegment].SetPosition(Segment.ControlHandle.TANGENT_B, anchorEnd - direction * tangentBEndLength);

            if(selectedSegment != startSegment && selectedSegment != endSegment)
                EHandleSegment.MarkForDeletion(selectedSegment);

            foreach (int id in spline.selectedAnchors)
            {
                int segmentId = SplineUtility.ControlPointIdToSegmentIndex(id);

                if (segmentId == startSegment || segmentId == endSegment)
                    continue;

                EHandleSegment.MarkForDeletion(segmentId);
            }

            EHandleSegment.DeleteAndUnlinkMarked(spline, true);
        }

        private static int GetHotControlId()
        {
            if (hotControlId == 0) hotControlId = GUIUtility.GetControlID(FocusType.Passive);
            return hotControlId;
        }
    }
}