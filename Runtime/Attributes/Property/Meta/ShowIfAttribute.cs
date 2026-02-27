namespace LoogaSoft.Inspector.Runtime
{
    public class ShowIfAttribute : ShowIfAttributeBase
    {
        public ShowIfAttribute(string propertyName) : base(propertyName)
        {
            inverted = false;
        }
    }
}