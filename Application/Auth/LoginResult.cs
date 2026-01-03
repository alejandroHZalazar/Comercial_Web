using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Auth
{
    public record LoginResult
    (
        bool Success,
        string? Error,
        string? TipoUsuario
    );
}
