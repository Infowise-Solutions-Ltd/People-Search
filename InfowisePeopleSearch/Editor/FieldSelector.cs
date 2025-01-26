using System;
using System.Collections.Generic;
using System.Text;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Collections;
using System.Xml;


namespace Infowise.Sharepoint.V3.Webparts
{
    class FieldSelector : UserControl
    {
        #region Consts
        internal const string ATT_ORDER = "Order";
        internal const string ATT_NAME = "DisplayName";
        internal const string ATT_INTERNALNAME = "InternalName";
        internal const string FIELD_NODE = "Field";
        internal const string FIELDS_NODE = "Fields";
        private const string VIEWSTATEKEY = "Fields";
        private const string VIEWSTATESELECTED = "SelectedFields";
        #endregion

        #region Private members
        TextBox hdnFields;
        List<Field> fields = new List<Field>(); 
        #endregion

        internal event ErrorEventHandler ErrorOccurred;

        internal struct Field
        {
            public int Order;
            public string Name;
            public string InternalName;
        }

        /// <summary>
        /// Deserialize view state data
        /// </summary>
        /// <param name="serializedData"></param>
        private void Deserialize(string serializedData)
        {
            if(string.IsNullOrEmpty(serializedData))
                return;

            XmlDocument serDoc = new XmlDocument();
            serDoc.LoadXml(serializedData);

            foreach (XmlNode fieldNode in serDoc.DocumentElement.ChildNodes)
            {
                Field field = new Field();
                field.InternalName = fieldNode.Attributes[ATT_INTERNALNAME].Value;
                field.Name = fieldNode.Attributes[ATT_NAME].Value;
                field.Order = int.Parse(fieldNode.Attributes[ATT_ORDER].Value);
                fields.Add(field);
            }
        }

        /// <summary>
        /// Serializes fields into XML
        /// </summary>
        /// <returns></returns>
        private string Serialize()
        {
            if (fields.Count == 0)
                return string.Empty;

            XmlDocument serDoc = new XmlDocument();
            serDoc.LoadXml("<" + FIELDS_NODE + "/>");

            foreach (Field field in fields)
            {
                XmlElement fieldNode = serDoc.CreateElement(FIELD_NODE);
                fieldNode.SetAttribute(ATT_INTERNALNAME, field.InternalName);
                fieldNode.SetAttribute(ATT_NAME, field.Name);
                fieldNode.SetAttribute(ATT_ORDER, field.Order.ToString());
                serDoc.DocumentElement.AppendChild(fieldNode);
            }

            return serDoc.InnerXml;
        }

        internal void ClearFields()
        {
            fields.Clear();
        }
        /// <summary>
        /// Adds field to control
        /// </summary>
        /// <param name="fieldInfo"></param>
        internal void AddField(PropertyInfo fieldInfo)
        {
            if(FieldExists(fieldInfo.DisplayName))
                return;
            Field field = new Field();
            field.Name = fieldInfo.DisplayName;
            field.InternalName = fieldInfo.FieldName;
            field.Order = fields.Count + 1;
            fields.Add(field);
        }

        /// <summary>
        /// Checks if field exists in list
        /// </summary>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        private bool FieldExists(string fieldName)
        {
            foreach(Field field in fields)
            {
                if(field.Name.Equals(fieldName))
                    return true;
            }
            return false;
        }

        protected override void CreateChildControls()
        {
            try
            {
                hdnFields = new TextBox();

#if DEBUG
#else
                hdnFields.Style.Add("display", "none");
#endif
                hdnFields.ID = "hdnFields";
                Controls.Add(hdnFields);

                string values = hdnFields.Text;

                if (ViewState[VIEWSTATEKEY] != null && fields.Count == 0)
                {
                    Deserialize((string)ViewState[VIEWSTATEKEY]);
                    if(ViewState[VIEWSTATESELECTED] != null)
                        values = ViewState[VIEWSTATESELECTED].ToString();
                }

                if (fields.Count > 0)
                    BuildControls(values);
            }
            catch (Exception ex)
            {
                OnErrorOccurred(ex.Message);
            }
        }

        protected void OnErrorOccurred(string errorMessage)
        {
            if (ErrorOccurred != null)
                ErrorOccurred(this, new ErrorEventArgs(errorMessage));
        }

        public string Value
        {
            get
            {

                return ConvertToXml(hdnFields.Text);
            }
            set
            {
                try
                {
                    EnsureChildControls();
                    string val = string.Empty;
                    if (!string.IsNullOrEmpty(value))
                        val = ConvertToString(value);
                    if (string.IsNullOrEmpty(hdnFields.Text) && !string.IsNullOrEmpty(value))
                        hdnFields.Text = val;
                    BuildControls(hdnFields.Text);
                    ViewState[VIEWSTATEKEY] = Serialize();
                    ViewState[VIEWSTATESELECTED] = val;
                }
                catch (Exception ex)
                {
                    OnErrorOccurred(ex.Message);
                }
            }
        }

