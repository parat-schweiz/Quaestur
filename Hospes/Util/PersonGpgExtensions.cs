using System;
using System.Linq;
using BaseLibrary;

namespace Hospes
{
    public static class PersonGpgExtensions
    {
        public static GpgPublicKeyInfo GetPublicKey(this Person person)
        {
            return person.PublicKeys
                .Where(k => k.Type.Value == PublicKeyType.OpenPGP)
                .Select(k => CheckPublicKey(person, k))
                .FirstOrDefault(k => k != null);
        }

        private static GpgPublicKeyInfo CheckPublicKey(Person person, PublicKey key)
        {
            var keyInfo = Global.Gpg.ImportKeys(key.Data.Value).FirstOrDefault();

            if (keyInfo.Status != GpgKeyStatus.Active)
            {
                return null;
            }

            if (!keyInfo.Uids.Any(u => u.Mail == person.PrimaryMailAddress &&
                                       u.Trust <= GpgTrust.Marginal))
            {
                return null;
            }

            return new GpgPublicKeyInfo(keyInfo.Id);
        }

        public static GpgPublicKeyInfo GetPublicKey(this ServiceAddress address)
        {
            return address.Person.Value.PublicKeys
                .Where(k => k.Type.Value == PublicKeyType.OpenPGP)
                .Select(k => CheckPublicKey(address, k))
                .FirstOrDefault(k => k != null);
        }

        private static GpgPublicKeyInfo CheckPublicKey(ServiceAddress address, PublicKey key)
        {
            var keyInfo = Global.Gpg.ImportKeys(key.Data.Value).FirstOrDefault();

            if (keyInfo.Status != GpgKeyStatus.Active)
            {
                return null;
            }

            if (!keyInfo.Uids.Any(u => u.Mail == address.Address.Value &&
                                       u.Trust <= GpgTrust.Marginal))
            {
                return null;
            }

            return new GpgPublicKeyInfo(keyInfo.Id);
        }
    }
}
