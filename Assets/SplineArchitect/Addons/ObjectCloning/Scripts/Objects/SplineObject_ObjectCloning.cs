// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: SplineObject_Cloning.cs
//
// Author: Mikael Danielsson
// Date Created: 21-04-2025
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

using UnityEngine;

namespace SplineArchitect.Objects
{
    public partial class SplineObject : MonoBehaviour
    {
#if UNITY_EDITOR

        public enum CloneDirection
        {
            FORWARD,
            BACKWARD
        }

        //Stored data
        [HideInInspector]
        public bool cloningEnabled;
        [HideInInspector]
        public bool cloneUseFixedAmount;
        [HideInInspector]
        public bool cloneSnapEnd;
        [HideInInspector]
        public int cloneAmount;
        [HideInInspector]
        public float cloneSnapEndOffset;
        [HideInInspector]
        public Vector3 cloneOffset;
        [HideInInspector]
        public CloneDirection cloneDirection;
        [HideInInspector]
        public List<SplineObject> clones;
        [HideInInspector]
        public List<SplineObject> originClones;
#endif
    }
}
