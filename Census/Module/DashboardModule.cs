using System;
using System.Collections.Generic;
using System.Linq;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using Newtonsoft.Json;
using SiteLibrary;

namespace Census
{
    public class DashboardViewModel : MasterViewModel
    {
        public DashboardViewModel(Translator translator, IDatabase db, Session session)
            : base(translator,
                   translator.Get("Dashboard.Title", "Dashboard page title", "Dashboard"),
                   session)
        {
        }
    }

    public class DashboardModule : CensusModule
    {
        public DashboardModule()
        {
            this.RequiresAuthentication();

            Get("/", parameters =>
            {
                return View["View/dashboard.sshtml",
                    new DashboardViewModel(Translator, Database, CurrentSession)];
            });
        }
    }
}
