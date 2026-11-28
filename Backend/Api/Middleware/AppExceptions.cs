namespace Api.Middleware;

// 400: the request data is invalid (e.g. weak password)
public class BadRequestException(string message) : Exception(message);

// 404: something doesn't exist (e.g. task not found)
public class NotFoundException(string message) : Exception(message);

// 409: conflicts with existing data (e.g. email already used)
public class ConflictException(string message) : Exception(message);