﻿// Copyright (C) 2014 - 2016 Stephan Bouchard - All Rights Reserved
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms


using UnityEngine;
using UnityEditor;
using System.Collections;

namespace TMPro.EditorUtilities
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(MeshRenderer))]
    public class TMP_MeshRendererEditor : Editor
    {
        private SerializedProperty m_Materials;


        void OnEnable()
        {
            m_Materials = serializedObject.FindProperty("m_Materials");
        }


        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Get a reference to the current material.
            SerializedProperty material_prop = m_Materials.GetArrayElementAtIndex(0);
            Material currentMaterial = material_prop.objectReferenceValue as Material;

            EditorGUI.BeginChangeCheck();
            base.OnInspectorGUI();
            if (EditorGUI.EndChangeCheck())
            {
                material_prop = m_Materials.GetArrayElementAtIndex(0);

                TMP_FontAsset newFontAsset = null;
                Material newMaterial = null;

                if (material_prop != null)
                    newMaterial = material_prop.objectReferenceValue as Material;

                // Check if the new material is referencing a different font atlas texture.
                if (newMaterial != null && currentMaterial.GetInstanceID() != newMaterial.GetInstanceID())
                {
                    // Search for the Font Asset matching the new font atlas texture.
                    newFontAsset = TMP_EditorUtility.FindMatchingFontAsset(newMaterial);
                }


                GameObject[] objects = Selection.gameObjects;

                for (int i = 0; i < objects.Length; i++)
                {
                    // Assign new font asset
                    if (newFontAsset != null)
                    {
                        TMP_Text textComponent = objects[i].GetComponent<TMP_Text>();

                        if (textComponent != null)
                        {
                            Undo.RecordObject(textComponent, "Font Asset Change");
                            textComponent.font = newFontAsset;
                        }
                    }

                    TMPro_EventManager.ON_DRAG_AND_DROP_MATERIAL_CHANGED(objects[i], currentMaterial, newMaterial);
                }
            }
        }
    }
}
