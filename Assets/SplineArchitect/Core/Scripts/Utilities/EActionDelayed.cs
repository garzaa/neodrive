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
        public enum ActionType : byte
        {
            unspecified
        }

        private static double lastTimeSinceStartup;
        private static double editorDeltaTime;
        private static List<EActionDelayed> delayedActions = new List<EActionDelayed>();

        public Action action;
        public double delay;
        public ActionType actionType { get; private set; }
        public bool stop { get; private set; }

        public EActionDelayed(Action action, double delay, ActionType actionType)
        {
            this.action = action;
            this.delay = delay;
            this.actionType = actionType;
            stop = false;
        }

        public static void Update()
        {
            editorDeltaTime = EditorApplication.timeSinceStartup - lastTimeSinceStartup;
            lastTimeSinceStartup = EditorApplication.timeSinceStartup;

            for (int i = delayedActions.Count - 1; i >= 0; i--)
            {
                if (delayedActions[i].delay <= 0 || delayedActions[i].stop)
                {
                    if (!delayedActions[i].stop)
                        delayedActions[i].action();

                    delayedActions.Remove(delayedActions[i]);
                    continue;
                }

                delayedActions[i].delay -= editorDeltaTime;
            }
        }

        public static void Stop(ActionType actionType)
        {
            foreach (EActionDelayed ad in delayedActions)
            {
                if (ad.actionType == actionType)
                    ad.stop = true;
            }
        }

        public static void Add(Action action, double timeDelay, ActionType actionType = ActionType.unspecified)
        {
            delayedActions.Add(new EActionDelayed(action, timeDelay, actionType));
        }
    }
}
#endif