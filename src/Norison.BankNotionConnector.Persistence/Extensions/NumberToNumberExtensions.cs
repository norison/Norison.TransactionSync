using Notion.Client;

namespace Norison.BankNotionConnector.Persistence.Extensions;

public static class NumberToNumberExtensions
{
    public static NumberPropertyValue ToNumberPropertyValue(this long number)
    {
        return new NumberPropertyValue { Number = (double)number };
    }

    public static NumberPropertyValue ToNumberPropertyValue(this decimal number)
    {
        return new NumberPropertyValue { Number = (double)number };
    }

    public static long ToLongValue(this PropertyValue propertyValue)
    {
        return ConvertToNumberPropertyValue<long>(propertyValue);
    }

    public static decimal ToDecimalValue(this PropertyValue propertyValue)
    {
        return ConvertToNumberPropertyValue<decimal>(propertyValue);
    }

    private static T ConvertToNumberPropertyValue<T>(PropertyValue propertyValue) where T : struct
    {
        if (propertyValue is not NumberPropertyValue numberPropertyValue)
        {
            throw new InvalidCastException("Property value is not a number.");
        }

        return (T)Convert.ChangeType(numberPropertyValue.Number, typeof(T))!;
    }
}