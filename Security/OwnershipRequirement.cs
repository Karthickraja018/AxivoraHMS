using Microsoft.AspNetCore.Authorization;

namespace Axivora.Security
{
    /// <summary>
    /// Requirement marker used by the resource-based ownership authorization policy.
    /// Controllers pass the domain entity as the resource when calling
    /// <c>_authorizationService.AuthorizeAsync(User, resource, "ResourceOwner")</c>.
    /// </summary>
    public class OwnershipRequirement : IAuthorizationRequirement { }
}
