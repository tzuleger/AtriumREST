using System;

namespace ThreeRiversTech.Zuleger.Atrium.REST.Exceptions
{
    /// <summary>
    /// Thrown when integrity is invalidated when checksums are compared between remote encrypted message and remote checksum
    /// </summary>
    class IntegrityException : Exception
    {
        /// <summary>
        /// Thrown when integrity is invalidated when checksums are compared between remote encrypted message and remote checksum
        /// </summary>
        public IntegrityException() : base("Checksum of the decrypted message does not match with the remote checksum.") { }
    }
}
