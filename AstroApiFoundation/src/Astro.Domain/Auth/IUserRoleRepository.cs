using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Astro.Domain.Auth;

public interface IUserRoleRepository
{
    Task<IReadOnlyList<string>> GetRoleCodesAsync(long userId, CancellationToken ct);
    Task EnsureUserHasRoleAsync(long userId, string roleCode, long? createdBy, CancellationToken ct);
}

