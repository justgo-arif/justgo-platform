namespace JustGo.Credential.Test.Helper
{
    public static class SqlTestExtensions
    {
        public static string NormalizeWhitespace(this string sql)
        {
            return string.Join(" ",
                sql.Split(new[] { '\r', '\n', '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries));
        }
    }
}
