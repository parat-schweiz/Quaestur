using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections.Generic;

namespace SiteLibrary
{
    public abstract class TemplateDocument : LatexDocument
    {
        protected abstract Templator GetTemplator();

        protected TemplateDocument() { }

        protected abstract string TexTemplate { get; }

        public override string TexDocument
        {
            get 
            {
                var templator = GetTemplator();
                return templator.Apply(TexTemplate);
            }
        }
    }
}
