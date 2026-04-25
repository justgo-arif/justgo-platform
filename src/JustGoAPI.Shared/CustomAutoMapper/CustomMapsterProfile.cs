using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Mapster;

namespace JustGoAPI.Shared.CustomAutoMapper
{
    public abstract class CustomMapsterProfile : IRegister
    {
        public abstract void Register(TypeAdapterConfig config);

        protected void CreateAutoMaps(TypeAdapterConfig config, Assembly domainAssembly, Assembly applicationAssembly,
            string domainNamespaceContains = "Entities", string dtoNamespaceContains = "DTOs")
        {
            var domainTypes = domainAssembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && t.Namespace != null &&
                           t.Namespace.Contains(domainNamespaceContains))
                .ToList();

            var dtoTypes = applicationAssembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && t.Namespace != null &&
                           t.Namespace.Contains(dtoNamespaceContains))
                .ToList();

            foreach (var domainType in domainTypes)
            {
                var dtoType = dtoTypes.FirstOrDefault(x => x.Name == $"{domainType.Name}Dto");
                if (dtoType != null)
                {
                    config.NewConfig(domainType, dtoType);
                    config.NewConfig(dtoType, domainType);
                }
            }
        }
    }
}
