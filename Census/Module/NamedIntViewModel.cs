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
    public class NamedIntViewModel
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

        public NamedIntViewModel(string name, bool disabled, bool selected)
        {
            Value = string.Empty;
            Name = name.EscapeHtml();
            Disabled = disabled;
            Selected = selected;
        }

        public NamedIntViewModel(int value, string name, bool selected)
        {
            Value = value.ToString();
            Name = name.EscapeHtml();
            Selected = selected;
        }

        public NamedIntViewModel(Translator translator, VariableType type, bool selected)
            : this((int)type, type.Translate(translator), selected)
        {
        }

        public NamedIntViewModel(Translator translator, VariableModification modification, bool selected)
            : this((int)modification, modification.Translate(translator), selected)
        {
        }

        public NamedIntViewModel(Translator translator, VariableModification modification, Func<VariableModification, bool> selected)
            : this((int)modification, modification.Translate(translator), selected(modification))
        {
        }

        public NamedIntViewModel(Translator translator, Language language, bool selected)
            : this((int)language, language.Translate(translator), selected)
        {
        }

        public NamedIntViewModel(Translator translator, QuestionType type, bool selected)
            : this((int)type, type.Translate(translator), selected)
        {
        }

        public NamedIntViewModel(Translator translator, PartAccess part, bool selected)
            : this((int)part, part.Translate(translator), selected)
        {
        }

        public NamedIntViewModel(Translator translator, SubjectAccess subject, bool selected)
            : this((int)subject, subject.Translate(translator), selected)
        {
        }

        public NamedIntViewModel(Translator translator, AccessRight right, bool selected)
            : this((int)right, right.Translate(translator), selected)
        {
        }
    }
}
