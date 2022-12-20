using System;

namespace ResultOfTask
{
    public class None
    {
        private None()
        {
        }
    }

    public struct Result<T>
    {
        public Result(string error, T value = default(T))
        {
            Error = error;
            Value = value;
        }

        public string Error { get; }
        internal T Value { get; }

        public T GetValueOrThrow()
        {
            if (IsSuccess) return Value;
            throw new InvalidOperationException($"No value. Only Error {Error}");
        }

        public bool IsSuccess => Error == null;
    }

    public static class Result
    {
        public static Result<T> AsResult<T>(this T value)
        {
            return Ok(value);
        }

        public static Result<T> Ok<T>(T value)
        {
            return new Result<T>(null, value);
        }

        public static Result<T> Fail<T>(string e)
        {
            return new Result<T>(e);
        }

        public static Result<T> Of<T>(Func<T> f, string error = null)
        {
            try
            {
                return Ok(f());
            }
            catch (Exception e)
            {
                return Fail<T>(error ?? e.Message);
            }
        }

        public static Result<TOutput> Then<TInput, TOutput>(
            this Result<TInput> input,
            Func<TInput, TOutput> continuation)
        {
            return input.Match(value => Of(() => continuation(value)));
        }

        public static Result<TOutput> Then<TInput, TOutput>(
            this Result<TInput> input,
            Func<TInput, Result<TOutput>> continuation)
        {
            return input.Match(value => continuation(input.Value));
        }

        public static Result<T> ReplaceError<T>(this Result<T> result, Func<string, string> error)
        {
            return result.Match(_ => result, fail => Fail<T>(error(fail.Error)));
        }

        public static Result<T> RefineError<T>(this Result<T> result, string error)
        {
            return result.ReplaceError(e => $"{error}. {e}");
        }

        public static Result<TOutput> Match<TInput, TOutput>(this Result<TInput> input,
            Func<TInput, Result<TOutput>> successful, Func<Result<TInput>, Result<TOutput>> fail)
        {
            return input.IsSuccess ? successful(input.Value) : fail(input);
        }

        public static Result<TOutput> Match<TInput, TOutput>(this Result<TInput> input,
            Func<TInput, Result<TOutput>> successful)
        {
            return input.Match(successful, result => Fail<TOutput>(result.Error));
        }

        public static Result<TInput> OnFail<TInput>(
            this Result<TInput> input,
            Action<string> handleError)
        {
            if (!input.IsSuccess)
                handleError(input.Error);
            return input;
        }
    }
}