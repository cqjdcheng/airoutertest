using CheapAI.Domain.Admin;

namespace CheapAI.Application.Common.Abstractions;

public interface IAccessTokenService
{
    string CreateToken(AdminUser adminUser);
}
