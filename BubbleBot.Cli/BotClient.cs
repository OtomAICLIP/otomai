using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Bubble.Core;
using Bubble.Core.Network;
using Bubble.Core.Network.Proxy;
using Bubble.Shared.Protocol;
using BubbleBot.Cli.Logging;
using BubbleBot.Cli.Models;
using BubbleBot.Cli.Repository;
using Com.ankama.dofus.server.connection.protocol;
using Discord;
using ProtoBuf;
using Serilog;
using Serilog.Core;
using Response = Com.ankama.dofus.server.connection.protocol.Response;

namespace BubbleBot.Cli;

public class BotSettings
{
    public required string Id { get; set; }
    public required SaharachAccount Account { get; set; }
    public required string ApiKey { get; set; }
    public required string Address { get; set; }
    public required int Port { get; set; }
    public Socks5Options? Proxy { get; set; }
    public int ServerId { get; set; }
    public bool IsBank { get; set; }
    public WebProxy? WebProxy { get; set; }
    public bool IsKoli { get; set; }
}

public class BotClient : TcpClient
{
    public const string Version = "1.5.0";

    private readonly string _apiKey;
    private readonly string _hwid;

    private int? _expectedLength;
    private int _expectedLengthSize;
    private bool _isWaitingForAnotherPacket;

    private int _serverId;
    private string _token = string.Empty;
    private string _gameIp = string.Empty;
    private int _gamePort;

    public bool Connecting { get; set; } = true;
    public DateTime LastReconnect { get; set; }
    public bool DisconnectionPlanned { get; set; }

    private readonly CircularBuffer _buffer = new(8192);

    private readonly BotSettings _settings;

    public BotGameClient? GameClient { get; private set; }

    public string BotId { get; }
    public long LastMessageSent { get; private set; }
    public SaharachAccount Account { get; set; }

    public BotClient(BotSettings settings)
        : base(settings.Address, settings.Port, settings.Proxy)
    {
        _apiKey = settings.ApiKey;
        _serverId = settings.ServerId;
        LastReconnect = DateTime.UtcNow;
        // we SHA512 hash the HWID
        var hwid = settings.Account.HardwareId;
        _hwid = Convert.ToHexString(SHA512.HashData(Encoding.UTF8.GetBytes(hwid))).ToUpper();

        BotId = settings.Id;
        Account = settings.Account;

        _settings = settings;
    }

    protected override void OnConnected()
    {
        LogInfo("Connected to {BotId}", BotId);

        SendLogin();
    }

    protected override void OnDisconnected()
    {
        LogInfo("Disconnected from {BotId}", BotId);
        Connecting = false;

        if (!DisconnectionPlanned)
        {
            LogDiscord("Déconnexion inattendue");

            //_ = BotManager.Instance.AddBot(BotId);
        }
    }

    protected override void OnReceived(byte[] buffer, long offset, long size)
    {
        LogInfo("Received {Size} bytes from {BotId}", size, BotId);
        _buffer.Write(buffer.AsMemory()[(int)offset..(int)(offset + size)]);

        while (_buffer.Size > 0)
        {
            if (!_isWaitingForAnotherPacket)
                (_expectedLength, _expectedLengthSize) = ReadVarInt32();

            if (_expectedLength == null || _expectedLength > _buffer.Size)
            {
                _isWaitingForAnotherPacket = true;
                return;
            }

            var messageData = new byte[_expectedLength.Value];
            _buffer.Read(messageData, 0, _expectedLength.Value);

            var message = Serializer.Deserialize<LoginMessage>(messageData.AsSpan());

            if (message.Response != null)
            {
                LogInfo("Received login response from {BotId}: {Response}", BotId, message.Response);
                var json = JsonSerializer.Serialize(message.Response,
                                                    new JsonSerializerOptions
                                                    {
                                                        WriteIndented = true
                                                    });

                OnMessageReceived(message.Response);
                LogInfo("{Response}", json);
            }

            if (message.Response == null && message.Event == null && message.Request == null)
            {
                if (_buffer.Seek(_expectedLength.Value + _expectedLengthSize))
                {
                    continue;
                }

                ResetState();
                continue;
            }

            _isWaitingForAnotherPacket = false;

            if (_buffer.Seek(_expectedLength.Value + _expectedLengthSize))
            {
                continue;
            }

            if (_buffer.Size == 0)
            {
                ResetState();
                return;
            }
        }
    }

