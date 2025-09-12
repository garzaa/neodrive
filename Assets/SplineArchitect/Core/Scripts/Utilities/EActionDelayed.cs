// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: EActionDelayed.cs
//
// Author: Mikael Danielsson
// Date Created: 15-02-2023
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

#if UNITY_EDITOR
using System.Collections.Generic;
using System;
using UnityEditor;

namespace SplineArchitect.Utility
{
    public class EActionDelayed
    {
        public enum Type : byte
        {
            DELAY,
            FRAMES,
            BOTH
        }

        private static double lastTimeSinceStartup;
        private static double editorDeltaTime;
        private static List<EActionDelayed> delayedActions = new List<EActionDelayed>();

        public Action action;
        public double delay;
        public int frames;
        public Type actionType { get; private set; }

        public EActionDelayed(Action action, double delay, int frames, Type actionType)
        {
            this.action = action;
            this.delay = delay;
            this.frames = frames;
            this.actionType = actionType;
        }

        public static void UpdateGlobal()
        {
            editorDeltaTime = EditorApplication.timeSinceStartup - lastTimeSinceStartup;
            lastTimeSinceStartup = EditorApplication.timeSinceStartup;

            for (int i = delayedActions.Count - 1; i >= 0; i--)
            {
                EActionDelayed da = delayedActions[i];

                if ((da.actionType == Type.DELAY || da.actionType == Type.BOTH) && da.delay <= 0)
                {
                    da.action();
                    delayedActions.Remove(da);
                    continue;
                }

                if ((da.actionType == Type.FRAMES || da.actionType == Type.BOTH) && da.frames <= 0)
                {
                    da.action();
                    delayedActions.Remove(da);
                    continue;
                }

                da.frames--;
                da.delay -= editorDeltaTime;
            }
        }

        public static void Add(Action action, double timeDelay, int frames, Type actionType)
        {
            delayedActions.Add(new EActionDelayed(action, timeDelay, frames, actionType));
        }
    }
}
#endif