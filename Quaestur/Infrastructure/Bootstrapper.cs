using System;
using Nancy;
using Nancy.Bootstrapper;
using Nancy.TinyIoc;
using Nancy.Authentication.Forms;
using Nancy.Conventions;
using Nancy.Cryptography;
using System.Security.Cryptography;
using System.Text;
using System.IO;

namespace Quaestur
{
    public class CustomBoostrapper : DefaultNancyBootstrapper
    {
        protected override void ConfigureConventions(NancyConventions conventions)
        {
            base.ConfigureConventions(conventions);

            conventions.StaticContentsConventions.Add(
                StaticContentConventionBuilder.AddDirectory("assets", "Assets")
            );
        }
    }

    public class Startup : IApplicationStartup
    {
        public void Initialize(IPipelines pipelines)
        {
            var formsAuthConfiguration =
                new FormsAuthenticationConfiguration()
                {
                    RedirectUrl = "~/login",
                    UserMapper = Global.Sessions,
                    CryptographyConfiguration = 
                        new CryptographyConfiguration(
                            new AesEncryptionProvider(new RandomKeyGenerator(), Global.Config.SessionEncryptionKey),
                            new DefaultHmacProvider(Global.Config.SessionAuthenticationKey)),
                };

            FormsAuthentication.Enable(pipelines, formsAuthConfiguration);

            pipelines.OnError += HandleException;
        }

        private static Response HandleException(NancyContext context, Exception exception)
        {
            Global.Log.Error(exception.ToString());
            return null;
        }
    }
}