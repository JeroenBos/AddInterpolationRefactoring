using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AddInterpolationRefactoring
{
    public static class Contract
    {
        public static void Assert(bool requirement, string message = "Assertion failed")
        {
            if (!requirement)
            {
                throw new Exception(message);
            }
        }
    }
}
