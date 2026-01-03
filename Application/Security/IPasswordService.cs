using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Security
{
    public interface IPasswordService
    {
        string Hash(string password);
        bool Verify(string hash, string password);
        bool IsHash(string value);
    }
}
