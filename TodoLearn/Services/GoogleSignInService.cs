using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Maui.ApplicationModel;
using TodoLearn.Resources.Strings;

namespace TodoLearn.Services;

public sealed class GoogleSignInService : IGoogleSignInService
{
    public const string ProviderName = "Google";
    private const string ClientId = "201062673130-5b0ble7qbm2rr5m3rmhtqm2jo5l6cnhg.apps.googleusercontent.com";
    private const string ClientSecretEnvironmentVariable = "TODOLEARN_GOOGLE_CLIENT_SECRET";
    private static readonly HttpClient HttpClient = new();

    public async Task<GoogleAccountIdentity> SignInAsync(CancellationToken cancellationToken = default)
    {
#if WINDOWS
        try
        {
            return await SignInWindowsAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException();
        }
#else
        throw new PlatformNotSupportedException();
#endif
    }

#if WINDOWS
    private static async Task<GoogleAccountIdentity> SignInWindowsAsync(CancellationToken cancellationToken)
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;

        var redirectUri = $"http://127.0.0.1:{port}/";
        var state = CreateRandomValue();
        var nonce = CreateRandomValue();
        var verifier = CreateRandomValue();
        var challenge = Base64UrlEncode(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromMinutes(3));

        var authorizationUrl = BuildUrl("https://accounts.google.com/o/oauth2/v2/auth", new Dictionary<string, string>
        {
            ["client_id"] = ClientId,
            ["redirect_uri"] = redirectUri,
            ["response_type"] = "code",
            ["scope"] = "openid email",
            ["state"] = state,
            ["nonce"] = nonce,
            ["code_challenge"] = challenge,
            ["code_challenge_method"] = "S256",
            ["prompt"] = "select_account"
        });

        await Launcher.Default.OpenAsync(new Uri(authorizationUrl));

        string? authorizationCode = null;
        while (authorizationCode is null)
        {
            TcpClient client;
            try
            {
                client = await listener.AcceptTcpClientAsync(timeout.Token);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                throw new TimeoutException();
            }

            using (client)
            {
                var stream = client.GetStream();
                using var reader = new StreamReader(stream, Encoding.ASCII, leaveOpen: true);
                var requestLine = await reader.ReadLineAsync(timeout.Token);
                while (!string.IsNullOrEmpty(await reader.ReadLineAsync(timeout.Token)))
                {
                }

                var query = ParseCallbackQuery(requestLine, redirectUri);
                var responseState = query.GetValueOrDefault("state");
                if (!FixedTimeEquals(responseState, state))
                {
                    await RespondAsync(stream, AppResources.GoogleAuthInvalidResponse);
                    continue;
                }

                var error = query.GetValueOrDefault("error");
                if (error == "access_denied")
                {
                    await RespondAsync(stream, AppResources.GoogleAuthCancelled);
                    throw new GoogleSignInCancelledException();
                }

                if (!string.IsNullOrWhiteSpace(error))
                {
                    await RespondAsync(stream, AppResources.GoogleAuthFailed);
                    throw new InvalidOperationException();
                }

                authorizationCode = query.GetValueOrDefault("code");
                if (string.IsNullOrWhiteSpace(authorizationCode))
                {
                    await RespondAsync(stream, AppResources.GoogleAuthInvalidResponse);
                    continue;
                }

                await RespondAsync(stream, AppResources.GoogleAuthBrowserMessage);
            }
        }

        var tokenRequest = new Dictionary<string, string>
        {
            ["client_id"] = ClientId,
            ["code"] = authorizationCode,
            ["code_verifier"] = verifier,
            ["grant_type"] = "authorization_code",
            ["redirect_uri"] = redirectUri
        };
        var clientSecret = Environment.GetEnvironmentVariable(ClientSecretEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(clientSecret))
            tokenRequest["client_secret"] = clientSecret;

        using var tokenResponse = await HttpClient.PostAsync(
            "https://oauth2.googleapis.com/token",
            new FormUrlEncodedContent(tokenRequest),
            cancellationToken);
        if (!tokenResponse.IsSuccessStatusCode)
        {
            var errorBody = await tokenResponse.Content.ReadAsStringAsync(cancellationToken);
            var error = "unknown_error";
            var description = string.Empty;
            try
            {
                using var errorDocument = JsonDocument.Parse(errorBody);
                if (errorDocument.RootElement.TryGetProperty("error", out var errorElement))
                    error = errorElement.GetString() ?? error;
                if (errorDocument.RootElement.TryGetProperty("error_description", out var descriptionElement))
                    description = descriptionElement.GetString() ?? string.Empty;
            }
            catch (JsonException)
            {
            }

            if (error == "invalid_request" && description.Contains("client_secret is missing", StringComparison.OrdinalIgnoreCase))
                throw new GoogleClientConfigurationException();

            var message = string.IsNullOrWhiteSpace(description)
                ? $"Google token exchange failed: {error}."
                : $"Google token exchange failed: {error}. {description}";
            throw new HttpRequestException(message, null, tokenResponse.StatusCode);
        }

        using var tokenDocument = JsonDocument.Parse(await tokenResponse.Content.ReadAsStringAsync(cancellationToken));
        if (!tokenDocument.RootElement.TryGetProperty("id_token", out var idTokenElement))
            throw new InvalidOperationException();

