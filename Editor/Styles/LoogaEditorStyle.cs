using UnityEditor;
using UnityEngine;

namespace LoogaSoft.Inspector.Editor
{
    /// <summary>
    /// Shared colors and pixel helpers for Looga Inspector editor UI. Keep common styling here
    /// so foldouts, lists, catalogs, and tabs stay visually consistent.
    /// </summary>
    public static class LoogaEditorStyle
    {
        public const int AccentRailWidth = 4;

        public static Color BoxColor => SkinColor(
            new Color(0.192f, 0.192f, 0.192f, 1f),
            new Color(0.765f, 0.765f, 0.765f, 1f));

        public static Color AlternateBoxColor => SkinColor(
            new Color(0.225f, 0.225f, 0.225f, 1f),
            new Color(0.815f, 0.815f, 0.815f, 1f));

        public static Color HoverColor => SkinColor(
            new Color(0.275f, 0.275f, 0.275f, 1f),
            new Color(0.68f, 0.68f, 0.68f, 1f));

        public static Color AccentRailColor => SkinColor(
            new Color(0.38f, 0.38f, 0.38f, 1f),
            new Color(0.52f, 0.52f, 0.52f, 1f));

        public static Color ActionAccentColor => SkinColor(
            new Color(0.22f, 0.56f, 0.95f, 1f),
            new Color(0.16f, 0.45f, 0.78f, 1f));

        public static Color SeparatorColor => SkinColor(
            new Color(0.13f, 0.13f, 0.13f, 1f),
            new Color(0.55f, 0.55f, 0.55f, 1f));

        public static Color ArrowColor => SkinColor(
            new Color(0.68f, 0.68f, 0.68f, 1f),
            new Color(0.28f, 0.28f, 0.28f, 1f));

        public static Color DragHandleColor => SkinColor(
            new Color(0.48f, 0.48f, 0.48f, 1f),
            new Color(0.36f, 0.36f, 0.36f, 1f));

        public static Color ListRowColor => SkinColor(
            new Color(0.255f, 0.255f, 0.255f, 1f),
            new Color(0.76f, 0.76f, 0.76f, 1f));

        public static Color ListHoverColor => SkinColor(
            new Color(0.30f, 0.30f, 0.30f, 1f),
            new Color(0.82f, 0.82f, 0.82f, 1f));

        public static Color SelectionColor => SkinColor(
            new Color(0.18f, 0.42f, 0.72f, 1f),
            new Color(0.28f, 0.55f, 0.90f, 1f));

        public static Color TreeLineColor => SkinColor(
            new Color(0.37f, 0.37f, 0.37f, 1f),
            new Color(0.55f, 0.55f, 0.55f, 1f));

        public static Color TabBarColor => SkinColor(
            new Color(0.18f, 0.18f, 0.18f, 1f),
            new Color(0.68f, 0.68f, 0.68f, 1f));

        public static Color TabColor => SkinColor(
            new Color(0.192f, 0.192f, 0.192f, 1f),
            new Color(0.74f, 0.74f, 0.74f, 1f));

        public static Color SelectedTabColor => SkinColor(
            new Color(0.225f, 0.225f, 0.225f, 1f),
            new Color(0.82f, 0.82f, 0.82f, 1f));

        public static Color TabHoverColor => SkinColor(
            new Color(0.275f, 0.275f, 0.275f, 1f),
            new Color(0.86f, 0.86f, 0.86f, 1f));

        public static Color TextColor => EditorStyles.label.normal.textColor;

        public static Rect PixelSnap(Rect rect)
        {
            return Rect.MinMaxRect(
                PixelSnapValue(rect.xMin),
                PixelSnapValue(rect.yMin),
                PixelSnapValue(rect.xMax),
                PixelSnapValue(rect.yMax));
        }

        public static float PixelSnapValue(float value)
        {
            float pixelsPerPoint = EditorGUIUtility.pixelsPerPoint;
            return Mathf.Round(value * pixelsPerPoint) / pixelsPerPoint;
        }

        public static float PixelCeil(float value)
        {
            float pixelsPerPoint = EditorGUIUtility.pixelsPerPoint;
            return Mathf.Ceil(value * pixelsPerPoint) / pixelsPerPoint;
        }

        public static float Pixels(float pixelCount)
        {
            return pixelCount / EditorGUIUtility.pixelsPerPoint;
        }

        private static Color SkinColor(Color pro, Color personal)
        {
            return EditorGUIUtility.isProSkin ? pro : personal;
        }
    }
}