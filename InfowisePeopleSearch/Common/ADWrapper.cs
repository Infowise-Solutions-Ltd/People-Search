using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.DirectoryServices;
using System.Collections;
using Infowise.Sharepoint.V3.Webparts.Common;
using System.DirectoryServices.ActiveDirectory;

namespace Infowise.Sharepoint.V3.Webparts
{
    class ADWrapper
    {
        internal static Dictionary<string, string> GetProfileProperties()
        {
            using (RevertToSelf rts = new RevertToSelf())
            {
                ActiveDirectorySchema currSchema = ActiveDirectorySchema.GetCurrentSchema();
                var person = currSchema.FindClass("user");
                List<string> props = person.MandatoryProperties.OfType<ActiveDirectorySchemaProperty>().Where(p => !p.IsDefunct).Select(p => p.Name).ToList();
                props.AddRange(person.OptionalProperties.OfType<ActiveDirectorySchemaProperty>().Where(p => !p.IsDefunct).Select(p => p.Name));
                RemoveBalckListedItems(props);
                return props.OrderBy(p => p).ToDictionary(p => p, p => p);
            }

            #region Old
            Dictionary<string, string> properties = new Dictionary<string, string>();
            string curUserAccountName = System.Web.HttpContext.Current.User.Identity.Name;
            curUserAccountName = curUserAccountName.Substring(curUserAccountName.LastIndexOf("\\") + 1);

            using (RevertToSelf rts = new RevertToSelf())
            {
                DirectoryEntry rootEntry = null;
                using (DirectoryEntry rootDSE = new DirectoryEntry("LDAP://RootDSE"))
                {
                    var rootContext = rootDSE.Properties["rootDomainNamingContext"].Value.ToString();
                    rootEntry = new DirectoryEntry("GC://" + rootContext);
                }


                DirectorySearcher sea = new DirectorySearcher(rootEntry);
                sea.Filter = "(sAMAccountName=" + curUserAccountName + ")";
                SearchResult seares = sea.FindOne();
                if (seares != null)
                {
                    var entry = seares.GetDirectoryEntry();
                    foreach (string propname in entry.Properties.PropertyNames)
                        properties.Add(propname, propname);

                    //ResultPropertyCollection prop = seares.Properties;
                    //ICollection coll = prop.PropertyNames;
                    //IEnumerator enu = coll.GetEnumerator();

                    //while (enu.MoveNext())
                    //{
                    //    properties.Add(enu.Current.ToString(), enu.Current.ToString());
                    //}
                }
            }

            var sortedDict = from entry in properties
                             orderby entry.Key ascending
                             select entry;

            return sortedDict.ToDictionary(x => x.Key, x => x.Value); 
            #endregion
        }
        private static string[] blackList = { "homeMTA", "instanceType", "objectCategory", "dSCorePropagationData", 
                            "aCSAggregateTokenRatePerUser", "aCSAllocableRSVPBandwidth","dc", "msExch*", "showInAddressBook",
                             "textEncodedORAddress", "sAMAccountType", "servicePrincipalName", "msNP*", "mSMQ*", "msIIS*", "msDS*", "msCOM*", "mDB*", "dLMem*", "badPwdCount", "badPasswordTime",
                                            "uSN*", "msRADIUS*", "mS-DS*", "frsComputerReferenceBL", "showInAdvancedViewOnly", "aCSPolicyName", "dBCSPwd", "kMServer", "isPrivilegeHolder",
                                            "lmPwdHistory", "logon*", "msRAS*", "objectClass","objectGUID", "deletedItemFlags", "allowedAttributes*", "attributeCertificate*", "bridgeheadServerListBL",
                                            "controlAccessRights", "defaultClassStore", "dSASignature", "flags", "garbageCollPeriod", "fSMORoleOwner", "jpegPhoto", "lastLog*","msDRM*", "partialAttribute*",
                                            "replicat*", "systemFlags", "supplementalCredentials", "accountNameHistory", "sIDHistory", "structuralObjectClass", "subSchemaSubEntry", "supportedAlgorithms", 
                                            "tokenGroups*", "userCert*", "userPassword", "userSMIMECertificate", "allowedChildClasses*", "dnQualifier", "dynamicLDAPServer", "authOrig*", "fRSMemberReferenceBL",
                                            "groupMembershipSAM", "isCriticalSystemObject", "labeledURI", "lockoutTime", "managedObjects", "modifyTimeStamp", "netbootSCPBL", "nTSecurityDescriptor",
                                            "ntPwdHistory", "nonSecurityMemberBL", "objectSid", "objectVersion", "ownerBL", "photo", "profilePath", "pwdLastSet", "sDRightsEffective", "serverReferenceBL",
                                            "siteObjectBL", "thumbnailPhoto", "unauthOrig*", "unicodePwd", "admin*", "securityProtocol", "securityIdentifier", "wbemPath", "userPKCS12", "userAccountControl",
                                            "proxiedObjectName", "replPropertyMetaData","replUpToDateVector", "protocolSettings", "queryPolicyBL", "publicDelegatesBL","submissionContLength", "displayName",
                                            "enabledProtocols", "groupPriority", "groupsToIgnore", "homeMDB", "isDeleted", "lastKnownParent", "maxStorage"};
        private static string curSearch = "";
        private static void RemoveBalckListedItems(List<string> props)
        {
            foreach (string term in blackList)
            {
                if (term.EndsWith("*"))
                {
                    curSearch = term.Replace("*", "");
                    props.RemoveAll(StartsWith);
                }
                else
                    if (props.Contains(term))
                        props.Remove(term);
            }
        }

        private static bool StartsWith(string term)
        {
            return term.StartsWith(curSearch, StringComparison.InvariantCultureIgnoreCase);
        }

    }
}
