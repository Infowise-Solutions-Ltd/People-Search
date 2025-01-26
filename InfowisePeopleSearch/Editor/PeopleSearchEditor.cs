using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.UI.WebControls;
using Infowise.Sharepoint.V3.Webparts.Common;

namespace Infowise.Sharepoint.V3.Webparts
{
    public class PeopleSearchEditor : BaseEditorPart
    {
        #region ctor
        public PeopleSearchEditor()
        {
            this.Title = "Infowise People Search";
        }
        #endregion

        TextBox txtADPath;
        FieldSelector fsProperties;
        CheckBox chkShowSearchButton;
        CheckBox chkIncludeImage, chkIncludeLabel, chkRequireExchangeAccount;
        TextBox txtImageFolderUrl;
        DropDownList ddlImageField;
        CheckBox chkAddExtension;
        DropDownList ddlImageExtension;
        TextBox txtImageSize;
        TextBox txtMaxResults;
        TextBox txtSearchText;

        Panel calImageFolderHead, calImageFolderBody;
        Panel calImageFieldHead, calImageFieldBody;
        Panel calAddExtensionHead;
        Panel calImageSizeHead, calImageSizeBody;

        Dictionary<string, string> properties = null;
        private Dictionary<string, string> Properties
        {
            get
            {
                if (properties == null)
                    properties = ADWrapper.GetProfileProperties();

                return properties;
            }
        }

        public override bool ApplyChanges()
        {
            try
            {
                if (WebPartToEdit == null)
                    return false;

                EnsureChildControls();

                PeopleSearch ps = (PeopleSearch)this.WebPartToEdit;
                ps.Path = txtADPath.Text;
                ps.ResultFields = fsProperties.Value;
                ps.ShowSearchButton = chkShowSearchButton.Checked;
                ps.SearchButtonText = txtSearchText.Text;
                ps.ShowImages = chkIncludeImage.Checked;
                ps.ShowLabels = chkIncludeLabel.Checked;
                ps.ImageFolderUrl = txtImageFolderUrl.Text;
                ps.ImageField = ddlImageField.SelectedValue;
                ps.AddExtension = chkAddExtension.Checked;
                ps.ImageExtension = ddlImageExtension.SelectedValue;
                ps.ImageSize = txtImageSize.Text;
                ps.RequireExchangeAccount = chkRequireExchangeAccount.Checked;

                int maxResults = 5;
                int.TryParse(txtMaxResults.Text, out maxResults);
                ps.MaxResults = maxResults;
                ps.Recreate();

                return true;
            }
            catch (Exception ex)
            {
                lblError.Text = ex.Message;
                Logger.Log(ex);
                return false;
            }
        }

        public override void SyncChanges()
        {
            try
            {
                if (WebPartToEdit == null)
                    return;

                EnsureChildControls();

                PeopleSearch ps = (PeopleSearch)this.WebPartToEdit;
                txtADPath.Text = ps.Path;
                int order = 1;
                foreach (string key in Properties.Keys)
                {
                    var pi = new PropertyInfo();
                    pi.FieldName = key;
                    pi.DisplayName = key;
                    pi.Order = order;
                    fsProperties.AddField(pi);
                    order++;
                }

                fsProperties.Value = ps.ResultFields;
                chkShowSearchButton.Checked = ps.ShowSearchButton;
                txtSearchText.Text = ps.SearchButtonText;
                chkIncludeImage.Checked = ps.ShowImages;
                ToggleViewImages(chkIncludeImage.Checked);
                chkIncludeLabel.Checked = ps.ShowLabels;
                txtImageFolderUrl.Text = ps.ImageFolderUrl;
                TrySetValue(ddlImageField, ps.ImageField);
                chkAddExtension.Checked = ps.AddExtension ;
                TrySetValue(ddlImageExtension, ps.ImageExtension);
                txtImageSize.Text = ps.ImageSize;
                txtMaxResults.Text = ps.MaxResults.ToString();
                chkRequireExchangeAccount.Checked = ps.RequireExchangeAccount;
            }
            catch (Exception ex)
            {
                lblError.Text = ex.Message;
                Logger.Log(ex);
            }
        }

        private void TrySetValue(DropDownList ddl, string value)
        {
            try
            {
                ddl.SelectedValue = value;
            }
            catch { }
        }

