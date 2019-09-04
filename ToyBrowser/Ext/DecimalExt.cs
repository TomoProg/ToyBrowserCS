using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToyBrowser
{
    namespace Ext
    {
        public static class DecimalExt
        {
            public static decimal Adjust(this decimal d, decimal min, decimal max)
            {
                if(min > max)
                {
                    throw new ArgumentException("The min value must be less than or equal max value");
                }
                if(d < min)
                {
                    return min;
                }
                if(d > max)
                {
                    return max;
                }
                return d;
            }
        }
    }
}
