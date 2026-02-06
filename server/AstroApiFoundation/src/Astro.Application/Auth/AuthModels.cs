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
    DateTime AccessTokenExpiresUtc,
    DateTime RefreshTokenExpiresUtc
);

// Cookie-based refresh: refresh token is stored in HttpOnly cookie.
// This request is kept for forward compatibility (e.g., mobile clients) but is unused by the web flow.
public sealed record RefreshRequest(
    string? RefreshToken
);
