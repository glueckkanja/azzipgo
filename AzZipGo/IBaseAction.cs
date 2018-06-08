using System.Threading.Tasks;

namespace AzZipGo
{
    public interface IBaseAction
    {
        Task<int> RunAsync();
    }
}