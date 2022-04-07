using UnityEditor;
using UnityEngine;
using System;

namespace Test
{
    [System.Serializable]
    public unsafe struct Bytes
    {
        public BytesType type;
        public const int MaxInlineSize = 16;
        public fixed byte bytes[MaxInlineSize];
    }

    public enum BytesType
    {
        Int, Float,
    }

    public class BytesPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Using BeginProperty / EndProperty on the parent property means that
            // prefab override logic works on the entire property.
            EditorGUI.BeginProperty(position, label, property);

            // Draw label
            var typeProp = property.FindPropertyRelative(nameof(Bytes.type));
            var type = (BytesType) typeProp.intValue;
            var newType = (BytesType) EditorGUI.EnumPopup(position, type);

            var bytesProp = property.FindPropertyRelative(nameof(Bytes.bytes));
            bytesProp.ClearArray();


            if (type != newType)
            {
                Span<byte> bytes = stackalloc byte[Bytes.MaxInlineSize];

                for (int i = 0; i < Bytes.MaxInlineSize; i++)
                    bytes[i] = (byte) bytesProp.GetArrayElementAtIndex(i).intValue;
                
                static int DoStuff(System.Type type, Rect position, SerializedProperty prop, Span<byte> dest)
                {
                    if (type == typeof(int) || type == typeof(uint))
                    {
                        if (EditorGUI.PropertyField(position, prop))
                        {
                            int value = prop.intValue;

                            dest[0] = (byte)((uint)(value & 0x000000FF) >> 0);
                            dest[1] = (byte)((uint)(value & 0x0000FF00) >> 8);
                            dest[2] = (byte)((uint)(value & 0x00FF0000) >> 16);
                            dest[3] = (byte)((uint)(value & 0xFF000000) >> 24);

                            ForceLittleEndian32Bits(dest[0 .. 4]);
                        }
                        return 4;
                    }
                    else if (type == typeof(float))
                    {
                        if (EditorGUI.PropertyField(position, prop))
                        {
                            float value = prop.floatValue;

                        }
                    }
                    return 0;
                }

                // f
                static void ForceLittleEndian32Bits(Span<byte> span)
                {
                    if (!BitConverter.IsLittleEndian)
                    {
                        {
                            byte t = span[0];
                            span[0] = span[4];
                            span[4] = t;
                        }
                        {
                            byte t = span[1];
                            span[1] = span[2];
                            span[2] = t;
                        }
                    }
                }
            }
            
            EditorGUI.EndProperty();
        }
    }

    

    public class Thing : MonoBehaviour
    {
        public Bytes bytes;
    }
}