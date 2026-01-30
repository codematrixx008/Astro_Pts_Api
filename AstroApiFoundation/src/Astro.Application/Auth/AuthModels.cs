namespace Astro.Application.Auth;

public sealed record RegisterRequest(
    string Email,
    string Password,
    string OrganizationName
);

public sealed record LoginRequest(
    string Email,
    string Password
);

public sealed record AuthTokens(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresUtc,
    DateTime RefreshTokenExpiresUtc
);

public sealed record RefreshRequest(
    string RefreshToken
);
