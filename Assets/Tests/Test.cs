using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using BytesSerializationEditor;

public class Test
{
    static unsafe uint GetBitsOfFloat(float a)
    {
        uint result = *(uint*)&a;
        if (!Converter.IsLittleEndian)
            result = Converter.FlipByteOrder32Bit(result);
        return result;
    }

    [Test]
    public void FloatPartsCorrespondToStandardFloat_AssumingLittleEndianByteOrderForFloats_AnyIntegerEndianness()
    {
        {
            float a = -(1.0f + 1.0f / 2 + 1.0f / 4);
            uint bits = GetBitsOfFloat(a);
            var parts = Converter.Float32.GetParts(bits);
            Assert.AreEqual(1, parts.sign);
            Assert.AreEqual(127, parts.exponent);
            Assert.AreEqual(0b_110_0000_0000_0000_0000_0000, parts.significand);
        }
        {
            float a = float.PositiveInfinity;
            uint bits = GetBitsOfFloat(a);
            var parts = Converter.Float32.GetParts(bits);
            Assert.AreEqual(0, parts.sign);
            Assert.AreEqual(255, parts.exponent);
            Assert.AreEqual(0b_000_0000_0000_0000_0000_0000, parts.significand);
        }
        {
            float a = 0;
            uint bits = GetBitsOfFloat(a);
            var parts = Converter.Float32.GetParts(bits);
            Assert.AreEqual(0, parts.sign);
            Assert.AreEqual(0, parts.exponent);
            Assert.AreEqual(0b_000_0000_0000_0000_0000_0000, parts.significand);
        }
    }
    
    [Test]
    public void FloatEncodingWorks_AssumingLittleEndianByteOrderForFloats_AnyIntegerEndianness()
    {
        static void TestFor(float a)
        {
            uint bits = Converter.EncodeFloatByDefinition(a);
            uint actualBits = GetBitsOfFloat(a);
            Assert.AreEqual(actualBits, bits);
        }

        TestFor(0.0f);
        TestFor(1.0f);
        TestFor(-1.0f);
        TestFor(float.PositiveInfinity);
        TestFor(float.NegativeInfinity);
        TestFor(1.75f);
        TestFor(Mathf.Pow(2.0f, 32));
        TestFor(Mathf.Pow(2.0f, -32));
        TestFor(1.75f * Mathf.Pow(2.0f, -32));
    }

    [Test]
    public void FloatDecodingWorks_AssumingLittleEndianByteOrderForFloats_AnyIntegerEndianness()
    {
        static void TestFor(float a)
        {
            uint actualBits = GetBitsOfFloat(a);
            float f = Converter.DecodeFloatByDefinition(actualBits);
            Assert.AreEqual(a, f);
        }

        TestFor(0.0f);
        TestFor(1.0f);
        TestFor(-1.0f);
        TestFor(float.PositiveInfinity);
        TestFor(float.NegativeInfinity);
        TestFor(1.75f);
        TestFor(Mathf.Pow(2.0f, 32));
        TestFor(Mathf.Pow(2.0f, -32));
        TestFor(1.75f * Mathf.Pow(2.0f, -32));
    }

    [UnityTest]
    public IEnumerator TestWithEnumeratorPasses()
    {
        yield return null;
    }
}
