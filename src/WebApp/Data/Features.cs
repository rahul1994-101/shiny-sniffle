using WebApp.Models;

namespace WebApp.Data;

public class Features(Persistence _repo)
{
    public async Task<IEnumerable<User>> GetUsers()
    {
        return await _repo.GetUsers().ConfigureAwait(false);
    }
}
