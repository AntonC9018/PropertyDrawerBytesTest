using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

namespace BytesSerializationEditor
{
    [System.Serializable]
    public unsafe struct TaggedUnion16Bytes<KeyType>
    {
        public KeyType key;
        internal fixed byte _bytes[16];
    }

    public class CorrespondsToKeyAttribute : Attribute
    {
        public int[] Keys { get; }

        public CorrespondsToKeyAttribute(params int[] keys)
        {
            Keys = keys;
        }
    }

    public static class Helper
    {
        // TODO: copy the iterator code
        public static void ForAllSetBitIndices(int bits, Action<int> thing)
        {
            Assert.AreNotEqual(0, bits);

            int index = 0;
            while (bits > 0)
            {
                if ((bits & 1) == 1)
                    thing(index);

                bits >>= 1;
                index++;
            }
        }
    }

    public class Workaround<T> : UnityEngine.Object
    {
        public T[] array;
    }

    public class BytesVersatilePropertyDrawer<TTaggedUnionKeyType, TTaggedUnionValueType> : PropertyDrawer
    {
        private Workaround<TTaggedUnionValueType> _selectedValues;
        private byte[] _cannotUseStackallocWorkaroundBuffer;
        private static readonly string[] _PropertyNamesByKeyIndex;

        
        static BytesVersatilePropertyDrawer()
        {
            var keyNames = typeof(TTaggedUnionKeyType).GetEnumNames();
            var numKeys = keyNames.Length;
            var propNamesByKeyIndex = new string[numKeys];

            // TODO: handle flags = multiple elements in fixed size buffer. why the hell not.
            foreach (var m in typeof(TTaggedUnionValueType).GetMembers())
            {
                var attribute = m.GetCustomAttribute<CorrespondsToKeyAttribute>();
                foreach (int index in attribute.Keys)
                {
                    ref var propName = ref propNamesByKeyIndex[index];
                    Assert.IsNull(propName);
                    propName = m.Name;
                }
            }

            foreach (var name in propNamesByKeyIndex)
                Assert.IsNotNull(name);
        }

        public override void OnGUI(Rect allPosition, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(allPosition, label, property);

            var position = allPosition;
            position.height = EditorGUIUtility.singleLineHeight;

            if (_selectedValues == null)
                _selectedValues = new Workaround<TTaggedUnionValueType>();

            var serializedLocalObject = new SerializedObject(_selectedValues);
            var serializedArrayProp = serializedLocalObject.FindProperty("array");

            if (property.isArray)
            {
                Array.Resize(ref _selectedValues.array, property.arraySize);

                for (int i = 0; i < property.arraySize; i++)
                {
                    var elementProperty = property.GetArrayElementAtIndex(i);
                    var keyProp = elementProperty.FindPropertyRelative("key");
                    {
                        var heightOfKey = EditorGUI.GetPropertyHeight(keyProp, includeChildren: true);
                        var keyPosition = position;
                        keyPosition.height = heightOfKey; 
                        EditorGUI.PropertyField(position, keyProp);
                        position.y += heightOfKey;
                    }

                    int currentKey = keyProp.intValue;
                    // No property selected
                    if (currentKey == 0)
                        continue;

                    var selectedPropName = _PropertyNamesByKeyIndex[currentKey];
                    var localValueProp = serializedArrayProp.GetArrayElementAtIndex(i);
                    var selectedProp = localValueProp.FindPropertyRelative(selectedPropName);

                    {
                        var height = EditorGUI.GetPropertyHeight(selectedProp, includeChildren: true);
                        var pos = position;
                        pos.height = height; 
                        EditorGUI.PropertyField(position, selectedProp);
                        position.y += height;
                    }

                    object newValue = typeof(TTaggedUnionValueType)
                        .GetField(selectedPropName)
                        .GetValue(_selectedValues.array[i]);

                    var bytesProp = elementProperty.FindPropertyRelative("_bytes");
                    int length = bytesProp.fixedBufferSize;
                    _cannotUseStackallocWorkaroundBuffer ??= new byte[length];
                    for (int j = 0; j < length; j++)
                        _cannotUseStackallocWorkaroundBuffer[j] = (byte) bytesProp.GetArrayElementAtIndex(j).intValue;
                    // newValue
                }
            }

            EditorGUI.EndProperty();
        }

        public void DrawGUISingle(int index, ref Rect position)
        {
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight * 10;
        }
    }
}