using UnityEditor;
using UnityEngine;
using System;
using UnityEngine.Assertions;

namespace BytesSerializationEditor
{
    [CustomPropertyDrawer(typeof(Bytes))]
    public class BytesPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect allPosition, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(allPosition, label, property);

            Rect position = allPosition;
            position.height = EditorGUIUtility.singleLineHeight;

            property.isExpanded = EditorGUI.Foldout(position, property.isExpanded, label);
            position.y += EditorGUIUtility.singleLineHeight;

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;

                var typeProp = property.FindPropertyRelative(nameof(Bytes.type));
                var bytesProp = property.FindPropertyRelative(nameof(Bytes.bytes));

                var type = (BytesType) typeProp.intValue;
                BytesType newType;

                {
                    float height = EditorGUI.GetPropertyHeight(typeProp);
                    var pos = position;
                    pos.height = height;

                    EditorGUI.PropertyField(position, typeProp);
                    newType = (BytesType) typeProp.intValue;

                    if (newType != type)
                    {
                        for (int i = 0; i < Bytes.MaxInlineSize; i++)
                            bytesProp.GetFixedBufferElementAtIndex(i).intValue = 0;
                    }
                    
                    position.y += height;
                }

                Assert.AreEqual(Bytes.MaxInlineSize, bytesProp.fixedBufferSize);

                Span<byte> bytes = stackalloc byte[Bytes.MaxInlineSize];
                for (int i = 0; i < Bytes.MaxInlineSize; i++)
                    bytes[i] = (byte) bytesProp.GetFixedBufferElementAtIndex(i).intValue;

                {
                    Span<byte> bytes0 = bytes;
                    switch (type)
                    {
                        case BytesType.Int:
                        {
                            DoStuff("Int", typeof(int), ref position, ref bytes0);
                            break;
                        }
                        case BytesType.Float:
                        {
                            DoStuff("Float", typeof(float), ref position, ref bytes0);
                            break;
                        }
                    }
                }

                for (int i = 0; i < Bytes.MaxInlineSize; i++)
                    bytesProp.GetFixedBufferElementAtIndex(i).intValue = (int) bytes[i];

