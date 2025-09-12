// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: EActionToUpdate.cs
//
// Author: Mikael Danielsson
// Date Created: 01-07-2023
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

#if UNITY_EDITOR
using System.Collections.Generic;
using System;

namespace SplineArchitect.Utility
{
    public static class EActionToUpdate
    {
        public enum Type : byte
        {
            EARLY,
            LATE
        }

        private static List<(int, Action)> earlyActions = new List<(int, Action)>();
        private static List<(int, Action)> lateActions = new List<(int, Action)>();

        public static void EarlyUpdate()
        {
            for (int i = earlyActions.Count - 1; i >= 0; i--)
            {
                earlyActions[i].Item2();
                earlyActions.Remove(earlyActions[i]);
            }
        }

        public static void LateUpdate()
        {
            for (int i = lateActions.Count - 1; i >= 0; i--)
            {
                lateActions[i].Item2();
                lateActions.Remove(lateActions[i]);
            }
        }

        /// <summary>
        /// Action will execute during the editor update loop. Only one entry per frame can have the same id, other entrys will be ignored. If you want multipule actions with the same id during the same frame use -1.
        /// </summary>
        public static void Add(Action action, Type type, int id = -1)
        {
            if (type == Type.EARLY && !earlyActions.Exists(item => id >= 0 && item.Item1 == id))
                earlyActions.Add((id, action));
            else if (type == Type.LATE && !lateActions.Exists(item => id >= 0 && item.Item1 == id))
                lateActions.Add((id, action));
        }

        public static void Insert(Action action, Type type, int id = -1)
        {
            if (type == Type.EARLY && !earlyActions.Exists(item => id >= 0 && item.Item1 == id))
                earlyActions.Insert(0, (id, action));
            else if (type == Type.LATE && !lateActions.Exists(item => id >= 0 && item.Item1 == id))
                lateActions.Insert(0, (id, action));
        }
    }
}
#endif
