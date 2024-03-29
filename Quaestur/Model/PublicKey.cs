﻿using System;
using System.Text;
using System.Collections.Generic;
using SiteLibrary;

namespace Quaestur
{
    public enum PublicKeyType
    { 
        OpenPGP,
    }

    public static class PublicKeyTypeExtensions
    {
        public static string Translate(this PublicKeyType type, Translator translator)
        {
            switch(type)
            {
                case PublicKeyType.OpenPGP:
                    return translator.Get("Enum.PublicKeyType.OpenPGP", "OpenPGP value in the public key type enum", "OpenPGP");
                default:
                    throw new NotSupportedException();
            }
        }
    }

    public class PublicKey : DatabaseObject
    {
        public ForeignKeyField<Person, PublicKey> Person { get; private set; }
        public ByteArrayField Data { get; private set; }
        public StringField KeyId { get; private set; }
        public EnumField<PublicKeyType> Type { get; private set; }

        public PublicKey() : this(Guid.Empty)
        {
        }

		public PublicKey(Guid id) : base(id)
        {
            Person = new ForeignKeyField<Person, PublicKey>(this, "personid", false, p => p.PublicKeys);
            Data = new ByteArrayField(this, "data", false);
            KeyId = new StringField(this, "keyid", 256);
            Type = new EnumField<PublicKeyType>(this, "type", PublicKeyType.OpenPGP, PublicKeyTypeExtensions.Translate);
        }

        public string ShortKeyId
        { 
            get
            {
                if (KeyId.Value.Length > 8)
                {
                    return KeyId.Value.Substring(KeyId.Value.Length - 8, 8);
                }
                else
                {
                    return KeyId.Value; 
                }
            }
        }

        public override string GetText(Translator translator)
        {
            return translator.Get(
                "PublicKey.Text",
                "Textual representation of a public key",
                "{0} {1}",
                Type.Value.Translate(translator),
                ShortKeyId);
        }

        public override void Delete(IDatabase database)
        {
            database.Delete(this);
        }
    }
}
