using System.Security.Cryptography;

namespace JF.AgenticEnterprise.Domain.Common;

public static class UlidGenerator
{
    private static readonly char[] Encoding = "0123456789ABCDEFGHJKMNPQRSTVWXYZ".ToCharArray();

    public static string NewUlid()
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var random = new byte[10];
        RandomNumberGenerator.Fill(random);

        var chars = new char[26];
        chars[0] = Encoding[(now >> 45) & 31];
        chars[1] = Encoding[(now >> 40) & 31];
        chars[2] = Encoding[(now >> 35) & 31];
        chars[3] = Encoding[(now >> 30) & 31];
        chars[4] = Encoding[(now >> 25) & 31];
        chars[5] = Encoding[(now >> 20) & 31];
        chars[6] = Encoding[(now >> 15) & 31];
        chars[7] = Encoding[(now >> 10) & 31];
        chars[8] = Encoding[(now >> 5) & 31];
        chars[9] = Encoding[now & 31];
        chars[10] = Encoding[(random[0] >> 3) & 31];
        chars[11] = Encoding[((random[0] & 7) << 2) | ((random[1] >> 6) & 3)];
        chars[12] = Encoding[(random[1] >> 1) & 31];
        chars[13] = Encoding[((random[1] & 1) << 4) | ((random[2] >> 4) & 15)];
        chars[14] = Encoding[((random[2] & 15) << 1) | ((random[3] >> 7) & 1)];
        chars[15] = Encoding[(random[3] >> 2) & 31];
        chars[16] = Encoding[((random[3] & 3) << 3) | ((random[4] >> 5) & 7)];
        chars[17] = Encoding[random[4] & 31];
        chars[18] = Encoding[(random[5] >> 3) & 31];
        chars[19] = Encoding[((random[5] & 7) << 2) | ((random[6] >> 6) & 3)];
        chars[20] = Encoding[(random[6] >> 1) & 31];
        chars[21] = Encoding[((random[6] & 1) << 4) | ((random[7] >> 4) & 15)];
        chars[22] = Encoding[((random[7] & 15) << 1) | ((random[8] >> 7) & 1)];
        chars[23] = Encoding[(random[8] >> 2) & 31];
        chars[24] = Encoding[((random[8] & 3) << 3) | ((random[9] >> 5) & 7)];
        chars[25] = Encoding[random[9] & 31];

        return new string(chars);
    }
}
