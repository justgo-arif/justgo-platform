namespace JustGoAPI.Shared.Miscellaneous;

public record struct KeyValue<TKey, TValue>(TKey Label, TValue Value);