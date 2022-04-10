using UnityEngine;
using System;
using UnityEngine.Assertions;

namespace BytesSerializationEditor
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

    public class Thing : MonoBehaviour
    {
        public Bytes bytes;
    }
}