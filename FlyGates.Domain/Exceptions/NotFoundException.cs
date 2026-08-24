namespace FlyGates.Application.Exceptions;

public class NotFoundException(string item) : BaseException($"{item} not found!", 404);