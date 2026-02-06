using Astro.Domain.Profile;
using Astro.Infrastructure.Data;
using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Astro.Infrastructure.Repositories
{
    public class ProfileRepository : IProfileRepository
    {
        private readonly IDbConnectionFactory _db;

        public ProfileRepository(IDbConnectionFactory db) => _db = db;

        public async Task<string?> GetProfileImageAsync(int userId)
        {
            using var conn = _db.Create();

            var sql = @"
                SELECT ProfileImagePath
                FROM dbo.[Users]
                WHERE UserId = @UserId;
            ";

            return await conn.ExecuteScalarAsync<string?>(sql, new
            {
                UserId = userId
            });
        }

        public async Task<string> UpdateProfileImageAsync(int userId, string imagePath)
        {
            try
            {
                using var conn = _db.Create();

                var sql = @"
            UPDATE dbo.[Users]
            SET ProfileImagePath = @ImagePath
            OUTPUT inserted.ProfileImagePath
            WHERE UserId = @UserId;
        ";

                var result = await conn.ExecuteScalarAsync<string>(sql, new
                {
                    UserId = userId,
                    ImagePath = imagePath
                });

                if (string.IsNullOrWhiteSpace(result))
                    throw new InvalidOperationException("User not found");

                return result;
            }
            catch (InvalidOperationException)
            {
                // business exception → let controller handle it
                throw;
            }
            catch (Exception ex)
            {
                // log + wrap infra errors
                // (replace Console.WriteLine with ILogger in real apps)
                Console.WriteLine(ex);

                throw new Exception("Failed to update profile image", ex);
            }
        }



    }
}
