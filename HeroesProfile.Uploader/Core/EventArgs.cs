using System;

namespace HeroesProfile.Uploader.Core;

public class EventArgs<T>(T input) : EventArgs
{
    public T Data { get; private set; } = input;
}
