namespace Blackbaud.Addin.TokenAuthentication.Exceptions
{
    /// <summary>
    /// Definition for TokenExpiredException
    /// </summary>
    public class TokenExpiredException : TokenValidationException
    {
        /// <summary>
        /// Initializes a new instance of the TokenExpiredException class.
        /// </summary>
        /// <param name="message">message describing that the token is expired</param>
        public TokenExpiredException(string message) : base(message)
        {
        }
    }
}