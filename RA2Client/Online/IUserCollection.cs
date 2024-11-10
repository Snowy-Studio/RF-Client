﻿using System;
using System.Collections.Generic;

namespace Ra2Client.Online
{
    public interface IUserCollection<T>
    {
        int Count { get; }

        void Add(string username, T item);
        void Clear();
        void DoForAllUsers(Action<T> action);
        T Find(string username);
        LinkedListNode<T> GetFirst();
        void Reinsert(string username);
        bool Remove(string username);
    }
}