// Cratesmith 2017

using System.IO;
using Cratesmith.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR

#endif

namespace Cratesmith.InspectorTypes
{
    [System.Serializable]
    public struct SceneId 
#if UNITY_EDITOR
        : ISerializationCallbackReceiver
#endif
    {
        public Scene loadedScene { get { return SceneManager.GetSceneByPath(path); }}
        public string path { get { return m_scenePath; } } 
        public string fullName { get { return m_sceneFullName; } }
        public string name { get { return m_sceneName; } }
	
#pragma warning disable 649
        [SerializeField] Object editorSceneObject;
        [SerializeField] string m_sceneFullName;
        [SerializeField] string m_scenePath;
        [SerializeField] string m_sceneName;
#pragma warning restore 649

        public override string ToString()
        {
            return fullName;
        }
		
        public static implicit operator string(SceneId source)
        {
            return source.ToString();
        }

#if UNITY_EDITOR
        public void OnBeforeSerialize()
        {		
            if (editorSceneObject != null)
            {
                var assetPath = AssetDatabase.GetAssetPath(editorSceneObject).Substring("Assets/".Length);	
                m_sceneFullName = Path.GetDirectoryName(assetPath).Replace('\\','/') + "/" + Path.GetFileNameWithoutExtension(assetPath);
                m_sceneName = editorSceneObject.name;
                m_scenePath = "Assets/" + m_sceneFullName + ".unity";
            }
            else
            {
                m_sceneFullName = m_scenePath = m_sceneName = "";
            }
        }

        public void OnAfterDeserialize()
        {
        }

        [CustomPropertyDrawer(typeof(SceneId))]
        [CanEditMultipleObjects]
        public class Drawer : UnityEditor.PropertyDrawer
        {
            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {
                var objectProp = property.FindPropertyRelative("editorSceneObject");
                LinkedAssetMetasGUI.ObjectField(position, objectProp, typeof(SceneAsset), label);
                EditorGUI.BeginChangeCheck();
                if (EditorGUI.EndChangeCheck())
                {
                    var toPath = AssetDatabase.GetAssetPath(objectProp.objectReferenceValue);
                    foreach (var target in property.serializedObject.targetObjects)
                    {
                        var fromPath = AssetDatabase.GetAssetPath(target);
                        LinkedAssetMetas.AddLink(fromPath,toPath);                    
                    }               
                }
            }
        }
#endif
    }
}
