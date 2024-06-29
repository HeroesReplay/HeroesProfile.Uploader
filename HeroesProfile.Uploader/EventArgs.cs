using System;

namespace HeroesProfile.Uploader;

public class EventArgs<T>(T input) : EventArgs
{
    public T Data { get; private set; } = input;
}
