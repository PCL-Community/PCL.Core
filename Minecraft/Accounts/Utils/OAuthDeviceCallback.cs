namespace PCL.Core.Minecraft.Accounts.Utils;

public class OAuthDeviceCallback(string userCode, string verification, string? verificationCompleteUri)
{
    public string UserCode = userCode;
    public string Verification = verification;
    public string? VerificationComplete = verificationCompleteUri;
}