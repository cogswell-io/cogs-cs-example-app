namespace Gambit.Client
{
    using System;
    using GambitCore;

    /* You can place this file in your project configuration directory or wherever you like */

    /// <summary>
    /// Configure GambitSDK Defaults 
    /// </summary>
    public static class GambitConfig
    {
        /// <summary>
        /// This method will configure the settings that are provided to <see cref="GambitSDKService" />
        /// when it is used with it's default constructor.
        /// It's mandatory to confugre "BaseUrl" and "SocketUrl" the reset of the properties have
        /// default values.
        /// 
        /// You must call this method once in you main method (app start point)
        /// before using the <see cref="GambitSDKService"/>
        /// </summary>
        public static void Init()
        {
            GambitSettings.DefaultSettings = new GambitSettings
            {
                BaseUrl = "https://api.cogswell.io",
                SocketUrl = "wss://api.cogswell.io:443/push",
                RetryCount = 2,
                Timeout = 30,
                ReceivedChunkSize = 1024 * 5,
                DefaultMaxSessionIdleTimeout = TimeSpan.FromMilliseconds(300 * 1000)
            };
        }
    }
}
