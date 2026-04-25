using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.Authentication.Persistence.Repositories.GenericRepositories
{
    public enum DatabaseType
    {
        Central = 0,
        Tenant = 1,
        AzolveCentral = 2,
        AddressPickerCore = 3
    }
}
