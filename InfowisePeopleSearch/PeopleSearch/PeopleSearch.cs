using System;
using System.Runtime.InteropServices;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.ComponentModel;
using System.Xml;
using System.Linq;
using System.DirectoryServices;
using System.Collections.Generic;
using Infowise.Sharepoint.V3.Webparts.Common;
using Microsoft.SharePoint;

namespace Infowise.Sharepoint.V3.Webparts
{
    /// <summary>
    /// Searches users in Active Directory
    /// </summary>
    [Guid("c867485b-d561-4394-b216-b3f3daacb181")]
    public class PeopleSearch : System.Web.UI.WebControls.WebParts.WebPart
    {
        
        #region Members
        private bool useAjax = false;

        private TextBox txtSearch = null;
        private Button btnSearch = null;
        private Panel pnlResults = null;
        private UpdatePanel up = null;

        bool showSearchButton;
        #endregion

        #region Public Properties
        [WebBrowsable(false),
        Personalizable(PersonalizationScope.Shared)]
        public string Path
        {
            get;
            set;
        }

        
        [WebBrowsable(false),
        Personalizable(PersonalizationScope.Shared)]
        public string ResultFields
        {
            get;
            set;
        }

        bool showImages = true;
        [WebBrowsable(false), Personalizable(PersonalizationScope.Shared)]
        public bool ShowImages
        {
            get { return showImages; }
            set { showImages = value; }
        }

        [WebBrowsable(false), Personalizable(PersonalizationScope.Shared)]
        public bool ShowSearchButton
        {
            get { return showSearchButton; }
            set { showSearchButton = value; }
        }

        string searchButtonText = null;
        [WebBrowsable(false), Personalizable(PersonalizationScope.Shared)]
        public string SearchButtonText
        {
            get {
                if(searchButtonText == null)
                    searchButtonText = Utils.GetString("Search");

                return searchButtonText;
            }
            set { searchButtonText = value; }
        }

        bool showLabels = false;
        [WebBrowsable(false), Personalizable(PersonalizationScope.Shared)]
        public bool ShowLabels
        {
            get { return showLabels; }
            set { showLabels = value; }
        }

        string imgFolderUrl = string.Empty;
        [WebBrowsable(false),
 Personalizable(PersonalizationScope.Shared)]
        public string ImageFolderUrl
        {
            get { return imgFolderUrl; }
            set { imgFolderUrl = value; }
        }

        [WebBrowsable(false),
Personalizable(PersonalizationScope.Shared)]
        public string ImageField
        {
            get;
            set;
        }

        [WebBrowsable(false),
Personalizable(PersonalizationScope.Shared)]
        public string ImageExtension
        {
            get;
            set;
        }

        [WebBrowsable(false),
         Personalizable(PersonalizationScope.Shared)]
        public bool AddExtension
        {
            get;
            set;
        }

        string imageSize = "72px";
        [WebBrowsable(false),
        Personalizable(PersonalizationScope.Shared)]
        public string ImageSize
        {
            get
            {
                return imageSize;
            }
            set
            {
                if (!string.IsNullOrEmpty(value))
                    imageSize = value;
            }
        }

        private int maxResults = 10;
        [WebBrowsable(false),
        Personalizable(PersonalizationScope.Shared)]
        public int MaxResults
        {
            get
            {
                return maxResults;
            }
            set
            {
                maxResults = value;
            }
        }

        [WebBrowsable(false),
Personalizable(PersonalizationScope.Shared)]
        public bool RequireExchangeAccount
        {
            get;
            set;
        }

        #endregion


        List<PropertyInfo> resultProperties = null;

        private List<PropertyInfo> ResultProperties
        {
            get
            {
                Logger.EnterMethod();

                if (resultProperties == null)
                {
                    resultProperties = new List<PropertyInfo>();

                    try
                    {
                        XmlDocument xmlDoc = new XmlDocument();
                        xmlDoc.LoadXml(ResultFields);


                        for (int i = 0; i < xmlDoc.DocumentElement.ChildNodes.Count; i++)
                        {
                            XmlElement elChild = xmlDoc.DocumentElement.ChildNodes[i] as XmlElement;
                            PropertyInfo curProp = new PropertyInfo();
                            curProp.DisplayName = elChild.GetAttribute(FieldSelector.ATT_NAME);
                            curProp.FieldName = elChild.GetAttribute(FieldSelector.ATT_INTERNALNAME);
                            curProp.Order = int.Parse(elChild.GetAttribute(FieldSelector.ATT_ORDER));
                            resultProperties.Add(curProp);
                        }
                    }
                    catch { }
                }

                return resultProperties;

            }
        }

