namespace Shiny.Permissions;

public interface IPermissionManager
{
    Task<AccessState> GetCurrentStatus();
    Task<AccessState> Request();
}
