namespace Blackbaud.Addin.TokenAuthentication.Exceptions
{
    /// <summary>
    /// Definition for InvalidTokenSignatureException
    /// </summary>
    public class InvalidTokenSignatureException : TokenValidationException
    {
        /// <summary>
        /// Initializes a new instance of the InvalidTokenSignatureException class.
        /// </summary>
        public InvalidTokenSignatureException() : base()
        {
        }
        
        /// <summary>
        /// Initializes a new instance of the InvalidTokenSignatureException class.
        /// </summary>
        /// <param name="message">message describing that the signature of the token is invalid.</param>
        public InvalidTokenSignatureException(string message) : base(message)
        {
        }
    }
}