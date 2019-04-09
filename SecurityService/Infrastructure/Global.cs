using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using BaseLibrary;

namespace SecurityService
{
    public static class Global
    {
		private static Config _config;
		private static Logger _logger;
		private static Mailer _mailer;
        private static SecurityThrottle _throttle;
        private static SecurityService _service;
        private static Gpg _gpg;

        public static SecurityService Service
        {
            get
            {
                if (_service == null)
                {
                    _service = new SecurityService(
                        Log, 
                        Gpg,
                        Config.PresharedKey,
                        Config.SecretKey);
                }

                return _service;
            }
        }

        public static Gpg Gpg
        {
            get
            {
                if (_gpg == null)
                {
                    _gpg = new LocalGpg(LocalGpg.LinuxGpgBinaryPath, Config.GpgHomedir);
                }

                return _gpg;
            }
        }

        public static SecurityThrottle Throttle
        {
            get
            {
                if (_throttle == null)
                {
                    _throttle = new SecurityThrottle();
                }

                return _throttle;
            }
        }

        private static IEnumerable<string> ConfigPaths
        {
            get
            {
                yield return "/Security/Test/securityservice.xml";
                yield return "config.xml";
            }
        }

        private static string FirstFileExists(IEnumerable<string> paths)
        {
            foreach (var path in paths)
            {
                if (File.Exists(path))
                {
                    return path; 
                }
            }

            return null;
        }

        public static Config Config
		{
			get
			{
				if (_config == null)
				{
					_config = new Config();
					_config.Load(FirstFileExists(ConfigPaths));
				}

				return _config;
			}
		}

		public static Logger Log
        {
            get
            {
				if (_logger == null)
                {
					_logger = new Logger();
                }

				return _logger;
            }
        }

		public static Mailer Mail
		{
            get
            {
				if (_mailer == null)
                {
					_mailer = new Mailer(Log, Config, Gpg);
                }

				return _mailer;
            }
        }
    }
}
