using Microsoft.AspNetCore.SignalR.Client;

namespace Ducks.Admin.Desktop.Services;

public class AdminRealtimeService
{
    private HubConnection? _hubConnection;
    public event Action<string, int>? MetricUpdated;
    public event Action<string, string>? AuditLogReceived;

    public async Task ConnectAsync(string apiBaseUrl, string token)
    {
        _hubConnection = new HubConnectionBuilder()
            .WithUrl($"{apiBaseUrl}/hubs/admin", options =>
            {
                options.AccessTokenProvider = () => Task.FromResult(token)!;
            })
            .WithAutomaticReconnect()
            .Build();

        _hubConnection.On<string, int>("ReceiveMetricUpdate", (metricName, value) =>
        {
            MetricUpdated?.Invoke(metricName, value);
        });

        _hubConnection.On<string, string>("ReceiveAuditLog", (user, action) =>
        {
            AuditLogReceived?.Invoke(user, action);
        });

        await _hubConnection.StartAsync();
    }

    public async Task DisconnectAsync()
    {
        if (_hubConnection != null)
        {
            await _hubConnection.StopAsync();
        }
    }
}