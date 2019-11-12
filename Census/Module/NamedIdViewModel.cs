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
    public class NamedIdViewModel
    {
        public string Id;
        public string Name;
        public bool Disabled;
        public bool Selected;

        public string Options
        {
            get
            {
                var options = string.Empty;

                if (Disabled)
                {
                    options += " disabled"; 
                }

                if (Selected)
                {
                    options += " selected";
                }

                return options;
            }
        }

        public NamedIdViewModel(string name, bool disabled, bool selected)
        {
            Id = string.Empty;
            Name = name.EscapeHtml();
            Disabled = disabled;
            Selected = selected;
        }

        public NamedIdViewModel(Translator translator, Variable variable, bool selected)
        {
            Id = variable.Id.ToString();
            Name = variable.GetText(translator);
            Selected = selected;
        }

        public NamedIdViewModel(Translator translator, Group group, bool selected)
        {
            Id = group.Id.ToString();
            Name = group.GetText(translator);
            Selected = selected;
        }

        public NamedIdViewModel(Translator translator, Organization organization, bool selected)
        {
            Id = organization.Id.ToString();
            Name = organization.Name.Value[translator.Language].EscapeHtml();
            Selected = selected;
        }

        public NamedIdViewModel(Translator translator, MasterRole masterRole, bool selected)
        {
            Id = masterRole.Id.ToString();
            Name = masterRole.Name.Value[translator.Language].EscapeHtml();
            Selected = selected;
        }

        public NamedIdViewModel(Translator translator, Role role, bool selected)
        {
            Id = role.Id.ToString();
            Name = role.Group.Value.Organization.Value.Name.Value[translator.Language].EscapeHtml() + " / " +
                   role.Group.Value.Name.Value[translator.Language].EscapeHtml() + " / " +
                   role.Name.Value[translator.Language].EscapeHtml();
            Selected = selected;
        }
    }
}
