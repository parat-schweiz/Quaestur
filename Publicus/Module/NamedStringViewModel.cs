using System;
using System.Collections.Generic;
using System.Linq;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using Newtonsoft.Json;

namespace Publicus
{
    public class NamedStringViewModel
    {
        public string Value;
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

        public NamedStringViewModel(string name, bool disabled, bool selected)
        {
            Value = string.Empty;
            Name = name.EscapeHtml();
            Disabled = disabled;
            Selected = selected;
        }

        public NamedStringViewModel(string value, string name, bool selected)
        {
            Value = value.EscapeHtml();
            Name = name.EscapeHtml();
            Selected = selected;
        }
    }
}
