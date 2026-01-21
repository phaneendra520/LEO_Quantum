using System;

namespace LEOQ.Core.Crypto;

/// <summary>
/// QKD key generation stub.
/// 
/// This is NOT a real QKD implementation.
/// It only generates random bytes to represent "shared key material".
/// </summary>
public static class QkdStub
{
    public static byte[] GenerateSharedKey(int lengthBytes, int? seed = null)
    {
        if (lengthBytes <= 0) throw new ArgumentOutOfRangeException(nameof(lengthBytes));

        var rnd = seed.HasValue ? new Random(seed.Value) : new Random();
        var key = new byte[lengthBytes];
        rnd.NextBytes(key);
        return key;
    }
}
