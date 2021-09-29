using System;

namespace ThreeRiversTech.Zuleger.Atrium.REST.Exceptions
{
    /// <summary>
    /// Thrown when multiple objects exist in the Atrium Controller with duplicate Guids.
    /// </summary>
    class DuplicateGuidException : Exception
    {
        /// <summary>
        /// Thrown when multiple objects exist in the Atrium Controller with duplicate Guids.
        /// </summary>
        public DuplicateGuidException(String guid) : base($"Multiple objects exist in the Atrium Controller with the Guid {guid}") { }
    }
}