        /// <summary>
        /// re-build controls
        /// </summary>
        internal void BuildControls()
        {
            EnsureChildControls();
            BuildControls(null);
        }

        private static string ConvertToString(string xml)
        {
            if(string.IsNullOrEmpty(xml))
                return string.Empty;
            List<string> result = new List<string>();

            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(xml);

                for (int i = 0; i < xmlDoc.DocumentElement.ChildNodes.Count; i++)
                {
                    XmlElement propEl = xmlDoc.DocumentElement.ChildNodes[i] as XmlElement;
                    result.Add(string.Format("{0};{1};{2}", propEl.GetAttribute(ATT_INTERNALNAME), propEl.GetAttribute(ATT_NAME), propEl.GetAttribute(ATT_ORDER)));
                }

                return string.Join("|", result.ToArray());
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string ConvertToXml(string value)
        {
            XmlDocument xmlDoc = new XmlDocument();
            var root = xmlDoc.CreateElement(FIELDS_NODE);
            xmlDoc.AppendChild(root);

            if (!string.IsNullOrEmpty(value))
            {
                var valueArr = new List<string>(value.Split(new string[] { "|" }, StringSplitOptions.RemoveEmptyEntries));
                int order = 1;
                foreach (string curValue in valueArr)
                {
                    string[] parts = curValue.Split(';');
                    XmlElement propEl = xmlDoc.CreateElement(FIELD_NODE);
                    propEl.SetAttribute(ATT_INTERNALNAME, parts[0]);
                    propEl.SetAttribute(ATT_NAME, parts[1]);
                    propEl.SetAttribute(ATT_ORDER, order.ToString());

                    root.AppendChild(propEl);
                    order++;
                }
            }

            return xmlDoc.OuterXml;
        }

        private void BuildControls(string values)
        {
            if (fields == null || fields.Count == 0)
                return;

            Dictionary<string, PropertyInfo> valueArray = new Dictionary<string, PropertyInfo>();
            if (!string.IsNullOrEmpty(values))
            {
                var valueArr = new List<string>(values.Split(new string[] { "|" }, StringSplitOptions.RemoveEmptyEntries));
                foreach (string value in valueArr)
                {
                    PropertyInfo pi = new PropertyInfo();
                    string key = value.Split(';')[0];
                    pi.FieldName = key;
                    pi.DisplayName = value.Split(';')[1];
                    pi.Order = int.Parse(value.Split(';')[2]);
                    valueArray.Add(key, pi);
                }
            }

            Table tblFields = new Table();
            if(Controls.Count > 1)
                Controls.RemoveAt(1);
            Controls.Add(tblFields);
           
            int counter = valueArray.Count+1;
            for (int i = 1; i <= fields.Count; i++)
            {
                foreach (Field field in fields)
                {
                    if (field.Order == i)
                    {
                        TableRow row = new TableRow();
                        tblFields.Rows.Add(row);

                        TableCell selectCell = new TableCell();
                        row.Cells.Add(selectCell);

                        CheckBox chk = new CheckBox();
                        chk.Checked = valueArray.ContainsKey(field.InternalName);
                        chk.Attributes.Add("internalname", field.InternalName);
                        selectCell.Controls.Add(chk);
                        chk.Attributes.Add("onclick", string.Format("IWPSSelectFields(this, '{0}')", hdnFields.ClientID));

                        TableCell orderCell = new TableCell();
                        row.Cells.Add(orderCell);

                        DropDownList ddl = new DropDownList();
                        for (int j = 1; j <= fields.Count; j++)
                        {
                            ddl.Items.Add(j.ToString());
                        }

                        if (chk.Checked)
                            ddl.SelectedValue = (valueArray[field.InternalName].Order).ToString();
                        else
                        {
                            ddl.SelectedValue = counter.ToString();
                            counter++;
                        }

                        orderCell.Controls.Add(ddl);
                        ddl.Attributes.Add("onchange", string.Format("IWPSReorderFields(this, '{0}')", hdnFields.ClientID));
                        ddl.Attributes.Add("oldVal", ddl.SelectedValue);

                        TableCell nameCell = new TableCell();
                        TextBox txt = new TextBox();
                        if (chk.Checked)
                            txt.Text = valueArray[field.InternalName].DisplayName;
                        else
                            txt.Text = field.Name;
                        txt.Attributes.Add("onchange", string.Format("IWPSSelectFieldsById('{0}', '{1}')", chk.ClientID, hdnFields.ClientID));
                        nameCell.Controls.Add(txt);
                        row.Cells.Add(nameCell);
                    }
                }
            }
        }
    }
}
