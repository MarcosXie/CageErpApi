using FlyGates.Application.Services.Halloween;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace FlyGates.Api.Hubs;

[AllowAnonymous]
public sealed class HalloweenHub(IHalloweenSessionService service) : Hub
{
    public async Task SubscribeRoom(Guid id)
    {
        await service.GetAsync(id, Context.ConnectionAborted);
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(id), Context.ConnectionAborted);
    }

    public static string GroupName(Guid id) => $"halloween:{id:D}";
}
