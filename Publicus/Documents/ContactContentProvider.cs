using System;
using System.Linq;
using System.Collections.Generic;

namespace Publicus
{
    public class ContactContentProvider : IContentProvider
    {
        private readonly Translator _translator;
        private readonly Contact _contact;

        public ContactContentProvider(Translator translator, Contact contact)
        {
            _translator = translator;
            _contact = contact;
        }

        public string Prefix
        {
            get { return "Contact"; } 
        }

        private string CreatePostalAddressFiveLines()
        {
            var address = _contact.PrimaryPostalAddressFiveLines(_translator);
            var lines = address.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
            var latexLines = new List<string>();

            foreach (var line in lines)
            {
                if (line.Trim().Length < 1)
                {
                    latexLines.Add("~");
                }
                else
                {
                    latexLines.Add(line.Trim());
                }
            }

            return string.Join(@"\\" + Environment.NewLine, latexLines);
        }

        public string GetContent(string variable)
        {
            switch (variable)
            {
                case "Contact.FirstName":
                    return _contact.FirstName.Value;
                case "Contact.ShortFirstNames":
                    return _contact.ShortFirstNames;
                case "Contact.FullFirstNames":
                    return _contact.FullFirstNames;
                case "Contact.LastName":
                    return _contact.LastName.Value;
                case "Contact.ShortHand":
                    return _contact.ShortHand;
                case "Contact.ShortTitleAndNames":
                    return _contact.ShortTitleAndNames;
                case "Contact.FullName":
                    return _contact.FullName;
                case "Contact.Organization":
                    return _contact.Organization.Value;
                case "Contact.PrimaryMailAddress":
                    return _contact.PrimaryMailAddress;
                case "Contact.Language":
                    return _contact.Language.Value.Translate(_translator);
                case "Contact.BirthDate":
                    return _contact.BirthDate.Value.ToString("dd.MM.yyyy");
                case "Contact.Address.FiveLines":
                    return CreatePostalAddressFiveLines();
                default:
                    throw new NotSupportedException();
            }
        }
    }
}
