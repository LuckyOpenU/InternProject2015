using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MvcApplication2.SignalR
{
    public class NFC
    {

        private readonly static Lazy<NFC> _instance = new Lazy<NFC>(() => new NFC(GlobalHost.ConnectionManager.GetHubContext<NFCHub>().Clients));

        private IHubConnectionContext Clients { get; set; }

        private NFC(IHubConnectionContext clients)
        {
            Clients = clients;
        }

        public static NFC Instance
        {
            get { return _instance.Value; }
        }

        public void UpdateNFCStatus(string status)
        {
            Clients.All.updateStatus(status);
        }


    }
}