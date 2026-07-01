using UnityEditor;
using UnityEngine;

namespace LoogaSoft.Inspector.Editor
{
    internal static class NamedBitMaskFieldUtility
    {
        public static int DrawMaskField(Rect position, GUIContent label, int actualMask, string[] names, int[] bitIndices)
        {
            int displayedMask = ToDisplayedMask(actualMask, bitIndices);
            int nextDisplayedMask = EditorGUI.MaskField(position, label, displayedMask, names);
            return ToActualMask(nextDisplayedMask, bitIndices);
        }

        private static int ToDisplayedMask(int actualMask, int[] bitIndices)
        {
            int displayedMask = 0;

            for (int i = 0; i < bitIndices.Length; i++)
            {
                if ((actualMask & (1 << bitIndices[i])) != 0)
                    displayedMask |= 1 << i;
            }

            return displayedMask;
        }

        private static int ToActualMask(int displayedMask, int[] bitIndices)
        {
            int actualMask = 0;

            for (int i = 0; i < bitIndices.Length; i++)
            {
                if ((displayedMask & (1 << i)) != 0)
                    actualMask |= 1 << bitIndices[i];
            }

            return actualMask;
        }
    }
}
