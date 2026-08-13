namespace GestorTareas.Api.Services;

public class NotFoundException : Exception
{
    public NotFoundException(string mensaje) : base(mensaje) { }
}

public class ForbiddenException : Exception
{
    public ForbiddenException(string mensaje) : base(mensaje) { }
}

public class ConflictException : Exception
{
    public ConflictException(string mensaje) : base(mensaje) { }
}
