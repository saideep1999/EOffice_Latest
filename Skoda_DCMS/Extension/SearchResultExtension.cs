using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Linq;
using System.Web;

namespace Skoda_DCMS.Extension
{
    public static class SearchResultExtension
    {
        public static bool ContainsProperty(this SearchResult searchResult, string propertyName)
        {
            if (searchResult.Properties.Contains(propertyName))
                return true;

            return false;
        }
    }
}