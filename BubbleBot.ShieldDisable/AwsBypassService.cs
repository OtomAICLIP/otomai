using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BubbleBot.ShieldDisable;

public class AwsBypassService
{
    private CookieContainer _jar = new();
    private HttpClientHandler _handler = new();
    private HttpClient _client = new();

    private readonly Random _rand = new Random();

    public int RandomBetween(int min, int max)
        => _rand.Next(min, max + 1);

    public double RandomBetweenDouble(double min, double max)
        => min + _rand.NextDouble() * (max - min);

    public class KeyProvider
    {
        public Dictionary<string, object> Provide() => new Dictionary<string, object>
        {
            ["identifier"] = "KramerAndRio",
            ["material"] = new byte[]
                { 0x4e, 0x2f, 0x88, 0xb3, 0x12, 0x9d, 0x1b, 0x4e, 0x79, 0xcf, 0x37, 0x69, 0xea, 0xb4, 0x5b, 0xcf }
        };
    }

    public class Encryptor
    {
        private readonly KeyProvider _keyProvider = new KeyProvider();
        private readonly byte[] _key = HexToBytes("93d9f6846b629edb2bdc4466af627d998496cb0c08f9cf043de68d6b25aa9693");

        public string Encrypt(string plaintext)
        {
            Console.WriteLine("Encrypting: " + _key.Select(x => x.ToString("X2")).Aggregate((x, y) => x + y));
            using var aes = new AesGcm(_key);
            var nonce = new byte[AesGcm.NonceByteSizes.MaxSize];
            RandomNumberGenerator.Fill(nonce);

            var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
            var ciphertext = new byte[plaintextBytes.Length];
            var tag = new byte[AesGcm.TagByteSizes.MaxSize];

            aes.Encrypt(nonce, plaintextBytes, ciphertext, tag);
            return
                $"{_keyProvider.Provide()["identifier"]}::{Convert.ToBase64String(nonce)}::{BytesToHex(tag)}::{BytesToHex(ciphertext)}";
        }

        private static byte[] HexToBytes(string hex)
        {
            var bytes = new byte[hex.Length / 2];
            for (var i = 0; i < bytes.Length; i++)
                bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            
            return bytes;
        }

        private static string BytesToHex(byte[] bytes)
        {
            var hex = new StringBuilder(bytes.Length * 2);
            foreach (var b in bytes) hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }
    }

