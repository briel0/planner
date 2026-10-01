namespace Planner.Application.Common;

/// <summary>Quem está fazendo o pedido. Em desenvolvimento, o usuário dev; depois, quem fez login com o Google.</summary>
public interface ICurrentUser {
    Guid Id { get; }
}
