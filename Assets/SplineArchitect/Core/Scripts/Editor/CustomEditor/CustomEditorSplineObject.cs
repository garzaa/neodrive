// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: CustomEditorSplineObject.cs
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
    [UnityEditor.CustomEditor(typeof(SplineObject))]
    [CanEditMultipleObjects]
    public class CustomEditorSplineObject : Editor
    {
        public override void OnInspectorGUI()
        {
            // Start the custom Inspector
            serializedObject.Update();

            //// Load the image
            //Texture2D myTexture = LibraryTexture.logo500x100;

            //if(myTexture != null)
            //{
            //    // Calculate the width and height of the image
            //    float aspect = 500 / 100;
            //    Rect previewArea = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(Screen.width / aspect));

            //    // Draw the texture in the Inspector
            //    GUI.DrawTexture(previewArea, myTexture);
            //}

            EditorGUILayout.LabelField("This component is removed/added automatically when adding or \n" +
                                       "removing objects from any Spline in the hierarchy. \n " +
                                       "You should not add it manually.",
                                       GUILayout.Height(65));

            DrawPropertiesExcluding(serializedObject, "m_Script");
            serializedObject.ApplyModifiedProperties();
        }
    }
}