namespace TradePilot.Api.DTOs;

/// <summary>
/// Standard API response wrapper for all endpoints.
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
    public List<string>? Errors { get; set; }

    public ApiResponse(T data)
    {
        Success = true;
        Data = data;
    }

    public ApiResponse(string message)
    {
        Success = false;
        Message = message;
    }

    public ApiResponse(string message, List<string> errors)
    {
        Success = false;
        Message = message;
        Errors = errors;
    }
}
