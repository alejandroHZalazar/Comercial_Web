using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Contracts
{
    public interface IParametroService
    {
        Task<Parametro?> GetLogoAsync();
        Task UpdateLogoAsync(string nombreArchivo, byte[] imagen);
    }

}
