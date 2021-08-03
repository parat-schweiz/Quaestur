using System;
using System.Collections.Generic;
using System.Linq;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using Newtonsoft.Json;
using SiteLibrary;

namespace Hospes
{
    public class DashboardItemViewModel
    {
        public string Tag;
        public string Indent;
        public string Width;
        public string Name;
        public string ValueOne;
        public string ValueTwo;
        public string ValueThree;

        public DashboardItemViewModel(string valueOne, string valueTwo, string valueThree)
        {
            Tag = "th";
            Name = string.Empty;
            Indent = "0%";
            Width = "40%";
            ValueOne = valueOne;
            ValueTwo = valueTwo;
            ValueThree = valueThree;
        }

        public DashboardItemViewModel(Translator translator, IDatabase db, Organization organization, int indent)
        {
            Tag = "td";
            Indent = indent.ToString() + "%";
            Width = (40 - indent).ToString() + "%";
            Name = organization.Name.Value[translator.Language];
            var members = db
                .Query<Membership>(DC.Equal("organizationid", organization.Id.Value))
                .Where(m => !m.Person.Value.Deleted)
                .ToList();
            ValueOne = members.Count().ToString();
            ValueTwo = string.Empty;
            ValueThree = string.Empty;
        }
    }

    public class DashboardViewModel : MasterViewModel
    {
        public List<DashboardItemViewModel> List;

        private void AddRecursive(Translator translator, IDatabase db, Organization organization, int indent)
        {
            List.Add(new DashboardItemViewModel(translator, db, organization, indent));

            foreach (var o in organization.Children)
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
                translator.Get("Dashboard.Members.Row.All", "All members row in the dashbaord", "All persons"),
                string.Empty,
                string.Empty));

            foreach (var o in db
                .Query<Organization>()
                .Where(o => o.Parent.Value == null)
                .OrderBy(o => o.Name.Value[translator.Language]))
            {
                AddRecursive(translator, db, o, 0);
            }
        }
    }

    public class DashboardModule : QuaesturModule
    {
        public DashboardModule()
        {
            RequireCompleteLogin();

            Get("/", parameters =>
            {
                return View["View/dashboard.sshtml",
                    new DashboardViewModel(Translator, Database, CurrentSession)];
            });
        }
    }
}
