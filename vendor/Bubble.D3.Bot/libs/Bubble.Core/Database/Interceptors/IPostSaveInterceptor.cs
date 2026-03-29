namespace Bubble.Core.Database.Interceptors;

public interface IPostSaveInterceptor
{
    void AfterSave();
}