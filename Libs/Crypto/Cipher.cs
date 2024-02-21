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
    private static Mutex m = new Mutex();

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
            BigInteger test = 1029375836273496197;
            if (BigInteger.ModPow(BigInteger.ModPow(test, e, n), d, n) != test)
                throw new InvalidDataException("Keys not match!");

            Debug.WriteLine($"n:{n}\n\rd:{d}\n\r");
        }
    }

    private static void PadZero(ref byte[] bts, int len)
    {
        var zeros = Enumerable.Repeat<byte>(0, len - bts.Length);
        List<byte> padded = new List<byte>();
        padded.AddRange(bts);
        padded.AddRange(zeros);
        bts = padded.ToArray();
    }

    private static Task EncodeBlock(ref byte[] block)
    {
        BigInteger num = new BigInteger(block);
        BigInteger enc = BigInteger.ModPow(num, e, n);
        Debug.WriteLine($"Message block: {num}");
        Debug.WriteLine($"Encrypted block: {enc}");
        m.WaitOne();
        block = enc.ToByteArray();
        List<byte> bts = new List<byte>();
        if (block.Length == n.GetByteCount())
        {
            bts.Add(1);
        }
        else
        {
            bts.Add(0);
        }

        bts.AddRange(block);
        block = bts.ToArray();
        m.ReleaseMutex();
        return Task.CompletedTask;
    }

    public static byte[] Encode(byte[] msg)
    {
        int keyByteNum = n.GetByteCount() - 1;
        int blockCount = (int)Math.Ceiling(msg.Length / (double)keyByteNum);
        byte[][] blocks = new byte[blockCount][];
        List<Task> tasks = new List<Task>();
        for (int i = 0, j = 0; i < msg.Length; i += keyByteNum, j++)
        {
            blocks[j] = msg[i..int.Min(i + keyByteNum, msg.Length)];
            var j1 = j;
            tasks.Add(Task.Run(() => EncodeBlock(ref blocks[j1])));
        }

        Task.WaitAll(tasks.ToArray());
        List<byte> enc = new List<byte>();
        foreach (var block in blocks)
        {
            enc.AddRange(block);
        }


        return enc.ToArray();
    }

    private static Task DecodeBlock(ref byte[] block)
    {
        BigInteger num = new BigInteger(block);
        BigInteger dec = BigInteger.ModPow(num, d, n);
        Debug.WriteLine($"Decrypted block: {dec}");
        m.WaitOne();
        block = dec.ToByteArray();
        m.ReleaseMutex();
        return Task.CompletedTask;
    }

    public static byte[] Decode(byte[] encrypted)
    {
        int keyByteNum = n.GetByteCount();
        int blockCount = (int)Math.Ceiling(encrypted.Length / ((double)keyByteNum + 1));
        byte[][] blocks = new byte[blockCount][];
        List<Task> tasks = new List<Task>();
        for (int i = 0, j = 0, padding = 0; i < encrypted.Length; i += keyByteNum + padding, j++)
        {
            padding = encrypted[i++] == 0 ? -1 : 0;
            blocks[j] = encrypted[i..int.Min(i + keyByteNum + padding, encrypted.Length)];
            var j1 = j;
            tasks.Add(Task.Run(() => DecodeBlock(ref blocks[j1])));
        }

        Task.WaitAll(tasks.ToArray());
        List<byte> dec = new List<byte>();
        foreach (byte[] block in blocks)
        {
            dec.AddRange(block);
        }

        return dec.ToArray();
    }
}