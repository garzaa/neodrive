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
        public static string GetFolderPath()
        {
            return EHandleFolder.GetMainFolderPath();
        }
    }
}