namespace Sleipner.Cache.Configuration.Parsers
{
    public interface IParameterParser
    {
        bool IsMatch(object value);
    }
}