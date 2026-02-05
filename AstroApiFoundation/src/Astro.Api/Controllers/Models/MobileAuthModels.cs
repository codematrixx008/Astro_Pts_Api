namespace Astro.Api.Controllers.Models;

// NEW: Mobile auth request/response models
public sealed record MobileRegisterRequest(
    string Email,
    string Password,
    string OrganizationName);

public sealed record MobileLoginRequest(
    string Email,
    string Password);

public sealed record MobileRefreshRequest(
    string RefreshToken);

// Returned to mobile: includes refresh token in body (store in SecureStore)
public sealed record MobileAuthResponse(
    string AccessToken,
    DateTime AccessTokenExpiresUtc,
    DateTime RefreshTokenExpiresUtc,
    string RefreshToken);
