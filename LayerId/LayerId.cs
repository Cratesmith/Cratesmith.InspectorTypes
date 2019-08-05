using System.Linq;
using UnityEditor;
using UnityEngine;
#if UNITY_EDITOR

#endif

namespace Cratesmith.InspectorTypes
{
    [System.Serializable]
    public class LayerId
    {
        public int value;
	
        public static implicit operator int(LayerId source)
        {
            return source.value;
        }
	
        public static implicit operator LayerId(int source)
        {
            return new LayerId() { value = source };
        }

        public LayerId()
        {
            value = 0;
        }

        public LayerId(string _layerName)
        {
            value = LayerMask.NameToLayer(_layerName);
        }

#if UNITY_EDITOR
        [CustomPropertyDrawer(typeof(LayerId))]
        public class LayerIdDrawer : PropertyDrawer 
        {
            public override void OnGUI (Rect position, SerializedProperty prop, GUIContent label) 
            {
                EditorGUI.BeginProperty (position, label, prop);

                var valueProp = prop.FindPropertyRelative("value");
                var names = Enumerable.Range(0, 31).Select(x => new GUIContent(LayerMask.LayerToName(x))).Where(x => !string.IsNullOrEmpty(x.text)).ToArray();
                var values = names.Select(x => LayerMask.NameToLayer(x.text)).ToArray();

                var currentValue = valueProp.intValue;

                if (valueProp.hasMultipleDifferentValues)
                {
                    currentValue = -1;
                }

                EditorGUI.BeginChangeCheck();
                var result = EditorGUI.IntPopup(position, label, currentValue, names, values);
                if (EditorGUI.EndChangeCheck())
                {
                    valueProp.intValue = result;
                }
		
                EditorGUI.EndProperty();
            }
        }
#endif
    }
}

