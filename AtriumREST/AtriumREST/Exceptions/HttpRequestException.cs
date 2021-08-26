using System;

namespace ThreeRiversTech.Zuleger.Atrium.REST.Exceptions
{
    /// <summary>
    /// Thrown when an HTTP Request fails, usually when some encryption went wrong.
    /// </summary>
    public class HttpRequestException : Exception
    {
        /// <summary>
        /// Thrown when an HTTP Request is made using Encryption but the Response did not contain expected Encryption variables.
        /// </summary>
        /// <param name="responseString"></param>
        public HttpRequestException(String responseString) : base("Request failed: " + responseString) { }
    }
}
