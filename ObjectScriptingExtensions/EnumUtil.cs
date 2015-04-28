using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ObjectScriptingExtensions
{

/// <summary>
/// Utility methods for enum values. This static type will fail to initialize 
/// (throwing a <see cref="TypeInitializationException"/>) if
/// you try to provide a value that is not an enum.
/// </summary>
/// Modified from StriplingWarrior code
/// <typeparam name="T">An enum type. </typeparam>
public static class EnumUtil<T>
    where T : struct, IConvertible // Try to get as much of a static check as we can.
{
    // The .NET framework doesn't provide a compile-checked
    // way to ensure that a type is an enum, so we have to check when the type
    // is statically invoked.
    static EnumUtil()
    {
        // Throw Exception on static initialization if the given type isn't an enum.
        if (!typeof(T).IsEnum) 
            throw new ApplicationException("Not an Enum");
    }

    public static T Parse(string enumValue)
    {
        var parsedValue = (T)System.Enum.Parse(typeof (T), enumValue);
        //Require that the parsed value is defined
        if (!IsDefined(parsedValue))
            throw new ApplicationException("Not an defined value");

        return parsedValue;
    }

    public static bool IsDefined(T enumValue)
    {
        return System.Enum.IsDefined(typeof (T), enumValue);
    }

}
}
