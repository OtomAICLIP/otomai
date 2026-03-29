namespace Bubble.Core.Database.Interceptors;

public interface IPreSaveInterceptor
{
    void BeforeSave();
}