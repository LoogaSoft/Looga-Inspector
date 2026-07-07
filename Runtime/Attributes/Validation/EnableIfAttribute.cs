namespace LoogaSoft.Inspector.Runtime
{
    public class EnableIfAttribute : EnableIfAttributeBase
    {
        public EnableIfAttribute(string condition)
            : base(condition)
        {
            inverted = false;
        }
    }
}