namespace LoogaSoft.Inspector.Runtime
{
    public class ShowIfAttribute : ShowIfAttributeBase
    {
        public ShowIfAttribute(string propertyName) : base(propertyName)
        {
            inverted = false;
        }

        public ShowIfAttribute(string propertyName, bool expectedValue) : base(propertyName, expectedValue)
        {
            inverted = false;
        }

        public ShowIfAttribute(string propertyName, int expectedValue) : base(propertyName, expectedValue)
        {
            inverted = false;
        }

        public ShowIfAttribute(string propertyName, float expectedValue) : base(propertyName, expectedValue)
        {
            inverted = false;
        }

        public ShowIfAttribute(string propertyName, string expectedValue) : base(propertyName, expectedValue)
        {
            inverted = false;
        }
    }
}
