using System;

namespace ThreeRiversTech.Zuleger.Atrium.REST.Exceptions
{
    /// <summary>
    /// Thrown when an SDK Request returns back with err not equaling "ok".
    /// </summary>
    public class SdkRequestException : Exception
    {
        /// <summary>
        /// Message to be printed when thrown.
        /// </summary>
        public override String Message { get => _msg; }
        private String _msg;

        /// <summary>
        /// Thrown when an SDK Request returns back with err not equaling "ok".
        /// </summary>
        public SdkRequestException() : base($"Controller responded with an unsuccessful status code for one or more XML requests.") { }

        /// <summary>
        /// Thrown when an SDK Request returns back with err not equaling "ok".
        /// </summary>
        /// <param name="errcode">Error Code that was given instead of "ok"</param>
        public SdkRequestException(String errcode)
        {
            _msg = $"Controller responded with an unsuccessful status code: {errcode}.";
        }
    }
}
