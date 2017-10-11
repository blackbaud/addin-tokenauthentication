using System;

namespace Blackbaud.Addin.TokenAuthentication.Exceptions

{
    /// <summary>
    /// Definition for TokenValidationException
    /// </summary>
    public abstract class TokenValidationException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the TokenValidationException class
        /// </summary>
        public TokenValidationException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the TokenValidationException class
        /// </summary>
        /// <param name="message">message to show</param>
        /// <param name="innerException">Inner exceptioin object.</param>
        public TokenValidationException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the TokenValidationException class
        /// </summary>
        /// <param name="message">information about the exceptions cause</param>
        public TokenValidationException(string message) : base(message)
        {
        }
    }
}