// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: PositionTool.cs
//
// Author: Mikael Danielsson
// Date Created: 24-09-2023
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using SplineArchitect.Objects;
using SplineArchitect.Utility;
using System;
using Unity.Burst.CompilerServices;
using UnityEditor;
using UnityEngine;

namespace SplineArchitect.CustomTools
{
    public class PositionTool
    {
        public enum ActivationType
        {
            ANCHOR,
            TANGENT,
            SPLINE_OBJECT
        }

        public enum Part : byte
        {
            NONE,
            X_AXEL,
            Y_AXEL,
            Z_AXEL,
            XZ_AXEL,
            XY_AXEL,
            YZ_AXEL,
            SURFACE
        }

        const float surfaceDotSize = 1.33f;
        const float positionToolSize = 1;
        const float pressSize = 0.166f;

        public static int hotControlId;
        public static bool locked;
        public static string lockedWarningMsg = "[Spline Architect] Position handle tool is locked!";
        public static bool active { get; private set; }
        public static bool released { get; private set; }
        public static ActivationType activationType { get; private set; }
        public static Part activePart { get; private set; }

        private static bool isSurfaceDotHovered;
        private static bool oldIsSurfaceDotHovered;
        private static bool supportGlobal;
        private static Vector3 deltaPressPosition;
        private static Vector3 handlePosition;
        private static Vector3 xDirection;
        private static Vector3 yDirection;
        private static Vector3 zDirection;
        private static bool xModifier = false;
        private static bool zModifier = false;
        private static bool yModifier = false;
        private static Plane projectionPlane = new Plane(Vector3.up, Vector3.zero);

        private static Vector3 activationPosition;
        private static Vector3 lastRecordedDif;

        private static Vector3[] xRectangel = new Vector3[4];
        private static Vector3[] yRectangel = new Vector3[4];
        private static Vector3[] zRectangel = new Vector3[4];

        private static RaycastHit[] groundHits = new RaycastHit[16];
        private static RaycastHit[] groundHitsCompere = new RaycastHit[16];

        public static void Activate()
        {
            active = true;
        }

        public static void Deactivate()
        {
            active = false;
        }

        public static void ActivateAndSetPosition(ActivationType type, Vector3 newPosition, bool _supportGlobal = true)
        {
            handlePosition = newPosition;
            activationPosition = newPosition;
            activationType = type;
            active = true;
            locked = false;
            lockedWarningMsg = "[Spline Architect] Position handle is locked!";

            supportGlobal = _supportGlobal;
        }

        public static void ActivateAndSetPosition(ActivationType type, Vector3 newPosition, Vector3 cameraPosition, Vector3 newForwardDirection, Vector3 newUpDirection, bool _supportGlobal = true)
        {
            ActivateAndSetPosition(type, newPosition, _supportGlobal);
            UpdateOrientation(cameraPosition, newForwardDirection, newUpDirection);
        }

        public static void UpdateHoveredData(Event e, Ray mouseRay)
        {
            isSurfaceDotHovered = false;

            if (!active)
                return;

            if (activePart == Part.SURFACE)
                isSurfaceDotHovered = true;

            if ((activationType == ActivationType.ANCHOR || activationType == ActivationType.TANGENT) && e.control && e.shift)
            {
                float distance = EMouseUtility.MouseDistanceToPoint(handlePosition, mouseRay);
                if (distance < EHandleSegment.GetControlPointSize(handlePosition) * surfaceDotSize * 1.8f)
                {
                    isSurfaceDotHovered = true;
                }
            }

            if (oldIsSurfaceDotHovered != isSurfaceDotHovered)
            {
                oldIsSurfaceDotHovered = isSurfaceDotHovered;
                EHandleSceneView.RepaintCurrent();
            }
        }

