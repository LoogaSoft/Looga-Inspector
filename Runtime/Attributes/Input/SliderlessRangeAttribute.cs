using UnityEngine;

namespace LoogaSoft.Inspector.Runtime
{
    public class SliderlessRangeAttribute : PropertyAttribute, ILoogaAttribute
    {
        public double min;
        public double max;
        
        public SliderlessRangeAttribute(float min, float max)
        {
            this.min = min;
            this.max = max;
        }

        public SliderlessRangeAttribute(double min, double max)
        {
            this.min = min;
            this.max = max;
        }
    }
}
