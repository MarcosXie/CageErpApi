namespace FlyGates.Application.Exceptions;

public class BadRequestException(string message) : BaseException(message, 400);