        public static bool Press(Event e, Ray mouseRay)
        {
            if (!active)
                return false;

            //Surface circle press
            if((activationType == ActivationType.ANCHOR || activationType == ActivationType.TANGENT) && EHandleModifier.CtrlShiftActive(e))
            {
                if(isSurfaceDotHovered)
                {
                    deltaPressPosition = Vector3.zero;
                    activePart = Part.SURFACE;
                    GUIUtility.hotControl = GetHotControlId();
                    return true;
                }

                return false;
            }

            //Default press
            Vector3 xPosition = handlePosition + xDirection * HandleUtility.GetHandleSize(handlePosition);
            Vector3 yPosition = handlePosition + yDirection * HandleUtility.GetHandleSize(handlePosition);
            Vector3 zPosition = handlePosition + zDirection * HandleUtility.GetHandleSize(handlePosition);

            Vector3 xClosest = Utility.LineUtility.GetNearestPointOnLineFromLine(handlePosition, xDirection, mouseRay.origin, mouseRay.direction);
            Vector3 yClosest = Utility.LineUtility.GetNearestPointOnLineFromLine(handlePosition, yDirection, mouseRay.origin, mouseRay.direction);
            Vector3 zClosest = Utility.LineUtility.GetNearestPointOnLineFromLine(handlePosition, zDirection, mouseRay.origin, mouseRay.direction);

            Vector3? tri1 = EMeshUtility.RayIntersectedWithTriangle(mouseRay.direction, mouseRay.origin, xRectangel[2], xRectangel[3], xRectangel[1]);
            Vector3? tri2 = EMeshUtility.RayIntersectedWithTriangle(mouseRay.direction, mouseRay.origin, xRectangel[0], xRectangel[3], xRectangel[1]);

            SceneView sceneView = EHandleSceneView.GetCurrent();
            Camera editorCamera = EHandleSceneView.GetCamera();
            Vector3Int disable = Vector3Int.zero;
            if (sceneView != null && editorCamera != null)
            {
                disable.x = Vector3.Dot(xDirection, editorCamera.transform.forward) > 0.98f ? 1 : 0;
                disable.y = Vector3.Dot(yDirection, editorCamera.transform.forward) > 0.98f ? 1 : 0;
                disable.z = Vector3.Dot(zDirection, editorCamera.transform.forward) > 0.98f ? 1 : 0;
            }

            activePart = Part.NONE;
            active = false;

            if (disable.y == 0  && disable.z == 0 && (tri1 != null || tri2 != null))
            {
                activePart = Part.YZ_AXEL;
                active = true;
                deltaPressPosition = (Vector3)(tri1 == null ? handlePosition - tri2 : handlePosition - tri1);
                GUIUtility.hotControl = GetHotControlId();
                return true;
            }

            tri1 = EMeshUtility.RayIntersectedWithTriangle(mouseRay.direction, mouseRay.origin, zRectangel[2], zRectangel[1], zRectangel[3]);
            tri2 = EMeshUtility.RayIntersectedWithTriangle(mouseRay.direction, mouseRay.origin, zRectangel[0], zRectangel[1], zRectangel[3]);

            if (disable.x == 0 && disable.y == 0 && (tri1 != null || tri2 != null))
            {
                activePart = Part.XY_AXEL;
                active = true;
                deltaPressPosition = (Vector3)(tri1 == null ? handlePosition - tri2 : handlePosition - tri1);
                GUIUtility.hotControl = GetHotControlId();
                return true;
            }

            tri1 = EMeshUtility.RayIntersectedWithTriangle(mouseRay.direction, mouseRay.origin, yRectangel[2], yRectangel[1], yRectangel[3]);
            tri2 = EMeshUtility.RayIntersectedWithTriangle(mouseRay.direction, mouseRay.origin, yRectangel[0], yRectangel[1], yRectangel[3]);

            if (disable.x == 0 && disable.z == 0 && (tri1 != null || tri2 != null))
            {
                activePart = Part.XZ_AXEL;
                active = true;
                deltaPressPosition = (Vector3)(tri1 == null ? handlePosition - tri2 : handlePosition - tri1);
                GUIUtility.hotControl = GetHotControlId();
                return true;
            }

            //X
            float distanceCheck = EMouseUtility.MouseDistanceToPoint(xPosition, mouseRay);
            if (disable.x == 0 && distanceCheck < pressSize * HandleUtility.GetHandleSize(xPosition))
            {
                deltaPressPosition = handlePosition - xClosest;
                activePart = Part.X_AXEL;
                active = true;
                GUIUtility.hotControl = GetHotControlId();
            }

            //Y
            distanceCheck = EMouseUtility.MouseDistanceToPoint(yPosition, mouseRay);
            if (disable.y == 0 && distanceCheck < pressSize * HandleUtility.GetHandleSize(yPosition))
            {
                deltaPressPosition = handlePosition - yClosest;
                activePart = Part.Y_AXEL;
                active = true;
                GUIUtility.hotControl = GetHotControlId();
            }

            //Z
            distanceCheck = EMouseUtility.MouseDistanceToPoint(zPosition, mouseRay);
            if (disable.z == 0 && distanceCheck < pressSize * HandleUtility.GetHandleSize(zPosition))
            {
                deltaPressPosition = handlePosition - zClosest;
                activePart = Part.Z_AXEL;
                active = true;
                GUIUtility.hotControl = GetHotControlId();
            }

            if (active) return true;

            return false;
        }

