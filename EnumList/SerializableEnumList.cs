using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
using System.Linq;
using System.Reflection;
#endif
using Cratesmith.Utils;
using UnityEngine;

namespace Cratesmith.InspectorTypes
{
    public class SerializableEnumList<TEnum, TData> : SerializableEnumListBase, IEnumerable<KeyValuePair<TEnum, TData>> where TEnum:Enum
    {
        [SerializeField] private TData[] m_values;
        public static Type EnumType				{ get { return typeof(TEnum); } }
        public static Type ValueType			{ get { return typeof(TData); } }

        public static TEnum[] EnumKeys			{ get { return s_enumKeys; } }
        public static string[] EnumNames		{ get { return s_enumNames; } }
        public static string[] EnumNamesLower	{ get { return s_enumNamesLower; } }
        public TEnum[] Keys						{ get { return s_enumKeys; } }
        public TData[] Values
        {
            get
            {
                if (m_values.Length != EnumKeys.Length)
                {
                    Array.Resize(ref m_values, EnumKeys.Length);
                }
                return m_values;
            }
        }

        private static readonly Dictionary<int, int>	s_intLookup;
        private static readonly Dictionary<TEnum, int>	s_enumValToValueIndex;
        private static readonly TEnum[]					s_enumKeys;
        private static readonly string[]				s_enumNames;
        private static readonly string[]				s_enumNamesLower;

        public void Add(KeyValuePair<TEnum, TData> _keyValue)
        {
            this[_keyValue.Key] = _keyValue.Value;
        }

        public static int GetIndex(TEnum @enum)
        {
            if (s_enumValToValueIndex.TryGetValue(@enum, out int result))
            {
                return result;
            }
            return -1;
        }

        public static int GetIndex(int intEnum)
        {
            if (s_intLookup.TryGetValue(intEnum, out int result))
            {
                return result;
            }
            return -1;
        }

        public static TEnum GetEnumAtIndex(int index)
        {		
            return s_enumKeys[index];
        }

        static SerializableEnumList()
        {
            var enumType = typeof(TEnum);
            var values = System.Enum.GetValues(enumType);
            s_enumValToValueIndex = new Dictionary<TEnum, int>();
            s_enumKeys = new TEnum[values.Length];
            s_enumNames = new string[values.Length];
            s_enumNamesLower = new string[values.Length];
            s_intLookup = new Dictionary<int, int>();

            IList list = values;
            for (int i = 0; i < list.Count; i++)
            {
                object val = list[i];
                var intVal = (int) val;
                var tval = (TEnum) val;
                s_intLookup[intVal] = i;
                s_enumValToValueIndex[tval] = i;
                s_enumKeys[i] = tval;
                s_enumNames[i] = tval.ToString();
                s_enumNamesLower[i] = tval.ToString().ToLowerInvariant();
            }
        }

        public SerializableEnumList()
        {
            m_values = new TData[s_enumKeys.Length];
        }

        public TData this[int intEnum]
        {
            get { return GetAtIndex(GetIndex(intEnum)); }
            set { SetAtIndex(GetIndex(intEnum),value); }
        }

        public TData this[TEnum @enum]
        {
            get { return GetAtIndex(GetIndex(@enum)); }
            set { SetAtIndex(GetIndex(@enum),value); }
        }

        public TData GetAtIndex(int index)
        {
            if (index < 0 || index >= Values.Length)
            {
                Debug.LogError($"SerializedEnumList<{nameof(TEnum)},{nameof(TData)}.GetAtIndex({index}) out of range. Has {Values.Length} items");
                return default;
            }
            return Values[index];
        }

	
        public void SetAtIndex(int index, TData value)
        {
            if (index < 0 || index >= Values.Length)
            {
                Debug.LogError($"SerializedEnumList<{nameof(TEnum)},{nameof(TData)}.SetAtIndex({index}, {value}) out of range. Has {Values.Length} items");
                return;
            }
            Values[index] = value;
        }

        #region IEnumerable 	
        public struct Enumerator : IEnumerator<KeyValuePair<TEnum, TData>>
        {
            private SerializableEnumList<TEnum, TData> list;
            private int index;

            public Enumerator(SerializableEnumList<TEnum, TData> _list)
            {
                list = _list;
                index = -1;
            }

            public bool MoveNext()
            {
                ++index;
                return index < list.Count;
            }

            public void Reset()
            {
                index = -1;
            }

            public KeyValuePair<TEnum, TData> Current
            {
                get { return new KeyValuePair<TEnum, TData>(list.Keys[index], list.Values[index]); }
            }
            object IEnumerator.Current => Current;

