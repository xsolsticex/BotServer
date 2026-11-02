namespace BotServer.Configuration
{
    public static class AppEnvironment
    {
        /// <summary>
        /// Define si el entorno actual es "local" o "remote".
        /// Cambiar este valor en este único archivo afecta a toda la aplicación.
        /// </summary>
        public static string Mode = "remote";

        public static bool IsLocal => Mode.Equals("local", StringComparison.OrdinalIgnoreCase);
        public static bool IsRemote => Mode.Equals("remote", StringComparison.OrdinalIgnoreCase);
    }
}
