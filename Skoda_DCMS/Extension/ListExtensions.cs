using Microsoft.SharePoint.Client;
using Skoda_DCMS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Skoda_DCMS.Extension
{
    public static class ListExtensions
    {
        public static bool ContainsField(this List list, string fieldName)
        {
            var ctx = list.Context;
            var result = ctx.LoadQuery(list.Fields.Where(f => f.InternalName == fieldName));
            ctx.ExecuteQuery();
            return result.Any();
        }

        public static int ContainsAllLevels(this List<ApprovalMatrix> approversList)
        {
            var appLevelList = approversList.Select(x => x.ApprovalLevel).Distinct();
            var appL = appLevelList.ToList();
            var levelList = Enumerable.Range(1,
                appLevelList.Max());

            var lvlList = levelList.ToList();

            if (appL.Count() == lvlList.Count())
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }

        public static int CheckApproverUserName(this List<ApprovalMatrix> approversList)
        {
            int checkCount = 0;
            for (int i = 0; i < approversList.Count; i++)
            {
                if (string.IsNullOrEmpty(approversList[i].ApproverUserName))
                {
                    checkCount = 0;
                    break;
                }
                else
                {
                    checkCount = 1;
                }
            }

            return checkCount;
        }
    }
}