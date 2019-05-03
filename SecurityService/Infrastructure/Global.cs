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

        public static Config Config
		{
			get
			{
				if (_config == null)
				{
					_config = new Config();
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
					_logger = new Logger(Config.LogFilePrefix);
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
