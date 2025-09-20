using System.Threading.Tasks;

namespace PCL.Core.Utils.Account.MicrosoftAccount;

public class MicrosoftAccount : IAccount<MicrosoftAccountData, MicrosoftAuthenticationData>
{
    private MicrosoftAccountData _accountData;

    public Task<IAccountAuthentication<MicrosoftAuthenticationData>> AskAuthenticationAsync()
    {
        throw new System.NotImplementedException();
    }

    public AccountDataRecord<MicrosoftAccountData> GetAccountData()
    {
        throw new System.NotImplementedException();
    }

    public Task RefreshAsync()
    {
        throw new System.NotImplementedException();
    }

    public AccountStatus Status => AccountStatus.Error;
}