        var idToken = idTokenElement.GetString();
        if (string.IsNullOrWhiteSpace(idToken))
            throw new InvalidOperationException();

        return await ValidateIdentityTokenAsync(idToken, nonce, cancellationToken);
    }

    private static async Task<GoogleAccountIdentity> ValidateIdentityTokenAsync(
        string idToken,
        string expectedNonce,
        CancellationToken cancellationToken)
    {
        var parts = idToken.Split('.');
        if (parts.Length != 3)
            throw new InvalidOperationException();

        using var header = JsonDocument.Parse(Base64UrlDecode(parts[0]));
        using var payload = JsonDocument.Parse(Base64UrlDecode(parts[1]));
        var keyId = header.RootElement.GetProperty("kid").GetString();
        var algorithm = header.RootElement.GetProperty("alg").GetString();
        if (string.IsNullOrWhiteSpace(keyId) || algorithm != "RS256")
            throw new InvalidOperationException();

        using var keysResponse = await HttpClient.GetAsync("https://www.googleapis.com/oauth2/v3/certs", cancellationToken);
        keysResponse.EnsureSuccessStatusCode();
        using var keys = JsonDocument.Parse(await keysResponse.Content.ReadAsStringAsync(cancellationToken));
        var key = keys.RootElement.GetProperty("keys").EnumerateArray()
            .FirstOrDefault(candidate => candidate.TryGetProperty("kid", out var kid) && kid.GetString() == keyId);
        if (key.ValueKind == JsonValueKind.Undefined)
            throw new InvalidOperationException();

        using var rsa = RSA.Create();
        rsa.ImportParameters(new RSAParameters
        {
            Modulus = Base64UrlDecode(key.GetProperty("n").GetString()!),
            Exponent = Base64UrlDecode(key.GetProperty("e").GetString()!)
        });

        var signedContent = Encoding.ASCII.GetBytes($"{parts[0]}.{parts[1]}");
        if (!rsa.VerifyData(signedContent, Base64UrlDecode(parts[2]), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1))
            throw new InvalidOperationException();

        var claims = payload.RootElement;
        var issuer = claims.GetProperty("iss").GetString();
        if (issuer is not ("https://accounts.google.com" or "accounts.google.com"))
            throw new InvalidOperationException();

        var audience = claims.GetProperty("aud");
        var hasValidAudience = audience.ValueKind == JsonValueKind.String
            ? audience.GetString() == ClientId
            : audience.ValueKind == JsonValueKind.Array && audience.EnumerateArray().Any(value => value.GetString() == ClientId);
        if (!hasValidAudience)
            throw new InvalidOperationException();

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (claims.GetProperty("exp").GetInt64() <= now
            || claims.GetProperty("iat").GetInt64() > now + 120
            || !FixedTimeEquals(claims.GetProperty("nonce").GetString(), expectedNonce)
            || !claims.TryGetProperty("email_verified", out var emailVerified)
            || emailVerified.ValueKind != JsonValueKind.True)
        {
            throw new InvalidOperationException();
        }

        var subject = claims.GetProperty("sub").GetString();
        var email = claims.GetProperty("email").GetString();
        if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(email))
            throw new InvalidOperationException();

        return new GoogleAccountIdentity(subject, email);
    }

    private static Dictionary<string, string> ParseCallbackQuery(string? requestLine, string redirectUri)
    {
        if (requestLine is null)
            return new Dictionary<string, string>();

        var requestParts = requestLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (requestParts.Length < 2 || requestParts[0] != "GET")
            return new Dictionary<string, string>();

        var requestUri = new Uri(new Uri(redirectUri), requestParts[1]);
        return requestUri.Query.TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Split('=', 2))
            .Where(part => part.Length == 2)
            .ToDictionary(
                part => Uri.UnescapeDataString(part[0].Replace('+', ' ')),
                part => Uri.UnescapeDataString(part[1].Replace('+', ' ')),
                StringComparer.Ordinal);
    }

    private static async Task RespondAsync(NetworkStream stream, string message)
    {
        var body = Encoding.UTF8.GetBytes(message);
        var headers = Encoding.ASCII.GetBytes(
            $"HTTP/1.1 200 OK\r\nContent-Type: text/plain; charset=utf-8\r\nContent-Length: {body.Length}\r\nConnection: close\r\n\r\n");
        await stream.WriteAsync(headers);
        await stream.WriteAsync(body);
        await stream.FlushAsync();
    }

    private static string BuildUrl(string endpoint, IReadOnlyDictionary<string, string> parameters) =>
        $"{endpoint}?{string.Join("&", parameters.Select(parameter => $"{Uri.EscapeDataString(parameter.Key)}={Uri.EscapeDataString(parameter.Value)}"))}";

    private static string CreateRandomValue() => Base64UrlEncode(RandomNumberGenerator.GetBytes(32));

    private static string Base64UrlEncode(byte[] value) =>
        Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] Base64UrlDecode(string value)
    {
        var base64 = value.Replace('-', '+').Replace('_', '/');
        base64 += new string('=', (4 - base64.Length % 4) % 4);
        return Convert.FromBase64String(base64);
    }

    private static bool FixedTimeEquals(string? actual, string expected) =>
        actual is not null && CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(actual),
            Encoding.UTF8.GetBytes(expected));
#endif
}
