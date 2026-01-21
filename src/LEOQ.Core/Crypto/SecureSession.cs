namespace LEOQ.Core.Crypto;

/// <summary>
/// Represents an end-to-end "secure session" established over a LEO-Q path.
/// 
/// In a real system:
/// - QKD would establish a shared secret key between endpoints.
/// - PQC algorithms would be used for key encapsulation / digital signatures.
/// 
/// Here we demonstrate only the integration points.
/// </summary>
public sealed class SecureSession
{
    public SecureSession(byte[] sessionKey)
    {
        SessionKey = sessionKey;
    }

    public byte[] SessionKey { get; }

    public string Protect(string plaintext)
        => PqcStub.EncryptToBase64(plaintext, SessionKey);

    public string Unprotect(string ciphertextBase64)
        => PqcStub.DecryptFromBase64(ciphertextBase64, SessionKey);
}
