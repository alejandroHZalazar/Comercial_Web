using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Auth
{
    public interface ILoginService
    {
        Task<LoginResult> LoginAsync(string usuario, string password);
    }
}