            public void Dispose()
            {
            }
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator<KeyValuePair<TEnum, TData>> IEnumerable<KeyValuePair<TEnum, TData>>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        #endregion

        public bool HasKey(TEnum key)
        {
            return s_enumValToValueIndex.ContainsKey(key);
        }

        public bool Contains(TData item)
        {
            return System.Array.IndexOf(Values,item) != -1;
        }

        public void CopyTo(TData[] array, int arrayIndex)
        {
            Values.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return Values.Length; }
        }

        public int IndexOf(TData item)
        {
            return System.Array.IndexOf(Values,item);
        }
    }

    public abstract class SerializableEnumListBase
    {
#if UNITY_EDITOR
        public static SerializedProperty Editor_GetArrayElementAtEnum(SerializedProperty property, int key, System.Type enumType)
        {
            var values = (int[])System.Enum.GetValues(enumType);
            for(int i=0; i<values.Length;++i)
            {
                if (values[i] != key)
                {
                    continue;
                }

                return property
                    .FindPropertyRelative("m_values")
                    .GetArrayElementAtIndex(i);
            }

            return null;
        }

        [CustomPropertyDrawer(typeof(SerializableEnumListBase),true)]
        public class Drawer : PropertyDrawer
        {
            private bool m_foldout;
            const int HEADER_HEIGHT = 17;
		
            public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            {
                var propertyType = property.GetSerializedPropertyType();
                var enumTypePI = propertyType.GetProperty("EnumType", BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy);
                var enumType = enumTypePI?.GetValue(null) as Type;
                if (enumType == null)
                {
                    return base.GetPropertyHeight(property,label);
                }

                m_foldout = EditorPrefs.GetBool(property.propertyPath+"_foldout");
                if (m_foldout)
                {
                    var arrayProp = property.FindPropertyRelative("m_values");
                    var enumNames = Enum.GetNames(enumType);

                    var height = 0f;
                    if (arrayProp != null)
                    {
                        for (int i = 0; i < Mathf.Min(enumNames.Length, arrayProp.arraySize); i++)
                        {
                            var enumName =  new GUIContent(enumNames[i]);
                            var arrayItemProp = arrayProp.GetArrayElementAtIndex(i);
                            if (arrayItemProp != null)
                            {
                                height +=  EditorGUI.GetPropertyHeight(arrayItemProp, enumName, true);
                            }
                        }
                    }
                    return HEADER_HEIGHT + height;
                }
                else
                {
                    return HEADER_HEIGHT;
                }
            }

            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {
                var propertyType = property.GetSerializedPropertyType();
                var enumTypePI = propertyType.GetProperty("EnumType", BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy);
                var enumType = enumTypePI?.GetValue(null) as Type;
                if (enumType==null)
                {
                    Debug.LogError($"SerializableEnumList Drawer: {property.serializedObject.targetObject.name} can't read Enum Type! Is it public?");
                    return;
                }

                var arrayProp = property.FindPropertyRelative("m_values");
                if (arrayProp==null)
                {
                    Debug.LogError($"SerializableEnumList Drawer: {property.serializedObject.targetObject.name} has a non serializable value type!");				
                    return;
                }

                var enumNames = Enum.GetNames(enumType);
                arrayProp.arraySize = enumNames.Length;

                EditorGUI.BeginChangeCheck();
                var foldoutRect = new Rect(position.position, new Vector2(position.width,HEADER_HEIGHT));
                m_foldout = EditorGUI.Foldout(foldoutRect, m_foldout, label, true);
                position.yMin += HEADER_HEIGHT;
                if (EditorGUI.EndChangeCheck())
                {
                    EditorPrefs.SetBool(property.propertyPath+"_foldout", m_foldout);
                }
            
                if (!m_foldout)
                    return;

                ++EditorGUI.indentLevel;
                for (int i = 0; i < enumNames.Length; i++)
                {
                    var enumName =  new GUIContent(enumNames[i]);
                    var arrayItemProp = arrayProp.GetArrayElementAtIndex(i);
                    var rPropertyField  = new Rect(position.position, new Vector2(position.width, EditorGUI.GetPropertyHeight(arrayItemProp, enumName, true)));
                    EditorGUI.PropertyField(rPropertyField, arrayItemProp, new  GUIContent(enumName), true);
				
                    position.yMin += rPropertyField.height;
                }
                --EditorGUI.indentLevel;
            }

        }
#endif
    }
}