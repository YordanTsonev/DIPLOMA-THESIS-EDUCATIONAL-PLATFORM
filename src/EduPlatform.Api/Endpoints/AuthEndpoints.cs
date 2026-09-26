using EduPlatform.BuildingBlocks.Application.Messaging;
using EduPlatform.Modules.Identity.Application.Authentication;
using EduPlatform.Modules.Identity.Application.Users;
using EduPlatform.Modules.Identity.Contracts;
using EduPlatform.Modules.Identity.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduPlatform.Api.Endpoints;

/// <summary>
/// Registration, sign-in and everything else that produces or destroys a session.
/// </summary>
/// <remarks>
/// The refresh token never appears in a response body. It is written to an <c>HttpOnly</c>
/// cookie, so a cross-site scripting flaw in the Angular app cannot read it; the access token
/// is short-lived and does live in memory on the client.
/// </remarks>
internal static class AuthEndpoints
{
    internal const string RefreshTokenCookie = "eduplatform_refresh";

    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/auth").WithTags("Authentication");

        group.MapPost("/register", async (
                RegisterRequest request,
                IDispatcher dispatcher,
                CancellationToken cancellationToken) =>
            {
                var user = await dispatcher.SendAsync(
                    new RegisterUserCommand(
                        request.Email,
                        request.FirstName,
                        request.MiddleName,
                        request.LastName,
                        request.Password,
                        request.Role),
                    cancellationToken);

                return Results.Created($"/api/v1/users/{user.Id}", user);
            })
            .AllowAnonymous()
            .WithName("Register")
            .WithSummary("Creates a student or parent account and sends a confirmation e-mail.")
            .Produces<UserDto>(StatusCodes.Status201Created);

        group.MapPost("/login", async (
                LoginRequest request,
                HttpContext context,
                IDispatcher dispatcher,
                CancellationToken cancellationToken) =>
            {
                var result = await dispatcher.SendAsync(
                    new SignInCommand(request.Email, request.Password, DescribeDevice(context)),
                    cancellationToken);

                WriteRefreshCookie(context, result);
                return Results.Ok(ToResponse(result));
            })
            .AllowAnonymous()
            .WithName("Login")
            .WithSummary("Exchanges credentials for an access token and a refresh cookie.")
            .Produces<AuthenticationResponse>();

        group.MapPost("/refresh", async (
                HttpContext context,
                IDispatcher dispatcher,
                CancellationToken cancellationToken) =>
            {
                // Read from the cookie, never from the body: the browser attaches it
                // automatically and JavaScript cannot see it.
                var token = context.Request.Cookies[RefreshTokenCookie];

                var result = await dispatcher.SendAsync(
                    new RefreshSessionCommand(token ?? string.Empty, DescribeDevice(context)),
                    cancellationToken);

                WriteRefreshCookie(context, result);
                return Results.Ok(ToResponse(result));
            })
            .AllowAnonymous()
            .WithName("RefreshSession")
            .WithSummary("Rotates the refresh cookie and issues a new access token.")
            .Produces<AuthenticationResponse>();

        group.MapPost("/logout", async (
                HttpContext context,
                IDispatcher dispatcher,
                CancellationToken cancellationToken) =>
            {
                await dispatcher.SendAsync(
                    new SignOutCommand(context.Request.Cookies[RefreshTokenCookie]),
                    cancellationToken);

                context.Response.Cookies.Delete(RefreshTokenCookie, BuildCookieOptions(DateTimeOffset.UnixEpoch));
                return Results.NoContent();
            })
            .AllowAnonymous()
            .WithName("Logout")
            .WithSummary("Revokes the session and clears the refresh cookie.")
            .Produces(StatusCodes.Status204NoContent);

        group.MapPost("/confirm-email", async (
                ConfirmEmailRequest request,
                IDispatcher dispatcher,
                CancellationToken cancellationToken) =>
            {
                await dispatcher.SendAsync(new ConfirmEmailCommand(request.Token), cancellationToken);
                return Results.NoContent();
            })
            .AllowAnonymous()
            .WithName("ConfirmEmail")
            .Produces(StatusCodes.Status204NoContent);

        group.MapPost("/forgot-password", async (
                ForgotPasswordRequest request,
                IDispatcher dispatcher,
                CancellationToken cancellationToken) =>
            {
                await dispatcher.SendAsync(new RequestPasswordResetCommand(request.Email), cancellationToken);

                // Always 204, whether or not the address is known — see the handler.
                return Results.NoContent();
            })
            .AllowAnonymous()
            .WithName("ForgotPassword")
            .WithSummary("Sends a reset link. Reports success even for an unknown address.")
            .Produces(StatusCodes.Status204NoContent);

        group.MapPost("/reset-password", async (
                ResetPasswordRequest request,
                HttpContext context,
                IDispatcher dispatcher,
                CancellationToken cancellationToken) =>
            {
                await dispatcher.SendAsync(
                    new ResetPasswordCommand(request.Token, request.NewPassword),
                    cancellationToken);

                // The reset revoked every session, so the cookie in this browser is dead too.
                context.Response.Cookies.Delete(RefreshTokenCookie, BuildCookieOptions(DateTimeOffset.UnixEpoch));
                return Results.NoContent();
            })
            .AllowAnonymous()
            .WithName("ResetPassword")
            .Produces(StatusCodes.Status204NoContent);

        group.MapGet("/me", async (IDispatcher dispatcher, CancellationToken cancellationToken) =>
                Results.Ok(await dispatcher.QueryAsync(new GetCurrentUserQuery(), cancellationToken)))
            .RequireAuthorization()
            .WithName("GetCurrentUser")
            .WithSummary("The signed-in user, so the client can restore state after a reload.")
            .Produces<UserDto>();

        return endpoints;
    }

    private static AuthenticationResponse ToResponse(AuthenticationResult result) =>
        new(result.AccessToken, result.AccessTokenExpiresAt, result.User);

    private static void WriteRefreshCookie(HttpContext context, AuthenticationResult result) =>
        context.Response.Cookies.Append(
            RefreshTokenCookie,
            result.RefreshToken,
            BuildCookieOptions(result.RefreshTokenExpiresAt));

    private static CookieOptions BuildCookieOptions(DateTimeOffset expiresAt) => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Expires = expiresAt,

        // Scoped to the auth endpoints, so it is not attached to every ordinary API call.
        Path = "/api/v1/auth",
    };

    /// <summary>
    /// A short description of the browser, shown when a user reviews their own sessions.
    /// Truncated because the header is attacker-controlled and can be arbitrarily long.
    /// </summary>
    private static string? DescribeDevice(HttpContext context)
    {
        var agent = context.Request.Headers.UserAgent.ToString();
        return string.IsNullOrWhiteSpace(agent) ? null : agent[..Math.Min(agent.Length, 400)];
    }
}

internal sealed record RegisterRequest(
    string Email,
    string FirstName,
    string? MiddleName,
    string LastName,
    string Password,
    UserRole Role);

internal sealed record LoginRequest(string Email, string Password);

internal sealed record ConfirmEmailRequest(string Token);

internal sealed record ForgotPasswordRequest(string Email);

internal sealed record ResetPasswordRequest(string Token, string NewPassword);

/// <summary>What the client receives. The refresh token is absent by design — it is in a cookie.</summary>
internal sealed record AuthenticationResponse(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    UserDto User);