    public async Task<string> Bypass(string state)
    {
        // HTTP Client configuration

        while (true)
        {
            try
            {
                var encryptor = new Encryptor();

                // Generate random metrics
                var (metrics, metricsObj) = GenerateMetrics(state);
                var checksum = CalculateChecksum(metrics);


                // Initial challenge request
                var challenge = await GetChallengeAsync();

                var payload = new Payload
                {
                    Input = challenge.Challenge.Input,
                    Checksum = checksum,
                    Difficulty = challenge.Difficulty,
                    Memory = 128
                };

                var solution = SolveChallenge(challenge, payload);
                
                if(string.IsNullOrEmpty(solution))
                    continue;

                // Final verification request
                var tokenSolution = await VerifySolution(challenge, solution, checksum, encryptor, metrics, metricsObj);

                var test = JsonSerializer.Deserialize<Dictionary<string, string>>(tokenSolution);

                return test!["token"];
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }

    private string SolveChallenge(ChallengeResponse challenge, Payload payload)
    {
        if (challenge.ChallengeType == "h7b0c470f0cfe3a80a9e26526ad185f484f6817d0832712a4a37a908786a6a67f")
        {
            Console.WriteLine("Use solution 2");
            return GetSolution2(payload);
        }
        else if (challenge.ChallengeType == "h72f957df656e80ba55f5d8ce2e8c7ccb59687dba3bfb273d54b08a261b2f3002")
        {
            Console.WriteLine("Use solution 1");
            return "";
            return GetSolution1(payload);
        }
        else
        {
            Console.WriteLine("Unknown challenge type: " + challenge.ChallengeType);
        }

        return "";
    }

    private string GetSolution2(Payload payload)
    {
        var difficulty = payload.Difficulty;
        var inputString = payload.Input;
        var checksum = payload.Checksum;

        var inputChecksumConcat = inputString + checksum;
        var incrementator = 0;

        while (true)
        {
            var inputChecksum0Concat = inputChecksumConcat + incrementator;
            // SHA 256 
            var hashResult = SHA256.HashData(Encoding.UTF8.GetBytes(inputChecksum0Concat));
            var hashResultString = Convert.ToHexString(hashResult);

            if (SatisfyDifficulty(difficulty, hashResultString))
            {
                return incrementator.ToString();
            }

            incrementator++;
        }
    }

    private string GetSolution1(Payload payload)
    {
        var difficulty = payload.Difficulty;
        var inputString = payload.Input;
        var checksum = payload.Checksum;

        var inputChecksumConcat = inputString + checksum;
        var incrementator = 0;

        while (true)
        {
            var inputChecksum0Concat = inputChecksumConcat + incrementator;
            var hashResult = CalculateScrypt(Encoding.UTF8.GetBytes(inputChecksum0Concat),
                                             Encoding.UTF8.GetBytes(checksum));
            var hashResultString = Convert.ToHexString(hashResult);

            if (SatisfyDifficulty(difficulty, hashResultString))
            {
                return incrementator.ToString();
            }

            incrementator++;
        }
    }

    public bool SatisfyDifficulty(int difficulty, string hashString)
    {
        // Dictionnaire de conversion hexadécimal -> binaire
        var hexToBin = new Dictionary<char, string>
        {
            { '0', "0000" }, { '1', "0001" }, { '2', "0010" }, { '3', "0011" },
            { '4', "0100" }, { '5', "0101" }, { '6', "0110" }, { '7', "0111" },
            { '8', "1000" }, { '9', "1001" }, { 'A', "1010" }, { 'B', "1011" },
            { 'C', "1100" }, { 'D', "1101" }, { 'E', "1110" }, { 'F', "1111" },
        };

        // Validation de l'entrée
        if (string.IsNullOrEmpty(hashString))
            throw new ArgumentException("Hash string ne peut pas être vide");

        // Construction de la chaîne binaire
        var binString = new StringBuilder();
        foreach (var c in hashString)
        {
            if (!hexToBin.TryGetValue(c, out var value))
                throw new ArgumentException($"Caractère hexadécimal invalide : {c}");

            binString.Append(value);
        }

        // Vérification de la difficulté
        if (difficulty < 0)
            throw new ArgumentException("La difficulté ne peut pas être négative");

        var binStr = binString.ToString();

        // Vérification des zéros initiaux
        for (var i = 0; i < difficulty; i++)
        {
            if (i >= binStr.Length || binStr[i] != '0')
                return false;
        }

        return true;
    }

    private const int N = 128;
    private static readonly int LogN = (int)Math.Log(N, 2);
    private const int DkLen = 16;
    private static readonly int BlockSize = 1 << LogN; // 128

    public static byte[] CalculateScrypt(byte[] inputChecksumI, byte[] checksum)
    {
        var _0x440d80 = new int[32 * 8];
        var _0x81d999 = new int[32 * BlockSize * 8];
        var _0x34e3fc = new int[16];

        var _0x1698a8 = CustomHmac(inputChecksumI, checksum, 1024);

        var result = ProcessBlocks(_0x440d80, _0x81d999, _0x34e3fc, _0x1698a8, inputChecksumI);
        return result;
    }

    private static byte[] ProcessBlocks(int[]  _0x440d80, int[] _0x81d999, int[] _0x34e3fc, byte[] _0x1698a8,
                                        byte[] inputChecksumI)
    {
        // Conversion initiale des bytes en int32
        for (var i = 0; i < 0x20 * 8; i++)
        {
            var index = 0x4 * i;
            _0x440d80[i] =
                ((_0x1698a8[index + 0x3] & 0xFF) << 0x18) |
                ((_0x1698a8[index + 0x2] & 0xFF) << 0x10) |
                ((_0x1698a8[index + 0x1] & 0xFF) << 0x8) |
                (_0x1698a8[index] & 0xFF);
        }

        var _0x1b2a27 = BlockSize;

        // Vérification initiale
        for (var _0x4808ca = 0; _0x4808ca < _0x1b2a27; _0x4808ca += 0x2)
        {
            for (var i = 0; i < 0x20 * 8; i++)
            {
                _0x81d999[_0x4808ca * (0x20 * 8) + i] = _0x440d80[i];
            }

            for (var i = 0; i < 0x10; i++)
            {
                _0x34e3fc[i] = _0x440d80[0x00 + 0x10 * (0x2 * 8 - 0x1) + i];
            }

            for (var _0x307b45 = 0; _0x307b45 < 0x2 * 8; _0x307b45 += 0x2)
            {
                (_0x34e3fc, _0x440d80) = CustomXor(Int32ToUint32(_0x34e3fc),
                                                   Int32ToUint32(_0x440d80),
                                                   0x0 + 0x10 * _0x307b45,
                                                   256 + 0x8 * _0x307b45);
                (_0x34e3fc, _0x440d80) = CustomXor(Int32ToUint32(_0x34e3fc),
                                                   Int32ToUint32(_0x440d80),
                                                   0x0 + 0x10 * _0x307b45 + 0x10,
                                                   256 + 0x8 * _0x307b45 + 0x10 * 8);
            }

            for (var i = 0; i < 0x20 * 8; i++)
            {
                _0x81d999[(_0x4808ca + 0x1) * (0x20 * 8) + i] = _0x440d80[256 + i];
            }

            for (var i = 0; i < 0x10; i++)
            {
                _0x34e3fc[i] = _0x440d80[256 + 0x10 * (0x2 * 8 - 0x1) + i];
            }


            for (var _0x307b45 = 0; _0x307b45 < 0x2 * 8; _0x307b45 += 0x2)
            {
                (_0x34e3fc, _0x440d80) = CustomXor(Int32ToUint32(_0x34e3fc),
                                                   Int32ToUint32(_0x440d80),
                                                   256 + 0x10 * _0x307b45,
                                                   0x0 + 0x8 * _0x307b45);
                (_0x34e3fc, _0x440d80) = CustomXor(Int32ToUint32(_0x34e3fc),
                                                   Int32ToUint32(_0x440d80),
                                                   256 + 0x10 * _0x307b45 + 0x10,
                                                   0x0 + 0x8 * _0x307b45 + 0x10 * 8);
            }
        }

        for (var _0x261706 = 0; _0x261706 < _0x1b2a27; _0x261706 += 0x2)
        {
            var _0x440374 = _0x440d80[0x0 + 0x10 * (0x2 * 8 - 0x1)] & _0x1b2a27 - 0x1;

            for (var i = 0; i < 0x20 * 8; i++)
            {
                _0x440d80[i] ^= _0x81d999[_0x440374 * (0x20 * 8) + i];
            }

            for (var i = 0; i < 0x10; i++)
            {
                _0x34e3fc[i] = _0x440d80[0x0 + 0x10 * (0x2 * 8 - 0x1) + i];
            }

            for (var _0x307b45 = 0; _0x307b45 < 0x2 * 8; _0x307b45 += 0x2)
            {
                (_0x34e3fc, _0x440d80) = CustomXor(Int32ToUint32(_0x34e3fc),
                                                   Int32ToUint32(_0x440d80),
                                                   0x0 + 0x10 * _0x307b45,
                                                   256 + 0x8 * _0x307b45);
                (_0x34e3fc, _0x440d80) = CustomXor(Int32ToUint32(_0x34e3fc),
                                                   Int32ToUint32(_0x440d80),
                                                   0x0 + 0x10 * _0x307b45 + 0x10,
                                                   256 + 0x8 * _0x307b45 + 0x10 * 8);
            }

            _0x440374 = _0x440d80[256 + 0x10 * (0x2 * 8 - 1)] & _0x1b2a27 - 0x1;

            for (var i = 0; i < 0x20 * 8; i++)
            {
                _0x440d80[256 + i] ^= _0x81d999[_0x440374 * (0x20 * 8) + i];
            }

            for (var i = 0; i < 0x10; i++)
            {
                _0x34e3fc[i] = _0x440d80[256 + 0x10 * (0x2 * 8 - 0x1) + i];
            }

            for (var _0x307b45 = 0; _0x307b45 < 0x2 * 8; _0x307b45 += 0x2)
            {
                (_0x34e3fc, _0x440d80) = CustomXor(Int32ToUint32(_0x34e3fc),
                                                   Int32ToUint32(_0x440d80),
                                                   256 + 0x10 * _0x307b45,
                                                   0x0 + 0x8 * _0x307b45);
                (_0x34e3fc, _0x440d80) = CustomXor(Int32ToUint32(_0x34e3fc),
                                                   Int32ToUint32(_0x440d80),
                                                   256 + 0x10 * _0x307b45 + 0x10,
                                                   0x0 + 0x8 * _0x307b45 + 0x10 * 8);
            }
        }

        // Final conversion
        for (var _0x5258e0 = 0; _0x5258e0 < 0x20 * 8; _0x5258e0++)
        {
            var _0x2b638d = _0x440d80[_0x5258e0];
            _0x1698a8[4 * _0x5258e0 + 0] = (byte)(_0x2b638d & 0xFF);
            _0x1698a8[4 * _0x5258e0 + 1] = (byte)((_0x2b638d >> 8) & 0xFF);
            _0x1698a8[4 * _0x5258e0 + 2] = (byte)((_0x2b638d >> 16) & 0xFF);
            _0x1698a8[4 * _0x5258e0 + 3] = (byte)((_0x2b638d >> 24) & 0xFF);
        }

        return CustomHmac(inputChecksumI, _0x1698a8, DkLen);
    }

    private static uint[] Int32ToUint32(int[] p0)
    {
        var result = new uint[p0.Length];
        for (var i = 0; i < p0.Length; i++)
        {
            result[i] = (uint)p0[i];
        }

        return result;
    }

    private static uint LeftRotate(uint value, int shift)
    {
        return (value << shift) | (value >> (32 - shift));
    }

    public static (int[] int32Arr0, int[] int32Arr) CustomXor(uint[] arr1, uint[] arr2, int idx1, int idx2)
    {
        var arr2List = new List<uint>(arr2);
        var state = new uint[16];
        var initialState = new uint[16];

        // Initialisation de l'état
        for (var i = 0; i < 16; i++)
        {
            // Étendre arr2 si nécessaire
            if (idx1 >= arr2List.Count)
            {
                var needed = idx1 - arr2List.Count + 1;
                arr2List.AddRange(new uint[needed]);
            }

            initialState[i] = arr1[i] ^ arr2List[idx1];
            state[i] = initialState[i];
            idx1++;
        }

        // Mélange de l'état (8 tours)
        for (var i = 0; i < 8; i += 2)
        {
            // Premier quadrant
            state[4] ^= LeftRotate(state[0] + state[12], 7);
            state[8] ^= LeftRotate(state[4] + state[0], 9);
            state[12] ^= LeftRotate(state[8] + state[4], 13);
            state[0] ^= LeftRotate(state[12] + state[8], 18);

            // Second quadrant
            state[9] ^= LeftRotate(state[5] + state[1], 7);
            state[13] ^= LeftRotate(state[9] + state[5], 9);
            state[1] ^= LeftRotate(state[13] + state[9], 13);
            state[5] ^= LeftRotate(state[1] + state[13], 18);

            // Troisième quadrant
            state[14] ^= LeftRotate(state[10] + state[6], 7);
            state[2] ^= LeftRotate(state[14] + state[10], 9);
            state[6] ^= LeftRotate(state[2] + state[14], 13);
            state[10] ^= LeftRotate(state[6] + state[2], 18);

            // Quatrième quadrant
            state[3] ^= LeftRotate(state[15] + state[11], 7);
            state[7] ^= LeftRotate(state[3] + state[15], 9);
            state[11] ^= LeftRotate(state[7] + state[3], 13);
            state[15] ^= LeftRotate(state[11] + state[7], 18);

            // Mélanges croisés
            state[1] ^= LeftRotate(state[0] + state[3], 7);
            state[2] ^= LeftRotate(state[1] + state[0], 9);
            state[3] ^= LeftRotate(state[2] + state[1], 13);
            state[0] ^= LeftRotate(state[3] + state[2], 18);

            state[6] ^= LeftRotate(state[5] + state[4], 7);
            state[7] ^= LeftRotate(state[6] + state[5], 9);
            state[4] ^= LeftRotate(state[7] + state[6], 13);
            state[5] ^= LeftRotate(state[4] + state[7], 18);

            state[11] ^= LeftRotate(state[10] + state[9], 7);
            state[8] ^= LeftRotate(state[11] + state[10], 9);
            state[9] ^= LeftRotate(state[8] + state[11], 13);
            state[10] ^= LeftRotate(state[9] + state[8], 18);

            state[12] ^= LeftRotate(state[15] + state[14], 7);
            state[13] ^= LeftRotate(state[12] + state[15], 9);
            state[14] ^= LeftRotate(state[13] + state[12], 13);
            state[15] ^= LeftRotate(state[14] + state[13], 18);
        }

        // Mise à jour des tableaux
        for (var i = 0; i < 16; i++)
        {
            arr1[i] = state[i] + initialState[i];

            // Extension dynamique de arr2
            if (idx2 >= arr2List.Count)
            {
                var needed = idx2 - arr2List.Count + 1;
                arr2List.AddRange(new uint[needed]);
            }

            arr2List[idx2] = arr1[i];
            idx2++;
        }

        // Conversion finale en int[]
        return (
            arr1.Select(x => (int)x).ToArray(),
            arr2List.Select(x => (int)x).ToArray()
        );
    }


    private static byte[] CustomHmac(byte[] key, byte[] message, int length)
    {
        if (key.Length > 64)
        {
            key = SHA256.HashData(key);
        }

        var totalLength = 64 + message.Length + 4;
        var concatenatedArray = new byte[totalLength];
        var paddingArray = new byte[64];
        var resultArray = new List<byte>(length);

        for (var i = 0; i < 64; i++)
        {
            paddingArray[i] = 0x36;
        }

        for (var i = 0; i < key.Length; i++)
        {
            paddingArray[i] ^= key[i];
        }

        for (var i = 0; i < message.Length; i++)
        {
            concatenatedArray[64 + i] = message[i];
        }

        for (var i = totalLength - 4; i < totalLength; i++)
        {
            concatenatedArray[i] = 0x00;
        }

        for (var i = 0; i < 64; i++)
        {
            paddingArray[i] = 0x5C;
        }

        for (var i = 0; i < key.Length; i++)
        {
            paddingArray[i] ^= key[i];
        }

        void IncrementLastBytes()
        {
            for (var i = totalLength - 1; i >= totalLength - 4; i--)
            {
                concatenatedArray[i]++;
                if (concatenatedArray[i] <= 0xff)
                {
                    return;
                }

                concatenatedArray[i] = 0x00;
            }
        }

        while (length > 32)
        {
            IncrementLastBytes();

            var hashArray = new byte[paddingArray.Length + concatenatedArray.Length];
            Array.Copy(paddingArray, hashArray, paddingArray.Length);
            var concatHash = SHA256.HashData(concatenatedArray);
            Array.Copy(concatHash, 0, hashArray, paddingArray.Length, concatHash.Length);

            resultArray.AddRange(SHA256.HashData(hashArray));
            length -= 32;
        }

        if (length > 0)
        {
            IncrementLastBytes();
            var hashArray = new byte[paddingArray.Length + concatenatedArray.Length];
            Array.Copy(paddingArray, hashArray, paddingArray.Length);

            var concatHash = SHA256.HashData(concatenatedArray);
            Array.Copy(concatHash, 0, hashArray, paddingArray.Length, concatHash.Length);

            resultArray.AddRange(SHA256.HashData(hashArray)[0..length]);
        }

        return resultArray.ToArray();
    }

    public class Metrics
    {
        public int F2p { get; set; }
        public int Browser { get; set; }
        public int Capabilities { get; set; }
        public int Gpu { get; set; }
        public int Dnt { get; set; }
        public int Canvas { get; set; }
        public int Be { get; set; }

        public double Timestamp1 { get; set; }
        public double Timestamp2 { get; set; }
    }

    private (string, Metrics) GenerateMetrics(string state)
    {
        var f2p = RandomBetween(0, 1);
        var browser = RandomBetween(0, 1);
        var capabilities = RandomBetween(1, 2);
        var dnt = RandomBetween(0, 1);
        var gpu = RandomBetween(5, 20);
        var be = RandomBetween(0, 2);
        var canvas = RandomBetween(10, 200);

        var timestamp1 = RandomBetweenDouble(20, 60);
        var timestamp2 = RandomBetweenDouble(10, 30);

        // Génération des timestamps comme en Go
        var startTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - 2;
        var endTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var id = Guid.NewGuid().ToString("N");

        var metrics = string.Format(
            @"{{""metrics"":{{
            ""fp2"":{0},
            ""browser"":{1},
            ""capabilities"":{2},
            ""gpu"":{3},
            ""dnt"":{4},
            ""math"":0,
            ""screen"":0,
            ""navigator"":0,
            ""auto"":1,
            ""stealth"":1,
            ""subtle"":0,
            ""canvas"":{5},
            ""formdetector"":1,
            ""be"":{6}
        }},
        ""start"":{7},
        ""flashVersion"":null,
        ""plugins"":[
            {{""name"":""PDF Viewer"",""str"":""PDF Viewer ""}},
            {{""name"":""Chrome PDF Viewer"",""str"":""Chrome PDF Viewer ""}},
            {{""name"":""Chromium PDF Viewer"",""str"":""Chromium PDF Viewer ""}},
            {{""name"":""Microsoft Edge PDF Viewer"",""str"":""Microsoft Edge PDF Viewer ""}},
            {{""name"":""WebKit built-in PDF"",""str"":""WebKit built-in PDF ""}}
        ],
        ""dupedPlugins"":""PDF Viewer Chrome PDF Viewer Chromium PDF Viewer Microsoft Edge PDF Viewer WebKit built-in PDF ||1536-864-816-24-*-*-*"",
        ""screenInfo"":""1536-864-816-24-*-*-*"",
        ""referrer"":"""",
        ""userAgent"":""Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/129.0.0.0 Safari/537.36"",
        ""location"":""https://auth.ankama.com/login/ankama/form?origin_tracker=https://www.dofus.com/fr/achat-kamas&redirect_uri=https://auth.ankama.com/login-authorized?state={8}"",
        ""webDriver"":false,
        ""capabilities"":{{
            ""css"":{{
                ""textShadow"":1,
                ""WebkitTextStroke"":1,
                ""boxShadow"":1,
                ""borderRadius"":1,
                ""borderImage"":1,
                ""opacity"":1,
                ""transform"":1,
                ""transition"":1
            }},
            ""js"":{{
                ""audio"":true,
                ""geolocation"":true,
                ""localStorage"":""supported"",
                ""touch"":false,
                ""video"":true,
                ""webWorker"":true
            }},
            ""elapsed"":1
        }},
        ""gpu"":{{
            ""vendor"":""Google Inc. (Intel)"",
            ""model"":""ANGLE (Intel, Intel(R) UHD Graphics (0x000046A3) Direct3D11 vs_5_0 ps_5_0, D3D11)"",
            ""extensions"":[
                ""ANGLE_instanced_arrays"",""EXT_blend_minmax"",""EXT_clip_control"",
                ""EXT_color_buffer_half_float"",""EXT_depth_clamp"",""EXT_disjoint_timer_query"",
                ""EXT_float_blend"",""EXT_frag_depth"",""EXT_polygon_offset_clamp"",
                ""EXT_shader_texture_lod"",""EXT_texture_compression_bptc"",
                ""EXT_texture_compression_rgtc"",""EXT_texture_filter_anisotropic"",
                ""EXT_texture_mirror_clamp_to_edge"",""EXT_sRGB"",""KHR_parallel_shader_compile"",
                ""OES_element_index_uint"",""OES_fbo_render_mipmap"",""OES_standard_derivatives"",
                ""OES_texture_float"",""OES_texture_float_linear"",""OES_texture_half_float"",
                ""OES_texture_half_float_linear"",""OES_vertex_array_object"",
                ""WEBGL_blend_func_extended"",""WEBGL_color_buffer_float"",
                ""WEBGL_compressed_texture_s3tc"",""WEBGL_compressed_texture_s3tc_srgb"",
                ""WEBGL_debug_renderer_info"",""WEBGL_debug_shaders"",""WEBGL_depth_texture"",
                ""WEBGL_draw_buffers"",""WEBGL_lose_context"",""WEBGL_multi_draw"",""WEBGL_polygon_mode""
            ]
        }},
        ""dnt"":null,
        ""math"":{{
            ""tan"":""-1.4214488238747245"",
            ""sin"":""0.8178819121159085"",
            ""cos"":""-0.5753861119575491""
        }},
        ""automation"":{{
            ""wd"":{{""properties"":{{""document"":[],""window"":[],""navigator"":[]}}}},
            ""phantom"":{{""properties"":{{""window"":[]}}}}
        }},
        ""stealth"":{{""t1"":0,""t2"":0,""i"":1,""mte"":0,""mtd"":false}},
        ""crypto"":{{
            ""crypto"":1,
            ""subtle"":1,
            ""encrypt"":true,
            ""decrypt"":true,
            ""wrapKey"":true,
            ""unwrapKey"":true,
            ""sign"":true,
            ""verify"":true,
            ""digest"":true,
            ""deriveBits"":true,
            ""deriveKey"":true,
            ""getRandomValues"":true,
            ""randomUUID"":true
        }},
        ""canvas"":{{
            ""hash"":-1191871006,
            ""emailHash"":null,
            ""histogramBins"":[
                14542,25,40,33,54,45,21,39,31,44,41,29,28,28,69,55,17,22,76,36,
                27,19,18,47,40,17,24,29,63,19,45,27,38,18,26,14,32,19,17,32,29,
                43,16,50,44,17,14,46,16,21,16,43,14,19,7,31,24,24,19,67,31,34,15,
                13,20,25,37,15,11,19,16,20,44,20,42,16,19,7,20,55,23,16,15,31,21,
                20,31,53,19,26,19,41,15,18,10,29,46,17,37,94,53,35,526,46,78,39,
                14,13,15,20,10,21,16,17,35,21,16,12,28,13,25,17,24,16,30,32,63,19,
                24,84,22,31,36,18,20,19,20,47,9,16,18,19,17,17,22,33,74,23,17,9,63,
                11,12,91,11,15,29,13,24,11,11,27,10,12,53,10,6,15,27,52,16,13,53,
                17,13,13,79,10,13,11,14,15,18,85,37,13,15,13,9,55,10,16,8,5,22,57,
                24,14,65,7,50,11,35,17,53,31,56,35,60,40,11,49,75,13,39,14,14,27,
                20,27,31,20,20,44,28,40,17,128,55,42,11,37,56,13,13,26,75,16,29,18,
                84,126,40,20,65,25,70,37,83,36,39,91,53,47,57,13173
            ]
        }},
        ""formDetected"":true,
        ""numForms"":1,
        ""numFormElements"":4,
        ""be"":{{""si"":false}},
        ""end"":{9},
        ""errors"":[],
        ""version"":""2.3.0"",
        ""id"":""{10}""
    }}",
            f2p,
            browser,
            capabilities,
            gpu,
            dnt,
            canvas,
            be,
            startTimestamp,
            state, // Variable à fournir
            endTimestamp,
            id
        );


        // we need to minify the JSON
        metrics = metrics.Replace("\n", "").Replace("\r", "").Replace("\n", "").Replace(" ", "");

        return (metrics, new Metrics
        {
            F2p = f2p,
            Browser = browser,
            Capabilities = capabilities,
            Gpu = gpu,
            Dnt = dnt,
            Canvas = canvas,
            Be = be,
            Timestamp1 = timestamp1,
            Timestamp2 = timestamp2
        });
    }


    private string CalculateChecksum(string payload)
    {
        // CRC32
        return CrcCalculator.CalculateChecksum(payload);
    }


    private async Task<ChallengeResponse> GetChallengeAsync()
    {
        var response =
            await _client.GetAsync(
                "https://3f38f7f4f368.83dbb5dc.eu-south-1.token.awswaf.com/3f38f7f4f368/e1fcfc58118e/inputs?client=browser");
        return JsonSerializer.Deserialize<ChallengeResponse>(await response.Content.ReadAsStringAsync())!;
    }

    private async Task<string> VerifySolution(ChallengeResponse challenge,
                                              string            solution,
                                              string            checksum,
                                              Encryptor         encryptor,
                                              string            metrics,
                                              Metrics           metricsObj)
    {
        var payload = new
        {
            challenge = challenge.Challenge,
            solution,
            checksum,
            existing_token = (object)null,
            domain = "auth.ankama.com",
            client = "Browser",
            signals = new[]
            {
                new
                {
                    name = "KramerAndRio",
                    value = new
                    {
                        Present = encryptor.Encrypt(checksum + "#" + metrics).Replace("KramerAndRio::", "")
                    }
                }
            },
            metrics = new object[]
            {
                new { name = "2", value = 0.5608000000000288, unit = "2" },
                new { name = "100", value = metricsObj.F2p, unit = "2" },
                new { name = "101", value = metricsObj.Browser, unit = "2" },
                new { name = "102", value = metricsObj.Capabilities, unit = "2" },
                new { name = "103", value = metricsObj.Gpu, unit = "2" },
                new { name = "104", value = metricsObj.Dnt, unit = "2" },
                new { name = "105", value = 0, unit = "2" },
                new { name = "106", value = 0, unit = "2" },
                new { name = "107", value = 0, unit = "2" },
                new { name = "108", value = 0, unit = "2" },
                new { name = "109", value = 0, unit = "2" },
                new { name = "110", value = 0, unit = "2" },
                new { name = "111", value = metricsObj.Canvas, unit = "2" },
                new { name = "112", value = 1, unit = "2" },
                new { name = "113", value = metricsObj.Be, unit = "2" },
                new { name = "3", value = 13.910499999999956, unit = "2" },
                new { name = "7", value = 0, unit = "4" },
                new { name = "1", value = metricsObj.Timestamp1, unit = "2" },
                new { name = "4", value = metricsObj.Timestamp2, unit = "2" },
                new { name = "5", value = 0.0013000000000147338, unit = "2" },
                new { name = "6", value = metricsObj.Timestamp1 + metricsObj.Timestamp2, unit = "2" },
                new { name = "8", value = 1, unit = "4" },
            }
        };

        _client.DefaultRequestHeaders.Add("accept", "*/*");
        _client.DefaultRequestHeaders.Add("accept-encoding", "gzip, deflate, br, zstd");
        _client.DefaultRequestHeaders.Add("accept-language", "fr-FR,fr;q=0.9,en-US;q=0.8,en;q=0.7");
        _client.DefaultRequestHeaders.TryAddWithoutValidation("content-type", "text/plain;charset=UTF-8");
        _client.DefaultRequestHeaders.Add("priority", "u=1, i");
        _client.DefaultRequestHeaders.Add("sec-ch-ua",
                                          "\"Chromium\";v=\"128\", \"Not;A=Brand\";v=\"24\", \"Google Chrome\";v=\"128\"");
        _client.DefaultRequestHeaders.Add("sec-ch-ua-mobile", "?0");
        _client.DefaultRequestHeaders.Add("sec-ch-ua-platform", "\"Windows\"");
        _client.DefaultRequestHeaders.Add("sec-fetch-dest", "empty");
        _client.DefaultRequestHeaders.Add("sec-fetch-mode", "cors");
        _client.DefaultRequestHeaders.Add("sec-fetch-site", "cross-site");
        _client.DefaultRequestHeaders.Add("user-agent",
                                          "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/129.0.0.0 Safari/537.36");

        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var response =
            await _client.PostAsync(
                "https://3f38f7f4f368.83dbb5dc.eu-south-1.token.awswaf.com/3f38f7f4f368/e1fcfc58118e/verify",
                content);
        return await response.Content.ReadAsStringAsync();
    }

    public class ChallengeResponse
    {
        [JsonPropertyName("challenge_type")] public string ChallengeType { get; set; }

        [JsonPropertyName("challenge")] public Challenge Challenge { get; set; }

        [JsonPropertyName("difficulty")] public int Difficulty { get; set; }
    }

    public class Challenge
    {
        [JsonPropertyName("input")] public string Input { get; set; }

        [JsonPropertyName("hmac")] public string Hmac { get; set; }

        [JsonPropertyName("region")] public string Region { get; set; }
    }

    public class Payload
    {
        public required string Input { get; set; }
        public required string Checksum { get; set; }
        public required int Difficulty { get; set; }
        public required int Memory { get; set; }
    }

    public async Task Initialize(WebProxy? proxy = null)
    {
        _client.Dispose();
        
        _jar = new CookieContainer();
        _handler = new HttpClientHandler { CookieContainer = _jar };

        if (proxy != null)
        {
            _handler.Proxy = proxy;
            _handler.UseDefaultCredentials = false;
            _handler.PreAuthenticate = true;
            _handler.UseProxy = true;
        }

        _client = new HttpClient(_handler) { Timeout = TimeSpan.FromSeconds(5) };

        _handler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

        try
        {
            var ip = await _client.GetStringAsync("https://api.ipify.org/?format=text");
            Console.WriteLine($"IP: {ip}");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
}

public class CrcCalculator
{
    public static string CalculateChecksum(string payload)
    {
        var utf8Encoded = Encoding.UTF8.GetBytes(payload);
        var crc32Value = CalculateCrc32(utf8Encoded);
        return HexEncode(crc32Value);
    }

    private static string HexEncode(uint value)
    {
        const string alphabet = "0123456789ABCDEF";
        var encoded = new char[8];

        encoded[0] = alphabet[(int)((value >> 28) & 0xF)];
        encoded[1] = alphabet[(int)((value >> 24) & 0xF)];
        encoded[2] = alphabet[(int)((value >> 20) & 0xF)];
        encoded[3] = alphabet[(int)((value >> 16) & 0xF)];
        encoded[4] = alphabet[(int)((value >> 12) & 0xF)];
        encoded[5] = alphabet[(int)((value >> 8) & 0xF)];
        encoded[6] = alphabet[(int)((value >> 4) & 0xF)];
        encoded[7] = alphabet[(int)(value & 0xF)];

        return new string(encoded);
    }

    private static uint[] BuildCrcTable()
    {
        const uint polynomial = 0xedb88320;
        var table = new uint[256];

        for (var i = 0; i < 256; i++)
        {
            var crc = (uint)i;
            for (var j = 0; j < 8; j++)
            {
                if ((crc & 1) != 0)
                    crc = (crc >> 1) ^ polynomial;
                else
                    crc >>= 1;
            }

            table[i] = crc;
        }

        return table;
    }

    public static uint CalculateCrc32(byte[] data)
    {
        var crcTable = BuildCrcTable();
        var crc = 0xffffffff;

        foreach (var b in data)
        {
            crc = (crc >> 8) ^ crcTable[(crc ^ b) & 0xFF];
        }

        return crc ^ 0xffffffff;
    }
}