        internal void Recreate()
        {
            Controls.Clear();
            CreateChildControls();
        }

        protected override void CreateChildControls()
        {
            Logger.EnterMethod();

            try
            {
                base.CreateChildControls();

                if (ShowImages)
                {
                    string javaFunction = "function LoadFail(oImage){if (oImage != null && typeof(oImage) != 'undefined')oImage.src='/_layouts/images/person.gif';}";
                    this.Page.ClientScript.RegisterClientScriptBlock(GetType(), "imageFailScript", javaFunction, true);
                }

                useAjax = ScriptManager.GetCurrent(Page) != null;


                Table tblMain = new Table();
                tblMain.CssClass = "iw-pplsearch";
                Controls.Add(tblMain);

                if (!string.IsNullOrEmpty(Title))
                {
                    TableRow firstRow = new TableRow();
                    tblMain.Rows.Add(firstRow);

                    TableCell firstCell = new TableCell();
                    firstRow.Cells.Add(firstCell);

                    firstCell.CssClass = "ms-WPTitle";
                    firstCell.Text = Title;
                }

                TableRow secondRow = new TableRow();
                tblMain.Rows.Add(secondRow);

                TableCell secondCell = new TableCell();
                secondRow.Cells.Add(secondCell);

                txtSearch = new TextBox();
                txtSearch.CssClass = "UserInput iwps-searchtxt";
                secondCell.Wrap = false;
                secondCell.Controls.Add(txtSearch);

                btnSearch = new Button();
                btnSearch.Click += new EventHandler(btnSearch_Click);
                btnSearch.ID = "btnSearch";
                btnSearch.CssClass = "iwps-searchbtn";
                btnSearch.Text = SearchButtonText;
                if (!ShowSearchButton)
                    btnSearch.Style.Add(HtmlTextWriterStyle.Display, "none");
                secondCell.Controls.Add(btnSearch);

                txtSearch.Attributes.Add("onkeypress", string.Format("if(event.keyCode == 13 && this.value != '') document.getElementById('{0}').click()", btnSearch.ClientID));

                if (useAjax)
                {
                    up = new UpdatePanel();
                    up.UpdateMode = UpdatePanelUpdateMode.Conditional;
                    up.ChildrenAsTriggers = false;
                    Controls.Add(up);

                    UpdateProgress uProg = new UpdateProgress();
                    uProg.ID = "uProg";
                    uProg.AssociatedUpdatePanelID = up.ID;
                    uProg.ProgressTemplate = new ProgressTemplate();
                    uProg.Controls.Add(new Literal() { Text = "&nbsp;" });
                    uProg.Controls.Add(new Image() { ImageUrl = Page.ClientScript.GetWebResourceUrl(GetType(), "Infowise.Sharepoint.V3.Webparts.Common.iwatf-loading.gif") });
                    secondCell.Controls.Add(uProg);
                }

                pnlResults = new Panel();
                pnlResults.Style.Add(HtmlTextWriterStyle.Position, "absolute");
                pnlResults.Style.Add(HtmlTextWriterStyle.Display, "none");
                pnlResults.Style.Add(HtmlTextWriterStyle.ZIndex, "10");
                pnlResults.ID = "pnlResults";
                pnlResults.EnableViewState = false;
                pnlResults.CssClass = "iw-pplsearch-resultpane";
                pnlResults.BackColor = System.Drawing.Color.White;
                pnlResults.Style.Add("border", "1px solid #CCCCCC");

                if (useAjax)
                {
                    up.ContentTemplateContainer.Controls.Add(pnlResults);
                    ScriptManager.GetCurrent(Page).RegisterAsyncPostBackControl(btnSearch);
                }
                else
                    Controls.Add(pnlResults);
            }
            catch (Exception ex)
            {
                Controls.Add(new Label() { CssClass = "ms-error", Text = ex.Message });
            }
        }

