namespace LoogaSoft.Inspector.Runtime
{
    public class HideIfAttribute : ShowIfAttributeBase
    {
        public HideIfAttribute(string propertyName) : base(propertyName)
        {
            inverted = true;
        }
    }
}