        protected override void CreateChildControls()
        {
            base.CreateChildControls();

            Page.ClientScript.RegisterClientScriptResource(GetType(), "Infowise.Sharepoint.V3.Webparts.Common.peopleSearch.js");

            try
            {
                #region AD Path
                txtADPath = new TextBox();
                txtADPath.ID = "txtADPath";
                txtADPath.CssClass = "UserInput";
                txtADPath.Width = new Unit(176, UnitType.Pixel);
                Panel calAdPathHead, calAdPathBody;
                AddTableRow(true, false, out calAdPathHead, out calAdPathBody);
                calAdPathBody.Controls.Add(txtADPath);

                Label lblADPath = new Label();
                lblADPath.ToolTip = Utils.GetString("ADPathDesc");
                lblADPath.Text = Utils.GetString("ADPath");
                lblADPath.AssociatedControlID = txtADPath.ID;
                calAdPathHead.Controls.Add(lblADPath);
                #endregion

                #region Properties
                fsProperties = new FieldSelector();
                fsProperties.ID = "fsProperties";

                Panel calProperttiesHead, calPropertiesBody;
                AddTableRow(true, false, out calProperttiesHead, out calPropertiesBody);
                calPropertiesBody.Controls.Add(fsProperties);

                Label lblProperties = new Label();
                lblProperties.ToolTip = Utils.GetString("PropertiesDesc");
                lblProperties.Text = Utils.GetString("Properties");
                lblProperties.AssociatedControlID = fsProperties.ID;
                calProperttiesHead.Controls.Add(lblProperties);
                #endregion

                #region Show search button
                chkShowSearchButton = new CheckBox();
                chkShowSearchButton.ID = "chkShowSearchButton";
                chkShowSearchButton.Text = Utils.GetString("ShowSearchButton");
                chkShowSearchButton.ToolTip = Utils.GetString("ShowSearchButtonDesc");
                Panel calShowSearchButtonHead, calShowSearchButtonBody;
                AddTableRow(false, false, out calShowSearchButtonHead, out calShowSearchButtonBody);
                calShowSearchButtonHead.Controls.Add(chkShowSearchButton);

                #endregion

                #region Search button text
                Panel searchTextHead, searchTextBody;
                txtSearchText = new TextBox();
                txtSearchText.ID = "txtSearchText";
                txtSearchText.CssClass = "UserInput";
                txtSearchText.Width = new Unit(176, UnitType.Pixel);
                AddTableRow(true, false, out searchTextHead, out searchTextBody);
                searchTextBody.Controls.Add(txtSearchText);

                Label lblSearchText = new Label();
                lblSearchText.ToolTip = Utils.GetString("SearchButtonTextDesc");
                lblSearchText.Text = Utils.GetString("SearchButtonText");
                lblSearchText.AssociatedControlID = txtSearchText.ID;
                searchTextHead.Controls.Add(lblSearchText);

                #endregion

                #region Include label
                chkIncludeLabel = new CheckBox();
                chkIncludeLabel.ID = "chkIncludeLabel";
                chkIncludeLabel.Text = Utils.GetString("IncludeLabel");
                chkIncludeLabel.ToolTip = Utils.GetString("IncludeLabelDesc");
                Panel calIncludeLabelHead, calIncludeLabelBody;
                AddTableRow(false, false, out calIncludeLabelHead, out calIncludeLabelBody);
                calIncludeLabelHead.Controls.Add(chkIncludeLabel);

                #endregion

                #region Include image
                chkIncludeImage = new CheckBox();
                chkIncludeImage.ID = "chkIncludeImage";
                chkIncludeImage.Enabled = true;
                chkIncludeImage.Text = Utils.GetString("IncludeImage");
                chkIncludeImage.ToolTip = Utils.GetString("IncludeImageDesc");
                Panel calIncludeImageHead, calIncludeImageBody;
                AddTableRow(false, false, out calIncludeImageHead, out calIncludeImageBody);
                calIncludeImageHead.Controls.Add(chkIncludeImage);

                chkIncludeImage.AutoPostBack = true;
                chkIncludeImage.CheckedChanged += new EventHandler(chkIncludeImage_CheckedChanged);
                #endregion

                #region Image folder URL
                txtImageFolderUrl = new TextBox();
                txtImageFolderUrl.ID = "txtImageFolderUrl";
                txtImageFolderUrl.CssClass = "UserInput";
                txtImageFolderUrl.Width = new Unit(176, UnitType.Pixel);
                AddTableRow(true, false, out calImageFolderHead, out calImageFolderBody);
                calImageFolderBody.Controls.Add(txtImageFolderUrl);

                Label lblImageFolderUrl = new Label();
                lblImageFolderUrl.ToolTip = Utils.GetString("ImageFolderUrlDesc");
                lblImageFolderUrl.Text = Utils.GetString("ImageFolderUrl");
                lblImageFolderUrl.AssociatedControlID = txtImageFolderUrl.ID;
                calImageFolderHead.Controls.Add(lblImageFolderUrl);
                #endregion

                #region Image field
                ddlImageField = new DropDownList();
                ddlImageField.ID = "ddlImageField";
                ddlImageField.CssClass = "UserInput";
                ddlImageField.Width = new Unit(176, UnitType.Pixel);

                AddTableRow(true, false, out calImageFieldHead, out calImageFieldBody);
                calImageFieldBody.Controls.Add(ddlImageField);
                Label lblImageField = new Label();
                lblImageField.ToolTip = Utils.GetString("ImageFieldDesc");
                lblImageField.Text = Utils.GetString("ImageField");
                lblImageField.AssociatedControlID = ddlImageField.ID;
                calImageFieldHead.Controls.Add(lblImageField);

                if (ddlImageField.Items.Count == 0)
                {
                    ddlImageField.DataTextField = "Key";
                    ddlImageField.DataValueField = "Key";
                    ddlImageField.DataSource = Properties;
                    ddlImageField.DataBind();
                }

                #endregion

                #region Add extension
                chkAddExtension = new CheckBox();
                chkAddExtension.ID = "chkAddExtension";
                chkAddExtension.Text = Utils.GetString("AddExtension");
                chkAddExtension.ToolTip = Utils.GetString("AddExtension");
                Panel calAddExtensionBody;
                AddTableRow(false, false, out calAddExtensionHead, out calAddExtensionBody);
                calAddExtensionHead.Controls.Add(chkAddExtension);

                calAddExtensionHead.Controls.Add(new Literal() { Text = "&nbsp;" });
                ddlImageExtension = new DropDownList();
                calAddExtensionHead.Controls.Add(ddlImageExtension);
                if (ddlImageExtension.Items.Count == 0)
                {
                    ddlImageExtension.Items.Add("jpg");
                    ddlImageExtension.Items.Add("gif");
                    ddlImageExtension.Items.Add("png");
                    ddlImageExtension.Items.Add("bmp");
                }
                #endregion

                #region Image size
                txtImageSize = new TextBox();
                txtImageSize.ID = "txtImageSize";
                txtImageSize.CssClass = "UserInput";
                txtImageSize.Width = new Unit(176, UnitType.Pixel);
                AddTableRow(true, false, out calImageSizeHead, out calImageSizeBody);
                calImageSizeBody.Controls.Add(txtImageSize);

                Label lblImageSize = new Label();
                lblImageSize.ToolTip = Utils.GetString("ImageSizeDesc");
                lblImageSize.Text = Utils.GetString("ImageSize");
                lblImageSize.AssociatedControlID = txtImageSize.ID;
                calImageSizeHead.Controls.Add(lblImageSize);
                #endregion

                #region Max results
                txtMaxResults = new TextBox();
                txtMaxResults.ID = "txtMaxResults";
                txtMaxResults.CssClass = "UserInput";
                Panel calMaxResultHead, calMaxResultBody;
                AddTableRow(true, false, out calMaxResultHead, out calMaxResultBody);
                calMaxResultBody.Controls.Add(txtMaxResults);

                Label lblMaxResults = new Label();
                lblMaxResults.ToolTip = Utils.GetString("MaxResultsDesc");
                lblMaxResults.Text = Utils.GetString("MaxResults");
                lblMaxResults.AssociatedControlID = txtMaxResults.ID;
                calMaxResultHead.Controls.Add(lblMaxResults);
                #endregion

                #region Require Exchange
                chkRequireExchangeAccount = new CheckBox();
                chkRequireExchangeAccount.ID = "chkRequireExchangeAccount";
                chkRequireExchangeAccount.Text = Utils.GetString("RequireExchangeAccount");
                chkRequireExchangeAccount.ToolTip = Utils.GetString("RequireExchangeAccount");
                Panel calRequireLabelHead, calRequireLabelBody;
                AddTableRow(false, true, out calRequireLabelHead, out calRequireLabelBody);
                calRequireLabelHead.Controls.Add(chkRequireExchangeAccount);
                #endregion
            }
            catch (Exception ex)
            {
                lblError.Text = ex.Message;
                Logger.Log(ex);
            }
        }

        void chkIncludeImage_CheckedChanged(object sender, EventArgs e)
        {
            ToggleViewImages(chkIncludeImage.Checked);
        }

        private void ToggleViewImages(bool show)
        {
            chkAddExtension.Enabled = show;
            ddlImageExtension.Enabled = show;
            txtImageFolderUrl.ReadOnly = !show;
            txtImageSize.ReadOnly = !show;
            ddlImageField.Enabled = show;
        }
    }
}
