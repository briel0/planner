namespace Planner.Domain;

/// <summary>
/// Ids de entidades são UUID versão 7: começam com o instante de criação, o que mantém os índices do banco
/// eficientes. Como os clientes geram os ids (criação idempotente), o domínio confere a versão.
/// </summary>
public static class EntityId {
    public static Guid Require(Guid id) {
        if(id.Version != 7) {
            throw new DomainException("invalid-id", $"'{id}' is not a UUID version 7.");
        }
        return id;
    }
}