    private void OnMessageReceived(Com.ankama.dofus.server.connection.protocol.Response response)
    {
        if (response.Identification != null)
        {
            OnIdentificationResponse(response.Identification);
        }

        if (response.SelectServer != null)
        {
            OnServerSelected(response.SelectServer);
        }
    }

    private void OnServerSelected(SelectServerResponse selectServer)
    {
        if (selectServer.ResultCase != SelectServerResponse.ResultOneofCase.SuccessValue)
        {
            LogDiscord($"Erreur lors de la sélection du serveur: {selectServer.ErrorValue} ({_serverId})");
            Connecting = false;
            return;
        }

        var server = ServerRepository.Instance.GetServer(_serverId);
        var serverName = server?.Name ?? "Inconnu";

        LogDiscord($"Sélection du serveur {serverName} ({_serverId}) réussie");

        _token = selectServer.SuccessValue.Token;
        _gameIp = selectServer.SuccessValue.Host;

        // Prefer port 443 (TLS) when available; fall back to first port (5555, plain TCP).
        // Port 5555 is the standard game port without TLS; port 443 is the TLS game port.
        var ports = selectServer.SuccessValue.Ports;
        var tlsPort = ports.Contains(443) ? 443 : 0;
        _gamePort = tlsPort > 0 ? tlsPort : (ports.Count > 0 ? ports[0] : 5555);
        var useTls = _gamePort == 443;


        if (Proxy != null)
        {
            Proxy.DestinationHost = BotManager.GetIpFromHost(_gameIp);
            Proxy.DestinationPort = _gamePort;
        }

        if (BotManager.Instance.GameClients.TryGetValue(BotId, out var value) &&
            DateTime.UtcNow - value.LastReconnect < TimeSpan.FromMinutes(1) &&
            !value.IsStopped)
        {
            LogDiscord("Reconnexion trop rapide, déconnexion");
            Disconnect();
            return;
        }

        GameClient = new BotGameClient(this,
                                       BotId,
                                       Account,
                                       _token,
                                       serverName,
                                       _serverId,
                                       _hwid,
                                       BotManager.GetIpFromHost(_gameIp),
                                       _gamePort,
                                       Proxy,
                                       _settings);

        // TLS is required on port 443; port 5555 uses plain TCP.
        // TlsTargetHost must be the original hostname (not resolved IP) for correct SNI.
        GameClient.UseTls = useTls;
        if (useTls)
            GameClient.TlsTargetHost = _gameIp;

        BotManager.Instance.AddGameBotClient(GameClient);

        GameClient.ConnectAsync();

        DisconnectionPlanned = true;
        Disconnect();
    }

    private void OnIdentificationResponse(IdentificationResponse response)
    {
        if (response.SuccessValue == null)
        {
            LogError("Failed to login to {BotId}", BotId);
            LogDiscord("\u26a0 Identification failed " + response.ErrorValue?.ReasonValue);

            if (response.ErrorValue != null)
            {
                if (response.ErrorValue.ReasonValue == IdentificationResponse.Error.Reason.Banned)
                {
                    Banned = true;
                    Log.Information("Bot {BotId} is banned", BotId);
                    //Environment.Exit(0);
                }
            }
            Connecting = false;
            return;
        }

        var success = response.SuccessValue;

        var minDate = DateTimeOffset.MinValue;

        if (_serverId == 0)
        {
            foreach (var server in success.ServerList.Servers)
            {
                if (server.Characters.Count == 0)
                    continue;

                foreach (var character in server.Characters)
                {
                    var lastConnectionDate = DateTimeOffset.Parse(character.LastConnectionDate);

                    if (minDate >= lastConnectionDate)
                        continue;

                    minDate = lastConnectionDate;
                    _serverId = server.Server.Id;
                }
            }
        }

        var serverData = success.ServerList.Servers.FirstOrDefault(s => s.Server.Id == _serverId);
        if (serverData == null)
        {
            LogDiscord($"Serveur introuvable: {_serverId}");
            return;
        }

        if (serverData.Server.StatusValue != Server.Status.Online)
        {
            LogDiscord($"Server {_serverId} is not online");
            _ = LoggerWebhook.LogAsync($"Le serveur {_serverId} n'est pas en ligne");
            return;
        }

        if (_serverId <= 0)
        {
            LogDiscord($"Le serveur {_serverId} n'à pas été trouvé");
            return;
        }

        SendServerSelection();
    }

