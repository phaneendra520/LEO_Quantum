using System;
using System.Text;

namespace LEOQ.Core.Crypto;

/// <summary>
/// Post-Quantum Cryptography (PQC) stub.
/// 
/// This is NOT real PQC.
/// We provide a trivial XOR-based transform to demonstrate interfaces.
/// Replace with a real library (e.g., standardized PQC algorithms) when required.
/// </summary>
public static class PqcStub
{
    public static string EncryptToBase64(string plaintext, byte[] key)
    {
        var bytes = Encoding.UTF8.GetBytes(plaintext);
        var outBytes = Xor(bytes, key);
        return Convert.ToBase64String(outBytes);
    }

    public static string DecryptFromBase64(string ciphertextBase64, byte[] key)
    {
        var bytes = Convert.FromBase64String(ciphertextBase64);
        var outBytes = Xor(bytes, key);
        return Encoding.UTF8.GetString(outBytes);
    }

    private static byte[] Xor(byte[] data, byte[] key)
    {
        if (key.Length == 0) throw new ArgumentException("Key must not be empty", nameof(key));
        var outBytes = new byte[data.Length];
        for (var i = 0; i < data.Length; i++)
        {
            outBytes[i] = (byte)(data[i] ^ key[i % key.Length]);
        }
        return outBytes;
    }
}
