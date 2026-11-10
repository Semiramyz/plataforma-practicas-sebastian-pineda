namespace Practicas.Domain;

/// <summary>Motivos estandarizados para marcar a un practicante como "No viable".</summary>
public enum MotivoRechazo
{
    /// <summary>Errores ortográficos, gramaticales o de redacción en la hoja de vida.</summary>
    ErroresOrtograficos = 1,

    /// <summary>Faltan datos importantes (contacto, formación, fechas) o están desactualizados.</summary>
    InformacionIncompleta = 2,

    /// <summary>Documento desordenado, difícil de leer o con una extensión inadecuada.</summary>
    FormatoInadecuado = 3,

    /// <summary>La formación o los intereses no corresponden al perfil de la práctica.</summary>
    PerfilNoAlineado = 4,

    /// <summary>No hay proyectos, prácticas ni experiencia que respalden el perfil.</summary>
    ExperienciaInsuficiente = 5,

    /// <summary>El PDF está dañado, escaneado sin legibilidad o no corresponde a una hoja de vida.</summary>
    DocumentoIlegible = 6,

    /// <summary>Otro motivo; exige explicarlo en las observaciones.</summary>
    Otro = 7
}
