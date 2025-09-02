namespace PCL.Core.Net.Nat.Stun;

public enum StunAttributes : ushort
{
    MappedAddress = 0x0001,
    ResponseAddress = 0x0002,
    ChangeAddress = 0x0003,
    SourceAddress = 0x0004,
    ChangedAddress = 0x0005,
    Username = 0x0006,
    Password = 0x0007,
    MessageIntegrity = 0x0008,
    ErrorCode = 0x0009,
    Unknown = 0x000A,
    ReflectedFrom = 0x000B,
    Realm = 0x0014,
    Nonce = 0x0015,
    MessageIntegritySha256 = 0x001C,
    PasswordAlgorithm = 0x001D,
    UserHash = 0x001E,
    XorMappedAddressAttribute = 0x0020,
    Software = 0x8022,
    AlternateServer = 0x8023,
    CacheTimeout = 0x8027,
    Fingerprint = 0x8028,
    ResponseOriginal = 0x802b,
    OtherAddress = 0x802c
}