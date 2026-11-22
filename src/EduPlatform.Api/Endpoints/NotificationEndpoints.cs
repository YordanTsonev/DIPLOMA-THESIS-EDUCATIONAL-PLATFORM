using EduPlatform.BuildingBlocks.Application.Messaging;
using EduPlatform.Modules.Identity.Application.Abstractions;
using EduPlatform.Modules.Notifications.Application.UseCases;
using EduPlatform.Modules.Notifications.Contracts;

namespace EduPlatform.Api.Endpoints;

internal static class NotificationEndpoints
{
    public static IEndpointRouteBuilder MapNotificationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/notifications")
            .WithTags("Notifications")
            .RequireAuthorization();

        group.MapGet("/", async (
                int? limit,
                ICurrentUser currentUser,
                IDispatcher dispatcher,
                CancellationToken cancellationToken) =>
            {
                if (currentUser.UserId is not { } userId)
                {
                    return Results.Unauthorized();
                }

                return Results.Ok(await dispatcher.QueryAsync(
                    new ListMyNotificationsQuery(userId, limit ?? 30), cancellationToken));
            })
            .WithName("ListMyNotifications")
            .Produces<NotificationFeedDto>();

        group.MapPut("/{id:guid}/read", async (
                Guid id,
                ICurrentUser currentUser,
                IDispatcher dispatcher,
                CancellationToken cancellationToken) =>
            {
                if (currentUser.UserId is not { } userId)
                {
                    return Results.Unauthorized();
                }

                await dispatcher.SendAsync(new MarkNotificationReadCommand(id, userId), cancellationToken);
                return Results.NoContent();
            })
            .WithName("MarkNotificationRead")
            .Produces(StatusCodes.Status204NoContent);

        group.MapPut("/read-all", async (
                ICurrentUser currentUser,
                IDispatcher dispatcher,
                CancellationToken cancellationToken) =>
            {
                if (currentUser.UserId is not { } userId)
                {
                    return Results.Unauthorized();
                }

                await dispatcher.SendAsync(new MarkAllNotificationsReadCommand(userId), cancellationToken);
                return Results.NoContent();
            })
            .WithName("MarkAllNotificationsRead")
            .Produces(StatusCodes.Status204NoContent);

        return endpoints;
    }
}
