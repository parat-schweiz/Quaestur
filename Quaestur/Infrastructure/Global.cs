using System;

namespace Quaestur
{
    public static class Global
    {
		private static Config _config;
		private static Logger _logger;
		private static Mailer _mailer;
        private static SessionManager _login;

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

        public static Config Config
		{
			get
			{
				if (_config == null)
				{
					_config = new Config();
					_config.Load("/Security/Test/quaestur.xml");
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
