using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Practicas.Application.Abstracciones;
using Practicas.Application.Errores;
using Practicas.Domain;

namespace Practicas.Infrastructure.Persistencia;

/// <summary>Repositorio de practicantes sobre EF Core.</summary>
public sealed class PracticanteRepositorio(PracticasDbContext db) : IPracticanteRepositorio
{
    private const int SqliteRestriccionViolada = 19;

    public Task<bool> ExisteCorreoAsync(string correo, CancellationToken cancellationToken = default) =>
        db.Practicantes.AsNoTracking().AnyAsync(p => p.Correo == correo, cancellationToken);

    public Task<Practicante?> ObtenerAsync(int id, CancellationToken cancellationToken = default) =>
        db.Practicantes.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Practicante>> ListarAsync(
        EstadoPracticante? estado, string? busqueda, CancellationToken cancellationToken = default)
    {
        var consulta = db.Practicantes.AsNoTracking();

        if (estado is not null)
        {
            consulta = consulta.Where(p => p.Estado == estado);
        }

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            // Consulta parametrizada por EF Core: el texto del usuario nunca se concatena en SQL.
            var patron = $"%{busqueda.Trim()}%";
            consulta = consulta.Where(p =>
                EF.Functions.Like(p.NombreCompleto, patron) ||
                EF.Functions.Like(p.Correo, patron) ||
                EF.Functions.Like(p.Carrera, patron));
        }

        return await consulta
            .OrderByDescending(p => p.FechaRegistroUtc)
            .ThenByDescending(p => p.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task AgregarAsync(Practicante practicante, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(practicante);
        db.Practicantes.Add(practicante);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqliteException { SqliteErrorCode: SqliteRestriccionViolada })
        {
            // Dos registros simultáneos con el mismo correo: lo detecta el índice único.
            db.Entry(practicante).State = EntityState.Detached;
            throw new CorreoDuplicadoException(practicante.Correo);
        }
    }

    public Task GuardarCambiosAsync(CancellationToken cancellationToken = default) =>
        db.SaveChangesAsync(cancellationToken);
}
