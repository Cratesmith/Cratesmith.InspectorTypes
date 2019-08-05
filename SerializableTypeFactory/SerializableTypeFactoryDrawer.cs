/*
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

namespace Cratesmith
{
    [CustomPropertyDrawer(typeof(SerializableTypeFactoryBase), true)]
    public class SerializableTypeFactoryDrawer : UnityEditor.PropertyDrawer
    {
        const int HEADER_HEIGHT = 17;
        const int LINE_HEIGHT = 17;
        const int ERROR_LINE_HEIGHT = 51;

        string[] GetKeyTypes(System.Type keyType)
        {
            return GetType().Assembly.GetTypes().Where(keyType.IsAssignableFrom).Select(x=>x.FullName).ToArray();
        }

        System.Type GetKeyType(string keyTypeName)
        {
            return GetType().Assembly.GetType(keyTypeName);
        }

        public override float GetPropertyHeight(UnityEditor.SerializedProperty property, GUIContent label)
        {
            var target = property.serializedObject.targetObject;
            if (target == null)
            {
                return base.GetPropertyHeight(property,label);
            }
            var factory = fieldInfo.GetValue(target) as SerializableTypeFactoryBase;
            if (factory == null) 
            {
                return base.GetPropertyHeight(property,label);
            }

            var foldout = EditorPrefs.GetBool(property.propertyPath+"_foldout");
            if (foldout)
            {
                var yHeight = HEADER_HEIGHT;

                var table = GetTable(property);
                var keyTypes = GetKeyTypes(factory.KeyType);
                foreach (var keyTypeName in keyTypes)
                {
                    Object prefab = null;
                    var keyType = GetKeyType(keyTypeName);
                    yHeight += (table.TryGetValue(keyTypeName, out prefab) && !factory.CheckPrefabCompatibility(keyType, prefab))
                        ? ERROR_LINE_HEIGHT
                        : LINE_HEIGHT;                   
                }
                return yHeight;
            }
            else
            {
                return HEADER_HEIGHT;
            }
        }

        public override void OnGUI(Rect position, UnityEditor.SerializedProperty property, GUIContent label)
        {
            var target = property.serializedObject.targetObject;
            if (target == null) return;
            var factory = fieldInfo.GetValue(target) as SerializableTypeFactoryBase;
            if (factory == null) return;

            var factoryKeyType = factory.KeyType;
            var factoryObjectType = factory.ObjectType;

            var foldout = EditorPrefs.GetBool(property.propertyPath+"_foldout");

            EditorGUI.BeginChangeCheck();
            var foldoutRect = new Rect(position.position, new Vector2(position.width,LINE_HEIGHT));
            foldout = EditorGUI.Foldout(foldoutRect, foldout, label, true);
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetBool(property.propertyPath+"_foldout", foldout);
            }
            
            if (!foldout)
                return;

            position.yMin += LINE_HEIGHT;

            var table = GetTable(property);


            EditorGUI.BeginChangeCheck();
            var keyTypes = GetKeyTypes(factoryKeyType);
            ++UnityEditor.EditorGUI.indentLevel;
            float currentY = 0;
            for (int i = 0; i < keyTypes.Length; i++)
            {
                Object prevValue = null;
                table.TryGetValue(keyTypes[i], out prevValue);

                var rLabelField  = new Rect(position.position + new Vector2(0, currentY), new Vector2(UnityEditor.EditorGUIUtility.labelWidth, LINE_HEIGHT));
                var rObjectField = new Rect(position.position + new Vector2(UnityEditor.EditorGUIUtility.labelWidth, currentY), new Vector2(position.width-UnityEditor.EditorGUIUtility.labelWidth, LINE_HEIGHT));
                var rHelpBox     = new Rect(position.position + new Vector2(UnityEditor.EditorGUIUtility.labelWidth, currentY+LINE_HEIGHT), new Vector2(position.width-UnityEditor.EditorGUIUtility.labelWidth, ERROR_LINE_HEIGHT-LINE_HEIGHT));
            
                var keyName = keyTypes[i];
                if (keyName == factoryKeyType.Name)
                {
                    keyName = "<default>";
                }
                else if (keyName.StartsWith(factoryKeyType.Name))
                {
                    keyName = keyName.Substring(factoryKeyType.Name.Length).TrimStart(new char[] {' ','_','-'});
                }

                UnityEditor.EditorGUI.LabelField(rLabelField, keyName);   
                var newValue = UnityEditor.EditorGUI.ObjectField(rObjectField, prevValue, factoryObjectType, true);
                if (newValue != prevValue)
                {
                    if (newValue)
                    {
                        table[keyTypes[i]] = newValue;
                    }
                    else
                    {
                        table.Remove(keyTypes[i]);
                    }               
                }

                var keyType = GetKeyType(keyTypes[i]);
                if (prevValue!= null && !factory.CheckPrefabCompatibility(keyType, newValue))
                {
                    var errorString = string.Format("{0} is not compatible with {1}", 
                        prevValue.GetType().Name, 
                        factoryKeyType.Name);

                    UnityEditor.EditorGUI.HelpBox(rHelpBox, errorString, MessageType.Error);
                    currentY += ERROR_LINE_HEIGHT;
                }
                else
                {
                    currentY += LINE_HEIGHT;
                }
            }
            --UnityEditor.EditorGUI.indentLevel;

            if (EditorGUI.EndChangeCheck())
            {
                var typeIds = property.FindPropertyRelative("_typeIds");
                var prefabs = property.FindPropertyRelative("_prefabs");

                var kvp = table.ToArray();
                prefabs.arraySize = typeIds.arraySize = kvp.Length;
                for (int i = 0; i < kvp.Length; i++)
                {
                    typeIds.GetArrayElementAtIndex(i).stringValue = kvp[i].Key;
                    prefabs.GetArrayElementAtIndex(i).objectReferenceValue = kvp[i].Value;
                }          
            }
        }

        Dictionary<string, Object> GetTable(SerializedProperty property)
        {
            var table = new Dictionary<string, Object>();
            var target = property.serializedObject.targetObject;
            if (target != null)
            {
                var factory = fieldInfo.GetValue(target) as SerializableTypeFactoryBase;
                if (factory != null)
                {
                    var objectType = factory.ObjectType;
                    var typeIds = property.FindPropertyRelative("_typeIds");
                    var prefabs = property.FindPropertyRelative("_prefabs");
                    for (int i = 0; i < Mathf.Min(typeIds.arraySize, prefabs.arraySize); i++)
                    {
                        var typeId = typeIds.GetArrayElementAtIndex(i).stringValue;
                        var prefab = prefabs.GetArrayElementAtIndex(i).objectReferenceValue;
                        if (prefab != null && objectType.IsInstanceOfType(prefab))
                            table[typeId] = prefab;
                    }
                }
            }
            return table;
        }
    }
}
#endif
*/