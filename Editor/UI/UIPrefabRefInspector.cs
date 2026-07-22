using System;
using Tritone.Editor.CodeGeneration;
using Tritone.Unity.UI;
using UnityEditor;
using UnityEngine;

namespace Tritone.Editor.UI
{
    /// <summary>
    /// Provides drag-and-drop reference authoring, view generation, binding, and preprocessing.
    /// </summary>
    [CustomEditor(typeof(UIPrefabRef))]
    public sealed class UIPrefabRefInspector : UnityEditor.Editor
    {
        /// <summary>
        /// Stores the serialized UI reference array.
        /// </summary>
        private SerializedProperty mReferences;

        /// <summary>
        /// Caches the serialized reference array when the inspector becomes active.
        /// </summary>
        private void OnEnable()
        {
            mReferences = serializedObject.FindProperty(nameof(UIPrefabRef.References));
        }

        /// <summary>
        /// Draws authoring settings, references, drag target, and processing actions.
        /// </summary>
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, "m_Script", nameof(UIPrefabRef.References), nameof(UIPrefabRef.SortingNodes));
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("View References", EditorStyles.boldLabel);
            for (var i = 0; i < mReferences.arraySize; i++)
                DrawReference(i);
            DrawDragArea();
            serializedObject.ApplyModifiedProperties();

            var prefabRef = (UIPrefabRef)target;
            EditorGUILayout.Space();
            if (GUILayout.Button("Generate UIView Script", GUILayout.Height(28f)))
            {
                if (UIViewCodeGenerator.Generate(prefabRef))
                    AssetDatabase.Refresh();
            }
            if (GUILayout.Button("Bind Generated UIView", GUILayout.Height(28f)))
                UIViewCodeGenerator.Bind(prefabRef);
            if (GUILayout.Button("Preprocess Sorting Hierarchy", GUILayout.Height(28f)))
                UIViewPreprocessor.Process(prefabRef);

            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.IntField("Recorded Sorting Nodes", prefabRef.SortingNodes?.Length ?? 0);
        }

        /// <summary>
        /// Draws one generated field definition and its component type selector.
        /// </summary>
        /// <param name="index">The serialized reference index.</param>
        private void DrawReference(int index)
        {
            var property    = mReferences.GetArrayElementAtIndex(index);
            var name        = property.FindPropertyRelative(nameof(UIViewReference.Name));
            var value       = property.FindPropertyRelative(nameof(UIViewReference.Value));
            var description = property.FindPropertyRelative(nameof(UIViewReference.Description));
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            name.stringValue = EditorGUILayout.TextField(name.stringValue);
            if (GUILayout.Button("-", GUILayout.Width(24f)))
            {
                mReferences.DeleteArrayElementAtIndex(index);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                return;
            }
            EditorGUILayout.EndHorizontal();
            value.objectReferenceValue = EditorGUILayout.ObjectField("Reference", value.objectReferenceValue, typeof(UnityEngine.Object), true);
            if (value.objectReferenceValue)
                DrawComponentSelector(value);
            description.stringValue = EditorGUILayout.TextField("Description", description.stringValue);
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// Allows a dragged GameObject reference to select one of its concrete components.
        /// </summary>
        /// <param name="value">The serialized object reference.</param>
        private static void DrawComponentSelector(SerializedProperty value)
        {
            var gameObject = value.objectReferenceValue as GameObject;
            if (!gameObject && value.objectReferenceValue is Component component)
                gameObject = component.gameObject;
            if (!gameObject)
                return;

            var components = gameObject.GetComponents<Component>();
            var names      = new string[components.Length + 1];
            names[0] = nameof(GameObject);
            var selected = ReferenceEquals(value.objectReferenceValue, gameObject) ? 0 : -1;
            for (var i = 0; i < components.Length; i++)
            {
                var current = components[i];
                names[i + 1] = current ? current.GetType().Name : "Missing Script";
                if (ReferenceEquals(value.objectReferenceValue, current))
                    selected = i + 1;
            }
            var next = EditorGUILayout.Popup("Component", Math.Max(0, selected), names);
            value.objectReferenceValue = next == 0 ? gameObject : components[next - 1];
        }

        /// <summary>
        /// Draws and handles the GameObject and Component drag target.
        /// </summary>
        private void DrawDragArea()
        {
            var area = GUILayoutUtility.GetRect(0f, 52f, GUILayout.ExpandWidth(true));
            GUI.Box(area, "Drag UI nodes here", EditorStyles.helpBox);
            var current = Event.current;
            if (!area.Contains(current.mousePosition) || current.type is not (EventType.DragUpdated or EventType.DragPerform))
                return;

            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            if (current.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                foreach (var dragged in DragAndDrop.objectReferences)
                    AddReference(dragged);
            }
            current.Use();
        }

        /// <summary>
        /// Adds one unique dragged object to the serialized field list.
        /// </summary>
        /// <param name="dragged">The dragged GameObject or Component.</param>
        private void AddReference(UnityEngine.Object dragged)
        {
            if (dragged is not GameObject && dragged is not Component)
                return;
            for (var i = 0; i < mReferences.arraySize; i++)
            {
                var existing = mReferences.GetArrayElementAtIndex(i).FindPropertyRelative(nameof(UIViewReference.Value));
                if (ReferenceEquals(existing.objectReferenceValue, dragged))
                    return;
            }

            var index = mReferences.arraySize;
            mReferences.InsertArrayElementAtIndex(index);
            var property = mReferences.GetArrayElementAtIndex(index);
            property.FindPropertyRelative(nameof(UIViewReference.Name)).stringValue = UIViewCodeGenerator.ToIdentifier(dragged.name);
            property.FindPropertyRelative(nameof(UIViewReference.Value)).objectReferenceValue = dragged;
            property.FindPropertyRelative(nameof(UIViewReference.Description)).stringValue = dragged.name;
        }
    }
}
