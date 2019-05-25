using System;
using System.Collections.Generic;
using System.Linq;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using Newtonsoft.Json;
using SiteLibrary;

namespace Petitio
{
    public class DashboardItemViewModel
    {
        public string Tag;
        public string Indent;
        public string Width;
        public string Name;
        public string ValueOne;

        public DashboardItemViewModel(string valueOne, string valueTwo, string valueThree)
        {
            Tag = "th";
            Name = string.Empty;
            Indent = "0%";
            Width = "40%";
            ValueOne = valueOne;
        }

        public DashboardItemViewModel(Translator translator, IDatabase db, Queue queue, int indent)
        {
            Tag = "td";
            Indent = indent.ToString() + "%";
            Width = (40 - indent).ToString() + "%";
            Name = queue.Name.Value[translator.Language];
            var members = db
                .Query<Subscription>(DC.Equal("queueid", queue.Id.Value))
                .Where(m => !m.Contact.Value.Deleted)
                .ToList();
            ValueOne = members.Count().ToString();
        }
    }

    public class DashboardViewModel : MasterViewModel
    {
        public List<DashboardItemViewModel> List;

        private void AddRecursive(Translator translator, IDatabase db, Queue queue, int indent)
        {
            List.Add(new DashboardItemViewModel(translator, db, queue, indent));

            foreach (var o in queue.Children)
            {
                AddRecursive(translator, db, o, indent + 5); 
            }
        }

        public DashboardViewModel(Translator translator, IDatabase db, Session session)
            : base(translator,
                   translator.Get("Dashboard.Title", "Dashboard page title", "Dashboard"),
                   session)
        {
            List = new List<DashboardItemViewModel>();
            List.Add(new DashboardItemViewModel(
                translator.Get("Dashboard.Members.Row.All", "All members row in the dashbaord", "All contacts"),
                translator.Get("Dashboard.Members.Row.Full", "Full members row in the dashbaord", "Full members"),
                translator.Get("Dashboard.Members.Row.Voting", "Voting members row in the dashbaord", "Voting rights")));

            foreach (var o in db
                .Query<Queue>()
                .Where(o => o.Parent.Value == null)
                .OrderBy(o => o.Name.Value[translator.Language]))
            {
                AddRecursive(translator, db, o, 0);
            }
        }
    }

    public class DashboardModule : PetitioModule
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
