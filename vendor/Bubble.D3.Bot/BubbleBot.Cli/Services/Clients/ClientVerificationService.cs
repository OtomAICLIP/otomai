using System.Numerics;
using System.Text;
using Bubble.Shared.Protocol;
using Org.BouncyCastle.Crypto.Digests;

namespace BubbleBot.Cli.Services.Clients;

internal sealed class ClientVerificationService
{
    private readonly BotClientContextBase _context;
    private readonly ClientTransportService _transportService;
    private readonly Action<string>? _notify;

    public ClientVerificationService(BotClientContextBase   context,
                                     ClientTransportService transportService,
                                     Action<string>?        notify = null)
    {
        _context = context;
        _transportService = transportService;
        _notify = notify;
    }

    public void OnServerVerificationEvent(bool forceNewCvlg = false)
    {
        _notify?.Invoke("Le serveur demande une vérification");
        GenerateCvlg(forceNewCvlg);
        GenerateCvlh();

        var challengeKey = BigInteger.ModPow(_context.Cvlf, _context.Cvlh, _context.Cvld);
        _transportService.SendRequest(new ClientChallengeInitRequest
                                      {
                                          Ehcu = challengeKey.ToString()
                                      },
                                      ClientChallengeInitRequest.TypeUrl);
    }

    public BigInteger GenerateCvlg(bool forceNew = false)
    {
        if (forceNew || _context.Cvlg == null)
        {
            var buffer = new byte[0x400];
            _context.SecureRandom.NextBytes(buffer);
            var tmp = new BigInteger(buffer, true, false);
            _context.Cvlg = tmp % _context.Cvld;
        }

        return _context.Cvlg.Value;
    }

    public void OnServerChallengeEvent(string? value)
    {
        var proof = string.IsNullOrEmpty(value)
            ? BuildProofWithoutServerValue()
            : BuildProofWithServerValue(value);

        _notify?.Invoke($"Server asked for proof with value `{value}` and we sent `{proof}`");

        _transportService.SendRequest(new ClientChallengeProofRequest
                                      {
                                          Proof = proof
                                      },
                                      ClientChallengeProofRequest.TypeUrl);
    }

    public string BuildPublicKey()
    {
        var cvlg = GenerateCvlg();
        return BigInteger.ModPow(_context.Cvlf, cvlg, _context.Cvld).ToString();
    }

    private void GenerateCvlh()
    {
        var buffer = new byte[50];
        _context.SecureRandom.NextBytes(buffer);
        _context.Cvlh = new BigInteger(buffer, true, false);
    }

    private string BuildProofWithoutServerValue()
    {
        var cvlg = GenerateCvlg();
        var b1 = BigInteger.ModPow(_context.Cvlf, cvlg, _context.Cvld);
        var b2 = BigInteger.ModPow(_context.Cvlf, _context.Cvlh, _context.Cvld);

        var concatenated = string.Concat(_context.Cvlf, b1, b2, _context.Hwid);
        var bytes = Encoding.UTF8.GetBytes(concatenated);

        var sha256 = new Sha256Digest();
        sha256.BlockUpdate(bytes, 0, bytes.Length);
        var output = new byte[sha256.GetDigestSize()];
        sha256.DoFinal(output, 0);

        var reversed = Org.BouncyCastle.Utilities.Arrays.Reverse(output);
        var multiplied = new BigInteger(reversed, true, false) * cvlg;
        var modulo = (_context.Cvlh + multiplied) % _context.Cvle;

        if (modulo.Sign < 0)
        {
            modulo += _context.Cvle;
        }

        return modulo.ToString();
    }

    private string BuildProofWithServerValue(string value)
    {
        var cvlg = GenerateCvlg();
        var serverValue = BigInteger.Parse(value);
        var modulo = (_context.Cvlh + (serverValue * cvlg)) % _context.Cvle;

        if (modulo.Sign < 0)
        {
            modulo += _context.Cvle;
        }

        return modulo.ToString();
    }
}
