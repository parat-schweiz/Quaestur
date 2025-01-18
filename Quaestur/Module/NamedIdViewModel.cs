using System;
using System.Collections.Generic;
using System.Linq;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using Newtonsoft.Json;
using SiteLibrary;

namespace Quaestur
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

        public NamedIdViewModel(Translator translator, Country country, bool selected)
        {
            Id = country.Id.ToString();
            Name = country.Name.Value[translator.Language].EscapeHtml();
            Selected = selected;
        }

        public NamedIdViewModel(Translator translator, CustomMenuEntry customMenuEntry, bool selected)
        {
            Id = customMenuEntry.Id.ToString();
            Name = customMenuEntry.Name.Value[translator.Language].EscapeHtml();
            Selected = selected;
        }

        public NamedIdViewModel(Translator translator, CustomPage customPage, bool selected)
        {
            Id = customPage.Id.ToString();
            Name = customPage.Name.Value[translator.Language].EscapeHtml();
            Selected = selected;
        }

        public NamedIdViewModel(Translator translator, State state, bool selected)
        {
            Id = state.Id.ToString();
            Name = state.Name.Value[translator.Language].EscapeHtml();
            Selected = selected;
        }

        public NamedIdViewModel(Translator translator, Organization organization, bool selected)
        {
            Id = organization.Id.ToString();
            Name = organization.Name.Value[translator.Language].EscapeHtml();
            Selected = selected;
        }

        public NamedIdViewModel(Translator translator, ITemplate template, bool selected)
        {
            Id = template.Id.ToString();
            Name = template.Label.EscapeHtml();
            Selected = selected;
        }

        public NamedIdViewModel(Translator translator, BudgetPeriod period, bool selected)
        {
            Id = period.Id.ToString();
            Name = period.GetText(translator).EscapeHtml();
            Selected = selected;
        }

        public NamedIdViewModel(Translator translator, PointBudget budget, bool selected)
        {
            Id = budget.Id.ToString();
            Name = budget.GetText(translator).EscapeHtml();
            Selected = selected;
        }

        public NamedIdViewModel(Translator translator, Tag tag, bool selected)
        {
            Id = tag.Id.ToString();
            Name = tag.Name.Value[translator.Language].EscapeHtml();
            Selected = selected;
        }

        public NamedIdViewModel(Session session, Person person, bool selected)
        {
            Id = person.Id.ToString();
            Name = session.HasAccess(person, PartAccess.Demography, AccessRight.Read) ?
                person.ShortHand.EscapeHtml() : person.UserName.Value.EscapeHtml();
            Selected = selected;
        }

        public NamedIdViewModel(MailingElement element, bool selected)
        {
            Id = element.Id.ToString();
            Name = element.Name.Value.EscapeHtml();
            Selected = selected;
        }

        public NamedIdViewModel(Translator translator, MembershipType type, bool selected)
        {
            Id = type.Id.ToString();
            Name = type.Name.Value[translator.Language].EscapeHtml();
            Selected = selected;
        }

        public NamedIdViewModel(Translator translator, BallotTemplate template, bool selected)
        {
            Id = template.Id.ToString();
            Name = template.Name.Value[translator.Language].EscapeHtml();
            Selected = selected;
        }

        public NamedIdViewModel(Translator translator, Organization organization, MembershipType type, bool selected)
        {
            Id = type.Id.ToString();
            Name = organization.Name.Value[translator.Language].EscapeHtml() + " / " + type.Name.Value[translator.Language].EscapeHtml();
            Selected = selected;
        }

        public NamedIdViewModel(Translator translator, Membership membership, bool selected)
        {
            Id = membership.Id.ToString();
            Name = membership.Organization.Value.Name.Value[translator.Language].EscapeHtml();
            Selected = selected;
        }

        public NamedIdViewModel(Translator translator, Group group, bool selected)
        {
            Id = group.Id.ToString();
            Name = group.Organization.Value.Name.Value[translator.Language].EscapeHtml() + " / " +
                   group.Name.Value[translator.Language].EscapeHtml();
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
