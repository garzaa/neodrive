// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: HandleDeformationWorker.cs
//
// Author: Mikael Danielsson
// Date Created: 03-07-2024
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System.Collections.Generic;

using UnityEngine;

using SplineArchitect.Objects;

namespace SplineArchitect
{
    public class HandleDeformationWorker
    {
        private static List<DeformationWorker> deformationWorkers = new List<DeformationWorker>();

        private static DeformationWorker GetWorker(DeformationWorker.Type type, Spline spline)
        {
            if (deformationWorkers.Count > 500)
                Debug.LogWarning($"[Spline Architect] Currently: {deformationWorkers.Count} deformation workers exists!.");

            foreach(DeformationWorker dw in deformationWorkers)
            {
                if (dw.type != type && dw.type != DeformationWorker.Type.NONE)
                    continue;

                if (dw.spline != spline && dw.spline != null)
                    continue;

                if (dw.state != DeformationWorker.State.IDLE && dw.state != DeformationWorker.State.READY)
                    continue;

                dw.type = type;
                return dw;
            }

            DeformationWorker newDw = new DeformationWorker(type);
            deformationWorkers.Add(newDw);

            return newDw;
        }

        public static void Clear()
        {
            deformationWorkers.Clear();
        }

        public static void Deform(SplineObject so, DeformationWorker.Type type, Spline spline)
        {
            DeformationWorker dw = GetWorker(type, spline);

            dw.Add(so, spline);
        }

        public static void Deform(SplineObject so, DeformationWorker.Type type)
        {
            DeformationWorker dw = GetWorker(type, so.splineParent);
            dw.Add(so, so.splineParent);
        }

        public static void GetActiveWorkers(DeformationWorker.Type type, Spline spline, List<DeformationWorker> activeWorkersList)
        {
            activeWorkersList.Clear();

            foreach (DeformationWorker dw in deformationWorkers)
            {
                if (dw.state == DeformationWorker.State.IDLE)
                    continue;

                if (dw.spline != spline)
                    continue;

                if (dw.type != type)
                    continue;

                activeWorkersList.Add(dw);
            }
        }

        public static void GetActiveWorkers(DeformationWorker.Type type, List<DeformationWorker> activeWorkersList)
        {
            activeWorkersList.Clear();

            foreach (DeformationWorker dw in deformationWorkers)
            {
                if (dw.state == DeformationWorker.State.IDLE)
                    continue;

                if (dw.type != type)
                    continue;

                activeWorkersList.Add(dw);
            }
        }
    }
}
