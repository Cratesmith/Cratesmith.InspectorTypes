using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Cratesmith.Utils;
using UnityEditor;
using UnityEditor.Experimental;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.XR.WSA;
using Object = UnityEngine.Object;

namespace Cratesmith.InspectorTypes
{
    [System.Serializable]
    public class ResourceId<T> : ResourceIdBase
#if UNITY_EDITOR
        , ISerializationCallbackReceiver
#endif
        where T : Object
    {
        [SerializeField] T m_value = default(T);
        [SerializeField] string m_resourcePath = string.Empty;

        public T value
        {
            get { return m_value; }
        }

        public string path
        {
            get
            {
#if UNITY_EDITOR
                if (m_value != null && string.IsNullOrEmpty(m_resourcePath))
                {
                    m_resourcePath = GetResourcePath(AssetDatabase.GetAssetPath(m_value));
                }
#endif

                if (m_value != null && string.IsNullOrEmpty(m_resourcePath))
                {
                    Debug.LogErrorFormat("{0} is not a resource!", m_value);
                }

                return m_resourcePath;
            }
        }

#if UNITY_EDITOR
        public static Y Build<Y>(T prefab) where Y:ResourceId<T>,new()
        {
            return new Y
            {
                m_value = prefab,
                m_resourcePath = GetResourcePath(AssetDatabase.GetAssetPath(prefab))
            };
        }
#endif

        public static implicit operator T(ResourceId<T> @this)
        {
            return @this.value;
        }

        public static implicit operator bool(ResourceId<T> @this)
        {
            return @this.value!=null;
        }

        public static implicit operator string(ResourceId<T> @this)
        {
            return @this.path;
        }

        public Param ToParam()
        {
            return new Param(value, path);
        }

        public static Param ToParam<Y>(ResourceId<Y> from) where Y:T, new()
        {
            return new Param(from.value, from.path);
        }

#if UNITY_EDITOR
        public void OnBeforeSerialize()
        {
            m_resourcePath = GetResourcePath(AssetDatabase.GetAssetPath(m_value));
        }
    
        public void OnAfterDeserialize()
        {
        }
#endif

        public struct Param
        {
            public readonly T value;
            public readonly string path;

            public Param(T _value, string _path)
            {
                value = _value;
                path = _path;
            }
        }
    }

#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    [Serializable]
    public abstract class ResourceIdBase
    {
#if UNITY_EDITOR
        [UnityEditor.Callbacks.DidReloadScripts]
        private static void DidReloadScripts()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer)
            {
                DoResourceCheck();            
            }
        }

        protected static readonly HashSet<ResourceIdBase> s_allResourceIds = new HashSet<ResourceIdBase>();
    
        private static void DoResourceCheck()
        {
            var resourcePaths = AssetDatabase.GetAllAssetPaths().Where(x=>System.IO.File.Exists(x) && x.Replace("\\","/").IndexOf("/Resources/", StringComparison.CurrentCultureIgnoreCase)!=-1).ToArray();        
            var resourceFiles = resourcePaths.Select(x=>new{asset=x, path=GetResourcePath(x), ext=Path.GetExtension(x)}).ToArray();
            var duplicates = resourceFiles.GroupBy(x => x.path + x.ext).Where(x=>x.Count() > 1).ToArray();
            
            foreach (var duplicate in duplicates)
            {
                var paths = string.Join(",", duplicate.Select(x => x.asset).ToArray());
                Debug.LogErrorFormat("Resource name collision for \"{0}\" caused by the following assets: {1}", duplicate.Key, paths);
            }
        }

        protected static string GetResourcePath(string assetPath)
        {
            assetPath = assetPath.Replace(@"\", "/");
            var startPos = assetPath.LastIndexOf("/Resources/", StringComparison.CurrentCultureIgnoreCase);
            if (startPos == -1)
            {
                return "";
            }

            var resourcePath = assetPath.Substring(startPos + "/Resources/".Length);
            return Path.Combine(Path.GetDirectoryName(resourcePath),Path.GetFileNameWithoutExtension(assetPath));
        }

        [CustomPropertyDrawer(typeof(ResourceIdBase), true)]
        [CanEditMultipleObjects]
        public class Drawer : UnityEditor.PropertyDrawer
        {
            const int ERROR_LINE_HEIGHT = 31;
            const int LINE_HEIGHT = 17;

            System.Type GetResourceIdType(SerializedProperty property)
            {
                var type = property.GetSerializedPropertyType();
                while (type != null)
                {
                    if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ResourceId<>))
                    {
                        return type.GetGenericArguments()[0];
                    }
                    type = type.BaseType;
                }
                return null;
            }

            public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            {
                return LINE_HEIGHT + ERROR_LINE_HEIGHT;
            }

            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {
                var boxRect = EditorGUI.IndentedRect(new Rect(position.x - 2, position.y - 2, position.width + 2, LINE_HEIGHT + ERROR_LINE_HEIGHT + 4));
                var propRect = new Rect(position.x, position.y, position.width, LINE_HEIGHT);
                var helpRect = EditorGUI.IndentedRect(new Rect(propRect.x + 2, propRect.yMax + 2, propRect.width - 4, ERROR_LINE_HEIGHT - 4));

                var objectProp = property.FindPropertyRelative("m_value");
                var resourcePathProp = property.FindPropertyRelative("m_resourcePath");

                var isError = (resourcePathProp.stringValue == "" && objectProp.objectReferenceValue != null);

                var prevColor = GUI.color;
                GUI.color = isError
                    ? Color.red
                    : Color.white;
                GUI.Box(boxRect, "");
                GUI.color = prevColor;

                LinkedAssetMetasGUI.ObjectField(propRect, objectProp, GetResourceIdType(property), label);

                if (objectProp.hasMultipleDifferentValues)
                {
                    UnityEditor.EditorGUI.HelpBox(helpRect, string.Format("Multple values selected"), MessageType.Info);
                }
                else if (isError)
                {
                    UnityEditor.EditorGUI.HelpBox(helpRect, "This asset is not in a Resources folder!", MessageType.Error);
                }
                else
                {
                    UnityEditor.EditorGUI.HelpBox(helpRect, string.Format("Resource path: \"{0}\"", resourcePathProp.stringValue), MessageType.Info);
                }
            }

        
        }    
#endif
    }
}