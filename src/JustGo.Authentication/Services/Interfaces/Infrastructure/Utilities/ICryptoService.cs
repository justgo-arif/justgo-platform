using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities
{
    public interface ICryptoService
    {
        string EncryptObject<T>(T payload);
        T DecryptObject<T>(string base64CipherText);
    }
}
