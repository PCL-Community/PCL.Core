using System.Threading.Tasks;

namespace PCL.Core.Utils.Account;

public interface IAccount<TAccountData, TAuthenticationData>
{
    AccountStatus Status { get; }

    AccountDataRecord<TAccountData> GetAccountData();

    Task<IAccountAuthentication<TAuthenticationData>> AskAuthenticationAsync();

    Task RefreshAsync();
}