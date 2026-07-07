using System;
using UnityEngine;

namespace LoogaSoft.Inspector.Runtime
{
    public class ShowIfAttributeBase : Attribute, ILoogaAttribute
    {
        public readonly string condition;
        public readonly string expectedValue;
        public readonly bool hasExpectedValue;
        public bool inverted;
        
        public ShowIfAttributeBase(string condition)
        {
            this.condition = condition;
        }

        public ShowIfAttributeBase(string condition, bool expectedValue)
        {
            this.condition = condition;
            this.expectedValue = expectedValue.ToString();
            hasExpectedValue = true;
        }

        public ShowIfAttributeBase(string condition, int expectedValue)
        {
            this.condition = condition;
            this.expectedValue = expectedValue.ToString();
            hasExpectedValue = true;
        }

        public ShowIfAttributeBase(string condition, float expectedValue)
        {
            this.condition = condition;
            this.expectedValue = expectedValue.ToString();
            hasExpectedValue = true;
        }

        public ShowIfAttributeBase(string condition, string expectedValue)
        {
            this.condition = condition;
            this.expectedValue = expectedValue;
            hasExpectedValue = true;
        }
    }
}
