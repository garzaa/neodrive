// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: NativeSegment.cs
//
// Author: Mikael Danielsson
// Date Created: 29-01-2023
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using UnityEngine;

namespace SplineArchitect.Objects
{
    public struct NativeSegment
    {
        public Vector3 anchor;
        public Vector3 tangentA;
        public Vector3 tangentB;

        //Deformation
        public float zRot;
        public float contrast;
        public float noise;
        public Vector2 sadleSkew;
        public Vector2 scale;

        public NativeSegment(Vector3 anchor, Vector3 tangentA, Vector3 tangentB)
        {
            this.anchor = anchor;
            this.tangentA = tangentA;
            this.tangentB = tangentB;

            zRot = Segment.defaultZRotation;
            contrast = Segment.defaultContrast;
            noise = 0;
            sadleSkew = new Vector2(Segment.defaultSaddleSkewX, Segment.defaultSaddleSkewY);
            scale = new Vector2(Segment.defaultScale, Segment.defaultScale);
        }

        public NativeSegment(Vector3 anchor, 
                              Vector3 tangentA, 
                              Vector3 tangentB,
                              float zRot,
                              float contrast,
                              float noise,
                              Vector2 sadleSkew,
                              Vector2 scale)
        {
            this.anchor = anchor;
            this.tangentA = tangentA;
            this.tangentB = tangentB;

            this.zRot = zRot;
            this.contrast = contrast;
            this.noise = noise;
            this.sadleSkew = sadleSkew;
            this.scale = scale;
        }
    }
}