        public override EditorPartCollection CreateEditorParts()
        {
            Logger.EnterMethod();

            List<EditorPart> editors = new List<EditorPart>();

            // create custom settings editor parts
            PeopleSearchEditor viewPart = new PeopleSearchEditor();
            viewPart.ID = this.ID + "_editorPart";
            editors.Add(viewPart);

            EditorPartCollection editorParts = new EditorPartCollection(editors);

            return editorParts;
        }

        void btnSearch_Click(object sender, EventArgs e)
        {
            Logger.EnterMethod();

            try
            {
                if (string.IsNullOrEmpty(txtSearch.Text))
                {
                    pnlResults.Controls.Clear();
                    if(useAjax)
                        up.Update();
                    return;
                }

                pnlResults.Style.Add(HtmlTextWriterStyle.Display, "");

                DirectorySearcher theSearcher = null;
                DirectoryEntry rootEntry = null;

                using (RevertToSelf rev = new RevertToSelf())
                {
                    if (string.IsNullOrEmpty(Path))
                    {
                        using (DirectoryEntry rootDSE = new DirectoryEntry("LDAP://RootDSE"))
                        {
                            var rootContext = rootDSE.Properties["rootDomainNamingContext"].Value.ToString();
                            rootEntry = new DirectoryEntry("GC://" + rootContext);
                        }
                    }
                    else
                    {
                        rootEntry = new DirectoryEntry(Path);
                    }

                    Logger.Log("Search root: " + rootEntry.Path);
                    theSearcher = new DirectorySearcher(rootEntry);
                    theSearcher.SearchScope = SearchScope.Subtree;

                    foreach (PropertyInfo prop in ResultProperties)
                        theSearcher.PropertiesToLoad.Add(prop.FieldName);

                    if (!theSearcher.PropertiesToLoad.Contains("displayName"))
                        theSearcher.PropertiesToLoad.Add("displayName");

                    if (ShowImages && !theSearcher.PropertiesToLoad.Contains(ImageField))
                        theSearcher.PropertiesToLoad.Add(ImageField);

                    theSearcher.Filter = string.Format("(&(objectCategory=person)(|(anr={0})(title=*{0}*)(company=*{0}*)(department=*{0}*)(co=*{0}*)(l=*{0}*)(description=*{0}*)){1})", txtSearch.Text, RequireExchangeAccount ? "(mail=*)(!msExchHideFromAddressLists=TRUE)" : "");
                    theSearcher.SizeLimit = MaxResults + 1;
                    SearchResultCollection results = null;


                    SortOption so = new SortOption("sn", System.DirectoryServices.SortDirection.Ascending);
                    theSearcher.Sort = so;
                    results = theSearcher.FindAll();

                    Table tblResults = new Table();
                    tblResults.CellSpacing = 0;
                    pnlResults.Controls.Add(tblResults);

                    if (results.Count == 0)
                    {
                        AddMessage(Utils.GetString("NoResults"), tblResults);
                        AddCloseLink(tblResults);
                    }
                    else
                    {
                        for (int i = 0; i < results.Count; i++)
                        {
                            if (i < MaxResults)
                                PrepareResult(tblResults, results[i]);
                            else if (i == MaxResults)
                                AddMessage(Utils.GetString("TooManyResults"), tblResults);
                            else
                                break;
                        }


                        AddCloseLink(tblResults);
                    }
                }

                if (useAjax)
                    up.Update();
            }
            catch (Exception ex)
            {
                Logger.Log(ex);

                Literal ltrError = new Literal();
                ltrError.Text = string.Format(Utils.GetString("ErrorFormat"), ex.ToString());
                pnlResults.Controls.Add(ltrError);
                if (useAjax)
                    up.Update();
            }
        }

