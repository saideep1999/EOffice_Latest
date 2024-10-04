using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using static Skoda_DCMS.Helpers.Flags;

namespace Skoda_DCMS.Helpers
{
    public class CustomException : Exception
    {
        public ExceptionType Type { get; set; }
    }
}