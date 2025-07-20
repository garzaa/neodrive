// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: CustomEditorSpline.cs
//
// Author: Mikael Danielsson
// Date Created: 07-01-2024
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using UnityEngine;
using UnityEditor;

using SplineArchitect.Objects;

namespace SplineArchitect.CustomEditor
{
    [UnityEditor.CustomEditor(typeof(Spline))]
    [CanEditMultipleObjects]
    public class CustomEditorSpline : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("A Spline component used by Spline Architect.",
                                       GUILayout.Height(25));

            DrawPropertiesExcluding(serializedObject, "m_Script");
            serializedObject.ApplyModifiedProperties();
        }
    }
}