        public static bool DragUsingSurface(out Vector3 newPosition)
        {
            newPosition = handlePosition;

            if (!active)
                return false;

            if (activePart == Part.NONE)
                return false;

            if (Event.current.type == EventType.MouseUp || !EHandleSceneView.mouseInsideSceneView)
            {
                UpdateReleased(true);

                //Set active part to none
                activePart = Part.NONE;
                return false;
            }

            if (Event.current.type != EventType.Repaint && Event.current.type != EventType.Layout)
                Event.current.Use();

            if (locked)
            {
                Debug.LogWarning(lockedWarningMsg);
                return false;
            }

            Ray mouseRay = EMouseUtility.GetMouseRay(Event.current.mousePosition);

            Vector3 oldPosition = newPosition;

            int hitCount = Physics.RaycastNonAlloc(mouseRay, groundHits);

            for (int i = 0; i < hitCount; i++)
                groundHitsCompere[i] = groundHits[i];

            for (int i = 0; i < hitCount; i++)
            {
                int index = 0;

                for (int i2 = 0; i2 < hitCount; i2++)
                {
                    if (i2 == i)
                        continue;

                    if (groundHitsCompere[i].distance > groundHitsCompere[i2].distance) 
                        index++;
                }

                groundHits[index] = groundHitsCompere[i];
            }

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = groundHits[i];

                if (hit.transform == null)
                    continue;

                if (hit.distance > 25000)
                    continue;

                SplineObject so = hit.transform.GetComponent<SplineObject>();
                Spline selectedSpline = EHandleSelection.selectedSpline;

                if (so != null && selectedSpline != null && so.splineParent == selectedSpline)
                    continue;

                newPosition = hit.point;
                break;
            }

            if (GeneralUtility.IsEqual(oldPosition, newPosition))
                return false;

            handlePosition = newPosition;

            return true;
        }