    public bool Banned { get; set; }

    public void LogDiscord(string message)
    {
        try
        {
            LogInfo(message);
            /*_ = LoggerWebhook.LoggingWebhook.SendMessageAsync(embeds: new List<Embed>()
            {
                new EmbedBuilder()
                    .WithAuthor(BotId)
                    .WithDescription(message)
                    .WithColor(Color.Magenta)
                    .WithCurrentTimestamp()
                    .WithFooter($"Version: {Version}")
                    .Build()
            });*/
        }
        catch (Exception e)
        {
            LogError(e, "Failed to send message to Discord");
        }
    }

    private void SendServerSelection()
    {
        SendMessage(new LoginMessage()
        {
            Request = new Com.ankama.dofus.server.connection.protocol.Request
            {
                Uuid = "1",
                SelectServer = new SelectServerRequest
                {
                    Server = _serverId
                }
            }
        });

        var server = ServerRepository.Instance.GetServer(_serverId);
        var serverName = server?.Name ?? "Inconnu";

        LogDiscord($"Sélection du serveur: {serverName} ({_serverId})");
    }

    private void SendLogin()
    {
        SendMessage(new LoginMessage()
        {
            Request = new Com.ankama.dofus.server.connection.protocol.Request
            {
                Uuid = "0",
                Identification = new Com.ankama.dofus.server.connection.protocol.IdentificationRequest
                {
                    DeviceIdentifier = _hwid,
                    ClientVersion = "3.5.8.9",
                    TokenRequest = new TokenRequest
                    {
                        Token = _apiKey,
                        ShieldValue = new TokenRequest.Shield
                        {
                            CertificateId = 0,
                            CertificateHash = null
                        },
                    }
                }
            }
        });

        LogDiscord($"Demande de connection au serveur d'authentification");
    }

    private void SendMessage(LoginMessage message)
    {
        LastMessageSent = Stopwatch.GetTimestamp();

        using var ms2 = new MemoryStream();
        Serializer.Serialize(ms2, message);

        var messageContent = ms2.ToArray();
        var headerSize = WriteVarInt32(messageContent.Length);

        var finalContent = new byte[headerSize.Item2 + messageContent.Length];
        var headerSpan = new Span<byte>(finalContent);

        headerSize.Item1.CopyTo(headerSpan);
        messageContent.CopyTo(headerSpan[headerSize.Item2..]);

        // Display as Hex dump
        LogInfo("Sending login message to {BotId}: {Data}",
                BotId,
                BitConverter.ToString(finalContent).Replace("-", " "));

        Send(finalContent);
    }

    private void ResetState()
    {
        _expectedLength = null;
        _expectedLengthSize = 0;
        _isWaitingForAnotherPacket = false;
        _buffer.Clear();
    }


    private (int, int) ReadVarInt32()
    {
        var nbBytes = 0;
        var result = 0;
        var shift = 0;
        byte b;
        do
        {
            b = _buffer.Peek();
            _buffer.Read(new byte[1], 0, 1); // Remove the read byte from the buffer
            result |= (b & 0x7F) << shift;
            shift += 7;
            nbBytes++;
        } while ((b & 0x80) != 0);

        return (result, nbBytes);
    }

    private static (byte[], int) WriteVarInt32(int value)
    {
        var buffer = new byte[5];

        var i = 0;

        do
        {
            var b = value & 0x7F;

            value >>= 7;

            if (value is not 0)
                b |= 0x80;

            buffer[i] = (byte)b;
            i++;
        } while (value is not 0);

        return (buffer, i);
    }


