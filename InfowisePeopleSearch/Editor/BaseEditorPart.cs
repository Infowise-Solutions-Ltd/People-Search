using System;
using System.Collections.Generic;
using System.Text;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.WebControls;
using System.Web.UI;

namespace Infowise.Sharepoint.V3.Webparts
{
    public abstract class BaseEditorPart : EditorPart
    {
        protected Table tblMain;
        protected Label lblError;
   
        protected override void CreateChildControls()
        {
            lblError = new Label();
            lblError.CssClass = "ms-error";
            lblError.EnableViewState = false;
            Controls.Add(lblError);

            tblMain = new Table();
            tblMain.CellPadding = 0;
            tblMain.CellSpacing = 0;
            tblMain.BorderWidth = 0;
            tblMain.Width = new Unit("100%");
            Controls.Add(tblMain);
        }

        protected void AddTableRow(bool hasBody, bool isLast, out Panel head, out Panel body)
        {
            TableRow row = new TableRow();
            tblMain.Rows.Add(row);

            TableCell cell = new TableCell();
            row.Cells.Add(cell);

            head = new Panel();
            head.CssClass = "UserSectionHead";
            cell.Controls.Add(head);

            if (hasBody)
            {
                Panel outerBody = new Panel();
                outerBody.CssClass = "UserSectionBody";
                cell.Controls.Add(outerBody);

                body = new Panel();
                body.CssClass = "UserControlGroup";
                outerBody.Controls.Add(body);
                body.Wrap = false;
            }
            else
                body = null;

            if (!isLast)
            {
                Panel dot = new Panel();
                dot.CssClass = "UserDottedLine";
                dot.Style.Add(HtmlTextWriterStyle.Width, "100%");
                cell.Controls.Add(dot);
            }

        }
    }
}
