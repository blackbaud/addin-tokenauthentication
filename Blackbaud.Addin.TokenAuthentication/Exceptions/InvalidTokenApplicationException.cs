namespace Blackbaud.Addin.TokenAuthentication.Exceptions
{
    /// <summary>
    /// Definition for InvalidTokenApplicationException
    /// </summary>
    public class InvalidTokenApplicationException : TokenValidationException
    {
        /// <summary>
        /// Initializes a new instance of the InvalidTokenApplicationException class.
        /// </summary>
        /// <param name="message">message describing that the application is invalid</param>
        public InvalidTokenApplicationException(string message) : base(message)
        {
        }
    }
}