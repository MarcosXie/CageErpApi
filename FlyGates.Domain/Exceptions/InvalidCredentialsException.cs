namespace FlyGates.Application.Exceptions;

public class InvalidCredentialsException() :
    BaseException($"Login failed. Please check your credential and try again.", 401);