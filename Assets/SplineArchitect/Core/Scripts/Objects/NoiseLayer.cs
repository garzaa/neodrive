// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: NoiseLayer.cs
//
// Author: Mikael Danielsson
// Date Created: 25-11-2024
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System;

using UnityEngine;

namespace SplineArchitect.Objects
{
    [Serializable]
    public struct NoiseLayer
    {
        public enum Type
        {
            PERLIN_NOISE,
            BILLOW_NOISE,
            DOMAIN_WARPED_NOISE,
            VORONOI_NOISE,
            TERRACE_NOISE,
            RIDGED_PERLIN_NOISE,
            FMB_NOISE,
            HYBRID_MULTI_FRACTAL,
        }

        public enum Group
        {
            NONE,
            A,
            B,
            C,
            D,
            E,
            F,
            G,
            H,
            I,
            J,
            K,
            L,
            M,
            N,
            O,
            P,
        }

        public Type type;
        public Vector3 scale;
        public float seed;
        public int octaves;
        public float frequency;
        public float amplitude;
        public Group group;
        public bool enabled;

#if UNITY_EDITOR
        public bool selected;
#endif

        public NoiseLayer(Type type, Vector3 scale, float seed, int octaves = 4, float frequency = 2, float amplitude = 2)
        {
            this.type = type;
            this.scale = scale;
            this.seed = seed;
            this.octaves = octaves;
            this.frequency = frequency;
            this.amplitude = amplitude;
            group = Group.A;
            enabled = true;

#if UNITY_EDITOR
            selected = false;
#endif
    }
}
}