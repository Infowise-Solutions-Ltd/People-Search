using System;
using System.Collections.Generic;
using System.Text;

namespace Infowise.Sharepoint.V3.Webparts
{
    public class ErrorEventArgs : EventArgs
    {
        public string ErrorMessage { get; set; }
        public ErrorEventArgs(string errorMessage)
        {
            this.ErrorMessage = errorMessage;
        }
    }
    public delegate void ErrorEventHandler(object sender, ErrorEventArgs e);
}
