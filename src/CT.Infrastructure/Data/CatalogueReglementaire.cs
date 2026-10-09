namespace CT.Infrastructure.Data;

// Extrait de l'annexe I de l'arrêté du 18 juin 1991 modifié : 29 des 133 points, 76 des 610 défaillances.
public static class CatalogueReglementaire
{
    public record PointRef(string Code, string Libelle, (string Code, string Libelle)[] Defaillances);

    public record FonctionRef(int Numero, string Libelle, PointRef[] Points);

    public static readonly FonctionRef[] Fonctions =
    [
        new(0, "Identification du véhicule",
        [
            new("0.1.1", "Plaques d'immatriculation",
            [
                ("0.1.1.a.2", "Plaque manquante ou mal fixée, risquant de tomber"),
                ("0.1.1.b.2", "Inscription manquante ou illisible"),
                ("0.1.1.c.2", "Ne correspond pas aux documents du véhicule")
            ]),
            new("0.2.1", "Numéro d'identification, de châssis ou de série du véhicule",
            [
                ("0.2.1.a.2", "Manquant ou introuvable"),
                ("0.2.1.b.1", "Légèrement différent du (des) document(s) du véhicule"),
                ("0.2.1.b.2", "Incomplet, illisible, manifestement falsifié ou ne correspondant pas aux documents du véhicule")
            ])
        ]),
        new(1, "Équipements de freinage",
        [
            new("1.1.12", "Flexibles de frein",
            [
                ("1.1.12.b.1", "Endommagement, points de friction, flexibles torsadés ou trop courts"),
                ("1.1.12.b.2", "Flexibles endommagés ou frottant contre une autre pièce"),
                ("1.1.12.c.3", "Manque d'étanchéité des flexibles ou des raccords")
            ]),
            new("1.1.13", "Garnitures ou plaquettes de frein",
            [
                ("1.1.13.a.1", "Usure importante"),
                ("1.1.13.a.2", "Usure excessive (marque minimale atteinte)"),
                ("1.1.13.a.3", "Usure excessive (marque minimale non visible)")
            ]),
            new("1.1.14", "Tambours de freins, disques de frein",
            [
                ("1.1.14.a.1", "Disque ou tambour légèrement usé"),
                ("1.1.14.a.2", "Disque ou tambour usé"),
                ("1.1.14.a.3", "Disque ou tambour excessivement usé, excessivement rayé, fissuré, mal fixé ou cassé")
            ]),
            new("1.2.2", "Efficacité du frein de service",
            [
                ("1.2.2.a.2", "Efficacité insuffisante"),
                ("1.2.2.a.3", "Efficacité inférieure à 50 % de la valeur limite")
            ]),
            new("1.4.2", "Efficacité du frein de stationnement",
            [
                ("1.4.2.a.2", "Efficacité insuffisante"),
                ("1.4.2.a.3", "Efficacité inférieure à 50 % de la valeur limite")
            ]),
            new("1.8.1", "Liquide de frein",
            [
                ("1.8.1.a.2", "Liquide de frein contaminé ou sédimenté"),
                ("1.8.1.a.3", "Liquide de frein contaminé ou sédimenté : risque imminent de défaillance")
            ])
        ]),
        new(2, "Direction",
        [
            new("2.1.1", "État du boîtier ou de la crémaillère de direction",
            [
                ("2.1.1.e.1", "Manque d'étanchéité"),
                ("2.1.1.e.2", "Manque d'étanchéité : formation de gouttelettes"),
                ("2.1.1.f.3", "Déformation, fissure, cassure")
            ]),
            new("2.1.3", "État de la timonerie de direction",
            [
                ("2.1.3.b.2", "Usure excessive des articulations"),
                ("2.1.3.b.3", "Usure excessive des articulations : risque très grave de détachement")
            ])
        ]),
        new(3, "Visibilité",
        [
            new("3.2.1", "État des vitrages",
            [
                ("3.2.1.a.1", "Vitrage fissuré ou décoloré"),
                ("3.2.1.a.2", "Vitrage fissuré ou décoloré dans la zone d'essuyage ou de vision des rétroviseurs"),
                ("3.2.1.a.3", "Vitrage fissuré ou décoloré : visibilité fortement entravée")
            ]),
            new("3.3.1", "Rétroviseurs ou dispositifs de vision indirecte",
            [
                ("3.3.1.a.2", "Rétroviseur ou dispositif manquant ou non fixé conformément aux exigences"),
                ("3.3.1.b.1", "Rétroviseur ou dispositif légèrement endommagé ou mal fixé")
            ]),
            new("3.4.1", "Essuie-glace",
            [
                ("3.4.1.a.2", "Essuie-glace ne fonctionnant pas, manquant ou non conforme"),
                ("3.4.1.b.1", "Balai d'essuie-glace défectueux")
            ]),
            new("3.5.1", "Lave-glace",
            [
                ("3.5.1.a.1", "Lave-glace ne fonctionnant pas correctement"),
                ("3.5.1.a.2", "Lave-glace ne fonctionnant pas")
            ])
        ]),
        new(4, "Feux, dispositifs réfléchissants et équipements électriques",
        [
            new("4.1.1", "État et fonctionnement (phares)",
            [
                ("4.1.1.a.1", "Lampe/source lumineuse défectueuse ou manquante"),
                ("4.1.1.a.2", "Lampe/source lumineuse défectueuse ou manquante : visibilité fortement réduite"),
                ("4.1.1.b.2", "Système de projection (réflecteur et glace) fortement défectueux ou manquant")
            ]),
            new("4.3.1", "État et fonctionnement (feux stop)",
            [
                ("4.3.1.a.1", "Source lumineuse défectueuse"),
                ("4.3.1.a.2", "Source lumineuse défectueuse ou manquante : visibilité fortement réduite"),
                ("4.3.1.a.3", "Toutes les sources lumineuses ne fonctionnent pas")
            ]),
            new("4.4.1", "État et fonctionnement (indicateur de direction et feux de signal de détresse)",
            [
                ("4.4.1.a.1", "Source lumineuse défectueuse"),
                ("4.4.1.a.2", "Source lumineuse défectueuse ou manquante : visibilité fortement réduite"),
                ("4.4.1.b.2", "Glace fortement défectueuse (lumière émise affectée)")
            ])
        ]),
        new(5, "Essieux, roues, pneus, suspension",
        [
            new("5.2.3", "Pneumatiques",
            [
                ("5.2.3.d.2", "Pneumatique gravement endommagé, entaillé ou montage inadapté"),
                ("5.2.3.d.3", "Corde visible ou endommagée"),
                ("5.2.3.e.1", "Usure anormale ou présence d'un corps étranger"),
                ("5.2.3.e.2", "L'indicateur d'usure de la profondeur des sculptures est atteint"),
                ("5.2.3.e.3", "La profondeur des sculptures n'est pas conforme aux exigences")
            ]),
            new("5.3.2", "Amortisseurs",
            [
                ("5.3.2.a.1", "Mauvaise attache des amortisseurs au châssis ou à l'essieu"),
                ("5.3.2.a.2", "Amortisseur mal fixé"),
                ("5.3.2.b.2", "Amortisseur endommagé ou donnant des signes de fuite ou de dysfonctionnement grave"),
                ("5.3.2.d.1", "Écart significatif entre la droite et la gauche")
            ])
        ]),
        new(6, "Châssis et accessoires du châssis",
        [
            new("6.1.1", "État général du châssis",
            [
                ("6.1.1.c.1", "Corrosion"),
                ("6.1.1.c.2", "Corrosion excessive affectant la rigidité de l'assemblage"),
                ("6.1.1.c.3", "Corrosion excessive affectant la rigidité de l'assemblage : résistance insuffisante des pièces")
            ]),
            new("6.1.2", "Tuyaux d'échappement et silencieux",
            [
                ("6.1.2.a.1", "Dispositif endommagé sans fuite ni risque de chute"),
                ("6.1.2.a.2", "Mauvaise fixation ou manque d'étanchéité du système d'échappement"),
                ("6.1.2.a.3", "Mauvaise fixation ou manque d'étanchéité du système d'échappement : très grand risque de chute")
            ]),
            new("6.2.3", "Portes et poignées de porte",
            [
                ("6.2.3.a.2", "Une portière ne s'ouvre ou ne se ferme pas correctement"),
                ("6.2.3.b.3", "Une portière est susceptible de s'ouvrir inopinément ou ne reste pas fermée (portes pivotantes)")
            ])
        ]),
        new(7, "Autre matériel",
        [
            new("7.1.2", "État des ceintures de sécurité et des boucles",
            [
                ("7.1.2.a.2", "Ceinture de sécurité obligatoire manquante ou non montée"),
                ("7.1.2.b.1", "Ceinture de sécurité endommagée"),
                ("7.1.2.b.2", "Ceinture de sécurité coupée ou distendue"),
                ("7.1.2.d.2", "Boucle de ceinture de sécurité endommagée ou ne fonctionnant pas correctement")
            ]),
            new("7.7.1", "Avertisseur sonore",
            [
                ("7.7.1.a.1", "Ne fonctionne pas correctement"),
                ("7.7.1.a.2", "Ne fonctionne pas du tout")
            ]),
            new("7.11.1", "Compteur kilométrique",
            [
                ("7.11.1.a.1", "Kilométrage inférieur à celui relevé lors d'un contrôle technique précédent"),
                ("7.11.1.b.2", "Compteur manifestement inopérant")
            ])
        ]),
        new(8, "Nuisances",
        [
            new("8.1.1", "Système de réduction du bruit",
            [
                ("8.1.1.a.2", "Niveaux de bruit anormalement élevés ou excessifs")
            ]),
            new("8.2.12", "Émissions gazeuses (moteurs à allumage commandé)",
            [
                ("8.2.12.a.2", "Les émissions gazeuses dépassent les niveaux spécifiques indiqués par le constructeur"),
                ("8.2.12.d.2", "Le relevé du système OBD indique un dysfonctionnement important")
            ]),
            new("8.2.22", "Opacité (moteurs à allumage par compression)",
            [
                ("8.2.22.a.2", "L'opacité dépasse la valeur de réception ou les mesures sont instables"),
                ("8.2.22.c.2", "Le relevé du système OBD indique un dysfonctionnement important")
            ]),
            new("8.4.1", "Pertes de liquides",
            [
                ("8.4.1.a.2", "Fuite excessive de liquide autre que de l'eau"),
                ("8.4.1.a.3", "Fuite excessive de liquide autre que de l'eau : écoulement permanent constituant un risque très grave")
            ])
        ])
    ];
}
