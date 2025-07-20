// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: EHandleMainFolder.cs
//
// Author: Mikael Danielsson
// Date Created: 23-11-2024
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;

using UnityEngine;
using UnityEditor;

using SplineArchitect.Objects;
using SplineArchitect.Utility;
using SplineArchitect.Ui;

namespace SplineArchitect
{
    public class EHandleMainFolder
    {
        private static string folderPath;

        public static string GetFolderPath()
        {
            if (string.IsNullOrEmpty(folderPath))
                folderPath = RetriveFolderPath();

            if (string.IsNullOrEmpty(folderPath))
                Debug.LogError($"Could not retrvice {MenuGeneral.toolName} main folder!");

            return folderPath;
        }

        private static string RetriveFolderPath()
        {
            string f = EGeneralUtility.GetScriptPath(typeof(Spline));
            string[] split = f.Split('/');

            string f2 = "";

            for (int i = 0; i < split.Length - 4; i++)
            {
                if(i == split.Length - 5)
                    f2 += split[i];
                else
                    f2 += split[i] + "/";
            }

            return f2;
        }
    }
}