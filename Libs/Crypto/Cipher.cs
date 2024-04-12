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
            BigInteger test = 7;
            if (BigInteger.ModPow(BigInteger.ModPow(test, e, n), d, n) != test)
                throw new InvalidDataException("Keys not match!");
            if (n.GetByteCount() < 2)
            {
                throw new InvalidDataException("p, q too low. p*q must be at least 16 bytes long!");
            }

            Debug.WriteLine($"n:{n}\n\rd:{d}\n\r");
        }
    }

    private static Task EncodeBlock(ref byte[] block)
    {
        List<byte> tmp = new List<byte>(block);
        tmp.Add(1);
        block = tmp.ToArray();
        BigInteger num = new BigInteger(block);
        BigInteger enc = BigInteger.ModPow(num, e, n);
        Debug.WriteLine($"Message block: {num}");
        Debug.WriteLine($"Encrypted block: {enc}");
        block = enc.ToByteArray();
        List<byte> bts = new List<byte>();
        byte[] bytes = BitConverter.GetBytes(block.Length);
        bts.Add((byte)bytes.Length);
        bts.AddRange(bytes);
        bts.AddRange(block);
        block = bts.ToArray();
        return Task.CompletedTask;
    }

    public static Task<byte[]> Encode(byte[] msg)
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


        return Task.FromResult(enc.ToArray());
    }

    private static Task DecodeBlock(ref byte[] block)
    {
        BigInteger num = new BigInteger(block);
        BigInteger dec = BigInteger.ModPow(num, d, n);
        Debug.WriteLine($"Decrypted block: {dec}");
        block = dec.ToByteArray();
        block = block[0..^1];
        return Task.CompletedTask;
    }

    public static Task<byte[]> Decode(byte[] encrypted)
    {
        int keyByteNum = n.GetByteCount();
        int blockCount = (int)Math.Ceiling(encrypted.Length / (double)keyByteNum);
        byte[][] blocks = new byte[blockCount][];
        Debug.WriteLine(
            $"Message byte length:{encrypted.Length}\nBlock count: {blockCount}\nKey byte count:{keyByteNum}");
        List<Task> tasks = new List<Task>();
        for (int i = 0, j = 0, len = 0; i < encrypted.Length; i += len, j++)
        {
            int len_bts = encrypted[i++];
            len = BitConverter.ToInt32(encrypted[i..(i + len_bts)]);
            i += len_bts;
            blocks[j] = encrypted[i..(i + len)];
            var j1 = j;
            tasks.Add(Task.Run(() => DecodeBlock(ref blocks[j1])));
        }

        Task.WaitAll(tasks.ToArray());
        List<byte> dec = new List<byte>();
        foreach (byte[] block in blocks)
        {
            if (block is null) continue;
            dec.AddRange(block);
        }

        return Task.FromResult(dec.ToArray());
    }
}