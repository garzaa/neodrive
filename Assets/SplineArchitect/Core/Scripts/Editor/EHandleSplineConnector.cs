// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: EHandleSplineConnector.cs
//
// Author: Mikael Danielsson
// Date Created: 23-05-2025
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System.Collections.Generic;

using UnityEngine;

using SplineArchitect.Objects;
using SplineArchitect.Utility;

namespace SplineArchitect
{
    public static class EHandleSplineConnector
    {
        public static void Update()
        {
            foreach (SplineConnector sc in HandleRegistry.GetSplineConnectors())
            {
                bool scPosChange = sc.monitor.PosChange(true);
                bool scRotChange = sc.monitor.RotChange(true);
                bool mirrorChange = sc.monitor.MirrorChange(true);

                for(int i = sc.connections.Count - 1; i >= 0; i--)
                {
                    Segment s = sc.connections[i];

                    if (s == null)
                        continue;

                    if (s.linkTarget != Segment.LinkTarget.SPLINE_CONNECTOR)
                    {
                        s.splineConnector.RemoveConnection(s);
                        continue;
                    }

                    if (scPosChange || scRotChange || mirrorChange)
                    {
                        sc.AlignSegment(s);
                        s.splineParent.UpdateCachedData();
                        EHandleDeformation.TryDeform(s.splineParent);
                    }
                }
            }
        }
    }
}
