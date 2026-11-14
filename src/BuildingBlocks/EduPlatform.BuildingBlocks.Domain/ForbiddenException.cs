namespace EduPlatform.BuildingBlocks.Domain;

public sealed class ForbiddenException(string message) : Exception(message);
