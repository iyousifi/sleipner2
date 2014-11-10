namespace Sleipner.Cache.Configuration.Parsers
{
    public class ConstantParser : IParameterParser
    {
        private readonly object _constant;

        public ConstantParser(object constant)
        {
            _constant = constant;
        }

        public bool IsMatch(object value)
        {
            return Equals(value, _constant);
        }
    }
}