        public static bool DragUsingPos(out Vector3 newPosition)
        {
            newPosition = handlePosition;

            if (!active)
                return false;

            if (activePart == Part.NONE)
                return false;

            //Cant use up the MouseUp event. If its used up and the user presses the right mouse button directly after, MouseDrag will be started and we dont want this (dont know why unity does this).
            if (Event.current.type == EventType.MouseUp || !EHandleSceneView.mouseInsideSceneView)
            {
                UpdateReleased(true);

                //Set active part to none
                activePart = Part.NONE;

                EHandleSceneView.RepaintCurrent();
                return false;
            }

            if (Event.current.type != EventType.Repaint && Event.current.type != EventType.Layout)
                Event.current.Use();

            if (locked && (activePart != Part.Z_AXEL || activationType != ActivationType.TANGENT || Tools.pivotRotation != PivotRotation.Local))
            {
                Debug.LogWarning(lockedWarningMsg);
                return false;
            }

            Vector3 oldOriginPosition = newPosition;
            Ray mouseRay = EMouseUtility.GetMouseRay(Event.current.mousePosition);
            Vector3 x = Utility.LineUtility.GetNearestPointOnLineFromLine(newPosition, xDirection, mouseRay.origin, mouseRay.direction) + deltaPressPosition;
            Vector3 y = Utility.LineUtility.GetNearestPointOnLineFromLine(newPosition, yDirection, mouseRay.origin, mouseRay.direction) + deltaPressPosition;
            Vector3 z = Utility.LineUtility.GetNearestPointOnLineFromLine(newPosition, zDirection, mouseRay.origin, mouseRay.direction) + deltaPressPosition;

            if (activePart == Part.X_AXEL)
                newPosition = x;
            else if (activePart == Part.Y_AXEL)
                newPosition = y;

            else if (activePart == Part.Z_AXEL)
                newPosition = z;
            else if (activePart == Part.YZ_AXEL)
            {
                projectionPlane.SetNormalAndPosition(xDirection, newPosition - deltaPressPosition);
                if (projectionPlane.Raycast(mouseRay, out float enter))
                    newPosition = mouseRay.GetPoint(enter) + deltaPressPosition;
            }
            else if (activePart == Part.XY_AXEL)
            {
                projectionPlane.SetNormalAndPosition(zDirection, newPosition - deltaPressPosition);
                if (projectionPlane.Raycast(mouseRay, out float enter))
                    newPosition = mouseRay.GetPoint(enter) + deltaPressPosition;
            }
            else if (activePart == Part.XZ_AXEL)
            {
                projectionPlane.SetNormalAndPosition(yDirection, newPosition - deltaPressPosition);
                if (projectionPlane.Raycast(mouseRay, out float enter))
                    newPosition = mouseRay.GetPoint(enter) + deltaPressPosition;
            }

            if (GeneralUtility.IsEqual(oldOriginPosition, newPosition))
                return false;

            if (Vector3.Distance(oldOriginPosition, newPosition) > 5000)
                newPosition = oldOriginPosition;

            handlePosition = newPosition;

            return true;
        }

