namespace FlyGates.Application.Exceptions;

public class ConflictException(string message) : BaseException(message, 409);