        /// <summary>
        /// Adds message table
        /// </summary>
        /// <param name="msg"></param>
        private void AddMessage(string msg, Table tblResults)
        {
            TableRow row1 = new TableRow();
            tblResults.Rows.Add(row1);

            TableCell cell1 = new TableCell();
            cell1.Style.Add(HtmlTextWriterStyle.Padding, "6px");
            row1.Cells.Add(cell1);

            Table tbl = new Table();
            cell1.Controls.Add(tbl);

            TableRow row = new TableRow();
            tbl.Rows.Add(row);

            TableCell imgCell = new TableCell();
            row.Cells.Add(imgCell);

            Image img = new Image();
            img.ImageUrl = "/_layouts/images/warning16by16.gif";
            imgCell.Controls.Add(img);

            TableCell cell = new TableCell();
            row.Cells.Add(cell);

            Literal lblNoResult = new Literal();
            lblNoResult.Text = msg;
            cell.Controls.Add(lblNoResult);
        }

        /// <summary>
        /// Adds close button link to result table
        /// </summary>
        /// <param name="tblResults"></param>
        private void AddCloseLink(Table tblResults)
        {
            TableRow closeRow = new TableRow();
            tblResults.Rows.Add(closeRow);

            TableCell closeCell = new TableCell();
            closeRow.Cells.Add(closeCell);
            closeCell.Style.Add("border-top", "1px solid #cccccc");
            closeCell.HorizontalAlign = HorizontalAlign.Right;

            Image imgClose = new Image();
            imgClose.Style.Add(HtmlTextWriterStyle.Cursor, "pointer");
            imgClose.ImageUrl = Page.ClientScript.GetWebResourceUrl(GetType(), "Infowise.Sharepoint.V3.Webparts.Common.ewr044.gif");
            imgClose.Attributes.Add("onclick", string.Format("javascript:document.getElementById('{0}').style.display='none';", pnlResults.ClientID));
            closeCell.Controls.Add(imgClose);
        }

