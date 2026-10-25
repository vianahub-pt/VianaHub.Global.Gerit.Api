using VianaHub.Global.Gerit.Domain.Base;

namespace VianaHub.Global.Gerit.Domain.Entities.Business;

/// <summary>
/// Entidade que representa um Tipo de Origem de Aquisição (catálogo global).
/// Tabela: dbo.AcquisitionSourceTypes — Code NVARCHAR(50) UK + Translations 1:N.
/// Name e Description residem exclusivamente na tabela de traduções (AcquisitionSourceTypeTranslations).
/// </summary>
public class AcquisitionSourceTypeEntity : Entity
{
    public string? Code { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsDeleted { get; private set; }

    // Navigation Properties
    public ICollection<AcquisitionSourceTypeTranslationsEntity> Translations { get; private set; } = new List<AcquisitionSourceTypeTranslationsEntity>();

    protected AcquisitionSourceTypeEntity() { }

    /// <summary>
    /// Construtor para criação de um novo AcquisitionSourceType.
    /// Name e Description devem ser persistidos via AddTranslation() na tabela de traduções.
    /// </summary>
    public AcquisitionSourceTypeEntity(string? code, int createdBy)
    {
        Code = code;
        IsActive = true;
        IsDeleted = false;
        CreatedBy = createdBy;
        CreatedAt = DateTime.UtcNow;
        Translations = new List<AcquisitionSourceTypeTranslationsEntity>();
    }

    /// <summary>
    /// Adiciona uma tradução (Name + Description por idioma) à entidade.
    /// O FK AcquisitionSourceTypeId será resolvido pelo EF Core no SaveChanges.
    /// </summary>
    public void AddTranslation(AcquisitionSourceTypeTranslationsEntity translation)
    {
        Translations.Add(translation);
    }

    /// <summary>
    /// Regista a modificação sem alterar a chave natural.
    /// </summary>
    public void MarkModified(int modifiedBy)
    {
        ModifiedBy = modifiedBy;
        ModifiedAt = DateTime.UtcNow;
    }

    public void Activate(int modifiedBy)
    {
        IsActive = true;
        ModifiedBy = modifiedBy;
        ModifiedAt = DateTime.UtcNow;
    }

    public void Deactivate(int modifiedBy)
    {
        IsActive = false;
        ModifiedBy = modifiedBy;
        ModifiedAt = DateTime.UtcNow;
    }

    public void Delete(int modifiedBy)
    {
        IsDeleted = true;
        IsActive = false;
        ModifiedBy = modifiedBy;
        ModifiedAt = DateTime.UtcNow;
    }
}
