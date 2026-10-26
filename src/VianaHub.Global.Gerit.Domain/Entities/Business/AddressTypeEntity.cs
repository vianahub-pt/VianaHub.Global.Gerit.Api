using VianaHub.Global.Gerit.Domain.Base;

namespace VianaHub.Global.Gerit.Domain.Entities.Business;

/// <summary>
/// Entidade que representa um Tipo de Endereço (Residencial, Comercial, etc.).
/// Name e Description residem exclusivamente na tabela de traduções (AddressTypeTranslations).
/// </summary>
public class AddressTypeEntity : Entity
{
    public string? Code { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsDeleted { get; private set; }

    // Navigation Properties
    public ICollection<AddressTypeTranslationsEntity> Translations { get; private set; } = new List<AddressTypeTranslationsEntity>();

    // Construtor protegido para o EF Core
    protected AddressTypeEntity() { }

    /// <summary>
    /// Construtor para criação de um novo Tipo de Endereço sem código.
    /// Name e Description devem ser persistidos via AddTranslation() na tabela de traduções.
    /// </summary>
    public AddressTypeEntity(int createdBy)
        : this(code: null, createdBy)
    {
    }

    /// <summary>
    /// Construtor completo para criação de um novo Tipo de Endereço, incluindo o Code de identificação.
    /// Name e Description devem ser persistidos via AddTranslation() na tabela de traduções.
    /// </summary>
    public AddressTypeEntity(string? code, int createdBy)
    {
        Code = code;
        IsActive = true;
        IsDeleted = false;
        CreatedBy = createdBy;
        CreatedAt = DateTime.UtcNow;
        Translations = new List<AddressTypeTranslationsEntity>();
    }

    /// <summary>
    /// Adiciona uma tradução (Name + Description por idioma) à entidade.
    /// O FK AddressTypeId será resolvido pelo EF Core no SaveChanges.
    /// </summary>
    public void AddTranslation(AddressTypeTranslationsEntity translation)
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
