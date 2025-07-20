// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: SearchData.cs
//
// Author: Mikael Danielsson
// Date Created: 09-03-2024
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

namespace SplineArchitect.Objects
{
    public struct SearchData
    {
        public float distanceToLink;
        public float closestDistanceToLink;
        public float distanceToClosest;
        public SplineObject closest;
        public bool onLink;
        public bool isForward;

        public SearchData(float distanceToLink, float closestDistanceToLink, float distanceToClosest, SplineObject closest, bool onLink, bool isForward)
        {
            this.distanceToLink = distanceToLink;
            this.closestDistanceToLink = closestDistanceToLink;
            this.distanceToClosest = distanceToClosest;
            this.closest = closest;
            this.onLink = onLink;
            this.isForward = isForward;
        }
    }
}
