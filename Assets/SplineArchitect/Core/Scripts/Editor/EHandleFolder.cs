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
    public class EHandleFolder
    {
        public static long tempFolderSize { get; private set; }
        public static long harddiskSpaceLeft { get; private set; }

        private static string mainFolderPath;

        public static string GetMainFolderPath()
        {
            if (string.IsNullOrEmpty(mainFolderPath))
                mainFolderPath = RetriveMainFolderPath();

            if (string.IsNullOrEmpty(mainFolderPath))
                Debug.LogError($"[Spline Architect] Could not retrvice {WindowInfo.toolName} main folder!");

            return mainFolderPath;
        }

        private static string RetriveMainFolderPath()
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

        public static void UpdateTempFolderSize()
        {
            string tempPath = $"{Application.dataPath.Substring(0, Application.dataPath.Length - 7)}/Temp";
            long size = 0;
            if (Directory.Exists(tempPath))
            {
                DirectoryInfo dirInfo = new DirectoryInfo(tempPath);
                FileInfo[] files = dirInfo.GetFiles("d_*.bin", SearchOption.TopDirectoryOnly);
                foreach (FileInfo file in files)
                    size += file.Length;
            }

            tempFolderSize = size;
        }

        public static void UpdateHarddiskSpaceLeft()
        {
            DriveInfo drive = new DriveInfo(Path.GetPathRoot(Environment.CurrentDirectory));
            if (drive.IsReady) harddiskSpaceLeft = drive.AvailableFreeSpace;
        }
    }
}