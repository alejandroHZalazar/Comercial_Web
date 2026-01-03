using Domain.Contracts;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class ParametroService : IParametroService
    {
        private readonly ComercialDbContext _context;

        public ParametroService(ComercialDbContext context)
        {
            _context = context;
        }

        public async Task<Parametro?> GetLogoAsync()
        {
            return await _context.Parametros
                .FirstOrDefaultAsync(p => p.Modulo == "login" && p.Parametro1 == "imagen");
        }

        public async Task UpdateLogoAsync(string nombreArchivo, byte[] imagen)
        {
            var parametro = await _context.Parametros
                .FirstOrDefaultAsync(p => p.Modulo == "login" && p.Parametro1 == "imagen");

            if (parametro is null)
            {
                parametro = new Parametro
                {
                    Modulo = "login",
                    Parametro1 = "imagen"
                };
                _context.Parametros.Add(parametro);
            }

            parametro.Valor = nombreArchivo;
            parametro.Imagen = imagen;

            await _context.SaveChangesAsync();
        }
    }
}
