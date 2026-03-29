namespace Bubble.Core.Cache;

public sealed class ObjectValidator<T>
{
    private readonly Func<T> _creator;
    private readonly TimeSpan? _lifetime;
    private readonly object _sync;
    private T? _instance;

    private bool _isValid;
    private DateTime _lastCreationDate;

    private bool IsValid =>
        _isValid && (_lifetime is null || DateTime.Now - _lastCreationDate < _lifetime.Value);

    public event Action<ObjectValidator<T>>? ObjectInvalidated;

    public ObjectValidator(Func<T> creator, TimeSpan? lifeTime = null)
    {
        _lifetime = lifeTime;
        _creator = creator;
        _sync = new object();
    }

    public void Invalidate()
    {
        _isValid = false;

        ObjectInvalidated?.Invoke(this);
    }

    public static implicit operator T(ObjectValidator<T> validator)
    {
        if (validator is { IsValid: true, _instance: not null })
            return validator._instance;

        lock (validator._sync)
        {
            if (validator is { IsValid: true, _instance: not null })
                return validator._instance;

            validator._instance = validator._creator();
            validator._lastCreationDate = DateTime.Now;
            validator._isValid = true;
        }

        return validator._instance;
    }
}