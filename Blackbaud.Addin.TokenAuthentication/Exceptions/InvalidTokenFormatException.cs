namespace Blackbaud.Addin.TokenAuthentication.Exceptions
{
    /// <summary>
    /// Definition for InvalidTokenFormatException
    /// </summary>
    public class InvalidTokenFormatException : TokenValidationException
    {
        /// <summary>
        /// Initializes a new instance of the InvalidTokenFormatException class.
        /// </summary>
        public InvalidTokenFormatException() : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the InvalidTokenFormatException class.
        /// </summary>
        /// <param name="message">message describing that the token format is invalid</param>
        public InvalidTokenFormatException(string message) : base(message)
        {
        }
    }
}