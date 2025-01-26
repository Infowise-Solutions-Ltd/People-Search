using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Principal;

namespace Infowise.Sharepoint.V3.Webparts.Common
{
    class RevertToSelf : IDisposable
    {
        private WindowsImpersonationContext winContext = null;

        public RevertToSelf()
        {
            winContext = WindowsIdentity.Impersonate(IntPtr.Zero);
        }
        #region IDisposable Members

        public void Dispose()
        {
            if (winContext != null)
                winContext.Undo();
        }

        #endregion
    }
}
