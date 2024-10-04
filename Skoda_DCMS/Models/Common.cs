using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.SharePoint.Client;

namespace Skoda_DCMS.Models
{
    public class Common
    {
        public ClientContext _context = new ClientContext(new Uri("http://win-6cgsdmg51od:8080/sites/DCMS/"));
        public string username = "cpu", password = "Mobinext@123";
    }
}