        public static bool DragUsingDif(out Vector3 dif)
        {
            dif = Vector3.zero;

            if (!active)
                return false;

            if (activePart == Part.NONE)
                return false;

            //Cant use up the MouseUp event. If its used up and the user presses the right mouse button directly after, MouseDrag will be started and we dont want this (dont know why unity does this).
            if (Event.current.type == EventType.MouseUp || !EHandleSceneView.mouseInsideSceneView)
            {
                UpdateReleased(true);

                //Set active part to none
                activePart = Part.NONE;

                EHandleSceneView.RepaintCurrent();
                return false;
            }

            if (Event.current.type != EventType.Repaint && Event.current.type != EventType.Layout)
                Event.current.Use();

            if (locked && (activePart != Part.Z_AXEL || activationType != ActivationType.TANGENT || Tools.pivotRotation != PivotRotation.Local))
            {
                Debug.LogWarning(lockedWarningMsg);
                return false;
            }

            Vector3 newHandlePosition = handlePosition;
            Vector3 newDif = Vector3.zero;
            Ray mouseRay = EMouseUtility.GetMouseRay(Event.current.mousePosition);

            //Closest point to handle on each axel 
            Vector3 xHandlePos = Utility.LineUtility.GetNearestPointOnLineFromLine(newHandlePosition - deltaPressPosition, xDirection, mouseRay.origin, mouseRay.direction) + deltaPressPosition;
            Vector3 yHandlePos = Utility.LineUtility.GetNearestPointOnLineFromLine(newHandlePosition - deltaPressPosition, yDirection, mouseRay.origin, mouseRay.direction) + deltaPressPosition;
            Vector3 zHandlePos = Utility.LineUtility.GetNearestPointOnLineFromLine(newHandlePosition - deltaPressPosition, zDirection, mouseRay.origin, mouseRay.direction) + deltaPressPosition;

            //Closest position to handle on eaxh axel origninated from activationPosition.
            Vector3 xActivationPos = Utility.LineUtility.GetNearestPoint(activationPosition, xDirection, xHandlePos, out float xTime);
            Vector3 yActivationPos = Utility.LineUtility.GetNearestPoint(activationPosition, yDirection, yHandlePos, out float yTime);
            Vector3 zActivationPos = Utility.LineUtility.GetNearestPoint(activationPosition, zDirection, zHandlePos, out float zTime);

            if (activePart == Part.X_AXEL)
            {
                newDif.x = Vector3.Distance(xHandlePos, activationPosition) * Mathf.Sign(xTime);
                newHandlePosition = xHandlePos;
            }
            else if (activePart == Part.Y_AXEL)
            {
                newDif.y = Vector3.Distance(yHandlePos, activationPosition) * Mathf.Sign(yTime);
                newHandlePosition = yHandlePos;
            }
            else if (activePart == Part.Z_AXEL)
            {
                newDif.z = Vector3.Distance(zHandlePos, activationPosition) * Mathf.Sign(zTime);
                newHandlePosition = zHandlePos;
            }
            else if (activePart == Part.YZ_AXEL)
            {
                projectionPlane.SetNormalAndPosition(xDirection, newHandlePosition - deltaPressPosition);
                if (projectionPlane.Raycast(mouseRay, out float enter))
                    newHandlePosition = mouseRay.GetPoint(enter) + deltaPressPosition;

                newDif.y = Vector3.Distance(yActivationPos, activationPosition) * Mathf.Sign(yTime);
                newDif.z = Vector3.Distance(zActivationPos, activationPosition) * Mathf.Sign(zTime);
            }
            else if (activePart == Part.XY_AXEL)
            {
                projectionPlane.SetNormalAndPosition(zDirection, newHandlePosition - deltaPressPosition);
                if (projectionPlane.Raycast(mouseRay, out float enter))
                    newHandlePosition = mouseRay.GetPoint(enter) + deltaPressPosition;

                newDif.x = Vector3.Distance(xActivationPos, activationPosition) * Mathf.Sign(xTime);
                newDif.y = Vector3.Distance(yActivationPos, activationPosition) * Mathf.Sign(yTime);
            }
            else if (activePart == Part.XZ_AXEL)
            {
                projectionPlane.SetNormalAndPosition(yDirection, newHandlePosition - deltaPressPosition);
                if (projectionPlane.Raycast(mouseRay, out float enter))
                    newHandlePosition = mouseRay.GetPoint(enter) + deltaPressPosition;

                newDif.x = Vector3.Distance(xActivationPos, activationPosition) * Mathf.Sign(xTime);
                newDif.z = Vector3.Distance(zActivationPos, activationPosition) * Mathf.Sign(zTime);
            }

            if (GeneralUtility.IsEqual(handlePosition, newHandlePosition))
                return false;

            if (Vector3.Distance(activationPosition, newHandlePosition) > 5000)
            {
                dif = lastRecordedDif;
                return true;
            }

            dif = newDif;
            lastRecordedDif = newDif;
            handlePosition = newHandlePosition;

            return true;
        }

