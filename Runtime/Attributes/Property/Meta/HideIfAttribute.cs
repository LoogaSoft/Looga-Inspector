namespace LoogaSoft.Inspector.Runtime
{
    public class HideIfAttribute : ShowIfAttributeBase
    {
        public HideIfAttribute(string propertyName) : base(propertyName)
        {
            inverted = true;
        }

        public HideIfAttribute(string propertyName, bool expectedValue) : base(propertyName, expectedValue)
        {
            inverted = true;
        }

        public HideIfAttribute(string propertyName, int expectedValue) : base(propertyName, expectedValue)
        {
            inverted = true;
        }

        public HideIfAttribute(string propertyName, float expectedValue) : base(propertyName, expectedValue)
        {
            inverted = true;
        }

        public HideIfAttribute(string propertyName, string expectedValue) : base(propertyName, expectedValue)
        {
            inverted = true;
        }
    }
}
