using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Security
{
    public static class PermisoExtensions
    {
        public static bool TienePermiso(this ClaimsPrincipal user, string permiso)
        {
            return user.Claims
                .Where(c => c.Type == "Permiso")
                .Any(c => c.Value == permiso);
        }
    }
}