        public static void Draw(Event e)
        {
            if (!active)
                return;

            float size = HandleUtility.GetHandleSize(handlePosition) * 0.3f * positionToolSize;
            float arrowSize = HandleUtility.GetHandleSize(handlePosition) * positionToolSize;
            float surfaceDot = EHandleSegment.GetControlPointSize(handlePosition) * surfaceDotSize * (isSurfaceDotHovered ? 1.2f : 1);
            Color lockColor = new Color(0.5f, 0.5f, 0.5f, 0.66f);
            Color lockColor2 = new Color(0.3f, 0.3f, 0.3f, 0.66f);

            //Get new directions from modifiers
            Vector3 newXDirection = xModifier ? xDirection : -xDirection;
            Vector3 newZDirection = zModifier ? zDirection : -zDirection;
            Vector3 newYDirection = yModifier ? yDirection : -yDirection;

            if (GeneralUtility.IsZero(xDirection + zDirection + yDirection, 0.001f))
                return;

            xRectangel[0] = handlePosition;
            xRectangel[1] = handlePosition + newYDirection * size;
            xRectangel[2] = handlePosition + newYDirection * size + newZDirection * size;
            xRectangel[3] = handlePosition + newZDirection * size;

            yRectangel[0] = handlePosition;
            yRectangel[1] = handlePosition + newXDirection * size;
            yRectangel[2] = handlePosition + newZDirection * size + newXDirection * size;
            yRectangel[3] = handlePosition + newZDirection * size;

            zRectangel[0] = handlePosition;
            zRectangel[1] = handlePosition + newXDirection * size;
            zRectangel[2] = handlePosition + newYDirection * size + newXDirection * size;
            zRectangel[3] = handlePosition + newYDirection * size;


            Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;

            bool anyXSelected = activePart == Part.XY_AXEL || activePart == Part.XZ_AXEL || activePart == Part.X_AXEL;
            bool anyYSelected = activePart == Part.YZ_AXEL || activePart == Part.XY_AXEL || activePart == Part.Y_AXEL;
            bool anyZSelected = activePart == Part.YZ_AXEL || activePart == Part.XZ_AXEL || activePart == Part.Z_AXEL;

            if(activePart == Part.SURFACE && !locked)
                Handles.color = Color.grey;
            else
                Handles.color = Color.white;

            if (activationType == ActivationType.ANCHOR || activationType == ActivationType.TANGENT)
            {
                if (activePart == Part.SURFACE || (e.control && e.shift && activePart == Part.NONE))
                {
                    Handles.DotHandleCap(0, handlePosition, Quaternion.identity, surfaceDot, EventType.Repaint);
                    return;
                }
            }

            Handles.color = Color.white;

            //ZY red
            if (activePart == Part.NONE || activePart == Part.YZ_AXEL)
            {
                Color color1 = activePart == Part.YZ_AXEL ? Handles.selectedColor : Libraries.LibraryColor.red_A25;
                Color color2 = activePart == Part.YZ_AXEL ? Handles.selectedColor : Handles.xAxisColor;

                if (locked)
                {
                    color1 = lockColor;
                    color2 = lockColor2;
                }

                Handles.DrawSolidRectangleWithOutline(xRectangel, color1, color2);
            }
            //XZ green
            if (activePart == Part.NONE || activePart == Part.XZ_AXEL)
            {
                Color color1 = activePart == Part.XZ_AXEL ? Handles.selectedColor : Libraries.LibraryColor.green_A25;
                Color color2 = activePart == Part.XZ_AXEL ? Handles.selectedColor : Handles.yAxisColor;

                if (locked)
                {
                    color1 = lockColor;
                    color2 = lockColor2;
                }

                Handles.DrawSolidRectangleWithOutline(yRectangel, color1, color2);
            }

            //XY blue
            if (activePart == Part.NONE || activePart == Part.XY_AXEL)
            {
                Color color1 = activePart == Part.XY_AXEL ? Handles.selectedColor : Libraries.LibraryColor.blue_A25;
                Color color2 = activePart == Part.XY_AXEL ? Handles.selectedColor : Handles.zAxisColor;

                if (locked)
                {
                    color1 = lockColor;
                    color2 = lockColor2;
                }

                Handles.DrawSolidRectangleWithOutline(zRectangel, color1, color2);
            }

            //x arrow
            if (activePart == Part.NONE || anyXSelected)
            {
                Color color = anyXSelected ? Handles.selectedColor : Handles.xAxisColor;
                if (locked) color = lockColor;

                Handles.color = color;
                Handles.ArrowHandleCap(0, handlePosition, Quaternion.LookRotation(xDirection), arrowSize, EventType.Repaint);
            }

            //Y arrow
            if (activePart == Part.NONE || anyYSelected)
            {
                Color color = anyYSelected ? Handles.selectedColor : Handles.yAxisColor;
                if (locked) color = lockColor;

                Handles.color = color;
                Handles.ArrowHandleCap(0, handlePosition, Quaternion.LookRotation(yDirection), arrowSize, EventType.Repaint);
            }

            //Z arrow
            if (activePart == Part.NONE || anyZSelected)
            {
                Color color = anyZSelected ? Handles.selectedColor : Handles.zAxisColor;
                if (locked && (activationType != ActivationType.TANGENT || Tools.pivotRotation != PivotRotation.Local)) color = lockColor;

                Handles.color = color;
                Handles.ArrowHandleCap(0, handlePosition, Quaternion.LookRotation(zDirection), arrowSize, EventType.Repaint);
            }
        }

