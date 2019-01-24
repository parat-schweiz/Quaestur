using System;
using System.Collections.Generic;
using System.Linq;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using Newtonsoft.Json;

namespace Quaestur
{
    public class DashboardModule : QuaesturModule
    {
        public DashboardModule()
        {
            RequireCompleteLogin();

            Get["/"] = parameters =>
            {
                return View["View/dashboard.sshtml",
                    new MasterViewModel(Translator, 
                        Translate("Dashboard.Title", "Dashboard page title", "Dashboard"),
                        CurrentSession)];
            };
        }
    }
}
