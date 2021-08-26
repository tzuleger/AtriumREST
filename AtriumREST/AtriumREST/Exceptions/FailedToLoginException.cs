using System;

namespace ThreeRiversTech.Zuleger.Atrium.REST.Exceptions
{
    /// <summary>
    /// Thrown when the UserID returned from the second Atrium Controller Answer is -1.
    /// </summary>
    public class FailedToLoginException : Exception
    {
        /// <summary>
        /// Thrown when a login fails where it gets past all phases but the User ID returned is "-1".
        /// </summary>
        public FailedToLoginException() : base("Login failed.") { }
    }
}
