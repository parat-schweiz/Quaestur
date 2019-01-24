using System;
using System.Linq;
using System.Collections.Generic;

namespace Quaestur
{
    public class PersonContentProvider : IContentProvider
    {
        private readonly Translator _translator;
        private readonly Person _person;

        public PersonContentProvider(Translator translator, Person person)
        {
            _translator = translator;
            _person = person;
        }

        public string Prefix
        {
            get { return "Person"; } 
        }

        private string CreatePostalAddressFiveLines()
        {
            var address = _person.PrimaryPostalAddressFiveLines(_translator);
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
                case "Person.FirstName":
                    return _person.FirstName.Value;
                case "Person.ShortFirstNames":
                    return _person.ShortFirstNames;
                case "Person.FullFirstNames":
                    return _person.FullFirstNames;
                case "Person.LastName":
                    return _person.LastName.Value;
                case "Person.ShortHand":
                    return _person.ShortHand;
                case "Person.ShortTitleAndNames":
                    return _person.ShortTitleAndNames;
                case "Person.FullName":
                    return _person.FullName;
                case "Person.UserName":
                    return _person.UserName.Value;
                case "Person.PrimaryMailAddress":
                    return _person.PrimaryMailAddress;
                case "Person.Language":
                    return _person.Language.Value.Translate(_translator);
                case "Person.BirthDate":
                    return _person.BirthDate.Value.ToString("dd.MM.yyyy");
                case "Person.Address.FiveLines":
                    return CreatePostalAddressFiveLines();
                default:
                    throw new NotSupportedException();
            }
        }
    }
}
