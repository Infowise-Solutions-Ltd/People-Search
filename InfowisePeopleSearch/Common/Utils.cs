using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SharePoint.Utilities;
using Microsoft.SharePoint;

namespace Infowise.Sharepoint.V3.Webparts.Common
{
    static class Utils
    {
        /// <summary>
        /// Gets localized string by key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        internal static string GetString(string key)
        {
            uint lcid = 1033;
            if (SPContext.Current != null)
                lcid = SPContext.Current.Web.Language;
            return SPUtility.GetLocalizedString("$Resources:" + key, "Infowise.PeopleSearch", lcid);
        }

        /// <summary>
        /// Gets localized string by key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        internal static string GetString(string key, uint lcid)
        {
            return SPUtility.GetLocalizedString("$Resources:" + key, "Infowise.PeopleSearch", lcid);
        }
    }
}
