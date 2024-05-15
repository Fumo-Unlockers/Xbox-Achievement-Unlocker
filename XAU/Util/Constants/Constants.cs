// Minimize total number of string allocations if .NET runtime is opting to not intern them
struct StringConstants
{
    public const string Gamerscore = "Gamerscore";
    public const string Achieved = "Achieved";
    public const string ZeroUid = "00000000-0000-0000-0000-000000000000";
}

struct HeaderNames
{
    public const string ContractVersion = "x-xbl-contract-version";

    public const string AcceptEncoding = "Accept-Encoding";
    public const string Accept = "accept";
    public const string Authorization = "Authorization";
    public const string AcceptLanguage = "accept-language";
}

struct HeaderValues
{
    public const string ContractVersionValue2 = "2";
    public const string ContractVersionValue3 = "3";
    public const string AcceptEncodingValue = "gzip, deflate";
    public const string AcceptValue = "application/json";


}
