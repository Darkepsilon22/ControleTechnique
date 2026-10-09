using CT.Domain.Enums;

namespace CT.Domain.Entities;

public class ResultatPoint
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ControleId { get; set; }
    public Controle? Controle { get; set; }
    public Guid PointControleId { get; set; }
    public PointControle? PointControle { get; set; }
    public EtatPoint Etat { get; set; }
    public string? Commentaire { get; set; }

    public List<Defaillance> Defaillances { get; set; } = [];
}
