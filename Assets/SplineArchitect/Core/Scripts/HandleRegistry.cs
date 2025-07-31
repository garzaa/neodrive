// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: HandleRegistry.cs
//
// Author: Mikael Danielsson
// Date Created: 05-09-2024
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System.Collections.Generic;

using UnityEngine;

using SplineArchitect.Objects;

namespace SplineArchitect
{
    public class HandleRegistry
    {
        private static HashSet<Spline> registrySplines = new HashSet<Spline>();
        private static HashSet<SplineConnector> registrySplineConnectors = new HashSet<SplineConnector>();

#if UNITY_EDITOR
        private static List<int> clearContainer = new List<int>();
        private static HashSet<int> registryInstanceIds = new HashSet<int>();
        private static HashSet<Spline> registrySplinesPrefabStage = new HashSet<Spline>();
        private static HashSet<SplineConnector> registrySplineConnectorsPrefabStage = new HashSet<SplineConnector>();
#endif

        /// <summary>
        /// Retrieves the current set of registered splines, using a prefab-stage-aware registry in the editor.
        /// </summary>
        /// <returns>A HashSet<Spline> containing all registered splines.</returns>
        public static HashSet<Spline> GetSplines()
        {
#if UNITY_EDITOR
            if (EHandlePrefab.IsPrefabStageActive())
                return registrySplinesPrefabStage;
#endif
            return registrySplines;
        }

        /// <summary>
        /// Adds a spline to the runtime or prefab-stage registry, depending on context.
        /// </summary>
        /// <param name="spline">The spline to register.</param>
        public static void AddSpline(Spline spline)
        {
#if UNITY_EDITOR
            if (EHandlePrefab.IsPartOfActivePrefabStage(spline.gameObject))
            {
                registrySplinesPrefabStage.Add(spline);
                return;
            }
#endif

            registrySplines.Add(spline);
        }

        /// <summary>
        /// Removes a spline from both the runtime and prefab-stage registries.
        /// </summary>
        /// <param name="spline">The spline to remove.</param>
        public static void RemoveSpline(Spline spline)
        {
#if UNITY_EDITOR
            registrySplinesPrefabStage.Remove(spline);
#endif
            registrySplines.Remove(spline);
        }

        /// <summary>
        /// Retrieves the current set of registered spline connectors, using a prefab-stage-aware registry in the editor.
        /// </summary>
        /// <returns>A HashSet<SplineConnector> containing all registered spline connectors.</returns>
        public static HashSet<SplineConnector> GetSplineConnectors()
        {
#if UNITY_EDITOR
            if (EHandlePrefab.IsPrefabStageActive())
                return registrySplineConnectorsPrefabStage;
#endif

            return registrySplineConnectors;
        }

        /// <summary>
        /// Adds a spline connector to the runtime or prefab-stage registry, depending on context.
        /// </summary>
        /// <param name="sc">The spline connector to register.</param>
        public static void AddSplineConnector(SplineConnector sc)
        {
#if UNITY_EDITOR
            if (EHandlePrefab.IsPartOfActivePrefabStage(sc.gameObject))
            {
                registrySplineConnectorsPrefabStage.Add(sc);
                return;
            }
#endif

            registrySplineConnectors.Add(sc);
        }

        /// <summary>
        /// Removes a spline connector from both the runtime and prefab-stage registries.
        /// </summary>
        /// <param name="sc">The spline connector to remove.</param>
        public static void RemoveSplineConnector(SplineConnector sc)
        {
#if UNITY_EDITOR
            registrySplineConnectorsPrefabStage.Remove(sc);
#endif

            registrySplineConnectors.Remove(sc);
        }

        /// <summary>
        /// Calculates the total length of all registered splines.
        /// </summary>
        /// <returns>A float representing the combined length of all splines.</returns>
        public static float GetTotalLengthOfAllSplines()
        {
            float totalLength = 0f;

            foreach (Spline spline in GetSplines())
            {
                totalLength += spline.length;
            }

            return totalLength;
        }

#if UNITY_EDITOR
        public static void ClearScene(string name)
        {
            clearContainer.Clear();
            foreach (int id in registryInstanceIds)
            {
                Transform t = UnityEditor.EditorUtility.InstanceIDToObject(id) as Transform;

                if (t == null || t.gameObject.scene.name == name)
                    clearContainer.Add(id);
            }
            foreach (int value in clearContainer)
            {
                registryInstanceIds.Remove(value);
            }
        }

        public static void AddInstanceId(Transform t)
        {
            registryInstanceIds.Add(t.GetInstanceID());
        }

        public static bool IsNew(Transform t)
        {
            if (registryInstanceIds.Contains(t.GetInstanceID()))
                return false;

            registryInstanceIds.Add(t.GetInstanceID());

            return true;
        }

        public static void DisposeNativeDataOnSplines()
        {
            foreach (Spline spline in registrySplines)
            {
                spline.DisposeCachedData();
            }

            foreach (Spline spline in registrySplinesPrefabStage)
            {
                spline.DisposeCachedData();
            }
        }
#endif
    }
}