        /// <summary>
        /// Renders results
        /// </summary>
        /// <param name="tblResults"></param>
        /// <param name="result"></param>
        private void PrepareResult(Table tblResults, SearchResult result)
        {
            Logger.EnterMethod(tblResults, result);

            try
            {
                TableRow rowResult = new TableRow();
                tblResults.Rows.Add(rowResult);

                TableCell cellResult = new TableCell();
                rowResult.Cells.Add(cellResult);

                if(tblResults.Rows.Count>0)
                    cellResult.Style.Add("border-top", "1px solid #cccccc");

                Table tblResult = new Table();
                tblResult.CssClass = "ms-formtable iw-resulttbl";
                tblResult.CellSpacing = 0;
                tblResult.CellPadding = 4;
                //tblResult.Width = new Unit("100%");
                cellResult.Controls.Add(tblResult);

                TableRow firstRow = new TableRow();
                tblResult.Rows.Add(firstRow);

                TableCell photoCell = null;
                if (ShowImages)
                {
                    photoCell = new TableCell();
                    photoCell.Style.Add(HtmlTextWriterStyle.VerticalAlign, "top");
                    firstRow.Cells.Add(photoCell);
                    Image imgPhoto = new Image();
                    imgPhoto.Height = new Unit(ImageSize);
                    imgPhoto.Width = new Unit(ImageSize);
                    imgPhoto.AlternateText = result.Properties["displayName"][0].ToString();
                    photoCell.Controls.Add(imgPhoto);

                    if (!result.Properties.Contains(ImageField))
                        imgPhoto.ImageUrl = "/_layouts/images/blank.gif";
                    else
                        imgPhoto.ImageUrl = ImageFolderUrl + (ImageFolderUrl.EndsWith("/") ? "" : "/") + result.Properties[ImageField][0].ToString() + (AddExtension ? "." + ImageExtension : "");
                    imgPhoto.Attributes.Add("onerror", "LoadFail(this);");
                }

                TableCell nameCell = new TableCell();
                nameCell.CssClass = "ms-vb iwps-fullname";
                if (ShowLabels)
                    nameCell.ColumnSpan = 2;
                firstRow.Cells.Add(nameCell);

                Label lblName = new Label();
                lblName.Text = result.Properties["displayName"][0].ToString();
                lblName.CssClass = "ms-standardheader";
                lblName.Style.Add(HtmlTextWriterStyle.FontWeight, "bold");
                nameCell.Controls.Add(lblName);

                int rowspan = 1;
                foreach (PropertyInfo pi in ResultProperties)
                {
                    if (pi.FieldName.Equals("displayName") || pi.FieldName.Equals(Utils.GetString("PhotoUrlField")) || pi.FieldName.ToLower().Equals("samaccountname"))
                        continue;

                    TableRow propRow = new TableRow();
                    tblResult.Rows.Add(propRow);

                    if (ShowLabels)
                    {
                        TableCell labelCell = new TableCell();
                        propRow.Cells.Add(labelCell);
                        labelCell.Wrap = true;
                        labelCell.CssClass = "ms-standardheader iwps-label";

                        labelCell.Text = pi.DisplayName;
                    }

                    TableCell propCell = new TableCell();
                    propRow.Cells.Add(propCell);
                    propCell.CssClass = "ms-vb iwps-value";

                    rowspan++;
                    if (!result.Properties.Contains(pi.FieldName))
                    {
                        propCell.Text = ShowLabels?"&nbsp;":string.Empty;
                        continue;
                    }

                    string fieldName = pi.FieldName.ToLower();
                    switch (fieldName)
                    {
                        case "mail":
                            {
                                string mail = result.Properties[pi.FieldName][0].ToString();
                                propCell.Text = string.Format("<a href=\"mailto:{0}\">{0}</a>", mail);
                                if (SPContext.Current.Site.WebApplication.PresenceEnabled)
                                {
                                    string presence = "&nbsp;<span style=\"padding-top:21px;\"><span><img border=\"0\" valign=\"middle\" height=\"12\" width=\"12\" src=\"/_layouts/images/IMNHDR.gif\" onload=\"IMNRC('" + mail + "', this)\" ShowOfflinePawn=\"1\" id=\"imnhdr" + Guid.NewGuid().ToString() + "\"></span></span>";
                                    nameCell.Controls.Add(new LiteralControl(presence));
                                }
                                break;
                            }
                        case "publicdelegates":
                        case "manager":
                            try
                            {
                                Logger.Log("Extracting " + pi.FieldName);
                                string managerPath = result.Properties[pi.FieldName][0].ToString();
                                if (!string.IsNullOrEmpty(managerPath))
                                {
                                    DirectoryEntry manEntry = new DirectoryEntry("GC://" + managerPath);
                                    propCell.Text = manEntry.Properties["displayName"][0].ToString();
                                }
                                else
                                    Logger.Log("No manager value exists in the Active Directory");
                            }
                            catch (Exception ex)
                            {
                                Logger.Log(ex);
                            }
                            break;
                        case "accountexpires":
                            long sec = Convert.ToInt64(result.Properties[pi.FieldName][0])/10;
                            if (sec > 0)
                            {
                                TimeSpan ts = TimeSpan.FromMilliseconds(sec);
                                DateTime startDate = new DateTime(1601, 1, 1);
                                propCell.Text = startDate.Add(ts).ToShortDateString();
                            }
                            break;
                        case "directreports":
                        case "memberof":
                            object[] groups = result.Properties[pi.FieldName].OfType<object>().ToArray();
                            List<string> groupText = new List<string>();
                            if (groups != null && groups.Length > 0)
                            {
                                foreach (object groupPath in groups)
                                {
                                    DirectoryEntry groupEntry = new DirectoryEntry("GC://" + groupPath.ToString());
                                    if (groupEntry.Properties.Contains("cn"))
                                        groupText.Add(groupEntry.Properties["cn"][0].ToString());
                                }
                            }
                            propCell.Text = string.Join(", ", groupText.ToArray());
                            break;
                        default:
                            propCell.Text = result.Properties[pi.FieldName][0].ToString();
                            break;
                    }

                }

                if(ShowImages)
                    photoCell.RowSpan = rowspan;
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
                tblResults.Rows.Remove(tblResults.Rows[tblResults.Rows.Count - 1]);
            }
        }
    }

    // THIS GOES IN YOUR PAGES CODE BEHIND ALSO!
    public class ProgressTemplate : ITemplate
    {
        public void InstantiateIn(Control container)
        {
        }
    }
}
