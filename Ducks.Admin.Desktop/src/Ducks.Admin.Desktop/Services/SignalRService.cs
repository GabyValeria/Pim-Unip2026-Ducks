using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Threading.Tasks;

namespace Ducks.Admin.Desktop.Services
{
    public interface ISignalRService
    {
        event Action<string, string>? OnAdminNotificationReceived;
        event Action<int>? OnModerationAlertReceived;
        Task ConnectAsync(string hubUrl);
        Task DisconnectAsync();
        bool IsConnected { get; }
    }

    public class SignalRService : ISignalRService
    {
        private HubConnection? _connection;
        public event Action<string, string>? OnAdminNotificationReceived;
        public event Action<int>? OnModerationAlertReceived;

        public bool IsConnected => _connection?.State == HubConnectionState.Connected;

        public async Task ConnectAsync(string hubUrl)
        {
            if (_connection != null && IsConnected) return;

            _connection = new HubConnectionBuilder()
                .WithUrl(hubUrl)
                .WithAutomaticReconnect()
                .Build();

            // Ouve notificações gerais de admin
            _connection.On<string, string>("ReceiveAdminNotification", (titulo, mensagem) =>
            {
                OnAdminNotificationReceived?.Invoke(titulo, mensagem);
            });

            // Ouve alertas de novos itens pendentes de moderação
            _connection.On<int>("ReceiveModerationAlert", (totalPendentes) =>
            {
                OnModerationAlertReceived?.Invoke(totalPendentes);
            });

            try
            {
                await _connection.StartAsync();
            }
            catch (Exception)
            {
                // Tratamento de falha de conexão em background
            }
        }

        public async Task DisconnectAsync()
        {
            if (_connection != null)
            {
                await _connection.StopAsync();
                await _connection.DisposeAsync();
                _connection = null;
            }
        }
    }
}