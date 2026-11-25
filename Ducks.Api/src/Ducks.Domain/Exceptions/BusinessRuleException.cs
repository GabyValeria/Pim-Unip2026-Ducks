namespace Ducks.Domain.Exceptions;

public class BusinessRuleException(string message) : Exception(message);
public class ConflictException(string message) : Exception(message);
public class ResourceNotFoundException(string message) : Exception(message);
