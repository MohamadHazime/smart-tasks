using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace SmartTasks.Api.Infrastructure.Converters;

public class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
    toDb => toDb.Kind == DateTimeKind.Local ? toDb.ToUniversalTime() : toDb,
    fromDb => DateTime.SpecifyKind(fromDb, DateTimeKind.Utc));
