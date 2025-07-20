// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: AssetModificationDetection.cs
//
// Author: Mikael Danielsson
// Date Created: 23-03-2023
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using UnityEngine;
using UnityEditor;

namespace SplineArchitect.PostProcessing
{
    public class AssetModificationDetection : AssetPostprocessor
    {
        void OnPostprocessModel(GameObject g)
        {
            EHandleMeshContainer.Refresh();
        }
    }
}
