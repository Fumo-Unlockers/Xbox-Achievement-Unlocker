﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Security.Cryptography;
using Microsoft.Identity.Client.AuthScheme.PoP;

public class ECDCertificatePopCryptoProvider : IPoPCryptoProvider
{
    public byte[] Sign(byte[] payload)
    {
        return Sign(_signingKey, payload);
    }

    public ECDCertificatePopCryptoProvider()
    {
        InitializeSigningKey();
    }

    private ECDsa _signingKey;

    public string CannonicalPublicKeyJwk {  get; private set; }

    public string CryptographicAlgorithm { get => "ES256"; }

    private object? _proofKey;
    public object ProofKey => _proofKey ??= generateNewProofKey();

    private void InitializeSigningKey()
    {
        _signingKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);

        ECParameters publicKeyInfo = _signingKey.ExportParameters(false);

        CannonicalPublicKeyJwk = ComputeCanonicalJwk(publicKeyInfo);
    }

    /// <summary>
    /// Creates the canonical representation of the JWK.  See https://tools.ietf.org/html/rfc7638#section-3
    /// The number of parameters as well as the lexicographic order is important, as this string will be hashed to get a thumbprint
    /// </summary>
    private static string ComputeCanonicalJwk(ECParameters ecdPublicKey)
    {
        string x = ecdPublicKey.Q.X != null ? Base64UrlHelpers.Encode(ecdPublicKey.Q.X) : null;
        string y = ecdPublicKey.Q.Y != null ? Base64UrlHelpers.Encode(ecdPublicKey.Q.Y) : null;
        return $@"{{""{JsonWebKeyParameterNames.Use}"":""sig"",""{JsonWebKeyParameterNames.Alg}"":""ES256"",""{JsonWebKeyParameterNames.Kty}"":""{JsonWebKeyParameterNames.EC}"",""{JsonWebKeyParameterNames.Crv}"":""{GetCrvParameterValue(ecdPublicKey.Curve)}"",""{JsonWebKeyParameterNames.X}"":""{x}"",""{JsonWebKeyParameterNames.Y}"":""{y}""}}";
    }

    private object generateNewProofKey()
    {
        var parameters = _signingKey.ExportParameters(false);
        return new
        {
            kty = "EC",
            x = parameters.Q.X != null ? Base64UrlHelpers.Encode(parameters.Q.X) : null,
            y = parameters.Q.Y != null ? Base64UrlHelpers.Encode(parameters.Q.Y) : null,
            crv = "P-256",
            alg = "ES256",
            use = "sig"
        };
    }

    public static byte[] Sign(ECDsa EcdKey, byte[] payload)
    {
        return EcdKey.SignData(payload, HashAlgorithmName.SHA256);
    }

    private static string GetCrvParameterValue(ECCurve curve)
    {
        if (string.Equals(curve.Oid.Value, ECCurve.NamedCurves.nistP256.Oid.Value, StringComparison.Ordinal) || string.Equals(curve.Oid.FriendlyName, ECCurve.NamedCurves.nistP256.Oid.FriendlyName, StringComparison.Ordinal))
            return JsonWebKeyECTypes.P256;
        else if (string.Equals(curve.Oid.Value, ECCurve.NamedCurves.nistP384.Oid.Value, StringComparison.Ordinal) || string.Equals(curve.Oid.FriendlyName, ECCurve.NamedCurves.nistP384.Oid.FriendlyName, StringComparison.Ordinal))
            return JsonWebKeyECTypes.P384;
        else if (string.Equals(curve.Oid.Value, ECCurve.NamedCurves.nistP521.Oid.Value, StringComparison.Ordinal) || string.Equals(curve.Oid.FriendlyName, ECCurve.NamedCurves.nistP521.Oid.FriendlyName, StringComparison.Ordinal))
            return JsonWebKeyECTypes.P521;
        else
            throw new ArgumentException();
    }

    /// <summary>
    /// Constants for JsonWebKey Elliptical Curve Types
    /// https://tools.ietf.org/html/rfc7518#section-6.2.1.1
    /// </summary>
    private static class JsonWebKeyECTypes
    {
        public const string P256 = "P-256";
        public const string P384 = "P-384";
        public const string P512 = "P-512";
        public const string P521 = "P-521";
    }
}