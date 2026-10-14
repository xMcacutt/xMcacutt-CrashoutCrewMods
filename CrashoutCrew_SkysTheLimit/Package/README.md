# Crashout Crew - Sky's The Limit

This mod increases the maximum number of boxes that can be stacked from four all the way up to 32.
It also exposes a function to allow other mods to change the count allowing it to be limited. For example:

```csharp
SkysTheLimitAPI.SetStackLimit(24);
```

Sky's The Limit makes significant changes to the networking code of grabbable objects and may be incompatible with mods
that also edit these pieces of code.
