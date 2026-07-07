namespace LoogaSoft.Inspector.Runtime
{
    public class DisableIfAttribute : EnableIfAttributeBase
    {
        public DisableIfAttribute(string condition) 
            : base(condition)
        {
            inverted = true;
        }
    }
}