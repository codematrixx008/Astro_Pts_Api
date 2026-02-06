using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Astro.Domain.Profile
{
    public interface IProfileRepository
    {
        Task<string?> GetProfileImageAsync(int userId);
        Task<string> UpdateProfileImageAsync(int userId, string imagePath);
    }
}
