using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Practicas.Api.Data;

public sealed class FechaUtcConverter() : ValueConverter<DateTime, DateTime>(
    valor => valor.ToUniversalTime(),
    valor => DateTime.SpecifyKind(valor, DateTimeKind.Utc));

public sealed class FechaUtcNullableConverter() : ValueConverter<DateTime?, DateTime?>(
    valor => valor.HasValue ? valor.Value.ToUniversalTime() : valor,
    valor => valor.HasValue ? DateTime.SpecifyKind(valor.Value, DateTimeKind.Utc) : valor);
