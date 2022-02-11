using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace AmadarePlugin;

public static class ReflectionExpressionsExtensions
{
    private static Dictionary<string, object> cache = new();
    
    public static T Field<TObj, T>(this TObj obj, string fieldName)
    {
        var cacheKey = $"{typeof(TObj).FullName}.{fieldName}";
        if (cache.TryGetValue(cacheKey, out var accessor))
        {
            return ((Func<TObj, T>)accessor)(obj);
        }

        accessor = GetFieldAccessor<TObj, T>(fieldName);
        cache[cacheKey] = accessor;
        return ((Func<TObj, T>)accessor)(obj);
    }

    public static void SetField<T>(this object obj, string fieldName, T value)
    {
        obj.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(obj, value);
    }
    
    public static Func<T,R> GetFieldAccessor<T,R>(this T obj, string fieldName) 
    { 
        ParameterExpression param = 
            Expression.Parameter (typeof(T),"arg");  

        MemberExpression member = 
            Expression.Field(param, fieldName);   

        LambdaExpression lambda = 
            Expression.Lambda(typeof(Func<T,R>), member, param);   

        Func<T,R> compiled = (Func<T,R>)lambda.Compile(); 
        return compiled; 
    }
        
    public static Func<T,R> GetFieldAccessor<T,R>(string fieldName) 
    { 
        ParameterExpression param = 
            Expression.Parameter (typeof(T),"arg");  

        MemberExpression member = 
            Expression.Field(param, fieldName);   

        LambdaExpression lambda = 
            Expression.Lambda(typeof(Func<T,R>), member, param);   

        Func<T,R> compiled = (Func<T,R>)lambda.Compile(); 
        return compiled; 
    }
}