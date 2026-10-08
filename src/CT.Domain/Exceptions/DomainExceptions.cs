namespace CT.Domain.Exceptions;

public class RegleMetierException(string message) : Exception(message);

public class ConflitException(string message) : Exception(message);

public class ControleClotureException()
    : ConflitException("Le contrôle est clôturé : aucune modification n'est possible.");

public class IntrouvableException(string message) : Exception(message);

public class AccesInterditException(string message) : Exception(message);
