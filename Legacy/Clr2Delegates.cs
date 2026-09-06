// CLR 2.0 includes Action<T>, but zero- and two-argument Action delegates
// arrived later. These signatures require no newer runtime functionality.
namespace System;

public delegate void Action();
public delegate void Action<T1, T2>(T1 first, T2 second);