    [MessageTemplateFormatMethod("messageTemplate")]
    public void LogWarning(string messageTemplate)
    {
        if (BotManager.NoLog)
            return;

        Log.Warning("[" + BotId + "] : " + messageTemplate);
    }

    [MessageTemplateFormatMethod("messageTemplate")]
    public void LogWarning<T>(string messageTemplate, T propertyValue)
    {
        if (BotManager.NoLog)
            return;

        Log.Warning("[" + BotId + "] : " + messageTemplate, propertyValue);
    }

    [MessageTemplateFormatMethod("messageTemplate")]
    public void LogWarning<T0, T1>(string messageTemplate, T0 propertyValue0, T1 propertyValue1)
    {
        Log.Warning("[" + BotId + "] : " + messageTemplate, propertyValue0, propertyValue1);
    }

    [MessageTemplateFormatMethod("messageTemplate")]
    public void LogWarning<T0, T1, T2>(string messageTemplate, T0 propertyValue0, T1 propertyValue1, T2 propertyValue2)
    {
        if (BotManager.NoLog)
            return;

        Log.Warning("[" + BotId + "] : " + messageTemplate, propertyValue0, propertyValue1, propertyValue2);
    }

    [MessageTemplateFormatMethod("messageTemplate")]
    public void LogInfo(string messageTemplate)
    {
        if (BotManager.NoLog)
            return;

        Log.Information("[" + BotId + "] : " + messageTemplate);
    }

    [MessageTemplateFormatMethod("messageTemplate")]
    public void LogInfo<T>(string messageTemplate, T propertyValue)
    {
        if (BotManager.NoLog)
            return;

        Log.Information("[" + BotId + "] : " + messageTemplate, propertyValue);
    }

    [MessageTemplateFormatMethod("messageTemplate")]
    public void LogInfo<T0, T1>(string messageTemplate, T0 propertyValue0, T1 propertyValue1)
    {
        Log.Information("[" + BotId + "] : " + messageTemplate, propertyValue0, propertyValue1);
    }

    [MessageTemplateFormatMethod("messageTemplate")]
    public void LogInfo<T0, T1, T2>(string messageTemplate, T0 propertyValue0, T1 propertyValue1, T2 propertyValue2)
    {
        if (BotManager.NoLog)
            return;

        Log.Information("[" + BotId + "] : " + messageTemplate, propertyValue0, propertyValue1, propertyValue2);
    }

    [MessageTemplateFormatMethod("messageTemplate")]
    public void LogError(Exception? exception, string messageTemplate)
    {
        if (BotManager.NoLog)
            return;

        Log.Error("[" + BotId + "] : " + messageTemplate);
    }

    [MessageTemplateFormatMethod("messageTemplate")]
    public void LogError(string messageTemplate)
    {
        if (BotManager.NoLog)
            return;

        Log.Error("[" + BotId + "] : " + messageTemplate);
    }

    [MessageTemplateFormatMethod("messageTemplate")]
    public void LogError<T>(string messageTemplate, T propertyValue)
    {
        if (BotManager.NoLog)
            return;

        Log.Error("[" + BotId + "] : " + messageTemplate, propertyValue);
    }

    [MessageTemplateFormatMethod("messageTemplate")]
    public void LogError<T0, T1>(string messageTemplate, T0 propertyValue0, T1 propertyValue1)
    {
        Log.Error("[" + BotId + "] : " + messageTemplate, propertyValue0, propertyValue1);
    }

    [MessageTemplateFormatMethod("messageTemplate")]
    public void LogError<T0>(Exception? e, string messageTemplate, T0 propertyValue0)
    {
        Log.Error(e, "[" + BotId + "] : " + messageTemplate, propertyValue0);
    }

    [MessageTemplateFormatMethod("messageTemplate")]
    public void LogError<T0, T1, T2>(string messageTemplate, T0 propertyValue0, T1 propertyValue1, T2 propertyValue2)
    {
        if (BotManager.NoLog)
            return;

        Log.Error("[" + BotId + "] : " + messageTemplate, propertyValue0, propertyValue1, propertyValue2);
    }
}