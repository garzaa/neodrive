// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: SplineConnectorUtility.cs
//
// Author: Mikael Danielsson
// Date Created: 26-05-2025
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using UnityEngine;

using SplineArchitect.Objects;

namespace SplineArchitect.Utility
{
    public class SplineConnectorUtility
    {
        public static SplineConnector GetClosest(Vector3 worldPoint)
        {
            float dCheck = 999999;
            SplineConnector closest = null;

            foreach (SplineConnector sc in HandleRegistry.GetSplineConnectors())
            {
                float distance = Vector3.Distance(worldPoint, sc.transform.position);

                if (distance < dCheck)
                {
                    dCheck = distance;
                    closest = sc;
                }
            }

            return closest;
        }
    }
}
