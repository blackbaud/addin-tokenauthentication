# SKY API addin tokenauthentication
The addin-tokenauthentication library gives SKY APIdevelopers building SKY API add-ins in .NET the ability to validate user identity tokens. 

# Installation

This library is distributed as a NuGet package named [Blackbaud.Addin.tokenAuthentication](https://www.nuget.org/packages/Blackbaud.Addin.tokenAuthentication).

# SKY API add-ins
SKY API add-ins support a single-sign-on (SSO) mechanism that can be used to correlate the Blackbaud user with a user in the add-in's native system.

Within the [Add-in Client JavaScript library](https://github.com/blackbaud/sky-api-addin), the AddinClient class provides a `getAuthtoken` function for getting a short-lived "user identity token" from the host page/application/SPA. This token is a signed value that is issued to the SKY API application and represents the Blackbaud user's identity.

The general flow is that when an add-in is instantiated, it can request a user identity token from the host page using the `getAuthtoken` function. The host will in turn request a user identity token from the SKY API OAuth 2.0 service. The token (a JWT) will be addressed to the SKY API application, and will contain the user's unique identifier (BBID). The OAuth service will return the token to the host, and the host will pass the token to the add-in iframe. The add-in can then pass the token to its own backend, where it can be validated and used to look up a user in the add-in's native system. If a user mapping exists, then the add-in can present content to the user. If no user mapping exists, the add-in can prompt the user to login. Once the user's identity in the native system is known, the add-in can persist the user mapping so that on subsequent loads the user doesn't have to log in again (even across devices).

Note that the user identity token is a JWT that is signed by the SKY API OAuth 2.0 service, but it cannot be used to make calls to the SKY API. In order to make SKY API calls, a proper SKY API access token must be obtained.

This flow is illustrated below:

![flow](https://sky.blackbaudcdn.net/skyuxapps/uiextensibility-docs/assets/add-in-sso.6121e14a352d0208ae00c92a4a5274d824550356.png)

Add-ins can makes the following request upon initialization to obtain a user identity token (typically handled within the init callback):

```
var client = new AddinClient({...});
client.getAuthToken().then((token) => {
  var userIdentityToken = token;
  . . .
});
```

# Validating the user identity token 
Before looking for a user mapping, add-in developers should first validate the signature of the user identity token against the OpenIDConnect endpoint within SKY API OAuth 2.0 service. This prevents certain types of attack vectors and provides a mechanism for the add-in to securely convey the Blackbaud user's identity to its own backend.

[SKY API OpenIDConnect configuration](https://oauth2.sky.blackbaud.com/.well-known/openid-configuration)

# Example validation code

```
// this represents the user identity token returned from getAuthToken()
var rawToken = "(raw token value)";

// this is the ID of the developer's SKY API application
var applicationId = "(some application ID)";

// create and validate the user identity token
UserIdentityToken uit;
try
{
    uit = await UserIdentityToken.ParseAsync(rawToken, applicationId);

    // if valid, the UserId property contains the Blackbaud user's ID
    var userId = uit.UserId;
}
catch (TokenValidationException ex)
{
    // process the exception
}
```

Once the token has been validated, the add-in's backend will know the Blackbaud user ID and can determine if a user mapping exists for a user in the add-in's system. If a mapping exists, then the add-in's backend can immediately present the content for the add-in. If no user mapping exists, the add-in can prompt the user to login.
