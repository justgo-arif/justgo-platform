using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.MemberProfile.Test.Helper
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
