namespace DpMapSubscribeTool.Services.SteamAPI;

public record CallResult<T>(bool IsSuccess, string ErrorMessage = default, T Result = default) : CallResult(IsSuccess,
    ErrorMessage)
{
}

public record CallResult(bool IsSuccess, string ErrorMessage = default)
{
}