using Domain.Contracts;
using Domain.DTO;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace Infrastructure.Services;

public class RubroService : IRubroService
{
    private readonly ComercialDbContext _db;
    public RubroService(ComercialDbContext db) => _db = db;

    public async Task<List<Rubro>> GetAllAsync() =>
        await _db.Rubros
            .OrderBy(r => r.Descripcion)
            .ToListAsync();

    public Task<List<RubroNodoDto>> GetAllConRutaAsync() =>
        RubroJerarquia.CargarAsync(_db);

    public async Task<Rubro?> GetByIdAsync(int id) =>
        await _db.Rubros.FindAsync(id);

    public async Task CreateAsync(string descripcion, int? fkRubroPadre = null)
    {
        if (string.IsNullOrWhiteSpace(descripcion))
            throw new ArgumentException("Debe indicar una descripción");

        await EjecutarAbmAsync(0, 1, descripcion.Trim().ToUpper(), fkRubroPadre);
    }

    public async Task UpdateAsync(int id, string descripcion, int? fkRubroPadre = null)
    {
        if (string.IsNullOrWhiteSpace(descripcion))
            throw new ArgumentException("Debe indicar una descripción");

        if (!await _db.Rubros.AnyAsync(r => r.Id == id))
            throw new InvalidOperationException("Rubro no encontrado");

        await EjecutarAbmAsync(id, 2, descripcion.Trim().ToUpper(), fkRubroPadre);
    }

    public async Task DeleteAsync(int id)
    {
        if (!await _db.Rubros.AnyAsync(r => r.Id == id))
            throw new InvalidOperationException("Rubro no encontrado");

        await EjecutarAbmAsync(id, 3, null, null);
    }

    // Reutiliza sp_RubrosABM (mismo SP que la aplicación de escritorio): valida ciclos, nombre repetido
    // bajo el mismo padre, subrubros y productos asociados, y mantiene la tabla RubrosArbol.
    // unTipo: 1 = alta, 2 = modificación, 3 = baja.
    private async Task EjecutarAbmAsync(int id, int tipo, string? descripcion, int? padre)
    {
        var conn   = _db.Database.GetDbConnection();
        var abrir  = conn.State != ConnectionState.Open;
        if (abrir) await conn.OpenAsync();
        try
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "sp_RubrosABM";
            cmd.CommandType = CommandType.StoredProcedure;

            void Param(string nombre, DbType tipoDb, object? valor, ParameterDirection dir = ParameterDirection.Input)
            {
                var p = cmd.CreateParameter();
                p.ParameterName = nombre;
                p.DbType        = tipoDb;
                p.Direction     = dir;
                p.Value         = valor ?? DBNull.Value;
                cmd.Parameters.Add(p);
            }

            Param("unId",           DbType.Int32,  id);
            Param("unTipo",         DbType.Int32,  tipo);
            Param("unaDescripcion", DbType.String, descripcion);
            Param("unPadre",        DbType.Int32,  padre);
            Param("salida",         DbType.Int32,  null, ParameterDirection.Output);

            await cmd.ExecuteNonQueryAsync();

            var salida = Convert.ToInt32(cmd.Parameters["salida"].Value);
            if (salida > 0) return;

            throw salida switch
            {
                -2 => new InvalidOperationException("El rubro padre no puede ser el mismo rubro ni uno de sus subrubros."),
                -3 => new InvalidOperationException("No se puede eliminar: el rubro tiene subrubros."),
                -4 => new InvalidOperationException("No se puede eliminar: el rubro tiene productos asignados."),
                -5 => new InvalidOperationException("Ya existe un rubro con esa descripción dentro del mismo rubro padre."),
                -6 => new InvalidOperationException("El rubro padre seleccionado no existe."),
                -7 => new ArgumentException("Debe indicar una descripción"),
                _  => new InvalidOperationException("No se pudo guardar el rubro.")
            };
        }
        finally
        {
            if (abrir) await conn.CloseAsync();
        }
    }
}
