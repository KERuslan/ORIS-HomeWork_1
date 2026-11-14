using System.Collections.Specialized;
using System.Globalization;
using System.Reflection;
using System.Web;

namespace MyHttpServer.Framework.Routing;

public static class RequestParameters
{
    public static NameValueCollection ParseForm(string text) => HttpUtility.ParseQueryString(text);

    public static bool TryBind(ParameterInfo[] parameters, NameValueCollection values, out object?[] arguments)
    {
        arguments = new object?[parameters.Length];
        for (int i = 0; i < parameters.Length; i++)
        {
            var parameter = parameters[i];
            string[]? supplied = values.GetValues(parameter.Name);
            if (supplied is null && parameter.HasDefaultValue)
            {
                arguments[i] = parameter.DefaultValue;
                continue;
            }

            if (supplied is not [var value] || string.IsNullOrWhiteSpace(value))
                return false;

            try
            {
                Type type = Nullable.GetUnderlyingType(parameter.ParameterType) ?? parameter.ParameterType;
                arguments[i] = type.IsEnum
                    ? Enum.Parse(type, value, ignoreCase: true)
                    : Convert.ChangeType(value, type, CultureInfo.InvariantCulture);
            }
            catch (Exception error) when (error is ArgumentException or FormatException or OverflowException or InvalidCastException)
            {
                return false;
            }
        }
        return true;
    }
}
