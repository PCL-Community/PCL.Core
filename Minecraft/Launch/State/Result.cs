using System;

namespace PCL.Core.Minecraft.Launch.State;

/// <summary>
/// 通用结果类型，用于替代异常驱动的控制流
/// </summary>
/// <typeparam name="T">成功时返回的数据类型</typeparam>
public class Result<T> {
    public bool IsSuccess { get; private set; }
    public T Value { get; private set; }
    public string Error { get; private set; }
    public Exception Exception { get; private set; }

    private Result(bool isSuccess, T value, string error, Exception exception = null) {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
        Exception = exception;
    }

    public static Result<T> Success(T value) {
        return new Result<T>(true, value, null);
    }

    public static Result<T> Failed(string error) {
        return new Result<T>(false, default(T), error);
    }

    public static Result<T> Failed(Exception exception) {
        return new Result<T>(false, default(T), exception.Message, exception);
    }

    public static Result<T> Failed(string error, Exception exception) {
        return new Result<T>(false, default(T), error, exception);
    }

    /// <summary>
    /// 如果失败则抛出异常
    /// </summary>
    public T Unwrap() {
        if (!IsSuccess) {
            if (Exception != null)
                throw Exception;
            throw new InvalidOperationException(Error ?? "Operation failed");
        }
        return Value;
    }

    /// <summary>
    /// 如果失败则返回默认值
    /// </summary>
    public T UnwrapOr(T defaultValue) {
        return IsSuccess ? Value : defaultValue;
    }

    /// <summary>
    /// 转换结果类型
    /// </summary>
    public Result<U> Map<U>(Func<T, U> mapper) {
        if (!IsSuccess)
            return Result<U>.Failed(Error, Exception);

        try {
            return Result<U>.Success(mapper(Value));
        } catch (Exception ex) {
            return Result<U>.Failed(ex);
        }
    }
}

/// <summary>
/// 无返回值的结果类型
/// </summary>
public class Result {
    public bool IsSuccess { get; private set; }
    public string Error { get; private set; }
    public Exception Exception { get; private set; }

    private Result(bool isSuccess, string error, Exception exception = null) {
        IsSuccess = isSuccess;
        Error = error;
        Exception = exception;
    }

    public static Result Success() {
        return new Result(true, null);
    }

    public static Result Failed(string error) {
        return new Result(false, error);
    }

    public static Result Failed(Exception exception) {
        return new Result(false, exception.Message, exception);
    }

    public static Result Failed(string error, Exception exception) {
        return new Result(false, error, exception);
    }

    /// <summary>
    /// 如果失败则抛出异常
    /// </summary>
    public void Unwrap() {
        if (!IsSuccess) {
            if (Exception != null)
                throw Exception;
            throw new InvalidOperationException(Error ?? "Operation failed");
        }
    }
}
