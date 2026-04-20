namespace Dashboard_Service.Security
{
    public interface ICurrentUserProvider
    {
        CurrentUser GetCurrentUser();
    }
}
