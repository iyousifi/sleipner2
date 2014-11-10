namespace Sleipner.Cache.Configuration.Parsers
{
    public class AnyValueParser : IParameterParser
    {
        public bool IsMatch(object value)
        {
            return true;
        }
    }
}