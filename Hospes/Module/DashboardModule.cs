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
            ValueTwo = members.Count(m => m.Type.Value.Rights.Value.HasFlag(MembershipRight.Voting)).ToString();

            foreach (var m in members)
            {
                if (!m.HasVotingRight.Value.HasValue)
                {
                    m.UpdateVotingRight(db);
                    db.Save(m); 
                }
            }

            ValueThree = members.Count(m => m.HasVotingRight.Value.Value).ToString();
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
                translator.Get("Dashboard.Members.Row.Full", "Full members row in the dashbaord", "Full members"),
                translator.Get("Dashboard.Members.Row.Voting", "Voting members row in the dashbaord", "Voting rights")));

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
