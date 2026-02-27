using UnityEngine;

namespace LoogaSoft.Inspector.Runtime
{
	public enum EditorColor 
    {
        Black,
        Blue,
        Gray,
        Green,
        Orange,
        Pink,
        Purple,
        Red,
        White
    }
    public static class EditorColorExtension
    {
        public static Color GetColor(this EditorColor color)
        {
            return color switch
            {
                EditorColor.Black => Color.black,
                EditorColor.Blue => Color.blue,
                EditorColor.Gray => Color.gray,
                EditorColor.Green => Color.green,
                EditorColor.Orange => Color.orange,
                EditorColor.Pink => Color.pink,
                EditorColor.Purple => Color.magenta,
                EditorColor.Red => Color.red,
                EditorColor.White => Color.white,
                _ => Color.gray
            };
        }
    }
}