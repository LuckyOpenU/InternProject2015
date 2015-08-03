using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;

namespace MvcApplication2.SignalR
{
    public class NFCHub : Hub
    {
        public void UpdateNFCStatus(string status)
        {
            Clients.All.updateStatus(status);
        }
    }
}