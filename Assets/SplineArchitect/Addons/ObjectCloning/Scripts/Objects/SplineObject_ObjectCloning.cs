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
        public bool isCloneHead;
        [HideInInspector]
        public bool isOriginClone;
        [HideInInspector]
        public bool isClone;
        [HideInInspector]
        public bool cloneUseFixedAmount;
        [HideInInspector]
        public int cloneAmount;
        [HideInInspector]
        public int cloneMenuAmount;
        [HideInInspector]
        public float cloneSectionLength;
        [HideInInspector]
        public float cloneShiftForward;
        [HideInInspector]
        public float cloneShiftBackward;
        [HideInInspector]
        public Vector3 cloneOffset;
        [HideInInspector]
        public CloneDirection cloneDirection;
        [HideInInspector]
        public List<SplineObject> originClones = new List<SplineObject>();

        //Runtime data
        [NonSerialized]
        public List<SplineObject> clones = null;
        [NonSerialized]
        public int oldCloneAmount;
        [NonSerialized]
        public Vector3 oldCloneOffset;
        [NonSerialized]
        public CloneDirection oldCloneDirection;

#endif
    }
}
