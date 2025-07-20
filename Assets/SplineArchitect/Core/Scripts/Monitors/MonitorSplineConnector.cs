// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: MonitorSplineConnector.cs
//
// Author: Mikael Danielsson
// Date Created: 23-05-2025
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using UnityEngine;

using SplineArchitect.Objects;
using SplineArchitect.Utility;

namespace SplineArchitect.Monitor
{
    public class MonitorSplineConnector
    {
        public const int dataUsage = 24 + 
                                     16 + 12;

        private int mirrorSum;
        private Quaternion rotation;
        private Vector3 position;
        private SplineConnector sc;

        public MonitorSplineConnector(SplineConnector sc)
        {
            this.sc = sc;
        }

        public bool MirrorChange(bool forceUpdate = false)
        {
            int mirrors = 0;

            foreach(Segment s in sc.connections)
            {
                if (s.mirrorConnector) mirrors++;
            }

            bool foundChange = false;
            if (mirrors != mirrorSum) foundChange = true;

            if (forceUpdate)
                mirrorSum = mirrors;

            return foundChange;
        }

        public bool PosChange(bool forceUpdate = false)
        {
            bool foundChange = false;
            if (!GeneralUtility.IsEqual(position, sc.transform.position)) foundChange = true;

            if (forceUpdate)
                position = sc.transform.position;

            return foundChange;
        }

        public bool RotChange(bool forceUpdate = false)
        {
            bool foundChange = false;
            if (!GeneralUtility.IsEqual(rotation.eulerAngles, sc.transform.rotation.eulerAngles)) foundChange = true;

            if(forceUpdate)
                rotation = sc.transform.rotation;

            return foundChange;
        }

        public void Update()
        {
            rotation = sc.transform.rotation;
            position = sc.transform.position;
        }
    }
}
