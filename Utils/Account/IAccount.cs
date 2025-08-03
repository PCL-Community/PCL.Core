using System.Threading.Tasks;

namespace PCL.Core.Utils.Account;

public interface IAccount<TAccountData, TAuthenticationData>
{
    AccountStatus Status { get; }

    AccountDataProvider<TAccountData> GetAccountData();

    Task<IAccountAuthentication<TAuthenticationData>> AskAuthenticationAsync();

    Task RefreshAsync();
}