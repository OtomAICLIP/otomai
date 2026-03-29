// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections;

namespace Bubble.Core.Network.Internal;

internal sealed class ConnectionLogScope : IReadOnlyList<KeyValuePair<string, object?>>
{
    // Name chosen so as not to collide with Kestrel's "ConnectionId"
    private const string ClientConnectionIdKey = "ClientConnectionId";
    private readonly string _connectionId;
    private string? _cachedToString;

    public KeyValuePair<string, object?> this[int index]
    {
        get
        {
            if (Count is 1 && index is 0)
                return new KeyValuePair<string, object?>(ClientConnectionIdKey, _connectionId);

            throw new ArgumentOutOfRangeException(nameof(index));
        }
    }

    public int Count =>
        string.IsNullOrEmpty(_connectionId) ? 0 : 1;

    public ConnectionLogScope(string connectionId)
    {
        _connectionId = connectionId;
    }

    public override string ToString()
    {
        return _cachedToString ??= FormattableString.Invariant($"{ClientConnectionIdKey}:{_connectionId}");
    }

    public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
    {
        for (var i = 0; i < Count; ++i)
            yield return this[i];
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}