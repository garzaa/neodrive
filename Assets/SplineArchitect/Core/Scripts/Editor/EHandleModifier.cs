// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: EHandleModifier.cs
//
// Author: Mikael Danielsson
// Date Created: 28-05-2025
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using UnityEngine;

namespace SplineArchitect
{
    public class EHandleModifier
    {
        public static bool CtrlActive(Event e)
        {
#if UNITY_EDITOR_OSX
            return e.command;
#else
            return e.control;
#endif
        }

        public static bool CtrlShiftActive(Event e)
        {
#if UNITY_EDITOR_OSX
            return e.command && e.shift;
#else
            return e.control && e.shift;
#endif
        }

        public static bool AltActive(Event e)
        {
            return e.alt;
        }
    }
}
