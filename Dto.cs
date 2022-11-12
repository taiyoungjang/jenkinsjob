
public record RequestModalDto(string type, string value, long action_time, string message );

public record SubmissionDto(string type, string actions, long action_time, string message, long value );