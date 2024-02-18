using System.Diagnostics;
using System.Numerics;
using Newtonsoft.Json;

namespace Crypto;

class Base
{
    public string p;
    public string q;
}

public static class Cipher
{
    public static BigInteger n, e, d, p, q;
    public const string ConfigDir = "./config/";

    static BigInteger ModInverse(BigInteger a, BigInteger m)
    {
        BigInteger m0 = m;
        BigInteger y = 0, x = 1;

        if (m == 1)
            return 0;

        while (a > 1)
        {
            // q - частное
            BigInteger q = a / m;

            BigInteger t = m;

            // m - остаток
            m = a % m;
            a = t;
            t = y;

            // Обновление x и y
            y = x - q * y;
            x = t;
        }

        // Проверка на отсутствие обратного элемента
        if (x < 0)
            x += m0;

        return x;
    }

    public static void ReadKeys()
    {
        using (StreamReader reader = new StreamReader(ConfigDir + "keys.json"))
        {
            var pq = JsonConvert.DeserializeObject<Base>(reader.ReadToEnd()) ??
                     throw new InvalidDataException("JSON is invalid!");
            p = BigInteger.Parse(pq.p);
            q = BigInteger.Parse(pq.q);

            n = p * q;
            var phi = (p - 1) * (q - 1);
            e = 65537;
            d = ModInverse(e, phi);
            BigInteger test = 307;
            if (BigInteger.ModPow(BigInteger.ModPow(test, e, n), d, n) != test)
                throw new InvalidDataException("Keys not match!");

            Debug.WriteLine($"n:{n}\n\rd:{d}\n\r");
        }
    }

    /// <summary>
    /// Encodes string
    /// </summary>
    /// <param name="msg"> Text to be encoded</param>
    /// <returns> Bytes of encoded text</returns>
    /// <exception cref="InvalidDataException"> If text not in ascii </exception>
    public static byte[] Encode(byte[] msg)
    {
        BigInteger message = new BigInteger(msg);
        BigInteger encrypted = BigInteger.ModPow(message, e, n);
        return encrypted.ToByteArray();
    }

    public static byte[] Decode(byte[] encrypted)
    {
        BigInteger encMsg = new BigInteger(encrypted);
        BigInteger msg = BigInteger.ModPow(encMsg, d, n);
        return msg.ToByteArray();
    }
}