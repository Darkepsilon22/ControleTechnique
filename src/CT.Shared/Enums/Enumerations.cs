namespace CT.Shared.Enums;

public enum Role { Administrateur = 0, Inspecteur = 1, Reception = 2 }

public enum Gravite { Mineur = 0, Majeur = 1, Critique = 2 }

public enum EtatPoint { Conforme = 0, NonConforme = 1, NonApplicable = 2 }

public enum StatutControle { Brouillon = 0, Cloture = 1 }

public enum ResultatControle { Favorable = 0, Defavorable = 1 }

public enum TypeVehicule { VoitureParticuliere = 0, Utilitaire = 1, PoidsLourd = 2, Moto = 3 }

public enum Energie { Essence = 0, Diesel = 1, Hybride = 2, Electrique = 3, Gpl = 4 }
