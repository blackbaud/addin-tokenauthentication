# Validating the user identity token 
Before looking for a user mapping, add-in developers should first validate the signature of the user identity token against the OpenIDConnect endpoint within SKY API OAuth 2.0 service. This prevents certain types of attack vectors and provides a mechanism for the add-in to securely convey the Blackbaud user's identity to its own backend.

[SKY API OpenIDConnect configuration](https://oauth2.sky.blackbaud.com/.well-known/openid-configuration)

Developers building add-ins in .NET can make use of this Blackbaud-provided library to assist with validating the UIT. This library is distributed as a NuGet package named [Blackbaud.Addin.tokenAuthentication](https://www.nuget.org/packages/Blackbaud.Addin.tokenAuthentication).

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