        public static void UpdateOrientation(Vector3 position, Vector3 newForwardDirection, Vector3 newUpDirection)
        {
            if(supportGlobal && Tools.pivotRotation == PivotRotation.Global)
            {
                newForwardDirection = Vector3.forward;
                newUpDirection = Vector3.up;

                Spline spline = EHandleSelection.selectedSpline;

                if(spline != null)
                {
                    newForwardDirection = spline.transform.forward;
                    newUpDirection = spline.transform.up;
                }
            }

            zDirection = newForwardDirection;
            xDirection = Vector3.Cross(newUpDirection, zDirection).normalized;
            yDirection = Vector3.Cross(zDirection, xDirection).normalized;

            float distance = 999999;
            Vector3 position1 = handlePosition + (xDirection + zDirection).normalized;
            Vector3 position2 = handlePosition + (-xDirection + zDirection).normalized;
            Vector3 position3 = handlePosition + (xDirection + -zDirection).normalized;
            Vector3 position4 = handlePosition + (-xDirection + -zDirection).normalized;
            Vector3 position5 = handlePosition + yDirection;
            Vector3 position6 = handlePosition + -yDirection;

            //Check positions in X and Z space
            float distanceCheck = Vector3.Distance(position1, position);
            if (distanceCheck < distance)
            {
                distance = distanceCheck;
                xModifier = true;
                zModifier = true;
            }

            distanceCheck = Vector3.Distance(position2, position);
            if (distanceCheck < distance)
            {
                distance = distanceCheck;
                xModifier = false;
                zModifier = true;
            }

            distanceCheck = Vector3.Distance(position3, position);
            if (distanceCheck < distance)
            {
                distance = distanceCheck;
                xModifier = true;
                zModifier = false;
            }

            distanceCheck = Vector3.Distance(position4, position);
            if (distanceCheck < distance)
            {
                xModifier = false;
                zModifier = false;
            }

            //Check positions in Y space.
            distance = 999999;
            distanceCheck = Vector3.Distance(position5, position);
            if (distanceCheck < distance)
            {
                distance = distanceCheck;
                yModifier = true;
            }

            distanceCheck = Vector3.Distance(position6, position);
            if (distanceCheck < distance)
            {
                yModifier = false;
            }
        }

        private static int GetHotControlId()
        {
            if (hotControlId == 0) hotControlId = GUIUtility.GetControlID(FocusType.Passive);
            return hotControlId;
        }

        private static void UpdateReleased(bool value)
        {
            released = value;
            if(released) EActionToSceneGUI.Add(() => { released = false; }, EActionToSceneGUI.Type.LATE, EventType.Repaint);
        }
    }
}
