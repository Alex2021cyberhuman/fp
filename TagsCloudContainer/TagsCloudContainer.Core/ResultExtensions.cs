﻿// ReSharper disable once CheckNamespace

namespace CSharpFunctionalExtensions;

public static class ResultExtensions
{
    public static Result CombineCheck(this Result result, Func<Result> check)
    {
        return Result.Combine(result, check());
    }

    public static Result CombineCheck(this Result result, Func<Result> check, string error)
    {
        return Result.Combine(result, check().MapError(_ => error));
    }

    public static Result CombineCheck(this Result result, Func<bool> check, string error)
    {
        return Result.Combine(result, Result.SuccessIf(check, error));
    }

    public static Result<TK> SuccessIfCast<T, TK>(this T value)
    {
        return value is TK castedValue
            ? Result.Success(castedValue)
            : Result.Failure<TK>($"Cannot cast {typeof(T)} to {typeof(TK)}");
    }

    public static Result<TK> Bind<T, TK>(this Result<T> result, Func<T, TK> resolve)
    {
        return result.Bind(value => Result.Success(resolve(value)));
    }

    public static T AnyWayDispose<T>(this T result, Func<T, IDisposable?> disposeResolver) where T : IResult
    {
        disposeResolver(result)?.Dispose();
        return result;
    }

    public static T Anyway<T>(this T result, Action<Result<T>> action) where T : IResult
    {
        action(result);
        return result;
    }
}