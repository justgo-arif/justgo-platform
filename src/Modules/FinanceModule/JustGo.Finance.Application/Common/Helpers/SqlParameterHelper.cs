using System.Data;
using Dapper;

namespace JustGo.Finance.Application.Common.Helpers
{
    public static class SqlParameterHelper
    {
        public static DynamicParameters CreateParams(params (string Name, object Value)[] parameters)
        {
            var dynamicParams = new DynamicParameters();

            foreach (var (name, value) in parameters)
            {
                dynamicParams.Add(name, value);
            }

            return dynamicParams;
        }
        public static DynamicParameters CreateParams(params (string Name, object Value, DbType? Type)[] parameters)
        {
            var dynamicParams = new DynamicParameters();

            foreach (var (name, value, type) in parameters)
            {
                if (type.HasValue)
                    dynamicParams.Add(name, value, type);
                else
                    dynamicParams.Add(name, value);
            }

            return dynamicParams;
        }

    }

}
