using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SharePoint;

namespace Infowise.Sharepoint.V3.Webparts.FeatureReceivers
{
    public class PeopleSearchFeatureReceiver : SPFeatureReceiver
    {
        #region base overrides

        public override void FeatureDeactivating(SPFeatureReceiverProperties properties)
        {
            Logger.EnterMethod(properties);

            try
            {
                SPSite site = properties.Feature.Parent as SPSite;

                using (SPWeb web = site.RootWeb)
                {
                    SPList list = web.GetCatalog(SPListTemplateType.WebPartCatalog);
                    SPField fldName = list.Fields[SPBuiltInFieldId.FileLeafRef];
                    string srpWpTitle = "InfowisePeopleSearch.webpart";
                    string selectQuery = string.Format(@"
                        <Where>
                          <Eq>
                            <FieldRef Name=""{0}""></FieldRef>
                            <Value Type=""Text"">{1}</Value>
                          </Eq>
                        </Where>", fldName.InternalName, srpWpTitle);

                    SPQuery query = new SPQuery()
                    {
                        Query = selectQuery
                    };

                    SPListItemCollection results = list.GetItems(query);

                    if (results == null || results.Count == 0)
                    {
                        Logger.Log("No webparts were found in the gallery, for query: {0}", selectQuery);
                        return;
                    }

                    foreach (SPListItem item in results)
                    {
                        Logger.Log("Removing {0} from the webparts gallery", item[fldName.Id].ToString());
                        list.Items.DeleteItemById(item.ID);
                    }
                }
            }
            catch (Exception exc)
            {
                Logger.Log("Error removing the webpart from webparts gallery");
                Logger.Log(exc);
            }
        }


        public override void FeatureActivated(SPFeatureReceiverProperties properties)
        {
            Logger.EnterMethod(properties);
        }

        public override void FeatureInstalled(SPFeatureReceiverProperties properties)
        {
            Logger.EnterMethod(properties);
        }

        public override void FeatureUninstalling(SPFeatureReceiverProperties properties)
        {
            Logger.EnterMethod(properties);
        }
        #endregion
    }
}