                EditorGUI.indentLevel--;
            }

            static void DoStuff(string name, System.Type type, ref Rect position, ref Span<byte> dest)
            {
                Rect toTheRigthOfLabel;
                {
                    var allSpace = position;
                    float heigth = EditorGUIUtility.singleLineHeight;
                    float width = heigth * 3;
                    allSpace.width = width;
                    EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), new GUIContent(name));

                    toTheRigthOfLabel = position;
                    toTheRigthOfLabel.width -= width;
                    toTheRigthOfLabel.x += width;

                    position.y += heigth;
                }

                if (type == typeof(int) || type == typeof(uint))
                {
                    var slice = dest[0 .. 4];
                    int a = Converter.DecodeInt(slice);
                    a = EditorGUI.IntField(toTheRigthOfLabel, a);
                    position.y += EditorGUIUtility.singleLineHeight;
                    Converter.EncodeInt(a, slice);
                    dest = dest[4 ..];
                }
                else if (type == typeof(float))
                {
                    var slice = dest[0 .. 4];

                    float a = Converter.DecodeFloat(slice);
                    a = EditorGUI.FloatField(toTheRigthOfLabel, a);
                    position.y += EditorGUIUtility.singleLineHeight;
                    Converter.EncodeFloat(a, slice);
                    dest = dest[4 ..];
                }
                else
                {
                    Assert.IsTrue(false, "This type is not yet supported.");
                }
            }
            
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.isExpanded)
                return EditorGUIUtility.singleLineHeight;
            return EditorGUIUtility.singleLineHeight * 10;
        }
    }

    

    public static class Converter
    {
        public static bool ShouldEndianFlipBytes => !BitConverter.IsLittleEndian;
        public static bool IsLittleEndian => BitConverter.IsLittleEndian;
        public static bool ShouldEncodeFloatsByDefinition => ShouldEndianFlipBytes;
        public static bool ShouldDecodeFloatsByDefinition => ShouldEndianFlipBytes;

        public static void MaybeFlipEndianness32Bit(Span<byte> span)
        {
            Assert.AreEqual(4, span.Length);

            if (BitConverter.IsLittleEndian)
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

        public static uint FlipByteOrder32Bit(uint a)
        {
            return (uint)(a & 0x000000FF) << 24
                | (uint)(a & 0x0000FF00) << 8
                | (uint)(a & 0x00FF0000) >> 8
                | (uint)(a & 0xFF000000) >> 24;
        }


        public static int GetIntLittleEndian(Span<byte> span)
        {
            Assert.AreEqual(4, span.Length);

            int a = 0;
            a |= ((int) span[0]) << 0;
            a |= ((int) span[1]) << 8;
            a |= ((int) span[2]) << 16;
            a |= ((int) span[3]) << 24;
            return a;
        }

        public static int GetIntBigEndian(Span<byte> span)
        {
            Assert.AreEqual(4, span.Length);

            int a = 0;
            a |= ((int) span[3]) << 0;
            a |= ((int) span[2]) << 8;
            a |= ((int) span[1]) << 16;
            a |= ((int) span[0]) << 24;
            return a;
        }
        
        public static int DecodeInt(Span<byte> span)
        {
            Assert.AreEqual(4, span.Length);

            if (ShouldEndianFlipBytes)
                return GetIntBigEndian(span);
            else
                return GetIntLittleEndian(span);
        }

        public static void SetIntLittleEndian(int a, Span<byte> span)
        {
            Assert.AreEqual(4, span.Length);

            span[0] = (byte)((uint)(a & 0x000000FF) >> 0);
            span[1] = (byte)((uint)(a & 0x0000FF00) >> 8);
            span[2] = (byte)((uint)(a & 0x00FF0000) >> 16);
            span[3] = (byte)((uint)(a & 0xFF000000) >> 24);
        }

        public static void SetIntBigEndian(int a, Span<byte> span)
        {
            Assert.AreEqual(4, span.Length);

            span[3] = (byte)((uint)(a & 0x000000FF) >> 0);
            span[2] = (byte)((uint)(a & 0x0000FF00) >> 8);
            span[1] = (byte)((uint)(a & 0x00FF0000) >> 16);
            span[0] = (byte)((uint)(a & 0xFF000000) >> 24);
        }

        public static void EncodeInt(int a, Span<byte> span)
        {
            Assert.AreEqual(4, span.Length);

            if (ShouldEndianFlipBytes)
                SetIntBigEndian(a, span);
            else
                SetIntLittleEndian(a, span);
        }

        public static class Float32
        {
            public const int numSignBits = 1;
            public const int numSignificandBits = 23;
            public const int numExponentBits = 8;
            
            public const int numBits = 32;
            
            public const int signBitOffset = numBits - numSignBits;
            public const int exponentBitOffset = numSignificandBits;
            public const int significandBitOffset = 0;
            
            public const int signBitShift = signBitOffset;
            public const int exponentBitShift = exponentBitOffset;
            public const int significandBitShift = significandBitOffset;

            public const uint signUnshiftedMask = (uint)((1 << numSignBits) - 1);
            public const uint significandUnshiftedMask = (uint)((1 << numSignificandBits) - 1);
            public const uint exponentUnshiftedMask = (uint)((1 << numExponentBits) - 1);

            public const uint signMask = (uint)(signUnshiftedMask << signBitShift);
            public const uint significandMask = (uint)(significandUnshiftedMask << significandBitOffset);
            public const uint exponentMask = (uint)(exponentUnshiftedMask << exponentBitOffset);

            public const uint smallestValueWithNormalizedExponent = 1;
            public const int exponentBias = (int) unchecked(exponentUnshiftedMask >> 1);

            public struct Parts
            {
                public int sign;
                public int exponent;
                public int significand;

                public Parts(int sign, int exponent, int significand)
                {
                    this.sign = sign;
                    this.exponent = exponent;
                    this.significand = significand;
                }

                public void Deconstruct(
                    out int sign,
                    out int exponent,
                    out int significand)
                {
                    sign = this.sign;
                    exponent = this.exponent;
                    significand = this.significand;
                }
            }

            public static uint GetBits(Parts parts)
            {
                return ((uint) (parts.sign << Float32.signBitShift) & Float32.signMask)
                    | ((uint) (parts.exponent << Float32.exponentBitShift) & Float32.exponentMask)
                    | ((uint) (parts.significand << Float32.significandBitShift) & Float32.significandMask);
            }

            public static Parts GetParts(uint bits)
            {
                Parts result;

                result.sign = (int)(((uint) bits & Float32.signMask) >> Float32.signBitShift);
                result.exponent = (int)(((uint) bits & Float32.exponentMask) >> Float32.exponentBitShift);
                result.significand = (int)(((uint) bits & Float32.significandMask) >> Float32.significandBitShift);

                return result;
            }
        }

        public static float DecodeFloatByDefinition(uint bits)
        {
            if (bits == 0)
                return 0;

            var (sign, biasedExponent, significand) = Float32.GetParts(bits);

            if (biasedExponent == Float32.exponentUnshiftedMask)
            {
                // infinity
                if (significand == 0)
                {
                    return sign > 0
                        ? float.NegativeInfinity
                        : float.PositiveInfinity;
                }
                
                // not a number
                else
                {
                    return float.NaN;
                }
            }

            float result = 1.0f;

            int unbiasedExponent = biasedExponent - Float32.exponentBias;
            result *= Mathf.Pow(2.0f, unbiasedExponent);
            result *= (float) significand * Mathf.Pow(2.0f, -Float32.numSignificandBits) + 1.0f;

            if (sign > 0)
                result = -result;
            return result;
        }


        public static unsafe float ReinterpretCastToFloat(uint bits)
        {
            return *(float*)&bits;
        }

        // I don't know how to determite whether the float is stored in little endian or big endian order.
        // I looking at a bit pattern example reliable?
        // So the current implementation just follows the float definition.
        public static float DecodeFloat(Span<byte> span)
        {
            Assert.AreEqual(4, span.Length);

            // short-circuit the direct cast.
            if (ShouldEncodeFloatsByDefinition)
            {
                return DecodeFloatByDefinition((uint) DecodeInt(span));
            }
            else
            {
                unsafe
                {
                    int a = DecodeInt(span);
                    return ReinterpretCastToFloat((uint) a);
                }
            }
        }

        public static uint EncodeFloatByDefinition(float a)
        {
            if (a == 0)
            {
                return 0;
            }

            int sign;

            if (a < 0)
            {
                sign = 1;
                a = -a;
            }
            else
            {
                sign = 0;
            }

            if (float.IsInfinity(a))
            {
                uint i = Float32.GetBits(new Float32.Parts(
                    sign: sign,

                    // all ones
                    exponent: (int) Float32.exponentUnshiftedMask,

                    significand: 0
                ));
                return i;
            }
            else if (float.IsNaN(a))
            {
                unchecked { return (uint) -1; }
            }
            
            int exponent = 127;
            if (a > 1.0f)
            {
                float t = a;
                while (true)
                {
                    t /= 2.0f;
                    if (t < 1.0f)
                        break;
                    a = t;
                    exponent += 1;
                }
            }
            else if (a < 1.0f)
            {
                do
                {
                    a *= 2.0f;
                    exponent -= 1;
                }
                while (a < 1.0f);
            }

            a -= 1.0f;

            float signficandWithIdentityExponent = a * Mathf.Pow(2.0f, Float32.numSignificandBits);
            int significand = (int) signficandWithIdentityExponent;

            uint result = Float32.GetBits(new Float32.Parts(sign, exponent, significand));
            return result;
        }

        public static void EncodeFloat(float a, Span<byte> span)
        {
            Assert.AreEqual(4, span.Length);

            if (!ShouldDecodeFloatsByDefinition)
            {
                unsafe
                {
                    EncodeInt(*(int*)&a, span);
                }
            }
            else
            {
                uint bits = EncodeFloatByDefinition(a);
                EncodeInt((int) bits, span);
            }
        }
    }
}