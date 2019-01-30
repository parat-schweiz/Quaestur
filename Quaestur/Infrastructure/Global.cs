using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;

namespace Quaestur
{
    public static class Global
    {
		private static Config _config;
		private static Logger _logger;
		private static Mailer _mailer;
        private static SessionManager _login;
        private static SecurityThrottle _throttle;

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

        public static SessionManager Sessions
        {
            get 
            {
                if (_login == null)
                {
                    _login = new SessionManager(); 
                }

                return _login;
            } 
        }

        private static IEnumerable<string> ConfigPaths
        {
            get
            {
                yield return "/Security/Test/quaestur.xml";
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

        public static IDatabase CreateDatabase()
        {
            return new PostgresDatabase(Config); 
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
					_mailer = new Mailer(Log, Config);
                }

				return _mailer;
            }
        }
    }
}
