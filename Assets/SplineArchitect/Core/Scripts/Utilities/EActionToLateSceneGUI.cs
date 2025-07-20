// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: EActionToLateSceneGUI.cs
//
// Author: Mikael Danielsson
// Date Created: 01-07-2023
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

#if UNITY_EDITOR
using System.Collections.Generic;
using System;
using UnityEngine;

namespace SplineArchitect.Utility
{
    public static class EActionToLateSceneGUI
    {
        private static List<(Action, EventType, int)> lateActions = new List<(Action, EventType, int)>();

        public static void LateOnSceneGUI(Event e)
        {
            for (int i = lateActions.Count - 1; i >= 0; i--)
            {
                if(lateActions[i].Item2 == e.type)
                {
                    lateActions[i].Item1();
                    lateActions.Remove(lateActions[i]);
                }
            }
        }

        public static void Add(Action action, EventType eventType, int id = -1, bool addToStart = false)
        {
            if (id != -1 && lateActions.Exists(item => item.Item3 == id))
                return;

            if (addToStart)
                lateActions.Insert(0, (action, eventType, id));
            else
                lateActions.Add((action, eventType, id));
        }
    }
}
#endif