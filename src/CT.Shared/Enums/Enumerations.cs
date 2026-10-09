namespace CT.Shared.Enums;

public enum Role { Administrateur = 0, Inspecteur = 1, Reception = 2 }

public enum NiveauDefaillance { Mineure = 1, Majeure = 2, Critique = 3 }

public enum EtatPoint { Conforme = 0, NonConforme = 1, NonApplicable = 2 }

public enum StatutControle { Brouillon = 0, Cloture = 1 }

public enum ResultatControle { Favorable = 0, DefavorableMajeur = 1, DefavorableCritique = 2 }

public enum TypeVehicule { VoitureParticuliere = 0, UtilitaireLeger = 1 }

public enum Energie { Essence = 0, Diesel = 1, Hybride = 2, Electrique = 3, Gpl = 4 }

public enum StatutEcheance { AJour = 0, ControleEnRetard = 1, ContreVisiteAFaire = 2, CirculationInterdite = 3 }
